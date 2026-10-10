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

            // --- Architectural & Luxury Lounge Materials (Dark, Rich, High-Contrast) ---
            // 1. Sophisticated Architectural Dark Charcoal Walls (#212327)
            createOrUpdateMat("Assets/_Project/Materials/M_LoungeWall.mat", new Color(0.130f, 0.138f, 0.152f, 1.0f), 0.04f, 0.22f, false);

            // 2. Rich Dark Walnut Wood Panels for Wainscoting (#2C1D16)
            createOrUpdateMat("Assets/_Project/Materials/M_LoungeWood.mat", new Color(0.172f, 0.114f, 0.086f, 1.0f), 0.02f, 0.45f, false);

            // 3. Brushed Architectural Bronze for Trims, Moldings & Baseboards (#50402C)
            createOrUpdateMat("Assets/_Project/Materials/M_LoungeTrim.mat", new Color(0.314f, 0.251f, 0.173f, 1.0f), 0.82f, 0.65f, false);

            // 4. Smoked Dark Herringbone Oak Hardwood Floor (#17191C)
            createOrUpdateMat("Assets/_Project/Materials/M_LoungeFloor.mat", new Color(0.090f, 0.098f, 0.110f, 1.0f), 0.08f, 0.55f, false);

            // 5. Nero Marquina Luxury Marble for Pedestal (#121316)
            createOrUpdateMat("Assets/_Project/Materials/M_PedestalMarble.mat", new Color(0.07f, 0.07f, 0.08f, 1f), 0.05f, 0.88f, false);

            // 6. Polished Satin Brass Accents for Pedestal & Buttons (#D4AF37)
            createOrUpdateMat("Assets/_Project/Materials/M_PedestalBrass.mat", new Color(0.83f, 0.68f, 0.22f, 1f), 0.92f, 0.80f, false);

            // 7. Micro-Suede Navy Velvet Display Pad (#10141D)
            createOrUpdateMat("Assets/_Project/Materials/M_PedestalPad.mat", new Color(0.06f, 0.08f, 0.12f, 1f), 0.02f, 0.18f, false);

            // Avatar & Glove
            createOrUpdateMat("Assets/_Project/Materials/M_Glove_Leather.mat", new Color(0.12f, 0.12f, 0.14f, 1.0f), 0.08f, 0.38f, false);
            createOrUpdateMat("Assets/_Project/Materials/M_Avatar_Skin.mat", new Color(0.85f, 0.68f, 0.58f, 1.0f), 0.0f, 0.28f, false);

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
            Material matWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_LoungeWood.mat");
            Material matTrim = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_LoungeTrim.mat");

            Action<GameObject, Material> setupMesh = (go, mat) => {
                Collider c = go.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null && mat != null) mr.sharedMaterial = mat;
            };

            // 1. Dark Smoked Oak Floor (10m x 10m)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(loungeRoot.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(10f, 0.1f, 10f);
            setupMesh(floor, matFloor);

            // Brass Perimeter Inlay on Floor around Pedestal
            GameObject floorInlay = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floorInlay.name = "Floor_BrassInlay";
            floorInlay.transform.SetParent(loungeRoot.transform, false);
            floorInlay.transform.localPosition = new Vector3(0f, 0.001f, 0.75f);
            floorInlay.transform.localScale = new Vector3(1.8f, 0.002f, 1.8f);
            setupMesh(floorInlay, matTrim);

            GameObject floorInlayInner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floorInlayInner.name = "Floor_InlayInner";
            floorInlayInner.transform.SetParent(loungeRoot.transform, false);
            floorInlayInner.transform.localPosition = new Vector3(0f, 0.002f, 0.75f);
            floorInlayInner.transform.localScale = new Vector3(1.72f, 0.002f, 1.72f);
            setupMesh(floorInlayInner, matFloor);

            // 2. Architectural Walls with Wainscoting, Baseboards & Crown Moldings
            float totalH = 3.5f;
            float wainscotH = 1.2f;
            float upperH = totalH - wainscotH;
            float roomHalf = 5f;

            Action<string, Vector3, Vector3, bool> buildArchitecturalWall = (wallName, pos, size, isZAligned) => {
                GameObject wallGroup = new GameObject(wallName);
                wallGroup.transform.SetParent(loungeRoot.transform, false);
                wallGroup.transform.localPosition = pos;

                // Baseboard (0.15m height, Dark Bronze Trim)
                GameObject baseboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseboard.name = "Baseboard";
                baseboard.transform.SetParent(wallGroup.transform, false);
                baseboard.transform.localPosition = new Vector3(0f, 0.075f, 0f);
                baseboard.transform.localScale = isZAligned ? new Vector3(size.x, 0.15f, 0.22f) : new Vector3(0.22f, 0.15f, size.z);
                setupMesh(baseboard, matTrim);

                // Lower Wainscot (1.2m height, Dark Walnut Wood)
                GameObject wainscot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wainscot.name = "Wainscot_Walnut";
                wainscot.transform.SetParent(wallGroup.transform, false);
                wainscot.transform.localPosition = new Vector3(0f, wainscotH * 0.5f, 0f);
                wainscot.transform.localScale = isZAligned ? new Vector3(size.x, wainscotH, 0.18f) : new Vector3(0.18f, wainscotH, size.z);
                setupMesh(wainscot, matWood);

                // Chair Rail Molded Border (Dark Bronze Trim)
                GameObject chairRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chairRail.name = "ChairRail";
                chairRail.transform.SetParent(wallGroup.transform, false);
                chairRail.transform.localPosition = new Vector3(0f, wainscotH + 0.025f, 0f);
                chairRail.transform.localScale = isZAligned ? new Vector3(size.x, 0.05f, 0.22f) : new Vector3(0.22f, 0.05f, size.z);
                setupMesh(chairRail, matTrim);

                // Upper Wall (2.3m height, Sophisticated Dark Charcoal Slate)
                GameObject upper = GameObject.CreatePrimitive(PrimitiveType.Cube);
                upper.name = "UpperWall_Charcoal";
                upper.transform.SetParent(wallGroup.transform, false);
                upper.transform.localPosition = new Vector3(0f, wainscotH + upperH * 0.5f, 0f);
                upper.transform.localScale = isZAligned ? new Vector3(size.x, upperH, 0.15f) : new Vector3(0.15f, upperH, size.z);
                setupMesh(upper, matWall);

                // Crown Molding along Ceiling
                GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crown.name = "CrownMolding";
                crown.transform.SetParent(wallGroup.transform, false);
                crown.transform.localPosition = new Vector3(0f, totalH - 0.06f, 0f);
                crown.transform.localScale = isZAligned ? new Vector3(size.x, 0.12f, 0.22f) : new Vector3(0.22f, 0.12f, size.z);
                setupMesh(crown, matTrim);
            };

            // North Wall (+Z)
            buildArchitecturalWall("Wall_North", new Vector3(0f, 0f, roomHalf), new Vector3(10f, 0f, 0f), true);

            // South Wall (-Z)
            buildArchitecturalWall("Wall_South", new Vector3(0f, 0f, -roomHalf), new Vector3(10f, 0f, 0f), true);

            // East Wall (+X)
            buildArchitecturalWall("Wall_East", new Vector3(roomHalf, 0f, 0f), new Vector3(0f, 0f, 10f), false);

            // West Wall (-X)
            buildArchitecturalWall("Wall_West", new Vector3(-roomHalf, 0f, 0f), new Vector3(0f, 0f, 10f), false);

            // 3. Four Corner Columns (Pilasters)
            float colOffset = 4.88f;
            Vector2[] cornerCoords = new Vector2[] {
                new Vector2(-colOffset, -colOffset), new Vector2(colOffset, -colOffset),
                new Vector2(-colOffset, colOffset), new Vector2(colOffset, colOffset)
            };
            for (int ci = 0; ci < cornerCoords.Length; ci++)
            {
                GameObject col = GameObject.CreatePrimitive(PrimitiveType.Cube);
                col.name = $"Pilaster_{ci}";
                col.transform.SetParent(loungeRoot.transform, false);
                col.transform.localPosition = new Vector3(cornerCoords[ci].x, totalH * 0.5f, cornerCoords[ci].y);
                col.transform.localScale = new Vector3(0.35f, totalH, 0.35f);
                setupMesh(col, matTrim);
            }

            // 4. Coffered Ceiling
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(loungeRoot.transform, false);
            ceiling.transform.localPosition = new Vector3(0f, totalH + 0.05f, 0f);
            ceiling.transform.localScale = new Vector3(10f, 0.1f, 10f);
            setupMesh(ceiling, matWall);

            // 5. Rich, Atmospheric Gallery Lighting (NO Blinding White Fog)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.20f, 0.19f, 0.18f, 1.0f) * 0.40f;

            // Directional Light: Warm gallery angle with soft intensity
            GameObject dirLightGo = GameObject.Find("Directional Light");
            if (dirLightGo != null)
            {
                Light dirLight = dirLightGo.GetComponent<Light>();
                if (dirLight != null)
                {
                    dirLight.color = new Color(1.0f, 0.94f, 0.88f);
                    dirLight.intensity = 0.85f;
                    dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                    dirLight.shadows = LightShadows.Soft;
                }
            }

            // Dedicated Spotlight over Display Pedestal (Dramatic Exhibition Lighting)
            GameObject spotGo = GameObject.Find("SpotLight_Pedestal");
            if (spotGo == null)
            {
                spotGo = new GameObject("SpotLight_Pedestal");
            }
            spotGo.transform.SetParent(loungeRoot.transform, false);
            spotGo.transform.localPosition = new Vector3(0f, 3.4f, 0.75f);
            spotGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Light spotLight = spotGo.GetComponent<Light>();
            if (spotLight == null) spotLight = spotGo.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.color = new Color(1.0f, 0.93f, 0.82f);
            spotLight.intensity = 2.2f;
            spotLight.range = 5.0f;
            spotLight.spotAngle = 55f;
            spotLight.shadows = LightShadows.Soft;

            return loungeRoot;
        }

        public static void BuildAvatarArmRigAndStabilizer()
        {
            GameObject trackingSpace = GameObject.Find("TrackingSpace");
            if (trackingSpace == null) return;

            Transform leftHandAnchor = trackingSpace.transform.Find("LeftHandAnchor");
            if (leftHandAnchor == null) return;

            // 1. Remove the artificial, detached rigid cylinder sleeve
            Transform oldSleeve = leftHandAnchor.Find("AvatarArm_Sleeve");
            if (oldSleeve != null) Undo.DestroyObjectImmediate(oldSleeve.gameObject);

            GameObject oldSleeveAny = GameObject.Find("AvatarArm_Sleeve");
            if (oldSleeveAny != null) Undo.DestroyObjectImmediate(oldSleeveAny);

            // 2. Position the WristWatch naturally and securely on the wrist
            GameObject watch = GameObject.Find("[WristWatch_Prototype]");
            if (watch == null) return;

            watch.transform.SetParent(leftHandAnchor, false);
            // Sits right on the dorsal (back) skin surface of the wrist
            watch.transform.localPosition = new Vector3(0.00f, 0.024f, -0.038f);
            // Rotated so 12 o'clock points along fingers (+Z) and Dial Normal points up (+Y)
            watch.transform.localRotation = Quaternion.identity;
            watch.transform.localScale = Vector3.one;

            // Remove obsolete followers/stabilizers
            var follower = watch.GetComponent<WristAnchorFollower>();
            if (follower != null) follower.enabled = false;
            var stabilizer = watch.GetComponent<WristTwistStabilizer>();
            if (stabilizer != null) stabilizer.enabled = false;

            // Attach dynamic WatchWristAligner to keep watch locked to wrist bone
            var aligner = watch.GetComponent<WatchWristAligner>();
            if (aligner == null) aligner = watch.AddComponent<WatchWristAligner>();

            // 3. Shape watch straps to curve downward around wrist curvature
            Transform sUpper = watch.transform.Find("WatchStrap_Upper");
            if (sUpper != null)
            {
                sUpper.localPosition = new Vector3(0f, -0.005f, 0.030f);
                sUpper.localRotation = Quaternion.Euler(32f, 0f, 0f);
                sUpper.localScale = new Vector3(0.020f, 0.003f, 0.024f);
            }

            Transform sLower = watch.transform.Find("WatchStrap_Lower");
            if (sLower != null)
            {
                sLower.localPosition = new Vector3(0f, -0.005f, -0.030f);
                sLower.localRotation = Quaternion.Euler(-32f, 0f, 0f);
                sLower.localScale = new Vector3(0.020f, 0.003f, 0.024f);
            }

            // Remove oversized floating white billboard on watch face
            Transform oldRibbon = watch.transform.Find("Btn_Wrist_MR_Toggle");
            if (oldRibbon != null) Undo.DestroyObjectImmediate(oldRibbon.gameObject);
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

            // 4. Update Active Wristwatch
            UpdateActiveWristwatchPrototype(watch, matGlass, matSteel, matLume, modeMgr);

            // 5. Setup Pedestal, Console, and Showcase Tray
            SetupPedestalConsoleAndTray(matMarble, matBrass, matPad, matSteel, matRoseGold, matTitanium, matBezelDiver, matDialSteel, matDialBlackHeritage, matDialNavy, matHandsIceBlue, matChronoRed, matStrapBlack, matStrapBrown, matStrapNavy, matLume, matGlass, modeMgr, lumeCtrl, catalogMgr);

            // 6. Configure Hand Tracking Poke Rig
            ConfigureHandTrackingPokeRig();

            // Save Prefab and Scene
            string prefabPath = "Assets/_Project/Prefabs/WristWatch_Prototype.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(watch, prefabPath, InteractionMode.AutomatedAction);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[LoungeBuilder] P1-P5 Full Remediation completed and saved.");
        }

        private static void UpdateActiveWristwatchPrototype(GameObject watch, Material matGlass, Material matMetal, Material matLume, LoungeModeManager modeMgr)
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

            // Upgrade Pushers with Meta XR Poke & Touch
            var chrono = watch.GetComponent<WatchChronographController>();
            Transform pusherStart = watch.transform.Find("Pusher_StartStop");
            if (pusherStart != null && chrono != null)
            {
                ConfigurePokeButton(pusherStart.gameObject, pusherStart, Vector3.up, 0.005f, PokeButtonFeedback.PokeButtonActionType.ChronographStartStop);
            }

            Transform pusherReset = watch.transform.Find("Pusher_Reset");
            if (pusherReset != null && chrono != null)
            {
                ConfigurePokeButton(pusherReset.gameObject, pusherReset, Vector3.up, 0.005f, PokeButtonFeedback.PokeButtonActionType.ChronographReset);
            }

            // Flush Tactile Case Pusher at 8 o'clock for VR/MR toggle (no giant floating billboard)
            Transform existingPusherMR = watch.transform.Find("Watch_Pusher_MR_Toggle");
            if (existingPusherMR != null) Undo.DestroyObjectImmediate(existingPusherMR.gameObject);

            GameObject pusherMR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pusherMR.name = "Watch_Pusher_MR_Toggle";
            pusherMR.transform.SetParent(watch.transform, false);
            pusherMR.transform.localPosition = new Vector3(-0.020f, 0f, -0.010f); // 8 o'clock on case rim
            pusherMR.transform.localRotation = Quaternion.Euler(0f, 150f, 90f);
            pusherMR.transform.localScale = new Vector3(0.0040f, 0.0030f, 0.0040f);
            var mrMR = pusherMR.GetComponent<MeshRenderer>();
            if (mrMR != null && matMetal != null) mrMR.sharedMaterial = matMetal;

            ConfigurePokeButton(pusherMR, pusherMR.transform, Vector3.up, 0.003f, PokeButtonFeedback.PokeButtonActionType.ToggleLoungeMode);
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
            // Remove any old pedestal
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

            // 1. Pedestal Base Ring (dia: 0.52m, Satin Brass)
            GameObject pBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pBase.name = "Pedestal_Base";
            pBase.transform.SetParent(pedestal.transform, false);
            pBase.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            pBase.transform.localScale = new Vector3(0.52f, 0.035f, 0.52f);
            setupMesh(pBase, matBrass);

            // 2. Pedestal Column (Nero Marquina Marble)
            GameObject pColumn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pColumn.name = "Pedestal_Column";
            pColumn.transform.SetParent(pedestal.transform, false);
            pColumn.transform.localPosition = new Vector3(0f, 0.435f, 0f);
            pColumn.transform.localScale = new Vector3(0.46f, 0.365f, 0.46f);
            setupMesh(pColumn, matMarble);

            // 3. Pedestal Fluted Collar Ring (Satin Brass)
            GameObject pCollar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pCollar.name = "Pedestal_Collar";
            pCollar.transform.SetParent(pedestal.transform, false);
            pCollar.transform.localPosition = new Vector3(0f, 0.810f, 0f);
            pCollar.transform.localScale = new Vector3(0.50f, 0.010f, 0.50f);
            setupMesh(pCollar, matBrass);

            // 4. Pedestal Tabletop Plinth (Nero Marquina Marble)
            GameObject pTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pTop.name = "Pedestal_TopTable";
            pTop.transform.SetParent(pedestal.transform, false);
            pTop.transform.localPosition = new Vector3(0f, 0.835f, 0f);
            pTop.transform.localScale = new Vector3(0.52f, 0.015f, 0.52f);
            setupMesh(pTop, matMarble);

            // 5. Plush Micro-Suede Velvet Pad
            GameObject pPad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pPad.name = "Pedestal_Pad";
            pPad.transform.SetParent(pedestal.transform, false);
            pPad.transform.localPosition = new Vector3(0f, 0.852f, 0f);
            pPad.transform.localScale = new Vector3(0.44f, 0.002f, 0.44f);
            setupMesh(pPad, matPad);

            // --- 3D Angled Mode Switcher Console ---
            GameObject console = new GameObject("Pedestal_Console");
            console.transform.SetParent(pedestal.transform, false);
            console.transform.localPosition = new Vector3(0f, 0.865f, -0.11f);
            console.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);

            // Console Plinth Housing (Dark Titanium)
            GameObject consoleHousing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            consoleHousing.name = "Console_Housing";
            consoleHousing.transform.SetParent(console.transform, false);
            consoleHousing.transform.localPosition = Vector3.zero;
            consoleHousing.transform.localScale = new Vector3(0.32f, 0.022f, 0.09f);
            setupMesh(consoleHousing, matTitanium);

            // Console Trim Lip (Brass)
            GameObject consoleTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            consoleTrim.name = "Console_Trim";
            consoleTrim.transform.SetParent(console.transform, false);
            consoleTrim.transform.localPosition = new Vector3(0f, -0.012f, 0f);
            consoleTrim.transform.localScale = new Vector3(0.325f, 0.005f, 0.095f);
            setupMesh(consoleTrim, matBrass);

            // Helper to build prominent mechanical buttons on console
            Action<string, Vector3, Material, Material, string, PokeButtonFeedback.PokeButtonActionType> createConsoleButton =
                (btnName, localPos, collarMat, capMat, labelText, actionType) =>
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
                ConfigurePokeButton(btnRoot, cap.transform, Vector3.up, 0.006f, actionType);
            };

            // Button 1: VR Lounge / MR Passthrough Toggle
            createConsoleButton("Btn_ToggleMode", new Vector3(-0.095f, 0f, 0f), matBrass, matBrass, "VR / MR\nPASSTHROUGH", PokeButtonFeedback.PokeButtonActionType.ToggleLoungeMode);

            // Button 2: Super-LumiNova Night Mode Toggle
            createConsoleButton("Btn_ToggleLume", new Vector3(0.0f, 0f, 0f), matTitanium, matLume, "SUPER\nLUMINOVA", PokeButtonFeedback.PokeButtonActionType.ToggleSuperLumiNova);

            // Button 3: Cycle Luxury Watch Preset
            createConsoleButton("Btn_NextPreset", new Vector3(0.095f, 0f, 0f), matSteel, matHandsIceBlue, "CYCLE\nWATCH", PokeButtonFeedback.PokeButtonActionType.CycleWatchPreset);

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
            BuildDetailedShowcaseWatch(
                tray.transform, "Showcase_Steel",
                new Vector3(-0.115f, 0.018f, 0.0f), Quaternion.Euler(25f, 0f, 0f),
                matSteel, matSteel, matDialSteel, matHandsIceBlue, matChronoRed, matStrapBlack, matLume, matGlass,
                isDiver: false, presetIndex: 0, catalogMgr: catalogMgr);

            BuildDetailedShowcaseWatch(
                tray.transform, "Showcase_RoseGold",
                new Vector3(0.0f, 0.026f, 0.035f), Quaternion.Euler(25f, 0f, 0f),
                matRoseGold, matRoseGold, matDialBlackHeritage, matRoseGold, matChronoRed, matStrapBrown, matRoseGold, matGlass,
                isDiver: false, presetIndex: 1, catalogMgr: catalogMgr);

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

            // 5. Hands
            GameObject hourHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hourHand.name = "HourHand";
            hourHand.transform.SetParent(watchHead.transform, false);
            hourHand.transform.localRotation = Quaternion.Euler(0f, -60f, 0f);
            hourHand.transform.localPosition = hourHand.transform.localRotation * new Vector3(0f, 0.0076f, 0.0055f);
            hourHand.transform.localScale = new Vector3(0.0015f, 0.0004f, 0.011f);
            setupMesh(hourHand, handsMat);

            GameObject minHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            minHand.name = "MinuteHand";
            minHand.transform.SetParent(watchHead.transform, false);
            minHand.transform.localRotation = Quaternion.Euler(0f, 60f, 0f);
            minHand.transform.localPosition = minHand.transform.localRotation * new Vector3(0f, 0.0080f, 0.0080f);
            minHand.transform.localScale = new Vector3(0.0012f, 0.0004f, 0.016f);
            setupMesh(minHand, handsMat);

            GameObject secHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            secHand.name = "SecondHand";
            secHand.transform.SetParent(watchHead.transform, false);
            secHand.transform.localRotation = Quaternion.Euler(0f, 190f, 0f);
            secHand.transform.localPosition = secHand.transform.localRotation * new Vector3(0f, 0.0084f, 0.0085f);
            secHand.transform.localScale = new Vector3(0.0004f, 0.0003f, 0.017f);
            setupMesh(secHand, chronoMat);

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

            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crown.name = "Crown";
            crown.transform.SetParent(watchHead.transform, false);
            crown.transform.localPosition = new Vector3(0.0225f, 0f, 0f);
            crown.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            crown.transform.localScale = new Vector3(0.0045f, 0.0025f, 0.0045f);
            setupMesh(crown, caseMat);

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

            // 8. Fitted Curving Straps
            GameObject sUpper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sUpper.name = "Strap_Upper";
            sUpper.transform.SetParent(watchHead.transform, false);
            sUpper.transform.localPosition = new Vector3(0f, -0.007f, 0.024f);
            sUpper.transform.localRotation = Quaternion.Euler(40f, 0f, 0f);
            sUpper.transform.localScale = new Vector3(0.020f, 0.003f, 0.022f);
            setupMesh(sUpper, strapMat);

            GameObject sLower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sLower.name = "Strap_Lower";
            sLower.transform.SetParent(watchHead.transform, false);
            sLower.transform.localPosition = new Vector3(0f, -0.007f, -0.024f);
            sLower.transform.localRotation = Quaternion.Euler(-40f, 0f, 0f);
            sLower.transform.localScale = new Vector3(0.020f, 0.003f, 0.022f);
            setupMesh(sLower, strapMat);

            // Setup Meta XR Poke & Touch interaction to Equip this Preset on poke
            int targetIdx = presetIndex;
            ConfigurePokeButton(watchHead, watchHead.transform, Vector3.up, 0.005f, PokeButtonFeedback.PokeButtonActionType.EquipWatchPreset, targetIdx);
        }

        public static void ConfigurePokeButton(
            GameObject buttonObj, Transform capTransform, Vector3 normalDir, float pressDepth,
            PokeButtonFeedback.PokeButtonActionType actionType, int presetIdx = 0)
        {
            // 1. BoxCollider for touch/trigger detection (generous bounds for reliable Quest touch)
            var col = buttonObj.GetComponent<BoxCollider>();
            if (col == null) col = buttonObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(0.065f, 0.050f, 0.065f);

            // 2. Child Surface GameObject with CircleSurface (Direct ISurfacePatch implementation)
            Transform surfChild = buttonObj.transform.Find("Surface");
            GameObject surfGo;
            if (surfChild == null)
            {
                surfGo = new GameObject("Surface");
                surfGo.transform.SetParent(buttonObj.transform, false);
            }
            else
            {
                surfGo = surfChild.gameObject;
            }

            if (capTransform != null)
            {
                surfGo.transform.position = capTransform.position + capTransform.up * 0.005f;
                surfGo.transform.rotation = Quaternion.LookRotation(normalDir, Vector3.up);
            }

            // Remove any old unconfigured BoundsClipper or ClippedPlaneSurface
            var oldClipped = surfGo.GetComponent<ClippedPlaneSurface>();
            if (oldClipped != null) UnityEngine.Object.DestroyImmediate(oldClipped);
            var oldClipper = surfGo.GetComponent<BoundsClipper>();
            if (oldClipper != null) UnityEngine.Object.DestroyImmediate(oldClipper);

            // Add PlaneSurface & CircleSurface as the official Meta XR surface patch
            var planeSurf = surfGo.GetComponent<PlaneSurface>();
            if (planeSurf == null) planeSurf = surfGo.AddComponent<PlaneSurface>();

            var circleSurf = surfGo.GetComponent<CircleSurface>();
            if (circleSurf == null) circleSurf = surfGo.AddComponent<CircleSurface>();

            var planeField = typeof(CircleSurface).GetField("_planeSurface", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (planeField != null) planeField.SetValue(circleSurf, planeSurf);

            var radiusField = typeof(CircleSurface).GetField("_radius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (radiusField != null) radiusField.SetValue(circleSurf, 0.030f);

            // 3. PokeInteractable
            var poke = buttonObj.GetComponent<PokeInteractable>();
            if (poke == null) poke = buttonObj.AddComponent<PokeInteractable>();

            var surfField = typeof(PokeInteractable).GetField("_surfacePatch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (surfField != null) surfField.SetValue(poke, circleSurf);

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

            // 4. PokeButtonFeedback with guaranteed direct action execution
            var feedback = buttonObj.GetComponent<PokeButtonFeedback>();
            if (feedback == null) feedback = buttonObj.AddComponent<PokeButtonFeedback>();

            feedback.SetAction(actionType, presetIdx);

            var capField = typeof(PokeButtonFeedback).GetField("movableCap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (capField != null) capField.SetValue(feedback, capTransform != null ? capTransform : buttonObj.transform);

            var dirField = typeof(PokeButtonFeedback).GetField("localPressDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (dirField != null) dirField.SetValue(feedback, -normalDir.normalized);

            var depthField = typeof(PokeButtonFeedback).GetField("pressDepth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (depthField != null) depthField.SetValue(feedback, pressDepth);

            // 5. InteractableUnityEventWrapper connecting PokeInteractable -> feedback.TriggerPush
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
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(evt, feedback.TriggerPush);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(evt, feedback.TriggerPush);
            }
        }

        public static void ConfigureHandTrackingPokeRig()
        {
            GameObject camRig = GameObject.Find("[BuildingBlock] Camera Rig");
            if (camRig == null) return;

            var pokeRig = camRig.GetComponent<HandFingertipPokeRig>();
            if (pokeRig == null) pokeRig = camRig.AddComponent<HandFingertipPokeRig>();

            // Setup Microgesture Locomotion (Lap-Resting Thumb Swipes)
            var microLoc = camRig.GetComponent<MicrogestureLocomotion>();
            if (microLoc == null) microLoc = camRig.AddComponent<MicrogestureLocomotion>();

            var rightHandGo = GameObject.Find("[BuildingBlock] Hand Tracking right");
            if (rightHandGo != null)
            {
                var sourceR = rightHandGo.GetComponent<OVRMicrogestureEventSource>();
                if (sourceR == null) sourceR = rightHandGo.AddComponent<OVRMicrogestureEventSource>();
                sourceR.Hand = rightHandGo.GetComponent<OVRHand>();

                var so = new SerializedObject(microLoc);
                so.FindProperty("rightGestureSource").objectReferenceValue = sourceR;
                so.ApplyModifiedProperties();
            }

            var leftHandGo = GameObject.Find("[BuildingBlock] Hand Tracking left");
            if (leftHandGo != null)
            {
                var sourceL = leftHandGo.GetComponent<OVRMicrogestureEventSource>();
                if (sourceL == null) sourceL = leftHandGo.AddComponent<OVRMicrogestureEventSource>();
                sourceL.Hand = leftHandGo.GetComponent<OVRHand>();

                var so = new SerializedObject(microLoc);
                so.FindProperty("leftGestureSource").objectReferenceValue = sourceL;
                so.ApplyModifiedProperties();
            }
        }
    }
}
