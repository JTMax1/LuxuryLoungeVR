using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using VRWatch.Core;
using VRWatch.Interaction;

namespace VRWatch.Editor
{
    public static class LoungeBuilder
    {
        [MenuItem("VRWatch/Build Collector Lounge and Arm Rig", false, 11)]
        public static void BuildLoungeAndArmRig()
        {
            SetupMaterials();
            BuildEnvironmentLounge();
            BuildAvatarArmRigAndStabilizer();
            ConfigureArticulatedHands();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[LoungeBuilder] Collector Lounge, Articulated Hands, and Stabilized Arm Rig built and scene saved.");
        }

        public static void SetupMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return;

            // 1. M_LoungeWall: Bright warm off-white / architectural limestone (Hex #ECEBE8)
            Material matWall = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_LoungeWall.mat");
            if (matWall == null)
            {
                matWall = new Material(shader);
                AssetDatabase.CreateAsset(matWall, "Assets/_Project/Materials/M_LoungeWall.mat");
            }
            matWall.shader = shader;
            matWall.SetColor("_BaseColor", new Color(0.9255f, 0.9216f, 0.9098f, 1.0f)); // #ECEBE8
            matWall.SetFloat("_Smoothness", 0.22f);
            matWall.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(matWall);

            // 2. M_LoungeFloor: Polished white marble / light natural oak (Hex #F5F4F0, Smoothness 0.70)
            Material matFloor = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_LoungeFloor.mat");
            if (matFloor == null)
            {
                matFloor = new Material(shader);
                AssetDatabase.CreateAsset(matFloor, "Assets/_Project/Materials/M_LoungeFloor.mat");
            }
            matFloor.shader = shader;
            matFloor.SetColor("_BaseColor", new Color(0.960f, 0.957f, 0.941f, 1.0f)); // #F5F4F0
            matFloor.SetFloat("_Smoothness", 0.70f);
            matFloor.SetFloat("_Metallic", 0.05f);
            EditorUtility.SetDirty(matFloor);

            // 3. M_Glove_Leather: Elegant dark charcoal/black leather driving glove
            Material matGlove = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Glove_Leather.mat");
            if (matGlove == null)
            {
                matGlove = new Material(shader);
                AssetDatabase.CreateAsset(matGlove, "Assets/_Project/Materials/M_Glove_Leather.mat");
            }
            matGlove.shader = shader;
            matGlove.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.14f, 1.0f));
            matGlove.SetFloat("_Smoothness", 0.38f);
            matGlove.SetFloat("_Metallic", 0.08f);
            EditorUtility.SetDirty(matGlove);

            // 4. M_Avatar_Skin: Warm realistic skin tone
            Material matSkin = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Avatar_Skin.mat");
            if (matSkin == null)
            {
                matSkin = new Material(shader);
                AssetDatabase.CreateAsset(matSkin, "Assets/_Project/Materials/M_Avatar_Skin.mat");
            }
            matSkin.shader = shader;
            matSkin.SetColor("_BaseColor", new Color(0.85f, 0.68f, 0.58f, 1.0f));
            matSkin.SetFloat("_Smoothness", 0.28f);
            matSkin.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(matSkin);

            // 5. M_SuitSleeve: Tailored wool fabric
            Material matSleeve = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_SuitSleeve.mat");
            if (matSleeve != null)
            {
                matSleeve.SetColor("_BaseColor", new Color(0.11f, 0.11f, 0.13f, 1.0f));
                matSleeve.SetFloat("_Smoothness", 0.15f);
                matSleeve.SetFloat("_Metallic", 0.05f);
                EditorUtility.SetDirty(matSleeve);
            }

            // 6. M_ShirtCuff: Crisp white shirt cotton
            Material matShirt = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_ShirtCuff.mat");
            if (matShirt != null)
            {
                matShirt.SetColor("_BaseColor", new Color(0.96f, 0.96f, 0.97f, 1.0f));
                matShirt.SetFloat("_Smoothness", 0.12f);
                matShirt.SetFloat("_Metallic", 0.0f);
                EditorUtility.SetDirty(matShirt);
            }

            AssetDatabase.SaveAssets();
        }

        public static GameObject BuildEnvironmentLounge()
        {
            GameObject existing = GameObject.Find("[Environment_Lounge]");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            GameObject loungeRoot = new GameObject("[Environment_Lounge]");
            Undo.RegisterCreatedObjectUndo(loungeRoot, "Create Environment Lounge");
            loungeRoot.transform.position = Vector3.zero;
            loungeRoot.transform.rotation = Quaternion.identity;

            Material matFloor = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_LoungeFloor.mat");
            Material matWall = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_LoungeWall.mat");
            Material matPedestal = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Pedestal.mat");
            Material matPad = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_PedestalPad.mat");

            void Setup(GameObject go, Material mat)
            {
                Collider c = go.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null && mat != null) mr.sharedMaterial = mat;
            }

            // Floor (10m x 10m)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(loungeRoot.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(10f, 0.1f, 10f);
            Setup(floor, matFloor);

            // Walls (North, South, East, West) - Height: 3.5m
            float wallH = 3.5f;
            float wallY = wallH * 0.5f;
            float roomHalf = 5f;

            // North Wall (+Z)
            GameObject wallN = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallN.name = "Wall_North";
            wallN.transform.SetParent(loungeRoot.transform, false);
            wallN.transform.localPosition = new Vector3(0f, wallY, roomHalf);
            wallN.transform.localScale = new Vector3(10f, wallH, 0.15f);
            Setup(wallN, matWall);

            // South Wall (-Z)
            GameObject wallS = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallS.name = "Wall_South";
            wallS.transform.SetParent(loungeRoot.transform, false);
            wallS.transform.localPosition = new Vector3(0f, wallY, -roomHalf);
            wallS.transform.localScale = new Vector3(10f, wallH, 0.15f);
            Setup(wallS, matWall);

            // East Wall (+X)
            GameObject wallE = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallE.name = "Wall_East";
            wallE.transform.SetParent(loungeRoot.transform, false);
            wallE.transform.localPosition = new Vector3(roomHalf, wallY, 0f);
            wallE.transform.localScale = new Vector3(0.15f, wallH, 10f);
            Setup(wallE, matWall);

            // West Wall (-X)
            GameObject wallW = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallW.name = "Wall_West";
            wallW.transform.SetParent(loungeRoot.transform, false);
            wallW.transform.localPosition = new Vector3(-roomHalf, wallY, 0f);
            wallW.transform.localScale = new Vector3(0.15f, wallH, 10f);
            Setup(wallW, matWall);

            // Ceiling
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(loungeRoot.transform, false);
            ceiling.transform.localPosition = new Vector3(0f, wallH + 0.05f, 0f);
            ceiling.transform.localScale = new Vector3(10f, 0.1f, 10f);
            Setup(ceiling, matWall);

            // Display Pedestal (0.90m forward)
            GameObject pedestal = new GameObject("DisplayPedestal");
            pedestal.transform.SetParent(loungeRoot.transform, false);
            pedestal.transform.localPosition = new Vector3(0f, 0f, 0.90f);

            GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            column.name = "Pedestal_Column";
            column.transform.SetParent(pedestal.transform, false);
            column.transform.localPosition = new Vector3(0f, 0.46f, 0f);
            column.transform.localScale = new Vector3(0.48f, 0.46f, 0.48f);
            Setup(column, matPedestal);

            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Pedestal_Rim";
            rim.transform.SetParent(pedestal.transform, false);
            rim.transform.localPosition = new Vector3(0f, 0.935f, 0f);
            rim.transform.localScale = new Vector3(0.52f, 0.015f, 0.52f);
            Setup(rim, matPedestal);

            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "Pedestal_Pad";
            pad.transform.SetParent(pedestal.transform, false);
            pad.transform.localPosition = new Vector3(0f, 0.955f, 0f);
            pad.transform.localScale = new Vector3(0.40f, 0.005f, 0.40f);
            Setup(pad, matPad);

            // 1. Ambient Lighting in RenderSettings
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(1.0f, 0.98f, 0.95f, 1.0f) * 1.2f;

            // 2. Strong warm Key Directional Light (Color: #FFF4E6, Intensity 1.5)
            GameObject dirLightGo = GameObject.Find("Directional Light");
            if (dirLightGo != null)
            {
                Light dirLight = dirLightGo.GetComponent<Light>();
                if (dirLight != null)
                {
                    dirLightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
                    dirLight.color = new Color(1.0f, 0.957f, 0.902f, 1.0f); // #FFF4E6
                    dirLight.intensity = 1.5f;
                    dirLight.shadows = LightShadows.Soft;
                }
            }

            // 3. Overhead Showcase Spotlight
            GameObject spotGo = new GameObject("Spotlight_Pedestal");
            spotGo.transform.SetParent(loungeRoot.transform, false);
            spotGo.transform.localPosition = new Vector3(0f, 2.85f, 0.90f);
            spotGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Light spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 3.8f;
            spot.spotAngle = 42f;
            spot.innerSpotAngle = 24f;
            spot.color = new Color(1.0f, 0.99f, 0.97f, 1.0f);
            spot.intensity = 3.5f;
            spot.shadows = LightShadows.Soft;

            // 4. Two soft point fill lights around display area to eliminate harsh dark shadows
            GameObject fillLeftGo = new GameObject("FillLight_Left");
            fillLeftGo.transform.SetParent(loungeRoot.transform, false);
            fillLeftGo.transform.localPosition = new Vector3(-1.4f, 1.9f, 0.70f);

            Light fillLeft = fillLeftGo.AddComponent<Light>();
            fillLeft.type = LightType.Point;
            fillLeft.range = 4.5f;
            fillLeft.color = new Color(1.0f, 0.97f, 0.94f, 1.0f); // Soft warm fill
            fillLeft.intensity = 2.0f;
            fillLeft.shadows = LightShadows.None;

            GameObject fillRightGo = new GameObject("FillLight_Right");
            fillRightGo.transform.SetParent(loungeRoot.transform, false);
            fillRightGo.transform.localPosition = new Vector3(1.4f, 1.9f, 1.10f);

            Light fillRight = fillRightGo.AddComponent<Light>();
            fillRight.type = LightType.Point;
            fillRight.range = 4.5f;
            fillRight.color = new Color(0.96f, 0.98f, 1.0f, 1.0f); // Soft clean showroom fill
            fillRight.intensity = 2.0f;
            fillRight.shadows = LightShadows.None;

            return loungeRoot;
        }

        public static void BuildAvatarArmRigAndStabilizer()
        {
            GameObject watchRoot = GameObject.Find("[WristWatch_Prototype]");
            if (watchRoot == null)
            {
                watchRoot = WatchBuilder.BuildWatch();
            }

            // Remove any primitive child blocks from watchRoot
            Transform childArm = watchRoot.transform.Find("AvatarArm_Rig");
            if (childArm != null)
            {
                Undo.DestroyObjectImmediate(childArm.gameObject);
            }

            // Clean up any old sleeve under LeftHandAnchor
            GameObject leftAnchor = GameObject.Find("LeftHandAnchor");
            if (leftAnchor != null)
            {
                Transform oldAnchorArm = leftAnchor.transform.Find("AvatarArm_Sleeve");
                if (oldAnchorArm != null)
                {
                    Undo.DestroyObjectImmediate(oldAnchorArm.gameObject);
                }

                // Attach the formal suit sleeve directly to LeftHandAnchor so it follows forearm orientation
                GameObject sleeveRoot = new GameObject("AvatarArm_Sleeve");
                sleeveRoot.transform.SetParent(leftAnchor.transform, false);
                sleeveRoot.transform.localPosition = Vector3.zero;
                sleeveRoot.transform.localRotation = Quaternion.identity;

                Material matSleeve = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_SuitSleeve.mat");
                Material matShirt = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_ShirtCuff.mat");

                void Clean(GameObject go, Material m)
                {
                    Collider c = go.GetComponent<Collider>();
                    if (c != null) Object.DestroyImmediate(c);
                    var mr = go.GetComponent<MeshRenderer>();
                    if (mr != null && m != null) mr.sharedMaterial = m;
                }

                // 1. Forearm Suit Sleeve (cylinder aligned along -Z towards elbow)
                GameObject forearmSleeve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                forearmSleeve.name = "Forearm_Sleeve";
                forearmSleeve.transform.SetParent(sleeveRoot.transform, false);
                forearmSleeve.transform.localPosition = new Vector3(0f, 0f, -0.15f);
                forearmSleeve.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                forearmSleeve.transform.localScale = new Vector3(0.075f, 0.11f, 0.075f);
                Clean(forearmSleeve, matSleeve);

                // 2. Suit Jacket Cuff (ring behind wrist)
                GameObject jacketCuff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                jacketCuff.name = "Jacket_Cuff";
                jacketCuff.transform.SetParent(sleeveRoot.transform, false);
                jacketCuff.transform.localPosition = new Vector3(0f, 0f, -0.035f);
                jacketCuff.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                jacketCuff.transform.localScale = new Vector3(0.078f, 0.012f, 0.078f);
                Clean(jacketCuff, matSleeve);

                // 3. Shirt Cuff (crisp white shirt cuff peeking 6mm out)
                GameObject shirtCuff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shirtCuff.name = "Shirt_Cuff";
                shirtCuff.transform.SetParent(sleeveRoot.transform, false);
                shirtCuff.transform.localPosition = new Vector3(0f, 0f, -0.022f);
                shirtCuff.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                shirtCuff.transform.localScale = new Vector3(0.072f, 0.006f, 0.072f);
                Clean(shirtCuff, matShirt);
            }

            // Parent watch directly to LeftHandAnchor for 100% steady, zero-latency physical anchoring
            if (leftAnchor != null)
            {
                watchRoot.transform.SetParent(leftAnchor.transform, false);
                watchRoot.transform.localPosition = new Vector3(0f, 0.038f, -0.040f);
                watchRoot.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                watchRoot.transform.localScale = Vector3.one;
            }

            // Disable runtime tracking scripts so they do not override the steady transform
            WristTwistStabilizer stabilizer = watchRoot.GetComponent<WristTwistStabilizer>();
            if (stabilizer != null)
            {
                stabilizer.ActiveFollow = false;
                stabilizer.enabled = false;
            }

            WristAnchorFollower oldFollower = watchRoot.GetComponent<WristAnchorFollower>();
            if (oldFollower != null)
            {
                oldFollower.FollowActive = false;
                oldFollower.enabled = false;
            }

            string prefabPath = "Assets/_Project/Prefabs/WristWatch_Prototype.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(watchRoot, prefabPath, InteractionMode.AutomatedAction);
        }

        public static void ConfigureArticulatedHands()
        {
            Material matGlove = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Glove_Leather.mat");
            if (matGlove == null) return;

            string[] handObjectNames = new string[] { "[BuildingBlock] Hand Tracking left", "[BuildingBlock] Hand Tracking right" };

            foreach (var name in handObjectNames)
            {
                GameObject handObj = GameObject.Find(name);
                if (handObj == null) continue;

                // Configure SkinnedMeshRenderer
                SkinnedMeshRenderer smr = handObj.GetComponent<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    smr.sharedMaterial = matGlove;
                    EditorUtility.SetDirty(smr);
                }

                // Configure OVRMeshRenderer to use the glove material for normal & system gestures
                OVRMeshRenderer meshRenderer = handObj.GetComponent<OVRMeshRenderer>();
                if (meshRenderer != null)
                {
                    SerializedObject so = new SerializedObject(meshRenderer);
                    SerializedProperty sysMat = so.FindProperty("_systemGestureMaterial");
                    if (sysMat != null)
                    {
                        sysMat.objectReferenceValue = matGlove;
                        so.ApplyModifiedProperties();
                    }
                }

                // Ensure OVRSkeleton is active and tracking bones 1:1
                OVRSkeleton skel = handObj.GetComponent<OVRSkeleton>();
                if (skel != null)
                {
                    skel.enabled = true;
                }

                // Ensure OVRSkeletonRenderer does not draw debug primitive capsules
                OVRSkeletonRenderer skelRenderer = handObj.GetComponent<OVRSkeletonRenderer>();
                if (skelRenderer != null)
                {
                    skelRenderer.enabled = false;
                }
            }
        }

        [MenuItem("VRWatch/Configure P1 to P3 (Audio, Passthrough, Chrono)", false, 12)]
        public static void BuildP1ToP3Features()
        {
            GameObject watch = GameObject.Find("[WristWatch_Prototype]");
            if (watch == null) return;

            Material matMetal = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchMetal.mat");
            Material matChrono = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchChronoHand.mat");

            // 1. P1: WatchAudioController
            WatchAudioController audioCtrl = watch.GetComponent<WatchAudioController>();
            if (audioCtrl == null) audioCtrl = watch.AddComponent<WatchAudioController>();

            // 2. P3: Pushers on watch body
            Transform existingStart = watch.transform.Find("Pusher_StartStop");
            if (existingStart != null) Undo.DestroyObjectImmediate(existingStart.gameObject);

            Transform existingReset = watch.transform.Find("Pusher_Reset");
            if (existingReset != null) Undo.DestroyObjectImmediate(existingReset.gameObject);

            // 2 o'clock Pusher (Start / Stop)
            GameObject pStart = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pStart.name = "Pusher_StartStop";
            pStart.transform.SetParent(watch.transform, false);
            pStart.transform.localPosition = new Vector3(0.019f, 0f, 0.011f);
            pStart.transform.localRotation = Quaternion.Euler(0f, 30f, 90f);
            pStart.transform.localScale = new Vector3(0.0035f, 0.0022f, 0.0035f);
            var mrStart = pStart.GetComponent<MeshRenderer>();
            if (mrStart != null && matMetal != null) mrStart.sharedMaterial = matMetal;
            var colStart = pStart.GetComponent<Collider>();
            if (colStart != null) colStart.isTrigger = true;
            var intStart = pStart.AddComponent<PusherInteractable>();

            // 4 o'clock Pusher (Reset)
            GameObject pReset = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pReset.name = "Pusher_Reset";
            pReset.transform.SetParent(watch.transform, false);
            pReset.transform.localPosition = new Vector3(0.019f, 0f, -0.011f);
            pReset.transform.localRotation = Quaternion.Euler(0f, -30f, 90f);
            pReset.transform.localScale = new Vector3(0.0035f, 0.0022f, 0.0035f);
            var mrReset = pReset.GetComponent<MeshRenderer>();
            if (mrReset != null && matMetal != null) mrReset.sharedMaterial = matMetal;
            var colReset = pReset.GetComponent<Collider>();
            if (colReset != null) colReset.isTrigger = true;
            var intReset = pReset.AddComponent<PusherInteractable>();

            // Chronograph Second Hand under WatchFace
            Transform face = watch.transform.Find("WatchFace");
            if (face != null)
            {
                Transform existingChronoPivot = face.Find("ChronoSecondPivot");
                if (existingChronoPivot != null) Undo.DestroyObjectImmediate(existingChronoPivot.gameObject);

                GameObject chronoPivot = new GameObject("ChronoSecondPivot");
                chronoPivot.transform.SetParent(face, false);
                chronoPivot.transform.localPosition = new Vector3(0f, 0.0011f, 0f);
                chronoPivot.transform.localRotation = Quaternion.identity;
                chronoPivot.transform.localScale = Vector3.one;

                GameObject chronoMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chronoMesh.name = "ChronoSecondHand_Mesh";
                chronoMesh.transform.SetParent(chronoPivot.transform, false);
                chronoMesh.transform.localPosition = new Vector3(0f, 0f, 0.0075f);
                chronoMesh.transform.localRotation = Quaternion.identity;
                chronoMesh.transform.localScale = new Vector3(0.0005f, 0.00025f, 0.018f);
                var cCol = chronoMesh.GetComponent<Collider>();
                if (cCol != null) Object.DestroyImmediate(cCol);
                var mrChrono = chronoMesh.GetComponent<MeshRenderer>();
                if (mrChrono != null && matChrono != null) mrChrono.sharedMaterial = matChrono;

                // WatchChronographController
                WatchChronographController chronoCtrl = watch.GetComponent<WatchChronographController>();
                if (chronoCtrl == null) chronoCtrl = watch.AddComponent<WatchChronographController>();
                chronoCtrl.SetReferences(chronoPivot.transform, pStart.transform, pReset.transform, audioCtrl);

                intStart.AddListener(() => chronoCtrl.ToggleStartStop());
                intReset.AddListener(() => chronoCtrl.ResetChronograph());

                SerializedObject soChrono = new SerializedObject(chronoCtrl);
                soChrono.FindProperty("chronoSecondHand").objectReferenceValue = chronoPivot.transform;
                soChrono.FindProperty("pusherStartStop").objectReferenceValue = pStart.transform;
                soChrono.FindProperty("pusherReset").objectReferenceValue = pReset.transform;
                soChrono.FindProperty("audioController").objectReferenceValue = audioCtrl;
                soChrono.ApplyModifiedProperties();
            }

            // 3. P2: LoungeModeManager
            GameObject modeManagerGo = GameObject.Find("[ModeManager]");
            if (modeManagerGo == null) modeManagerGo = new GameObject("[ModeManager]");
            LoungeModeManager modeManager = modeManagerGo.GetComponent<LoungeModeManager>();
            if (modeManager == null) modeManager = modeManagerGo.AddComponent<LoungeModeManager>();

            // Add 3D Mode Toggle Plaque on DisplayPedestal
            GameObject pedestal = GameObject.Find("DisplayPedestal");
            if (pedestal != null)
            {
                Transform existingToggle = pedestal.transform.Find("Pedestal_ModeToggle");
                if (existingToggle != null) Undo.DestroyObjectImmediate(existingToggle.gameObject);

                GameObject toggleBtn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                toggleBtn.name = "Pedestal_ModeToggle";
                toggleBtn.transform.SetParent(pedestal.transform, false);
                toggleBtn.transform.localPosition = new Vector3(0.24f, 0.94f, 0f);
                toggleBtn.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                toggleBtn.transform.localScale = new Vector3(0.035f, 0.012f, 0.035f);
                var mrToggle = toggleBtn.GetComponent<MeshRenderer>();
                if (mrToggle != null && matMetal != null) mrToggle.sharedMaterial = matMetal;
                var colToggle = toggleBtn.GetComponent<Collider>();
                if (colToggle != null) colToggle.isTrigger = true;
                var intToggle = toggleBtn.AddComponent<PusherInteractable>();
                intToggle.AddListener(() => modeManager.ToggleMode());
            }

            string prefabPath = "Assets/_Project/Prefabs/WristWatch_Prototype.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(watch, prefabPath, InteractionMode.AutomatedAction);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[LoungeBuilder] P1, P2, and P3 configured and saved.");
        }
    }
}
