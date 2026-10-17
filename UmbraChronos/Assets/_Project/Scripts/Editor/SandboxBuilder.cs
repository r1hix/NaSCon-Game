#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UmbraChronos.Player;
using UmbraChronos.Time;
using UmbraChronos.Environment;

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
                    "Successfully generated:\n- SunDial.prefab (Interactive celestial sun with [Q]/[E] rotation!)\n- Monolith.prefab (Walkable solid shadow bridge!)\n- LethalLightZone.prefab (Visible sunbeams & danger)\n- PostProcessingVolume.prefab (Rewind VFX & Solar glare)\n- PressurePlate.prefab & TimedGate.prefab\n- PushableBlock.prefab (Mobile umbrella shield!)\n- Sandbox_Shaf.unity with interactive puzzle testbed!\n\nHit Play to test your time and echo puzzles!",
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
            sr.color = new Color(0.05f, 0.05f, 0.08f, 1.0f); // Obsidian Black (#0D0D15)
            sr.sortingOrder = 10;

            var rb = playerObj.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;
            rb.gravityScale = 3.0f;

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

            GameObject managerObj = new GameObject("RewindManager");
            var rewindComp = managerObj.AddComponent<RewindManager>();

            SerializedObject so = new SerializedObject(rewindComp);
            SerializedProperty prop = so.FindProperty("echoPrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = echoPrefab;
                so.ApplyModifiedProperties();
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(managerObj, path);
            Object.DestroyImmediate(managerObj);
            return prefab;
        }

        private static GameObject CreateTimedGatePrefab(Sprite sprite)
        {
            string path = $"{InteractablePrefabFolder}/TimedGate.prefab";

            GameObject gateObj = new GameObject("TimedGate");
            gateObj.transform.localScale = new Vector3(0.6f, 3.0f, 1f);

            var sr = gateObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.08f, 0.08f, 0.12f, 1f);
            sr.sortingOrder = 2;

            var col = gateObj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            gateObj.AddComponent<TimedGate>();

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
            sunLight.pointLightOuterRadius = 26f;
            sunLight.intensity = 1.35f;

            // Visual Sun Orb in the sky
            GameObject orb = new GameObject("Sun_Orb_Visual");
            orb.transform.SetParent(sunObj.transform);
            orb.transform.localPosition = Vector3.zero;
            orb.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            var orbSr = orb.AddComponent<SpriteRenderer>();
            orbSr.sprite = sprite;
            orbSr.color = new Color(1f, 0.96f, 0.7f, 1f); // Radiant white-gold core
            orbSr.sortingOrder = 10;

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
            monolithObj.transform.localScale = new Vector3(1.6f, 5.5f, 1f);

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

            // 1. Camera & Audio
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0.5f, 1.5f, -10f);

            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7.5f;
            cam.backgroundColor = new Color(0.08f, 0.07f, 0.16f); // Midnight Indigo (#141128)
            cam.clearFlags = CameraClearFlags.SolidColor;
            camObj.AddComponent<AudioListener>();

            var additionalCamData = camObj.AddComponent<UniversalAdditionalCameraData>();
            if (additionalCamData != null)
            {
                additionalCamData.renderPostProcessing = true;
            }

            // 2. Large Background Canvas (catches 2D lighting falloff in Game view)
            GameObject bgWall = new GameObject("Background_Canvas");
            bgWall.transform.position = new Vector3(0f, 3f, 0f);
            bgWall.transform.localScale = new Vector3(45f, 25f, 1f);
            var bgSr = bgWall.AddComponent<SpriteRenderer>();
            bgSr.sprite = sprite;
            bgSr.color = new Color(0.12f, 0.10f, 0.22f, 1f); // Deep dusk canvas
            bgSr.sortingOrder = -20;

            // 3. Global Ambient Sky Light 2D
            GameObject lightObj = new GameObject("Global Light 2D");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = new Color(0.35f, 0.30f, 0.55f, 1f); // Deep atmospheric dusk
            light2D.intensity = 0.5f;

            // 4. Interactive Celestial SunDial (Rotates with [Q] and [E]!)
            GameObject sunInst = (GameObject)PrefabUtility.InstantiatePrefab(sunDialPrefab);

            // 5. Level Geometry: Shelters, Monoliths, and Safe Havens (Open, spacious layout)
            // Ground Floor
            CreatePlatform("Ground Floor", new Vector3(0f, -2.5f, 0f), new Vector3(36f, 1.5f, 1f), sprite);

            // Left Border Wall (prevents falling off the back edge)
            CreatePlatform("Boundary_Wall_Left", new Vector3(-16f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);

            // Haven 1: Spawn Shelter (Floating overhead monolith at y=2.2f with plenty of jump clearance)
            CreatePlatform("Spawn_Shelter_Roof", new Vector3(-10f, 2.2f, 0f), new Vector3(6f, 0.8f, 1f), sprite);

            // Haven 2: Center Ruins Floating Monolith (Safe intermediate haven with open walking floor below)
            CreatePlatform("Center_Ruins_Roof", new Vector3(-2f, 2.2f, 0f), new Vector3(6f, 0.8f, 1f), sprite);

            // Stepping Stone Ledge (optional high route)
            CreatePlatform("High_Ledge", new Vector3(-6f, 0.2f, 0f), new Vector3(3f, 0.5f, 1f), sprite);

            // 5b. Monolith Pillar with Walkable Solid Shadow Bridge!
            GameObject monolithInst = (GameObject)PrefabUtility.InstantiatePrefab(monolithPrefab);
            monolithInst.transform.position = new Vector3(-1.0f, 1.0f, 0f);
            var shadowComp = monolithInst.GetComponent<DynamicShadowCollider>();
            if (shadowComp != null)
            {
                SerializedObject soMonolith = new SerializedObject(shadowComp);
                soMonolith.FindProperty("sunTransform").objectReferenceValue = sunInst.transform;
                soMonolith.FindProperty("fallbackGroundY").floatValue = -1.75f;
                soMonolith.ApplyModifiedProperties();
            }

            // 6. Pushable Physics Box (On floor at x=-6f, can be pushed as a mobile shadow shield)
            GameObject blockInst = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab);
            blockInst.transform.position = new Vector3(-6f, -1.0f, 0f);

            // 7. Interactive Puzzle: PressurePlate & TimedGate
            GameObject gateInst = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gateInst.transform.position = new Vector3(7.5f, -0.25f, 0f);

            GameObject plateInst = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
            plateInst.transform.position = new Vector3(2.0f, -1.75f, 0f);
            var plateComp = plateInst.GetComponent<PressurePlate>();
            if (plateComp != null)
            {
                SerializedObject soPlate = new SerializedObject(plateComp);
                SerializedProperty propGate = soPlate.FindProperty("targetGate");
                if (propGate != null)
                {
                    propGate.objectReferenceValue = gateInst.GetComponent<TimedGate>();
                    soPlate.ApplyModifiedProperties();
                }
            }

            // 8. Goal Sanctuary Arch behind the gate
            GameObject goalMarker = CreatePlatform("Goal_Sanctuary", new Vector3(12.5f, -1.25f, 0f), new Vector3(3.2f, 1.2f, 1f), sprite);
            var goalSr = goalMarker.GetComponent<SpriteRenderer>();
            if (goalSr != null)
            {
                goalSr.color = new Color(0.98f, 0.82f, 0.4f, 1f); // Radiant Gold (#FFD166)
            }

            // 9. Post Processing & Lethal Light Zone
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

            // 10. Player & RewindManager (Spawned inside the safe spawn shelter)
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
