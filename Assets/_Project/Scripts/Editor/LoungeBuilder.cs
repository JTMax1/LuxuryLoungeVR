using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using VRWatch.Core;
using VRWatch.Interaction;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using Oculus.Interaction.Input;

namespace VRWatch.Editor
{
    public static class LoungeBuilder
    {
        [MenuItem("VRWatch/Execute Full Physical Remediation (Phases 1-4)", false, 10)]
        public static void ExecuteFullRemediation()
        {
            SetupMaterials();
            BuildEnvironmentLounge();
            BuildAvatarArmRigAndStabilizer();
            ConfigureArticulatedHands();
            BuildP4AndP5Features();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[LoungeBuilder] Full Physical Remediation (Phases 1-4) successfully executed and saved.");
        }

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

            Action<string, Color, float, float, bool> createOrUpdateMat = (path, baseColor, metallic, smoothness, transparent) => {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(shader);
                    AssetDatabase.CreateAsset(mat, path);
                }
                else
                {
                    mat.shader = shader;
                }

                mat.SetColor("_BaseColor", baseColor);
                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Smoothness", smoothness);

                if (transparent)
                {
                    mat.SetFloat("_Surface", 1.0f);
                    mat.SetFloat("_Blend", 0.0f);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = 3000;
                    mat.SetOverrideTag("RenderType", "Transparent");
                }
                else
                {
                    mat.SetFloat("_Surface", 0.0f);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1);
                    mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = -1;
                    mat.SetOverrideTag("RenderType", "Opaque");
                }

                EditorUtility.SetDirty(mat);
            };

            // Architectural & Lounge Materials
            createOrUpdateMat("Assets/_Project/Materials/M_LoungeWall.mat", new Color(0.9255f, 0.9216f, 0.9098f, 1.0f), 0.0f, 0.22f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_LoungeFloor.mat", new Color(0.960f, 0.957f, 0.941f, 1.0f), 0.05f, 0.70f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_PedestalMarble.mat", new Color(0.07f, 0.07f, 0.08f, 1f), 0.05f, 0.88f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_PedestalBrass.mat", new Color(0.83f, 0.68f, 0.22f, 1f), 0.92f, 0.80f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_PedestalPad.mat", new Color(0.08f, 0.10f, 0.14f, 1f), 0.02f, 0.15f, false);

            // Avatar & Sleeve
            createOrUpdateMat("Assets/_Project/Materials/M_Glove_Leather.mat", new Color(0.12f, 0.12f, 0.14f, 1.0f), 0.08f, 0.38f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_Avatar_Skin.mat", new Color(0.85f, 0.68f, 0.58f, 1.0f), 0.0f, 0.28f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_SuitSleeve.mat", new Color(0.11f, 0.11f, 0.13f, 1.0f), 0.05f, 0.15f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_ShirtCuff.mat", new Color(0.96f, 0.96f, 0.97f, 1.0f), 0.0f, 0.12f, false);

            // Watch & Presets
            createOrUpdateMat("Assets/_Project/Materials/M_WatchMetal.mat", new Color(0.92f, 0.92f, 0.94f, 1f), 0.95f, 0.92f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_WatchFace.mat", new Color(0.88f, 0.88f, 0.90f, 1f), 0.30f, 0.60f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_WatchHands.mat", new Color(0.85f, 0.72f, 0.30f, 1f), 0.95f, 0.90f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_WatchChronoHand.mat", new Color(0.95f, 0.22f, 0.05f, 1f), 0.40f, 0.80f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_WatchStrap.mat", new Color(0.10f, 0.10f, 0.12f, 1f), 0.05f, 0.35f, false);

            createOrUpdateMat("Assets/_Project/Materials/M_WatchRoseGold.mat", new Color(0.86f, 0.62f, 0.46f, 1f), 0.90f, 0.85f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_WatchDialChampagne.mat", new Color(0.88f, 0.80f, 0.68f, 1f), 0.35f, 0.65f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_WatchDialBlackHeritage.mat", new Color(0.08f, 0.08f, 0.08f, 1f), 0.20f, 0.55f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_WatchStrapBrown.mat", new Color(0.38f, 0.20f, 0.12f, 1f), 0.04f, 0.40f, false);

            createOrUpdateMat("Assets/_Project/Materials/M_WatchTitanium.mat", new Color(0.18f, 0.18f, 0.20f, 1f), 0.85f, 0.60f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_WatchBezelDiver.mat", new Color(0.09f, 0.10f, 0.11f, 1f), 0.85f, 0.95f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_WatchDialNavy.mat", new Color(0.07f, 0.11f, 0.20f, 1f), 0.25f, 0.60f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_WatchHandsIceBlue.mat", new Color(0.20f, 0.55f, 0.95f, 1f), 0.85f, 0.85f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_WatchStrapNavy.mat", new Color(0.08f, 0.12f, 0.22f, 1f), 0.05f, 0.35f, false);

            createOrUpdateMat("Assets/_Project/Materials/M_GlassSapphire.mat", new Color(0.92f, 0.96f, 1.0f, 0.18f), 0.05f, 0.98f, true);

            // Super-LumiNova Material
            var matLume = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchLumeGlow.mat");
            if (matLume == null)
            {
                matLume = new Material(shader);
                AssetDatabase.CreateAsset(matLume, "Assets/_Project/Materials/M_WatchLumeGlow.mat");
            }
            matLume.shader = shader;
            matLume.SetColor("_BaseColor", new Color(0.75f, 0.95f, 0.88f, 1.0f));
            matLume.EnableKeyword("_EMISSION");
            matLume.SetColor("_EmissionColor", new Color(0.08f, 0.85f, 0.60f, 1.0f) * 0.1f);
            matLume.SetFloat("_Metallic", 0.10f);
            matLume.SetFloat("_Smoothness", 0.75f);
            EditorUtility.SetDirty(matLume);

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

            Action<GameObject, Material> setupMesh = (go, mat) => {
                Collider c = go.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null && mat != null) mr.sharedMaterial = mat;
            };

            // Floor (10m x 10m)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(loungeRoot.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(10f, 0.1f, 10f);
            setupMesh(floor, matFloor);

            // Walls (North, South, East, West) - Height: 3.5m
            float wallH = 3.5f;
            float wallY = wallH * 0.5f;
            float roomHalf = 5f;

            GameObject wallN = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallN.name = "Wall_North";
            wallN.transform.SetParent(loungeRoot.transform, false);
            wallN.transform.localPosition = new Vector3(0f, wallY, roomHalf);
            wallN.transform.localScale = new Vector3(10f, wallH, 0.15f);
            setupMesh(wallN, matWall);

            GameObject wallS = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallS.name = "Wall_South";
            wallS.transform.SetParent(loungeRoot.transform, false);
            wallS.transform.localPosition = new Vector3(0f, wallY, -roomHalf);
            wallS.transform.localScale = new Vector3(10f, wallH, 0.15f);
            setupMesh(wallS, matWall);

            GameObject wallE = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallE.name = "Wall_East";
            wallE.transform.SetParent(loungeRoot.transform, false);
            wallE.transform.localPosition = new Vector3(roomHalf, wallY, 0f);
            wallE.transform.localScale = new Vector3(0.15f, wallH, 10f);
            setupMesh(wallE, matWall);

            GameObject wallW = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallW.name = "Wall_West";
            wallW.transform.SetParent(loungeRoot.transform, false);
            wallW.transform.localPosition = new Vector3(-roomHalf, wallY, 0f);
            wallW.transform.localScale = new Vector3(0.15f, wallH, 10f);
            setupMesh(wallW, matWall);

            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(loungeRoot.transform, false);
            ceiling.transform.localPosition = new Vector3(0f, wallH + 0.05f, 0f);
            ceiling.transform.localScale = new Vector3(10f, 0.1f, 10f);
            setupMesh(ceiling, matWall);

            // Ambient Lighting & Key Light
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(1.0f, 0.98f, 0.95f, 1.0f) * 1.2f;

            GameObject dirLightGo = GameObject.Find("Directional Light");
            if (dirLightGo != null)
            {
                Light dirLight = dirLightGo.GetComponent<Light>();
                if (dirLight != null)
                {
                    dirLight.color = new Color(1.0f, 0.957f, 0.902f);
                    dirLight.intensity = 1.5f;
                    dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                }
            }

            return loungeRoot;
        }

        public static void BuildAvatarArmRigAndStabilizer()
        {
            GameObject trackingSpace = GameObject.Find("TrackingSpace");
            if (trackingSpace == null) return;

            Transform leftHandAnchor = trackingSpace.transform.Find("LeftHandAnchor");
            if (leftHandAnchor == null) return;

            GameObject watch = GameObject.Find("[WristWatch_Prototype]");
            if (watch == null) return;

            watch.transform.SetParent(leftHandAnchor, false);
            watch.transform.localPosition = new Vector3(0.0f, 0.038f, -0.040f);
            watch.transform.localRotation = Quaternion.Euler(0.0f, 270.0f, 0.0f);
            watch.transform.localScale = Vector3.one;

            var follower = watch.GetComponent<WristAnchorFollower>();
            if (follower != null) follower.enabled = false;
            var stabilizer = watch.GetComponent<WristTwistStabilizer>();
            if (stabilizer != null) stabilizer.enabled = false;

            Transform oldSleeve = leftHandAnchor.Find("AvatarArm_Sleeve");
            if (oldSleeve != null) Undo.DestroyObjectImmediate(oldSleeve.gameObject);

            GameObject sleeveRoot = new GameObject("AvatarArm_Sleeve");
            sleeveRoot.transform.SetParent(leftHandAnchor, false);
            sleeveRoot.transform.localPosition = new Vector3(0.0f, 0.038f, -0.040f);
            sleeveRoot.transform.localRotation = Quaternion.Euler(0.0f, 270.0f, 0.0f);

            Material matSleeve = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_SuitSleeve.mat");
            Material matShirt = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_ShirtCuff.mat");

            Action<GameObject, Material> setupCuffMesh = (go, mat) => {
                Collider c = go.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null && mat != null) mr.sharedMaterial = mat;
            };

            // Forearm sleeve
            GameObject forearm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            forearm.name = "Forearm_Sleeve";
            forearm.transform.SetParent(sleeveRoot.transform, false);
            forearm.transform.localPosition = new Vector3(-0.095f, 0.0f, 0.0f);
            forearm.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
            forearm.transform.localScale = new Vector3(0.062f, 0.080f, 0.054f);
            setupCuffMesh(forearm, matSleeve);

            // Jacket cuff
            GameObject jacketCuff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            jacketCuff.name = "Jacket_Cuff";
            jacketCuff.transform.SetParent(sleeveRoot.transform, false);
            jacketCuff.transform.localPosition = new Vector3(-0.026f, 0.0f, 0.0f);
            jacketCuff.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
            jacketCuff.transform.localScale = new Vector3(0.066f, 0.012f, 0.058f);
            setupCuffMesh(jacketCuff, matSleeve);

            // Shirt cuff
            GameObject shirtCuff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shirtCuff.name = "Shirt_Cuff";
            shirtCuff.transform.SetParent(sleeveRoot.transform, false);
            shirtCuff.transform.localPosition = new Vector3(-0.017f, 0.0f, 0.0f);
            shirtCuff.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
            shirtCuff.transform.localScale = new Vector3(0.060f, 0.005f, 0.052f);
            setupCuffMesh(shirtCuff, matShirt);
        }

        public static void ConfigureArticulatedHands()
        {
            Material matSkin = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Avatar_Skin.mat");
            if (matSkin == null) return;

            string[] handBlockNames = new string[] { "[BuildingBlock] Hand Tracking left", "[BuildingBlock] Hand Tracking right" };
            foreach (var blockName in handBlockNames)
            {
                GameObject handBlock = GameObject.Find(blockName);
                if (handBlock == null) continue;

                var smr = handBlock.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (smr != null)
                {
                    smr.sharedMaterial = matSkin;
                    smr.enabled = true;
                }
            }
        }

        [MenuItem("VRWatch/Configure P4 & P5 (VIP Catalog, Super-LumiNova)", false, 13)]
        public static void BuildP4AndP5Features()
        {
            GameObject watch = GameObject.Find("[WristWatch_Prototype]");
            if (watch == null) return;

            // Load Materials
            Material matSteel = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchMetal.mat");
            Material matDialSteel = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchFace.mat");
            Material matHandsGold = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchHands.mat");
            Material matChronoRed = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchChronoHand.mat");
            Material matStrapBlack = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchStrap.mat");

            Material matRoseGold = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchRoseGold.mat");
            Material matDialBlackHeritage = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchDialBlackHeritage.mat");
            Material matStrapBrown = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchStrapBrown.mat");

            Material matTitanium = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchTitanium.mat");
            Material matBezelDiver = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchBezelDiver.mat");
            Material matDialNavy = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchDialNavy.mat");
            Material matHandsIceBlue = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchHandsIceBlue.mat");
            Material matStrapNavy = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchStrapNavy.mat");

            Material matLume = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchLumeGlow.mat");
            Material matGlass = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_GlassSapphire.mat");
            Material matMarble = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_PedestalMarble.mat");
            Material matBrass = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_PedestalBrass.mat");
            Material matPad = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_PedestalPad.mat");

            // 1. Setup WatchCatalogManager
            WatchCatalogManager catalogMgr = watch.GetComponent<WatchCatalogManager>();
            if (catalogMgr == null) catalogMgr = watch.AddComponent<WatchCatalogManager>();
            catalogMgr.ClearPresets();

            catalogMgr.AddPreset(new WatchCatalogManager.WatchModelPreset
            {
                modelName = "Classic Steel Chronograph",
                caseMaterial = matSteel,
                bezelMaterial = matSteel,
                dialMaterial = matDialSteel,
                handsMaterial = matHandsIceBlue,
                chronoHandMaterial = matChronoRed,
                strapMaterial = matStrapBlack,
                markersMaterial = matLume
            });

            catalogMgr.AddPreset(new WatchCatalogManager.WatchModelPreset
            {
                modelName = "Rose Gold Royal Heritage",
                caseMaterial = matRoseGold,
                bezelMaterial = matRoseGold,
                dialMaterial = matDialBlackHeritage,
                handsMaterial = matRoseGold,
                chronoHandMaterial = matChronoRed,
                strapMaterial = matStrapBrown,
                markersMaterial = matRoseGold
            });

            catalogMgr.AddPreset(new WatchCatalogManager.WatchModelPreset
            {
                modelName = "Titanium Stealth Diver",
                caseMaterial = matTitanium,
                bezelMaterial = matBezelDiver,
                dialMaterial = matDialNavy,
                handsMaterial = matHandsIceBlue,
                chronoHandMaterial = matHandsIceBlue,
                strapMaterial = matStrapNavy,
                markersMaterial = matLume
            });

            // 2. Setup WatchLumeController
            WatchLumeController lumeCtrl = watch.GetComponent<WatchLumeController>();
            if (lumeCtrl == null) lumeCtrl = watch.AddComponent<WatchLumeController>();

            SerializedObject soLume = new SerializedObject(lumeCtrl);
            soLume.FindProperty("lumeMaterial").objectReferenceValue = matLume;
            soLume.FindProperty("dayEmission").colorValue = Color.black;
            soLume.FindProperty("nightEmission").colorValue = new Color(0.20f, 1.0f, 0.75f) * 3.8f;
            soLume.ApplyModifiedProperties();

            // 3. Setup LoungeModeManager
            GameObject modeManagerGo = GameObject.Find("[ModeManager]");
            if (modeManagerGo == null) modeManagerGo = new GameObject("[ModeManager]");
            LoungeModeManager modeMgr = modeManagerGo.GetComponent<LoungeModeManager>();
            if (modeMgr == null) modeMgr = modeManagerGo.AddComponent<LoungeModeManager>();

            // 4. Update Active Wristwatch with Sapphire Crystal & Dimensional Face
            UpdateActiveWristwatchPrototype(watch, matGlass, matSteel, matLume);

            // 5. Phase 1 & 2: Setup Real Visual Pedestal, 3D Mode Console, and Poke Surfaces
            SetupPedestalConsoleAndTray(matMarble, matBrass, matPad, matSteel, matRoseGold, matTitanium, matBezelDiver, matDialSteel, matDialBlackHeritage, matDialNavy, matHandsIceBlue, matChronoRed, matStrapBlack, matStrapBrown, matStrapNavy, matLume, matGlass, modeMgr, lumeCtrl, catalogMgr);

            // 6. Floating Wrist Ribbon / Menu on the Watch
            SetupFloatingWristMenu(watch, matGlass, matBrass, matLume, modeMgr);

            // 7. Configure Hand Tracking Poke Rig
            ConfigureHandTrackingPokeRig();

            // Save Prefab and Scene
            string prefabPath = "Assets/_Project/Prefabs/WristWatch_Prototype.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(watch, prefabPath, InteractionMode.AutomatedAction);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[LoungeBuilder] P1-P5 Full Remediation completed and saved.");
        }

        private static void UpdateActiveWristwatchPrototype(GameObject watch, Material matGlass, Material matMetal, Material matLume)
        {
            Transform face = watch.transform.Find("WatchFace");
            if (face == null) return;

            // Ensure 12 hour markers use luminous/accent material
            for (int h = 0; h < 12; h++)
            {
                Transform marker = face.Find($"Marker_{h}");
                if (marker != null)
                {
                    var mr = marker.GetComponent<MeshRenderer>();
                    if (mr != null && matLume != null) mr.sharedMaterial = matLume;
                }
            }

            // Sapphire Crystal disc over dial
            Transform crystal = face.Find("SapphireCrystal");
            if (crystal == null)
            {
                GameObject glassObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                glassObj.name = "SapphireCrystal";
                glassObj.transform.SetParent(face, false);
                glassObj.transform.localPosition = new Vector3(0f, 0.0035f, 0f);
                glassObj.transform.localRotation = Quaternion.identity;
                glassObj.transform.localScale = new Vector3(0.038f, 0.0006f, 0.038f);

                Collider c = glassObj.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);

                var mr = glassObj.GetComponent<MeshRenderer>();
                if (mr != null && matGlass != null) mr.sharedMaterial = matGlass;
            }

            // Upgrade Pushers with Meta XR Poke Interaction
            var chrono = watch.GetComponent<WatchChronographController>();
            Transform pusherStart = watch.transform.Find("Pusher_StartStop");
            if (pusherStart != null && chrono != null)
            {
                ConfigurePokeButton(pusherStart.gameObject, pusherStart, Vector3.up, 0.005f, () => chrono.ToggleStartStop());
            }

            Transform pusherReset = watch.transform.Find("Pusher_Reset");
            if (pusherReset != null && chrono != null)
            {
                ConfigurePokeButton(pusherReset.gameObject, pusherReset, Vector3.up, 0.005f, () => chrono.ResetChronograph());
            }
        }

        private static void SetupPedestalConsoleAndTray(
            Material matMarble, Material matBrass, Material matPad,
            Material matSteel, Material matRoseGold, Material matTitanium, Material matBezelDiver,
            Material matDialSteel, Material matDialBlackHeritage, Material matDialNavy,
            Material matHandsIceBlue, Material matChronoRed,
            Material matStrapBlack, Material matStrapBrown, Material matStrapNavy,
            Material matLume, Material matGlass,
            LoungeModeManager modeMgr, WatchLumeController lumeCtrl, WatchCatalogManager catalogMgr)
        {
            // Remove any old pedestal (inside lounge or root)
            GameObject oldPedestalInLounge = GameObject.Find("[Environment_Lounge]/DisplayPedestal");
            if (oldPedestalInLounge != null) Undo.DestroyObjectImmediate(oldPedestalInLounge);

            GameObject oldPedestalRoot = GameObject.Find("DisplayPedestal");
            if (oldPedestalRoot != null) Undo.DestroyObjectImmediate(oldPedestalRoot);

            // Create standalone DisplayPedestal at root: Z = +0.75m forward, tabletop at Y = 0.85m
            GameObject pedestal = new GameObject("DisplayPedestal");
            Undo.RegisterCreatedObjectUndo(pedestal, "Create Standalone DisplayPedestal");
            pedestal.transform.position = new Vector3(0f, 0f, 0.75f);
            pedestal.transform.rotation = Quaternion.identity;

            Action<GameObject, Material> setupMesh = (go, mat) => {
                Collider c = go.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null && mat != null) mr.sharedMaterial = mat;
            };

            // 1. Pedestal Base Ring (Y: 0.00 -> 0.04m, dia: 0.52m)
            GameObject pBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pBase.name = "Pedestal_Base";
            pBase.transform.SetParent(pedestal.transform, false);
            pBase.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            pBase.transform.localScale = new Vector3(0.52f, 0.035f, 0.52f);
            setupMesh(pBase, matBrass);

            // 2. Pedestal Column (Y: 0.07 -> 0.81m, dia: 0.46m)
            GameObject pColumn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pColumn.name = "Pedestal_Column";
            pColumn.transform.SetParent(pedestal.transform, false);
            pColumn.transform.localPosition = new Vector3(0f, 0.435f, 0f);
            pColumn.transform.localScale = new Vector3(0.46f, 0.365f, 0.46f);
            setupMesh(pColumn, matMarble);

            // 3. Pedestal Fluted Collar Ring (Y: 0.80 -> 0.82m, dia: 0.50m)
            GameObject pCollar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pCollar.name = "Pedestal_Collar";
            pCollar.transform.SetParent(pedestal.transform, false);
            pCollar.transform.localPosition = new Vector3(0f, 0.810f, 0f);
            pCollar.transform.localScale = new Vector3(0.50f, 0.010f, 0.50f);
            setupMesh(pCollar, matBrass);

            // 4. Pedestal Tabletop Plinth (Y: 0.82 -> 0.85m, dia: 0.52m)
            GameObject pTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pTop.name = "Pedestal_TopTable";
            pTop.transform.SetParent(pedestal.transform, false);
            pTop.transform.localPosition = new Vector3(0f, 0.835f, 0f);
            pTop.transform.localScale = new Vector3(0.52f, 0.015f, 0.52f);
            setupMesh(pTop, matMarble);

            // 5. Plush Velvet Tabletop Pad (Y: 0.85m, dia: 0.44m)
            GameObject pPad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pPad.name = "Pedestal_Pad";
            pPad.transform.SetParent(pedestal.transform, false);
            pPad.transform.localPosition = new Vector3(0f, 0.852f, 0f);
            pPad.transform.localScale = new Vector3(0.44f, 0.002f, 0.44f);
            setupMesh(pPad, matPad);

            // --- 3D Angled Mode Switcher Console ---
            // Angled 30 degrees upward toward user eyes, located at front edge of tabletop
            GameObject console = new GameObject("Pedestal_Console");
            console.transform.SetParent(pedestal.transform, false);
            console.transform.localPosition = new Vector3(0f, 0.865f, -0.11f);
            console.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);

            // Console Plinth Housing (32cm wide, 9cm deep, 2.2cm thick)
            GameObject consoleHousing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            consoleHousing.name = "Console_Housing";
            consoleHousing.transform.SetParent(console.transform, false);
            consoleHousing.transform.localPosition = Vector3.zero;
            consoleHousing.transform.localScale = new Vector3(0.32f, 0.022f, 0.09f);
            setupMesh(consoleHousing, matTitanium);

            // Console Trim Lip
            GameObject consoleTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            consoleTrim.name = "Console_Trim";
            consoleTrim.transform.SetParent(console.transform, false);
            consoleTrim.transform.localPosition = new Vector3(0f, -0.012f, 0f);
            consoleTrim.transform.localScale = new Vector3(0.325f, 0.005f, 0.095f);
            setupMesh(consoleTrim, matBrass);

            // Helper to build prominent mechanical buttons on console
            Action<string, Vector3, Material, Material, string, UnityEngine.Events.UnityAction> createConsoleButton =
                (btnName, localPos, collarMat, capMat, labelText, onClick) =>
            {
                GameObject btnRoot = new GameObject(btnName);
                btnRoot.transform.SetParent(console.transform, false);
                btnRoot.transform.localPosition = localPos;

                // Raised Collar Base
                GameObject collar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                collar.name = "Collar";
                collar.transform.SetParent(btnRoot.transform, false);
                collar.transform.localPosition = new Vector3(0f, 0.008f, 0f);
                collar.transform.localScale = new Vector3(0.038f, 0.008f, 0.038f);
                setupMesh(collar, collarMat);

                // Raised Moving Button Cap (Mechanical cylinder)
                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.name = "ButtonCap";
                cap.transform.SetParent(btnRoot.transform, false);
                cap.transform.localPosition = new Vector3(0f, 0.018f, 0f);
                cap.transform.localScale = new Vector3(0.032f, 0.008f, 0.032f);
                setupMesh(cap, capMat);

                // 3D Text Label floating directly above button
                GameObject textObj = new GameObject("Label");
                textObj.transform.SetParent(btnRoot.transform, false);
                textObj.transform.localPosition = new Vector3(0f, 0.030f, 0f);
                textObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var tm = textObj.AddComponent<TextMesh>();
                tm.text = labelText;
                tm.fontSize = 26;
                tm.characterSize = 0.0016f;
                tm.alignment = TextAlignment.Center;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.color = Color.white;

                // Setup Meta XR Poke & Touch
                ConfigurePokeButton(btnRoot, cap.transform, Vector3.up, 0.005f, onClick);
            };

            // Button 1: VR Lounge / MR Passthrough Toggle
            createConsoleButton("Btn_ToggleMode", new Vector3(-0.095f, 0f, 0f), matBrass, matBrass, "VR / MR\nPASSTHROUGH", () => modeMgr.ToggleMode());

            // Button 2: Super-LumiNova Night Mode Toggle
            createConsoleButton("Btn_ToggleLume", new Vector3(0.0f, 0f, 0f), matTitanium, matLume, "SUPER\nLUMINOVA", () => lumeCtrl.ToggleNightMode());

            // Button 3: Cycle Luxury Watch Preset
            createConsoleButton("Btn_NextPreset", new Vector3(0.095f, 0f, 0f), matSteel, matHandsIceBlue, "CYCLE\nWATCH", () => catalogMgr.CyclePreset());

            // --- Showcase Tray & Detailed 3D Watches ---
            GameObject tray = new GameObject("WatchShowcase_Tray");
            tray.transform.SetParent(pedestal.transform, false);
            tray.transform.localPosition = new Vector3(0f, 0.858f, 0.07f);

            // Velvet Tray Platter
            GameObject trayBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trayBase.name = "Tray_Platter";
            trayBase.transform.SetParent(tray.transform, false);
            trayBase.transform.localPosition = Vector3.zero;
            trayBase.transform.localScale = new Vector3(0.36f, 0.010f, 0.16f);
            setupMesh(trayBase, matPad);

            // Tray Brass Border Rim
            GameObject trayRim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trayRim.name = "Tray_BrassRim";
            trayRim.transform.SetParent(tray.transform, false);
            trayRim.transform.localPosition = new Vector3(0f, -0.005f, 0f);
            trayRim.transform.localScale = new Vector3(0.365f, 0.004f, 0.165f);
            setupMesh(trayRim, matBrass);

            // Build 3 fully detailed luxury watches on angled showcase stands
            // Preset 0: Classic Steel Chronograph
            BuildDetailedShowcaseWatch(
                tray.transform, "Showcase_Steel",
                new Vector3(-0.115f, 0.018f, 0.0f), Quaternion.Euler(25f, 0f, 0f),
                matSteel, matSteel, matDialSteel, matHandsIceBlue, matChronoRed, matStrapBlack, matLume, matGlass,
                isDiver: false, presetIndex: 0, catalogMgr: catalogMgr);

            // Preset 1: Rose Gold Royal Heritage (Elevated center centerpiece)
            BuildDetailedShowcaseWatch(
                tray.transform, "Showcase_RoseGold",
                new Vector3(0.0f, 0.026f, 0.035f), Quaternion.Euler(25f, 0f, 0f),
                matRoseGold, matRoseGold, matDialBlackHeritage, matRoseGold, matChronoRed, matStrapBrown, matRoseGold, matGlass,
                isDiver: false, presetIndex: 1, catalogMgr: catalogMgr);

            // Preset 2: Titanium Stealth Diver
            BuildDetailedShowcaseWatch(
                tray.transform, "Showcase_Titanium",
                new Vector3(0.115f, 0.018f, 0.0f), Quaternion.Euler(25f, 0f, 0f),
                matTitanium, matBezelDiver, matDialNavy, matHandsIceBlue, matHandsIceBlue, matStrapNavy, matLume, matGlass,
                isDiver: true, presetIndex: 2, catalogMgr: catalogMgr);
        }

        private static void BuildDetailedShowcaseWatch(
            Transform parent, string name, Vector3 pos, Quaternion rot,
            Material caseMat, Material bezelMat, Material dialMat, Material handsMat, Material chronoMat, Material strapMat, Material lumeMat, Material glassMat,
            bool isDiver, int presetIndex, WatchCatalogManager catalogMgr)
        {
            GameObject stand = new GameObject(name);
            stand.transform.SetParent(parent, false);
            stand.transform.localPosition = pos;
            stand.transform.localRotation = rot;

            Action<GameObject, Material> setupMesh = (go, mat) => {
                Collider c = go.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null && mat != null) mr.sharedMaterial = mat;
            };

            // Display Cushion Plinth
            GameObject cushion = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cushion.name = "Cushion";
            cushion.transform.SetParent(stand.transform, false);
            cushion.transform.localPosition = new Vector3(0f, -0.005f, 0f);
            cushion.transform.localScale = new Vector3(0.055f, 0.010f, 0.055f);
            setupMesh(cushion, strapMat);

            // --- Watch Head ---
            GameObject watchHead = new GameObject("WatchHead");
            watchHead.transform.SetParent(stand.transform, false);
            watchHead.transform.localPosition = new Vector3(0f, 0.012f, 0f);

            // 1. Case Body (beveled cylinder)
            GameObject caseBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            caseBody.name = "CaseBody";
            caseBody.transform.SetParent(watchHead.transform, false);
            caseBody.transform.localPosition = Vector3.zero;
            caseBody.transform.localScale = new Vector3(0.042f, 0.0055f, 0.042f);
            setupMesh(caseBody, caseMat);

            // 2. Stepped Bezel
            GameObject bezel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bezel.name = "Bezel";
            bezel.transform.SetParent(watchHead.transform, false);
            bezel.transform.localPosition = new Vector3(0f, 0.0058f, 0f);
            bezel.transform.localScale = new Vector3(0.040f, 0.0012f, 0.040f);
            setupMesh(bezel, bezelMat);

            // Fluted notches if Diver
            if (isDiver)
            {
                for (int i = 0; i < 12; i++)
                {
                    float angle = i * 30f * Mathf.Deg2Rad;
                    GameObject notch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    notch.name = $"Notch_{i}";
                    notch.transform.SetParent(bezel.transform, false);
                    notch.transform.localPosition = new Vector3(Mathf.Sin(angle) * 0.49f, 0.4f, Mathf.Cos(angle) * 0.49f);
                    notch.transform.localRotation = Quaternion.Euler(0f, i * 30f, 0f);
                    notch.transform.localScale = new Vector3(0.05f, 0.4f, 0.05f);
                    setupMesh(notch, caseMat);
                }
            }

            // 3. Dial Face Disc
            GameObject dial = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dial.name = "DialFace";
            dial.transform.SetParent(watchHead.transform, false);
            dial.transform.localPosition = new Vector3(0f, 0.0065f, 0f);
            dial.transform.localScale = new Vector3(0.034f, 0.0006f, 0.034f);
            setupMesh(dial, dialMat);

            // 4. 12 Hour Markers
            for (int h = 0; h < 12; h++)
            {
                float angle = h * 30f * Mathf.Deg2Rad;
                float r = 0.0135f;
                Vector3 markerPos = new Vector3(Mathf.Sin(angle) * r, 0.0072f, Mathf.Cos(angle) * r);
                Quaternion markerRot = Quaternion.Euler(0f, h * 30f, 0f);

                if (h == 0) // 12 o'clock dual batons
                {
                    GameObject mLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    mLeft.name = "Marker_12A";
                    mLeft.transform.SetParent(watchHead.transform, false);
                    mLeft.transform.localPosition = markerPos + new Vector3(-0.0008f, 0f, 0f);
                    mLeft.transform.localRotation = markerRot;
                    mLeft.transform.localScale = new Vector3(0.0008f, 0.0006f, 0.0035f);
                    setupMesh(mLeft, lumeMat);

                    GameObject mRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    mRight.name = "Marker_12B";
                    mRight.transform.SetParent(watchHead.transform, false);
                    mRight.transform.localPosition = markerPos + new Vector3(0.0008f, 0f, 0f);
                    mRight.transform.localRotation = markerRot;
                    mRight.transform.localScale = new Vector3(0.0008f, 0.0006f, 0.0035f);
                    setupMesh(mRight, lumeMat);
                }
                else
                {
                    GameObject m = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    m.name = $"Marker_{h}";
                    m.transform.SetParent(watchHead.transform, false);
                    m.transform.localPosition = markerPos;
                    m.transform.localRotation = markerRot;
                    bool isMajor = (h == 3 || h == 6 || h == 9);
                    m.transform.localScale = isMajor ? new Vector3(0.0012f, 0.0006f, 0.0032f) : new Vector3(0.0008f, 0.0005f, 0.0022f);
                    setupMesh(m, isMajor ? lumeMat : handsMat);
                }
            }

            // 5. Hands (Hour: 12mm, Minute: 18mm, Seconds Needle, Center Pin)
            // Hour hand pointing toward 10:10
            GameObject hourHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hourHand.name = "HourHand";
            hourHand.transform.SetParent(watchHead.transform, false);
            hourHand.transform.localRotation = Quaternion.Euler(0f, -60f, 0f);
            hourHand.transform.localPosition = hourHand.transform.localRotation * new Vector3(0f, 0.0076f, 0.0055f);
            hourHand.transform.localScale = new Vector3(0.0015f, 0.0004f, 0.011f);
            setupMesh(hourHand, handsMat);

            // Minute hand pointing toward 10:10 (2 o'clock)
            GameObject minHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            minHand.name = "MinuteHand";
            minHand.transform.SetParent(watchHead.transform, false);
            minHand.transform.localRotation = Quaternion.Euler(0f, 60f, 0f);
            minHand.transform.localPosition = minHand.transform.localRotation * new Vector3(0f, 0.0080f, 0.0080f);
            minHand.transform.localScale = new Vector3(0.0012f, 0.0004f, 0.016f);
            setupMesh(minHand, handsMat);

            // Seconds sweep needle
            GameObject secHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            secHand.name = "SecondHand";
            secHand.transform.SetParent(watchHead.transform, false);
            secHand.transform.localRotation = Quaternion.Euler(0f, 190f, 0f);
            secHand.transform.localPosition = secHand.transform.localRotation * new Vector3(0f, 0.0084f, 0.0085f);
            secHand.transform.localScale = new Vector3(0.0004f, 0.0003f, 0.017f);
            setupMesh(secHand, chronoMat);

            // Center Pin
            GameObject pin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pin.name = "CenterPin";
            pin.transform.SetParent(watchHead.transform, false);
            pin.transform.localPosition = new Vector3(0f, 0.0087f, 0f);
            pin.transform.localScale = new Vector3(0.0025f, 0.0006f, 0.0025f);
            setupMesh(pin, caseMat);

            // 6. Sapphire Crystal
            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glass.name = "SapphireCrystal";
            glass.transform.SetParent(watchHead.transform, false);
            glass.transform.localPosition = new Vector3(0f, 0.0092f, 0f);
            glass.transform.localScale = new Vector3(0.038f, 0.0005f, 0.038f);
            setupMesh(glass, glassMat);

            // 7. Lugs, Crown, Pushers
            // 4 Lugs
            float lugX = 0.014f;
            float lugZ = 0.018f;
            for (int li = 0; li < 4; li++)
            {
                float signX = (li % 2 == 0) ? -1f : 1f;
                float signZ = (li < 2) ? 1f : -1f;
                GameObject lug = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lug.name = $"Lug_{li}";
                lug.transform.SetParent(watchHead.transform, false);
                lug.transform.localPosition = new Vector3(signX * lugX, -0.001f, signZ * lugZ);
                lug.transform.localRotation = Quaternion.Euler(signZ * 15f, 0f, 0f);
                lug.transform.localScale = new Vector3(0.0035f, 0.005f, 0.008f);
                setupMesh(lug, caseMat);
            }

            // Crown at 3 o'clock
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crown.name = "Crown";
            crown.transform.SetParent(watchHead.transform, false);
            crown.transform.localPosition = new Vector3(0.0225f, 0f, 0f);
            crown.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            crown.transform.localScale = new Vector3(0.0045f, 0.0025f, 0.0045f);
            setupMesh(crown, caseMat);

            // Pushers at 2 and 4 o'clock
            GameObject p2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p2.name = "Pusher_Top";
            p2.transform.SetParent(watchHead.transform, false);
            p2.transform.localPosition = new Vector3(0.020f, 0f, 0.010f);
            p2.transform.localRotation = Quaternion.Euler(0f, -30f, 90f);
            p2.transform.localScale = new Vector3(0.0035f, 0.0025f, 0.0035f);
            setupMesh(p2, caseMat);

            GameObject p4 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p4.name = "Pusher_Bottom";
            p4.transform.SetParent(watchHead.transform, false);
            p4.transform.localPosition = new Vector3(0.020f, 0f, -0.010f);
            p4.transform.localRotation = Quaternion.Euler(0f, 30f, 90f);
            p4.transform.localScale = new Vector3(0.0035f, 0.0025f, 0.0035f);
            setupMesh(p4, caseMat);

            // 8. Fitted Curving Straps wrapping over cushion
            // Upper strap segment
            GameObject sUpper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sUpper.name = "Strap_Upper";
            sUpper.transform.SetParent(watchHead.transform, false);
            sUpper.transform.localPosition = new Vector3(0f, -0.007f, 0.024f);
            sUpper.transform.localRotation = Quaternion.Euler(40f, 0f, 0f);
            sUpper.transform.localScale = new Vector3(0.020f, 0.003f, 0.022f);
            setupMesh(sUpper, strapMat);

            // Lower strap segment
            GameObject sLower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sLower.name = "Strap_Lower";
            sLower.transform.SetParent(watchHead.transform, false);
            sLower.transform.localPosition = new Vector3(0f, -0.007f, -0.024f);
            sLower.transform.localRotation = Quaternion.Euler(-40f, 0f, 0f);
            sLower.transform.localScale = new Vector3(0.020f, 0.003f, 0.022f);
            setupMesh(sLower, strapMat);

            // Setup Meta XR Poke & Touch interaction to Equip this Preset on poke
            int targetIdx = presetIndex;
            ConfigurePokeButton(watchHead, watchHead.transform, Vector3.up, 0.005f, () => catalogMgr.EquipPreset(targetIdx));
        }

        private static void SetupFloatingWristMenu(GameObject watch, Material matGlass, Material matBrass, Material matLume, LoungeModeManager modeMgr)
        {
            Transform existing = watch.transform.Find("Btn_Wrist_MR_Toggle");
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

            // Floating wrist ribbon pill 5cm above the watch strap
            GameObject ribbon = new GameObject("Btn_Wrist_MR_Toggle");
            ribbon.transform.SetParent(watch.transform, false);
            ribbon.transform.localPosition = new Vector3(0f, 0.048f, 0f);
            ribbon.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

            Action<GameObject, Material> setupMesh = (go, mat) => {
                Collider c = go.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null && mat != null) mr.sharedMaterial = mat;
            };

            // Glassmorphic housing pill
            GameObject pill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pill.name = "Pill_Housing";
            pill.transform.SetParent(ribbon.transform, false);
            pill.transform.localPosition = Vector3.zero;
            pill.transform.localScale = new Vector3(0.046f, 0.014f, 0.008f);
            setupMesh(pill, matGlass);

            // Raised Movable Button Cap
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "ButtonCap";
            cap.transform.SetParent(ribbon.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0f, 0.004f);
            cap.transform.localScale = new Vector3(0.040f, 0.010f, 0.004f);
            setupMesh(cap, matLume);

            // Text Label
            GameObject label = new GameObject("Label");
            label.transform.SetParent(ribbon.transform, false);
            label.transform.localPosition = new Vector3(0f, 0f, 0.007f);
            var tm = label.AddComponent<TextMesh>();
            tm.text = "VR / MR";
            tm.fontSize = 24;
            tm.characterSize = 0.0012f;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(0.05f, 0.15f, 0.20f, 1f);

            // Configure Poke Interaction
            ConfigurePokeButton(ribbon, cap.transform, Vector3.forward, 0.005f, () => modeMgr.ToggleMode());
        }

        public static void ConfigurePokeButton(
            GameObject buttonObj, Transform capTransform, Vector3 normalDir, float pressDepth,
            UnityEngine.Events.UnityAction onPushedAction)
        {
            // 1. BoxCollider for touch/trigger detection
            var col = buttonObj.GetComponent<BoxCollider>();
            if (col == null) col = buttonObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(0.045f, 0.045f, 0.030f);

            // 2. Child Surface GameObject with PlaneSurface and ClippedPlaneSurface
            Transform surfChild = buttonObj.transform.Find("Surface");
            GameObject surfGo;
            if (surfChild == null)
            {
                surfGo = new GameObject("Surface");
                surfGo.transform.SetParent(buttonObj.transform, false);
                surfGo.transform.localPosition = Vector3.zero;
                surfGo.transform.localRotation = Quaternion.identity;
            }
            else
            {
                surfGo = surfChild.gameObject;
            }

            var plane = surfGo.GetComponent<PlaneSurface>();
            if (plane == null) plane = surfGo.AddComponent<PlaneSurface>();

            var clipped = surfGo.GetComponent<ClippedPlaneSurface>();
            if (clipped == null) clipped = surfGo.AddComponent<ClippedPlaneSurface>();

            var clipper = surfGo.GetComponent<BoundsClipper>();
            if (clipper == null) clipper = surfGo.AddComponent<BoundsClipper>();

            // Set plane on clipped
            var planeField = typeof(ClippedPlaneSurface).GetField("_planeSurface", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (planeField != null) planeField.SetValue(clipped, plane);

            // Set clippers on clipped
            var clippersField = typeof(ClippedPlaneSurface).GetField("_clippers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (clippersField != null)
            {
                var list = (IList)clippersField.GetValue(clipped);
                if (!list.Contains(clipper)) list.Add(clipper);
            }

            // 3. PokeInteractable
            var poke = buttonObj.GetComponent<PokeInteractable>();
            if (poke == null) poke = buttonObj.AddComponent<PokeInteractable>();

            var surfField = typeof(PokeInteractable).GetField("_surfacePatch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (surfField != null) surfField.SetValue(poke, clipped);

            var dragField = typeof(PokeInteractable).GetField("_dragThresholds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (dragField != null)
            {
                var dragConfig = dragField.GetValue(poke);
                var enabledField = dragConfig.GetType().GetField("Enabled");
                var normField = dragConfig.GetType().GetField("DragNormal");
                if (enabledField != null) enabledField.SetValue(dragConfig, true);
                if (normField != null) normField.SetValue(dragConfig, pressDepth);
                dragField.SetValue(poke, dragConfig);
            }

            // 4. InteractableUnityEventWrapper
            var wrapper = buttonObj.GetComponent<InteractableUnityEventWrapper>();
            if (wrapper == null) wrapper = buttonObj.AddComponent<InteractableUnityEventWrapper>();

            var intField = typeof(InteractableUnityEventWrapper).GetField("_interactableView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (intField != null) intField.SetValue(wrapper, poke);

            var selectField = typeof(InteractableUnityEventWrapper).GetField("_whenSelect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (selectField != null)
            {
                var evt = selectField.GetValue(wrapper) as UnityEngine.Events.UnityEvent;
                if (evt == null)
                {
                    evt = new UnityEngine.Events.UnityEvent();
                    selectField.SetValue(wrapper, evt);
                }
                if (onPushedAction != null)
                {
                    evt.AddListener(onPushedAction);
                }
            }

            // 5. PokeButtonFeedback for visual depression, spring return, audio click
            var feedback = buttonObj.GetComponent<PokeButtonFeedback>();
            if (feedback == null) feedback = buttonObj.AddComponent<PokeButtonFeedback>();

            var capField = typeof(PokeButtonFeedback).GetField("movableCap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (capField != null) capField.SetValue(feedback, capTransform != null ? capTransform : buttonObj.transform);

            var dirField = typeof(PokeButtonFeedback).GetField("localPressDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (dirField != null) dirField.SetValue(feedback, -normalDir.normalized);

            var depthField = typeof(PokeButtonFeedback).GetField("pressDepth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (depthField != null) depthField.SetValue(feedback, pressDepth);

            if (onPushedAction != null)
            {
                feedback.AddListener(onPushedAction);
            }
        }

        public static void ConfigureHandTrackingPokeRig()
        {
            GameObject camRig = GameObject.Find("[BuildingBlock] Camera Rig");
            if (camRig == null) return;

            var pokeRig = camRig.GetComponent<HandFingertipPokeRig>();
            if (pokeRig == null) pokeRig = camRig.AddComponent<HandFingertipPokeRig>();

            pokeRig.SetupHandRigs();
        }
    }
}
