#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UmbraChronos.Player;
using UmbraChronos.Time;
using UmbraChronos.Environment;
using UmbraChronos.Core;

namespace UmbraChronos.Editor
{
    /// <summary>
    /// Automation tool to build the Player prefab, Umbral Echo prefab, RewindManager prefab,
    /// interactive puzzle prefabs (PressurePlate, TimedGate, PushableBlock),
    /// and generate the Sandbox_Shaf playground scene with single-click setup.
    /// </summary>
    public static class SandboxBuilder
    {
        private const string PlayerPrefabFolder = "Assets/_Project/Prefabs/Player";
        private const string CorePrefabFolder = "Assets/_Project/Prefabs/Core";
        private const string InteractablePrefabFolder = "Assets/_Project/Prefabs/Interactables";
        private const string EnvironmentPrefabFolder = "Assets/_Project/Prefabs/Environment";
        private const string SceneFolderPath = "Assets/_Project/Scenes/Sandboxes";
        private const string SpriteFolderPath = "Assets/_Project/Sprites";

        [MenuItem("Umbra Chronos/Setup Shaf Sandbox & Prefabs")]
        public static void BuildSandboxAndPrefabs()
        {
            EnsureDirectoriesExist();

            Sprite squareSprite = GetOrCreateSquareSprite();

            // 1. Core Player & Time Prefabs
            GameObject echoPrefab = CreateEchoPrefab(squareSprite);
            GameObject playerPrefab = CreatePlayerPrefab(squareSprite);
            GameObject rewindManagerPrefab = CreateRewindManagerPrefab(echoPrefab);
            GameObject postProcessingPrefab = CreatePostProcessingPrefab();

            // 2. Interactive Puzzle Prefabs
            GameObject gatePrefab = CreateTimedGatePrefab(squareSprite);
            GameObject platePrefab = CreatePressurePlatePrefab(squareSprite);
            GameObject blockPrefab = CreatePushableBlockPrefab(squareSprite);
            GameObject lethalLightPrefab = CreateLethalLightZonePrefab();
            GameObject sunDialPrefab = CreateSunDialPrefab(squareSprite);
            GameObject monolithPrefab = CreateMonolithPrefab(squareSprite);

            // 3. Assemble Sandbox Scene
            BuildSandboxScene(playerPrefab, rewindManagerPrefab, postProcessingPrefab, lethalLightPrefab, sunDialPrefab, gatePrefab, platePrefab, blockPrefab, monolithPrefab, squareSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Umbra Chronos Playground Ready!",
                    "Successfully generated:\n- SunDial.prefab (Interactive celestial sun with [Q]/[E] rotation!)\n- Monolith.prefab (Reachable height + glowing cyan shadow bridge!)\n- LethalLightZone.prefab (Visible sunbeams & danger)\n- PostProcessingVolume.prefab (Rewind VFX & Solar glare)\n- PressurePlate.prefab & TimedGate.prefab\n- PushableBlock.prefab (Mobile umbrella shield!)\n- Sandbox_Shaf.unity with interactive puzzle testbed!\n\nHit Play to test your time and echo puzzles!",
                    "Let's Go!");
            }
        }

        private static void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(PlayerPrefabFolder)) Directory.CreateDirectory(PlayerPrefabFolder);
            if (!Directory.Exists(CorePrefabFolder)) Directory.CreateDirectory(CorePrefabFolder);
            if (!Directory.Exists(InteractablePrefabFolder)) Directory.CreateDirectory(InteractablePrefabFolder);
            if (!Directory.Exists(EnvironmentPrefabFolder)) Directory.CreateDirectory(EnvironmentPrefabFolder);
            if (!Directory.Exists(SceneFolderPath)) Directory.CreateDirectory(SceneFolderPath);
            if (!Directory.Exists(SpriteFolderPath)) Directory.CreateDirectory(SpriteFolderPath);
        }

        private static Sprite GetOrCreateSquareSprite()
        {
            string path = $"{SpriteFolderPath}/Square_32.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject CreateEchoPrefab(Sprite sprite)
        {
            string path = $"{PlayerPrefabFolder}/UmbralEcho.prefab";

            GameObject echoObj = new GameObject("UmbralEcho");
            echoObj.transform.localScale = new Vector3(0.8f, 1.6f, 1f);

            var sr = echoObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.12f, 0.11f, 0.29f, 0.70f); // Translucent Cyan-Indigo (#1E1B4B)
            sr.sortingOrder = 5;

            var col = echoObj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            echoObj.AddComponent<UmbralEcho>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(echoObj, path);
            Object.DestroyImmediate(echoObj);
            return prefab;
        }

        private static GameObject CreatePlayerPrefab(Sprite sprite)
        {
            string path = $"{PlayerPrefabFolder}/Player.prefab";

            GameObject playerObj = new GameObject("Player");
            playerObj.tag = "Player";
            playerObj.transform.localScale = new Vector3(0.8f, 1.6f, 1f);

            var sr = playerObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.05f, 0.05f, 0.08f, 1f); // #0D0D15 Obsidian silhouette
            sr.sortingOrder = 10;

            var rb = playerObj.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = playerObj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            playerObj.AddComponent<PlayerController>();
            playerObj.AddComponent<TimeBody>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(playerObj, path);
            Object.DestroyImmediate(playerObj);
            return prefab;
        }

        private static GameObject CreateRewindManagerPrefab(GameObject echoPrefab)
        {
            string path = $"{CorePrefabFolder}/RewindManager.prefab";

            GameObject rmObj = new GameObject("RewindManager");
            var rm = rmObj.AddComponent<RewindManager>();

            SerializedObject so = new SerializedObject(rm);
            so.FindProperty("echoPrefab").objectReferenceValue = echoPrefab;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rmObj, path);
            Object.DestroyImmediate(rmObj);
            return prefab;
        }

        private static GameObject CreateTimedGatePrefab(Sprite sprite)
        {
            string path = $"{InteractablePrefabFolder}/TimedGate.prefab";

            GameObject gateObj = new GameObject("TimedGate");
            var gateComp = gateObj.AddComponent<TimedGate>();

            var col = gateObj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            // Visual child
            GameObject visualChild = new GameObject("Visual");
            visualChild.transform.SetParent(gateObj.transform, false);
            visualChild.transform.localScale = new Vector3(0.6f, 2.8f, 1f);

            var sr = visualChild.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.2f, 0.2f, 0.25f, 1f); // Stone slab
            sr.sortingOrder = 3;

            SerializedObject so = new SerializedObject(gateComp);
            so.FindProperty("gateRenderer").objectReferenceValue = sr;
            so.FindProperty("openOffset").vector2Value = new Vector2(0f, 3.2f);
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gateObj, path);
            Object.DestroyImmediate(gateObj);
            return prefab;
        }

        private static GameObject CreatePressurePlatePrefab(Sprite sprite)
        {
            string path = $"{InteractablePrefabFolder}/PressurePlate.prefab";

            GameObject plateObj = new GameObject("PressurePlate");
            var plateComp = plateObj.AddComponent<PressurePlate>();

            var col = plateObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.0f, 0.4f);
            col.offset = new Vector2(0f, 0.2f);
            col.isTrigger = true;

            // Visual child
            GameObject visualChild = new GameObject("Visual");
            visualChild.transform.SetParent(plateObj.transform, false);
            visualChild.transform.localScale = new Vector3(2.0f, 0.2f, 1f);
            visualChild.transform.localPosition = new Vector3(0f, 0.1f, 0f);

            var sr = visualChild.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.85f, 0.45f, 0.1f, 1f); // Amber
            sr.sortingOrder = 1;

            SerializedObject so = new SerializedObject(plateComp);
            so.FindProperty("plateRenderer").objectReferenceValue = sr;
            so.FindProperty("sensorSize").vector2Value = new Vector2(2.0f, 0.5f);
            so.FindProperty("sensorOffset").vector2Value = new Vector2(0f, 0.25f);
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(plateObj, path);
            Object.DestroyImmediate(plateObj);
            return prefab;
        }

        private static GameObject CreatePushableBlockPrefab(Sprite sprite)
        {
            string path = $"{EnvironmentPrefabFolder}/PushableBlock.prefab";

            GameObject blockObj = new GameObject("PushableBlock");
            blockObj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var sr = blockObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.25f, 0.25f, 0.35f, 1f); // Slate stone
            sr.sortingOrder = 8;

            var rb = blockObj.AddComponent<Rigidbody2D>();
            rb.mass = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = blockObj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            blockObj.AddComponent<TimeBody>(); // Rewindable physics box!

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(blockObj, path);
            Object.DestroyImmediate(blockObj);
            return prefab;
        }

        private static GameObject CreateSunDialPrefab(Sprite sprite)
        {
            string path = $"{EnvironmentPrefabFolder}/SunDial.prefab";
            GameObject sunObj = new GameObject("SunDial");
            try { sunObj.tag = "Sun"; } catch (System.Exception) { }

            var sunLight = sunObj.AddComponent<Light2D>();
            sunLight.lightType = Light2D.LightType.Point;
            sunLight.color = new Color(1f, 0.94f, 0.75f, 1f); // Searing solar flare
            sunLight.pointLightOuterRadius = 28f;
            sunLight.intensity = 1.35f;

            // Visual Sun Orb in the sky
            GameObject orb = new GameObject("Sun_Orb_Visual");
            orb.transform.SetParent(sunObj.transform);
            orb.transform.localPosition = Vector3.zero;
            orb.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            var orbSr = orb.AddComponent<SpriteRenderer>();
            orbSr.sprite = sprite;
            orbSr.color = new Color(1f, 0.96f, 0.7f, 1f); // Radiant white-gold core
            orbSr.sortingOrder = 15;

            var dial = sunObj.AddComponent<SunDialController>();
            SerializedObject so = new SerializedObject(dial);
            so.FindProperty("sunLight").objectReferenceValue = sunLight;
            so.FindProperty("sunVisualOrb").objectReferenceValue = orb.transform;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sunObj, path);
            Object.DestroyImmediate(sunObj);
            return prefab;
        }

        private static GameObject CreatePostProcessingPrefab()
        {
            string path = $"{CorePrefabFolder}/PostProcessingVolume.prefab";
            GameObject volumeObj = new GameObject("PostProcessingVolume");
            var volume = volumeObj.AddComponent<UnityEngine.Rendering.Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volumeObj.AddComponent<RewindScreenFX>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(volumeObj, path);
            Object.DestroyImmediate(volumeObj);
            return prefab;
        }

        private static GameObject CreateLethalLightZonePrefab()
        {
            string path = $"{EnvironmentPrefabFolder}/LethalLightZone.prefab";
            GameObject zoneObj = new GameObject("LethalLightZone");
            zoneObj.AddComponent<LethalLightZone>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(zoneObj, path);
            Object.DestroyImmediate(zoneObj);
            return prefab;
        }

        private static GameObject CreateMonolithPrefab(Sprite sprite)
        {
            string path = $"{EnvironmentPrefabFolder}/Monolith.prefab";

            GameObject monolithObj = new GameObject("Monolith");
            monolithObj.transform.localScale = new Vector3(1.4f, 3.6f, 1f);

            var sr = monolithObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.05f, 0.05f, 0.08f, 1f); // #0D0D15 Obsidian silhouette
            sr.sortingOrder = 2;

            var col = monolithObj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            monolithObj.AddComponent<EdgeCollider2D>();
            var shadowCollider = monolithObj.AddComponent<DynamicShadowCollider>();
            SerializedObject so = new SerializedObject(shadowCollider);
            so.FindProperty("fallbackGroundY").floatValue = -1.75f;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(monolithObj, path);
            Object.DestroyImmediate(monolithObj);
            return prefab;
        }

        private static void BuildSandboxScene(
            GameObject playerPrefab, 
            GameObject rewindManagerPrefab,
            GameObject postProcessingPrefab,
            GameObject lethalLightPrefab,
            GameObject sunDialPrefab,
            GameObject gatePrefab,
            GameObject platePrefab,
            GameObject blockPrefab,
            GameObject monolithPrefab,
            Sprite sprite)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera with ScreenShift room-paging (moves over when player goes off-screen!)
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0f, 1.0f, -10f);

            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7.5f;
            cam.backgroundColor = new Color(0.75f, 0.32f, 0.22f); // Warm Radiant Sunset
            cam.clearFlags = CameraClearFlags.SolidColor;
            camObj.AddComponent<AudioListener>();

            var additionalCamData = camObj.AddComponent<UniversalAdditionalCameraData>();
            if (additionalCamData != null)
            {
                additionalCamData.renderPostProcessing = true;
            }

            var camCtrl = camObj.AddComponent<CameraController>();
            camCtrl.ConfigureRoom(new Vector2(24f, 15f), new Vector2(0f, 1.0f), 0.35f);

            // 2. Large Background Canvas - High contrast vivid Dusk sky spanning multiple screens
            GameObject bgWall = new GameObject("Background_Canvas");
            bgWall.transform.position = new Vector3(0f, 3f, 0f);
            bgWall.transform.localScale = new Vector3(120f, 40f, 1f);
            var bgSr = bgWall.AddComponent<SpriteRenderer>();
            bgSr.sprite = sprite;
            bgSr.color = new Color(0.78f, 0.35f, 0.22f, 1f); // Vivid Dusk Sky (#C75938) - High Contrast!
            bgSr.sortingOrder = -20;

            // 3. Global Ambient Sky Light 2D
            GameObject lightObj = new GameObject("Global Light 2D");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = new Color(1.0f, 0.85f, 0.70f, 1f);
            light2D.intensity = 0.5f;

            // 4. Interactive Celestial SunDial (starts in the sky at noon!)
            GameObject sunInst = (GameObject)PrefabUtility.InstantiatePrefab(sunDialPrefab);
            sunInst.transform.position = new Vector3(1f, 14f, 0f);
            var dialComp = sunInst.GetComponent<SunDialController>();
            if (dialComp != null)
            {
                SerializedObject soDial = new SerializedObject(dialComp);
                soDial.FindProperty("followCameraX").boolValue = true;
                soDial.ApplyModifiedProperties();
            }

            // 5. SCREEN 1: Left Cliff, Shelter, Monolith 1, Chasm 1, Plate 1, Gate 1
            CreatePlatform("Cliff_Left", new Vector3(-9.0f, -2.5f, 0f), new Vector3(12f, 1.5f, 1f), sprite);
            CreatePlatform("Boundary_Wall_Left", new Vector3(-15.0f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);
            CreatePlatform("Spawn_Shelter_Roof", new Vector3(-10.5f, 1.2f, 0f), new Vector3(6f, 0.5f, 1f), sprite);

            // Stepping Stone Ledge 1 (hop 0.95 from floor at y = -1.75 to y = -0.8)
            CreatePlatform("Step_Ledge_1", new Vector3(-5.0f, -1.0f, 0f), new Vector3(2.2f, 0.4f, 1f), sprite);

            // Pushable Physics Box in Screen 1
            GameObject blockInst = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab);
            blockInst.transform.position = new Vector3(-8f, -1.15f, 0f);

            // Monolith 1 standing at edge of left cliff (base at y = -1.75, height = 3.6, pos = (-3.2, 0.05))
            GameObject monolithInst1 = (GameObject)PrefabUtility.InstantiatePrefab(monolithPrefab);
            monolithInst1.name = "Monolith_1";
            monolithInst1.transform.position = new Vector3(-3.2f, 0.05f, 0f);
            monolithInst1.transform.localScale = new Vector3(1.4f, 3.6f, 1f);
            var shadowComp1 = monolithInst1.GetComponent<DynamicShadowCollider>();
            if (shadowComp1 != null)
            {
                SerializedObject soMonolith1 = new SerializedObject(shadowComp1);
                soMonolith1.FindProperty("sunTransform").objectReferenceValue = sunInst.transform;
                soMonolith1.FindProperty("fallbackGroundY").floatValue = -1.75f;
                soMonolith1.ApplyModifiedProperties();
            }

            // Mid Plateau connecting Screen 1 and Screen 2 (x = 3.5 to 14.5)
            CreatePlatform("Mid_Plateau", new Vector3(9.0f, -2.5f, 0f), new Vector3(11f, 1.5f, 1f), sprite);

            // Puzzle 1: PressurePlate 1 & TimedGate 1 (opens passageway to Screen 2!)
            GameObject gateInst1 = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gateInst1.name = "TimedGate_1";
            gateInst1.transform.position = new Vector3(8.5f, -0.25f, 0f);

            GameObject plateInst1 = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
            plateInst1.name = "PressurePlate_1";
            plateInst1.transform.position = new Vector3(5.5f, -1.65f, 0f);
            var plateComp1 = plateInst1.GetComponent<PressurePlate>();
            if (plateComp1 != null)
            {
                SerializedObject soPlate1 = new SerializedObject(plateComp1);
                soPlate1.FindProperty("targetGate").objectReferenceValue = gateInst1.GetComponent<TimedGate>();
                soPlate1.ApplyModifiedProperties();
            }

            // 6. SCREEN 2: Monolith 2, Chasm 2, and Grand Sanctuary Citadel!
            GameObject monolithInst2 = (GameObject)PrefabUtility.InstantiatePrefab(monolithPrefab);
            monolithInst2.name = "Monolith_2";
            monolithInst2.transform.position = new Vector3(14.5f, 0.05f, 0f);
            monolithInst2.transform.localScale = new Vector3(1.4f, 3.6f, 1f);
            var shadowComp2 = monolithInst2.GetComponent<DynamicShadowCollider>();
            if (shadowComp2 != null)
            {
                SerializedObject soMonolith2 = new SerializedObject(shadowComp2);
                soMonolith2.FindProperty("sunTransform").objectReferenceValue = sunInst.transform;
                soMonolith2.FindProperty("fallbackGroundY").floatValue = -1.75f;
                soMonolith2.ApplyModifiedProperties();
            }

            // Sanctuary Citadel (x = 21.5 to 33.0)
            CreatePlatform("Sanctuary_Citadel", new Vector3(27.0f, -2.5f, 0f), new Vector3(11f, 1.5f, 1f), sprite);
            CreatePlatform("Boundary_Wall_Right", new Vector3(32.5f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);

            // Lethal Chasm Floor (deep abyss spanning under both rooms)
            CreatePlatform("Chasm_Floor", new Vector3(10.0f, -6.5f, 0f), new Vector3(50f, 1.5f, 1f), sprite);

            // Goal Sanctuary Arch on the Citadel
            GameObject goalMarker = CreatePlatform("Goal_Sanctuary", new Vector3(29.5f, -1.25f, 0f), new Vector3(3.2f, 1.2f, 1f), sprite);
            var goalSr = goalMarker.GetComponent<SpriteRenderer>();
            if (goalSr != null)
            {
                goalSr.color = new Color(0.98f, 0.82f, 0.4f, 1f); // Radiant Gold (#FFD166)
            }

            // 7. Post Processing & Lethal Light Zone
            PrefabUtility.InstantiatePrefab(postProcessingPrefab);
            GameObject lightZoneInst = (GameObject)PrefabUtility.InstantiatePrefab(lethalLightPrefab);
            var lightZoneComp = lightZoneInst.GetComponent<LethalLightZone>();
            if (lightZoneComp != null)
            {
                SerializedObject soLight = new SerializedObject(lightZoneComp);
                SerializedProperty propSun = soLight.FindProperty("sunTransform");
                if (propSun != null)
                {
                    propSun.objectReferenceValue = sunInst.transform;
                    soLight.ApplyModifiedProperties();
                }
            }

            // 8. Player & RewindManager (Spawned safely inside the Screen 1 shelter)
            GameObject playerInst = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInst.transform.position = new Vector3(-10.5f, -1.0f, 0f);

            PrefabUtility.InstantiatePrefab(rewindManagerPrefab);

            // Save Scene
            string scenePath = $"{SceneFolderPath}/Sandbox_Shaf.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static GameObject CreatePlatform(string name, Vector3 pos, Vector3 scale, Sprite sprite)
        {
            GameObject plat = new GameObject(name);
            plat.transform.position = pos;
            plat.transform.localScale = scale;

            var sr = plat.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.08f, 0.08f, 0.12f, 1.0f); // Dark stone obsidian
            sr.sortingOrder = 0;

            plat.AddComponent<BoxCollider2D>();
            return plat;
        }
    }
}
#endif
