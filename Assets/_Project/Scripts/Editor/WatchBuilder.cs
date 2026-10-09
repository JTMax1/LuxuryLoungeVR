using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using VRWatch.Core;
using VRWatch.Interaction;

namespace VRWatch.Editor
{
    public static class WatchBuilder
    {
        [MenuItem("VRWatch/Assemble Watch Rig", false, 10)]
        public static GameObject BuildWatch()
        {
            // 1. Remove existing instance in active scene if present
            GameObject existing = GameObject.Find("[WristWatch_Prototype]");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            // 2. Load Materials
            Material matMetal = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchMetal.mat");
            Material matFace = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchFace.mat");
            Material matHands = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchHands.mat");
            Material matStrap = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WatchStrap.mat");

            // 3. Create Root GameObject
            GameObject root = new GameObject("[WristWatch_Prototype]");
            Undo.RegisterCreatedObjectUndo(root, "Create WristWatch Prototype");
            root.transform.position = new Vector3(0f, 0.1f, 0.35f); // Placed comfortably in front of user rig
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            // Helper to clean collider and assign material
            void SetupPrimitive(GameObject go, Material mat)
            {
                Collider c = go.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null && mat != null) mr.sharedMaterial = mat;
            }

            // 4. Watch Body (Cylinder: flattened along Y, ~42mm diameter, ~10mm total case thickness)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "WatchBody";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(0.042f, 0.005f, 0.042f);
            SetupPrimitive(body, matMetal);

            // Watch Crown (Winding knob at 3 o'clock / +X)
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crown.name = "WatchCrown";
            crown.transform.SetParent(root.transform, false);
            crown.transform.localPosition = new Vector3(0.022f, 0f, 0f);
            crown.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            crown.transform.localScale = new Vector3(0.004f, 0.002f, 0.004f);
            SetupPrimitive(crown, matMetal);

            // 4 Lugs
            Vector3[] lugPositions = new Vector3[]
            {
                new Vector3(-0.013f, 0f, 0.021f),
                new Vector3(0.013f, 0f, 0.021f),
                new Vector3(-0.013f, 0f, -0.021f),
                new Vector3(0.013f, 0f, -0.021f)
            };
            for (int i = 0; i < lugPositions.Length; i++)
            {
                GameObject lug = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lug.name = $"Lug_{i}";
                lug.transform.SetParent(root.transform, false);
                lug.transform.localPosition = lugPositions[i];
                lug.transform.localRotation = Quaternion.identity;
                lug.transform.localScale = new Vector3(0.0025f, 0.006f, 0.008f);
                SetupPrimitive(lug, matMetal);
            }

            // Watch Straps
            GameObject strapUpper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strapUpper.name = "WatchStrap_Upper";
            strapUpper.transform.SetParent(root.transform, false);
            strapUpper.transform.localPosition = new Vector3(0f, -0.001f, 0.038f);
            strapUpper.transform.localRotation = Quaternion.identity;
            strapUpper.transform.localScale = new Vector3(0.021f, 0.0025f, 0.035f);
            SetupPrimitive(strapUpper, matStrap);

            GameObject strapLower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strapLower.name = "WatchStrap_Lower";
            strapLower.transform.SetParent(root.transform, false);
            strapLower.transform.localPosition = new Vector3(0f, -0.001f, -0.038f);
            strapLower.transform.localRotation = Quaternion.identity;
            strapLower.transform.localScale = new Vector3(0.021f, 0.0025f, 0.035f);
            SetupPrimitive(strapLower, matStrap);

            // 5. Watch Face Root (Unscaled so children have clean, non-skewed local coordinates)
            GameObject watchFace = new GameObject("WatchFace");
            watchFace.transform.SetParent(root.transform, false);
            watchFace.transform.localPosition = new Vector3(0f, 0.0051f, 0f);
            watchFace.transform.localRotation = Quaternion.identity;
            watchFace.transform.localScale = Vector3.one;

            // Dial Disc
            GameObject dialMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dialMesh.name = "DialMesh";
            dialMesh.transform.SetParent(watchFace.transform, false);
            dialMesh.transform.localPosition = Vector3.zero;
            dialMesh.transform.localRotation = Quaternion.identity;
            dialMesh.transform.localScale = new Vector3(0.038f, 0.0002f, 0.038f);
            SetupPrimitive(dialMesh, matFace);

            // Dial Hour Markers (12 ticks around perimeter at radius ~16mm)
            for (int h = 0; h < 12; h++)
            {
                float angle = h * 30f;
                float rad = angle * Mathf.Deg2Rad;
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"Marker_{h}";
                marker.transform.SetParent(watchFace.transform, false);
                marker.transform.localPosition = new Vector3(Mathf.Sin(rad) * 0.016f, 0.0003f, Mathf.Cos(rad) * 0.016f);
                marker.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
                bool isMajor = (h % 3 == 0);
                marker.transform.localScale = isMajor
                    ? new Vector3(0.0012f, 0.0003f, 0.003f)
                    : new Vector3(0.0007f, 0.0002f, 0.0018f);
                SetupPrimitive(marker, matHands);
            }

            // 6. Clock Hand Pivots under WatchFace
            // Hour Pivot (Base centered at 0, 0, 0; hand visual offset along +Z)
            GameObject hourPivot = new GameObject("HourPivot");
            hourPivot.transform.SetParent(watchFace.transform, false);
            hourPivot.transform.localPosition = new Vector3(0f, 0.0004f, 0f);
            hourPivot.transform.localRotation = Quaternion.identity;
            hourPivot.transform.localScale = Vector3.one;

            GameObject hourMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hourMesh.name = "HourHand_Mesh";
            hourMesh.transform.SetParent(hourPivot.transform, false);
            hourMesh.transform.localPosition = new Vector3(0f, 0f, 0.0055f);
            hourMesh.transform.localRotation = Quaternion.identity;
            hourMesh.transform.localScale = new Vector3(0.0018f, 0.0004f, 0.011f);
            SetupPrimitive(hourMesh, matHands);

            // Minute Pivot
            GameObject minutePivot = new GameObject("MinutePivot");
            minutePivot.transform.SetParent(watchFace.transform, false);
            minutePivot.transform.localPosition = new Vector3(0f, 0.0007f, 0f);
            minutePivot.transform.localRotation = Quaternion.identity;
            minutePivot.transform.localScale = Vector3.one;

            GameObject minuteMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            minuteMesh.name = "MinuteHand_Mesh";
            minuteMesh.transform.SetParent(minutePivot.transform, false);
            minuteMesh.transform.localPosition = new Vector3(0f, 0f, 0.0075f);
            minuteMesh.transform.localRotation = Quaternion.identity;
            minuteMesh.transform.localScale = new Vector3(0.0012f, 0.0004f, 0.015f);
            SetupPrimitive(minuteMesh, matHands);

            // Second Pivot
            GameObject secondPivot = new GameObject("SecondPivot");
            secondPivot.transform.SetParent(watchFace.transform, false);
            secondPivot.transform.localPosition = new Vector3(0f, 0.0010f, 0f);
            secondPivot.transform.localRotation = Quaternion.identity;
            secondPivot.transform.localScale = Vector3.one;

            GameObject secondMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            secondMesh.name = "SecondHand_Mesh";
            secondMesh.transform.SetParent(secondPivot.transform, false);
            secondMesh.transform.localPosition = new Vector3(0f, 0f, 0.0075f);
            secondMesh.transform.localRotation = Quaternion.identity;
            secondMesh.transform.localScale = new Vector3(0.0006f, 0.0003f, 0.018f);
            SetupPrimitive(secondMesh, matHands);

            // Center Pin
            GameObject centerPin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            centerPin.name = "CenterPin";
            centerPin.transform.SetParent(watchFace.transform, false);
            centerPin.transform.localPosition = new Vector3(0f, 0.0012f, 0f);
            centerPin.transform.localRotation = Quaternion.identity;
            centerPin.transform.localScale = new Vector3(0.0025f, 0.0005f, 0.0025f);
            SetupPrimitive(centerPin, matMetal);

            // 7. Attach & Bind AnalogClockController
            AnalogClockController clock = root.AddComponent<AnalogClockController>();
            clock.SetHands(hourPivot.transform, minutePivot.transform, secondPivot.transform);
            clock.SmoothSecondHand = true;

            SerializedObject soClock = new SerializedObject(clock);
            soClock.FindProperty("hourHand").objectReferenceValue = hourPivot.transform;
            soClock.FindProperty("minuteHand").objectReferenceValue = minutePivot.transform;
            soClock.FindProperty("secondHand").objectReferenceValue = secondPivot.transform;
            soClock.FindProperty("smoothSecondHand").boolValue = true;
            soClock.FindProperty("rotationAxis").vector3Value = Vector3.up;
            soClock.ApplyModifiedProperties();

            // 8. Attach & Bind WristAnchorFollower
            WristAnchorFollower follower = root.AddComponent<WristAnchorFollower>();
            GameObject leftAnchor = GameObject.Find("LeftHandAnchor");
            if (leftAnchor != null)
            {
                follower.TargetWristBone = leftAnchor.transform;
            }

            SerializedObject soFollower = new SerializedObject(follower);
            if (leftAnchor != null)
            {
                soFollower.FindProperty("targetWristBone").objectReferenceValue = leftAnchor.transform;
            }
            soFollower.FindProperty("offsetPosition").vector3Value = new Vector3(0f, 0.02f, -0.05f);
            soFollower.FindProperty("offsetRotation").vector3Value = Vector3.zero;
            soFollower.FindProperty("followActive").boolValue = true;
            soFollower.FindProperty("autoFindLeftWrist").boolValue = true;
            soFollower.ApplyModifiedProperties();

            // 9. Save as Prefab
            string prefabPath = "Assets/_Project/Prefabs/WristWatch_Prototype.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
            Debug.Log($"[{nameof(WatchBuilder)}] Successfully assembled watch rig and saved prefab at '{prefabPath}'.");

            // 10. Mark scene dirty and save
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            return root;
        }
    }
}
