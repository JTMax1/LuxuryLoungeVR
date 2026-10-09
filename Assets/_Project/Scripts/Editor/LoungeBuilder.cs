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
            BuildEnvironmentLounge();
            BuildAvatarArmRigAndStabilizer();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[LoungeBuilder] Collector Lounge and Stabilized Arm Rig built and scene saved.");
        }

        public static GameObject BuildEnvironmentLounge()
        {
            // 1. Remove existing environment if present
            GameObject existing = GameObject.Find("[Environment_Lounge]");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            GameObject loungeRoot = new GameObject("[Environment_Lounge]");
            Undo.RegisterCreatedObjectUndo(loungeRoot, "Create Environment Lounge");
            loungeRoot.transform.position = Vector3.zero;
            loungeRoot.transform.rotation = Quaternion.identity;

            // Load Materials
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

            // Display Pedestal (Center: 0.90m in front of player origin)
            GameObject pedestal = new GameObject("DisplayPedestal");
            pedestal.transform.SetParent(loungeRoot.transform, false);
            pedestal.transform.localPosition = new Vector3(0f, 0f, 0.90f);

            // Column: Cylinder, height 0.92m, diameter 0.48m
            GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            column.name = "Pedestal_Column";
            column.transform.SetParent(pedestal.transform, false);
            column.transform.localPosition = new Vector3(0f, 0.46f, 0f);
            column.transform.localScale = new Vector3(0.48f, 0.46f, 0.48f);
            Setup(column, matPedestal);

            // Top Bevel / Rim: height 0.03m, diameter 0.52m -> Top sits at Y = 0.95m
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Pedestal_Rim";
            rim.transform.SetParent(pedestal.transform, false);
            rim.transform.localPosition = new Vector3(0f, 0.935f, 0f);
            rim.transform.localScale = new Vector3(0.52f, 0.015f, 0.52f);
            Setup(rim, matPedestal);

            // Velvet Display Pad: diameter 0.40m, height 0.01m -> sits atop pedestal
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "Pedestal_Pad";
            pad.transform.SetParent(pedestal.transform, false);
            pad.transform.localPosition = new Vector3(0f, 0.955f, 0f);
            pad.transform.localScale = new Vector3(0.40f, 0.005f, 0.40f);
            Setup(pad, matPad);

            // Configure Directional Key Light
            GameObject dirLightGo = GameObject.Find("Directional Light");
            if (dirLightGo != null)
            {
                Light dirLight = dirLightGo.GetComponent<Light>();
                if (dirLight != null)
                {
                    dirLightGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                    dirLight.color = new Color(1.0f, 0.96f, 0.90f, 1.0f); // Warm luxury tone
                    dirLight.intensity = 1.1f;
                    dirLight.shadows = LightShadows.Soft;
                }
            }

            // Pedestal Spotlight (Directly overhead)
            GameObject spotGo = GameObject.Find("Spotlight_Pedestal");
            if (spotGo == null)
            {
                spotGo = new GameObject("Spotlight_Pedestal");
            }
            spotGo.transform.SetParent(loungeRoot.transform, false);
            spotGo.transform.localPosition = new Vector3(0f, 2.85f, 0.90f);
            spotGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Points straight down

            Light spot = spotGo.GetComponent<Light>();
            if (spot == null) spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 3.5f;
            spot.spotAngle = 38f;
            spot.innerSpotAngle = 20f;
            spot.color = new Color(1.0f, 0.98f, 0.96f, 1.0f);
            spot.intensity = 3.2f;
            spot.shadows = LightShadows.Soft;

            return loungeRoot;
        }

        public static void BuildAvatarArmRigAndStabilizer()
        {
            GameObject watchRoot = GameObject.Find("[WristWatch_Prototype]");
            if (watchRoot == null)
            {
                Debug.LogWarning("[LoungeBuilder] [WristWatch_Prototype] not found, invoking WatchBuilder first.");
                watchRoot = WatchBuilder.BuildWatch();
            }

            // Load Materials
            Material matSleeve = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_SuitSleeve.mat");
            Material matShirt = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_ShirtCuff.mat");
            Material matSkin = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_AvatarSkin.mat");

            void Clean(GameObject go, Material m)
            {
                Collider c = go.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null && m != null) mr.sharedMaterial = m;
            }

            // Remove existing arm rig if present
            Transform existingArm = watchRoot.transform.Find("AvatarArm_Rig");
            if (existingArm != null)
            {
                Undo.DestroyObjectImmediate(existingArm.gameObject);
            }

            // Avatar Arm Container: offset down by standard wrist thickness (~2.3cm)
            GameObject armRoot = new GameObject("AvatarArm_Rig");
            armRoot.transform.SetParent(watchRoot.transform, false);
            armRoot.transform.localPosition = new Vector3(0f, -0.023f, 0f);
            armRoot.transform.localRotation = Quaternion.identity;
            armRoot.transform.localScale = Vector3.one;

            // 1. Forearm Suit Sleeve (cylinder aligned along Z axis towards -Z)
            GameObject forearmSleeve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            forearmSleeve.name = "Forearm_Sleeve";
            forearmSleeve.transform.SetParent(armRoot.transform, false);
            forearmSleeve.transform.localPosition = new Vector3(0f, 0f, -0.15f);
            forearmSleeve.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            forearmSleeve.transform.localScale = new Vector3(0.075f, 0.11f, 0.075f); // 22cm long, 7.5cm wide
            Clean(forearmSleeve, matSleeve);

            // 2. Suit Jacket Cuff (ring right behind the wrist)
            GameObject jacketCuff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            jacketCuff.name = "Jacket_Cuff";
            jacketCuff.transform.SetParent(armRoot.transform, false);
            jacketCuff.transform.localPosition = new Vector3(0f, 0f, -0.034f);
            jacketCuff.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            jacketCuff.transform.localScale = new Vector3(0.078f, 0.010f, 0.078f);
            Clean(jacketCuff, matSleeve);

            // 3. Shirt Cuff (crisp white cuff peeking 6mm out)
            GameObject shirtCuff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shirtCuff.name = "Shirt_Cuff";
            shirtCuff.transform.SetParent(armRoot.transform, false);
            shirtCuff.transform.localPosition = new Vector3(0f, 0f, -0.020f);
            shirtCuff.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shirtCuff.transform.localScale = new Vector3(0.070f, 0.005f, 0.070f);
            Clean(shirtCuff, matShirt);

            // 4. Wrist Skin (under watch case and strap)
            GameObject wristSkin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wristSkin.name = "Wrist_Skin";
            wristSkin.transform.SetParent(armRoot.transform, false);
            wristSkin.transform.localPosition = new Vector3(0f, 0f, 0.002f);
            wristSkin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            wristSkin.transform.localScale = new Vector3(0.065f, 0.018f, 0.065f);
            Clean(wristSkin, matSkin);

            // 5. Hand Palm Stub (extending forward along +Z towards fingers)
            GameObject palm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palm.name = "Hand_Palm";
            palm.transform.SetParent(armRoot.transform, false);
            palm.transform.localPosition = new Vector3(0f, -0.003f, 0.055f);
            palm.transform.localRotation = Quaternion.identity;
            palm.transform.localScale = new Vector3(0.072f, 0.024f, 0.065f);
            Clean(palm, matSkin);

            // Configure WristTwistStabilizer on watchRoot
            WristTwistStabilizer stabilizer = watchRoot.GetComponent<WristTwistStabilizer>();
            if (stabilizer == null)
            {
                stabilizer = watchRoot.AddComponent<WristTwistStabilizer>();
            }

            // Disable or keep WristAnchorFollower as dormant backup
            WristAnchorFollower oldFollower = watchRoot.GetComponent<WristAnchorFollower>();
            if (oldFollower != null)
            {
                oldFollower.FollowActive = false;
            }

            GameObject leftAnchor = GameObject.Find("LeftHandAnchor");
            if (leftAnchor != null)
            {
                stabilizer.TargetWristJoint = leftAnchor.transform;
            }

            SerializedObject soStabilizer = new SerializedObject(stabilizer);
            if (leftAnchor != null)
            {
                soStabilizer.FindProperty("targetWristJoint").objectReferenceValue = leftAnchor.transform;
            }
            soStabilizer.FindProperty("twistInfluence").floatValue = 0.45f;
            soStabilizer.FindProperty("localTwistAxis").vector3Value = Vector3.forward;
            soStabilizer.FindProperty("offsetPosition").vector3Value = new Vector3(0f, 0.025f, -0.04f);
            soStabilizer.FindProperty("offsetRotation").vector3Value = new Vector3(0f, -90f, 0f);
            soStabilizer.FindProperty("activeFollow").boolValue = true;
            soStabilizer.FindProperty("smoothingSpeed").floatValue = 35f;
            soStabilizer.ApplyModifiedProperties();

            // Save updated Prefab
            string prefabPath = "Assets/_Project/Prefabs/WristWatch_Prototype.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(watchRoot, prefabPath, InteractionMode.AutomatedAction);
            Debug.Log($"[LoungeBuilder] Updated watch prefab with avatar arm and stabilizer at '{prefabPath}'.");
        }
    }
}
