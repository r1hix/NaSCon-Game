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
    /// Programmatic level authoring system for Umbra Chronos.
    /// Builds and packages the 3 showcase puzzle levels for the 3-minute trailer:
    /// - Level 01: "Dawn of the Sundial" (Sun rotation + solid shadow bridge across lethal void)
    /// - Level 02: "Echo of the Vault" (Rewind + Umbral Echo holding pressure plate)
    /// - Level 03: "The Eclipse Paradox" (Grand climax: dual shadow bridges + Echo switch holding)
    /// </summary>
    public static class LevelBuilder
    {
        private const string LevelsFolder = "Assets/_Project/Scenes/Levels";
        private const string PrefabEnvFolder = "Assets/_Project/Prefabs/Environment";
        private const string PrefabCoreFolder = "Assets/_Project/Prefabs/Core";
        private const string PrefabInteractFolder = "Assets/_Project/Prefabs/Interactables";
        private const string PrefabPlayerFolder = "Assets/_Project/Prefabs/Player";
        private const string SpriteFolder = "Assets/_Project/Sprites";

        [MenuItem("Umbra Chronos/Build All Showcase Levels (01, 02, 03)")]
        public static void BuildAllLevels()
        {
            BuildLevel01();
            BuildLevel02();
            BuildLevel03();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "All 3 Showcase Levels Generated!",
                    "Successfully built:\n- Level_01.unity (Dawn of the Sundial)\n- Level_02.unity (Echo of the Vault)\n- Level_03.unity (The Eclipse Paradox)\n\nAll levels are ready in Assets/_Project/Scenes/Levels/!",
                    "Awesome!");
            }
        }

        [MenuItem("Umbra Chronos/Build Level 01 (Dawn of the Sundial)")]
        public static void BuildLevel01()
        {
            EnsureLevelsDirectory();
            Sprite sprite = GetOrCreateSquareSprite();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera & Audio
            SetupLevelCamera(new Vector3(0f, 1.0f, -10f), 7.0f);

            // 2. Visual Sky & Lighting
            SetupAtmosphere(sprite, new Color(0.25f, 0.20f, 0.40f, 1f));

            // 3. SunDial
            GameObject sunInst = SetupSunDial(sprite, new Vector2(0f, -1f), 15f, 50f);

            // 4. Lethal Light Zone & Post Processing
            SetupPostProcessingAndLight(sunInst);

            // 5. Level Geometry: Starting Cliff, Lethal Void, Monolith, and Landing Cliff
            // Left Cliff (Player starts here)
            CreatePlatform("Cliff_Left", new Vector3(-8.5f, -2.5f, 0f), new Vector3(9f, 2f, 1f), sprite);
            CreatePlatform("Cliff_Left_Backdrop", new Vector3(-13.5f, 1.5f, 0f), new Vector3(1f, 8f, 1f), sprite); // Wall

            // Right Cliff (Sanctuary Landing)
            CreatePlatform("Cliff_Right", new Vector3(8.5f, -2.5f, 0f), new Vector3(9f, 2f, 1f), sprite);

            // Lethal Chasm Floor (Deep deadly pit at y = -7f)
            CreatePlatform("Chasm_Floor", new Vector3(0f, -7.5f, 0f), new Vector3(30f, 2f, 1f), sprite);

            // Center Monolith: Standing in the chasm, reaching up to y = 2.5f!
            // When sun is rotated with [E], casts an ink-black solid shadow bridge from x = -3.5 to x = 4.5!
            GameObject monolith = CreateMonolithObject(sprite, new Vector3(-3.5f, -0.5f, 0f), new Vector2(1.8f, 7.0f), -1.5f, sunInst.transform);

            // Sanctuary Goal Arch on the right cliff
            CreateSanctuaryArch(sprite, new Vector3(10f, -0.5f, 0f));

            // 6. Player & Rewind Manager
            SetupPlayer(new Vector3(-8.5f, -0.5f, 0f));

            string path = $"{LevelsFolder}/Level_01.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[LevelBuilder] Saved Level 01 to {path}");
        }

        [MenuItem("Umbra Chronos/Build Level 02 (Echo of the Vault)")]
        public static void BuildLevel02()
        {
            EnsureLevelsDirectory();
            Sprite sprite = GetOrCreateSquareSprite();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera & Audio
            SetupLevelCamera(new Vector3(0.5f, 1.5f, -10f), 7.5f);

            // 2. Atmosphere & Indoor Vault Light
            SetupAtmosphere(sprite, new Color(0.20f, 0.18f, 0.32f, 1f));

            // 3. Sun & Light
            GameObject sunInst = SetupSunDial(sprite, new Vector2(0f, 0f), 14f, 90f);
            SetupPostProcessingAndLight(sunInst);

            // 4. Vault Geometry
            // Ground
            CreatePlatform("Vault_Floor", new Vector3(0f, -2.5f, 0f), new Vector3(32f, 1.5f, 1f), sprite);
            CreatePlatform("Wall_Left", new Vector3(-14f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);
            CreatePlatform("Wall_Right", new Vector3(14f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);

            // High Platform with Pressure Plate
            CreatePlatform("High_Plate_Ledge", new Vector3(-4f, 0.8f, 0f), new Vector3(4.5f, 0.6f, 1f), sprite);

            // Stepping platform to reach high ledge
            CreatePlatform("Step_Ledge", new Vector3(-8.5f, -0.5f, 0f), new Vector3(2.5f, 0.5f, 1f), sprite);

            // TimedGate sealing the Vault Exit
            GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/TimedGate.prefab");
            GameObject platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/PressurePlate.prefab");

            GameObject gateInst = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gateInst.transform.position = new Vector3(6.5f, -0.25f, 0f);

            GameObject plateInst = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
            plateInst.transform.position = new Vector3(-4.0f, 1.35f, 0f);

            var plateComp = plateInst.GetComponent<PressurePlate>();
            if (plateComp != null)
            {
                SerializedObject soPlate = new SerializedObject(plateComp);
                soPlate.FindProperty("targetGate").objectReferenceValue = gateInst.GetComponent<TimedGate>();
                soPlate.ApplyModifiedProperties();
            }

            // Pushable block for versatile solving
            GameObject blockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabEnvFolder}/PushableBlock.prefab");
            if (blockPrefab != null)
            {
                GameObject blockInst = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab);
                blockInst.transform.position = new Vector3(-1.0f, -1.0f, 0f);
            }

            // Sanctuary Arch inside vault
            CreateSanctuaryArch(sprite, new Vector3(10.5f, -1.25f, 0f));

            // Player spawn
            SetupPlayer(new Vector3(-11f, -1.0f, 0f));

            string path = $"{LevelsFolder}/Level_02.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[LevelBuilder] Saved Level 02 to {path}");
        }

        [MenuItem("Umbra Chronos/Build Level 03 (The Eclipse Paradox)")]
        public static void BuildLevel03()
        {
            EnsureLevelsDirectory();
            Sprite sprite = GetOrCreateSquareSprite();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera & Audio
            SetupLevelCamera(new Vector3(0f, 2.0f, -10f), 8.5f);

            // 2. Climax Atmosphere: Eclipse Purple Sky
            SetupAtmosphere(sprite, new Color(0.12f, 0.08f, 0.25f, 1f));

            // 3. SunDial
            GameObject sunInst = SetupSunDial(sprite, new Vector2(0f, 0f), 16f, 90f);
            SetupPostProcessingAndLight(sunInst);

            // 4. Geometry: Central Hub Island, Left Pedestal (Switch 1), Right Pedestal (Switch 2), Final Sanctuary
            // Center Island (Where player starts)
            CreatePlatform("Center_Hub", new Vector3(0f, -2.5f, 0f), new Vector3(8f, 1.5f, 1f), sprite);

            // Left Pedestal across Chasm 1 (Switch 1)
            CreatePlatform("Left_Pedestal", new Vector3(-9.5f, -1.0f, 0f), new Vector3(4f, 1.0f, 1f), sprite);

            // Right Pedestal across Chasm 2 (Switch 2)
            CreatePlatform("Right_Pedestal", new Vector3(9.5f, -1.0f, 0f), new Vector3(4f, 1.0f, 1f), sprite);

            // Deep Lethal Void
            CreatePlatform("Eclipse_Abyss", new Vector3(0f, -8.0f, 0f), new Vector3(40f, 2f, 1f), sprite);

            // Center Tall Monolith that can cast bridges to BOTH sides depending on Q/E rotation!
            CreateMonolithObject(sprite, new Vector3(0f, 1.0f, 0f), new Vector2(1.8f, 7.5f), -1.5f, sunInst.transform);

            // TimedGate sealing the Sanctuary
            GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/TimedGate.prefab");
            GameObject platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/PressurePlate.prefab");

            GameObject gateInst = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gateInst.transform.position = new Vector3(0f, 3.5f, 0f); // Overhead gate to sanctuary arch

            // Plate on Left Pedestal
            GameObject plate1 = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
            plate1.transform.position = new Vector3(-9.5f, -0.25f, 0f);

            // Plate on Right Pedestal
            GameObject plate2 = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
            plate2.transform.position = new Vector3(9.5f, -0.25f, 0f);

            var plateComp1 = plate1.GetComponent<PressurePlate>();
            if (plateComp1 != null)
            {
                SerializedObject so = new SerializedObject(plateComp1);
                so.FindProperty("targetGate").objectReferenceValue = gateInst.GetComponent<TimedGate>();
                so.ApplyModifiedProperties();
            }

            // High Sanctuary Arch
            CreatePlatform("Sanctuary_Roof", new Vector3(0f, 2.5f, 0f), new Vector3(5f, 0.5f, 1f), sprite);
            CreateSanctuaryArch(sprite, new Vector3(0f, 4.0f, 0f));

            // Player starts on Central Hub
            SetupPlayer(new Vector3(0f, -1.0f, 0f));

            string path = $"{LevelsFolder}/Level_03.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[LevelBuilder] Saved Level 03 to {path}");
        }

        private static void SetupLevelCamera(Vector3 pos, float orthoSize)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = pos;

            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            cam.backgroundColor = new Color(0.06f, 0.05f, 0.12f); // Deep Void
            cam.clearFlags = CameraClearFlags.SolidColor;
            camObj.AddComponent<AudioListener>();

            var addData = camObj.AddComponent<UniversalAdditionalCameraData>();
            if (addData != null) addData.renderPostProcessing = true;
        }

        private static void SetupAtmosphere(Sprite sprite, Color ambientColor)
        {
            GameObject bgWall = new GameObject("Background_Canvas");
            bgWall.transform.position = new Vector3(0f, 2f, 0f);
            bgWall.transform.localScale = new Vector3(50f, 30f, 1f);
            var bgSr = bgWall.AddComponent<SpriteRenderer>();
            bgSr.sprite = sprite;
            bgSr.color = ambientColor;
            bgSr.sortingOrder = -20;

            GameObject lightObj = new GameObject("Global Light 2D");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = ambientColor;
            light2D.intensity = 0.45f;
        }

        private static GameObject SetupSunDial(Sprite sprite, Vector2 center, float radius, float startAngle)
        {
            GameObject sunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabEnvFolder}/SunDial.prefab");
            GameObject sunInst = sunPrefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(sunPrefab) : new GameObject("SunDial");

            var dial = sunInst.GetComponent<SunDialController>();
            if (dial != null)
            {
                SerializedObject so = new SerializedObject(dial);
                so.FindProperty("orbitCenter").vector2Value = center;
                so.FindProperty("orbitRadius").floatValue = radius;
                so.FindProperty("currentAngle").floatValue = startAngle;
                so.ApplyModifiedProperties();
            }

            return sunInst;
        }

        private static void SetupPostProcessingAndLight(GameObject sunInst)
        {
            GameObject postPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabCoreFolder}/PostProcessingVolume.prefab");
            if (postPrefab != null) PrefabUtility.InstantiatePrefab(postPrefab);

            GameObject lightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabEnvFolder}/LethalLightZone.prefab");
            if (lightPrefab != null)
            {
                GameObject lightInst = (GameObject)PrefabUtility.InstantiatePrefab(lightPrefab);
                var lightComp = lightInst.GetComponent<LethalLightZone>();
                if (lightComp != null && sunInst != null)
                {
                    SerializedObject so = new SerializedObject(lightComp);
                    so.FindProperty("sunTransform").objectReferenceValue = sunInst.transform;
                    so.ApplyModifiedProperties();
                }
            }
        }

        private static void SetupPlayer(Vector3 spawnPos)
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPlayerFolder}/Player.prefab");
            if (playerPrefab != null)
            {
                GameObject playerInst = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                playerInst.transform.position = spawnPos;
            }

            GameObject rewindPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabCoreFolder}/RewindManager.prefab");
            if (rewindPrefab != null) PrefabUtility.InstantiatePrefab(rewindPrefab);
        }

        private static GameObject CreatePlatform(string name, Vector3 pos, Vector3 scale, Sprite sprite)
        {
            GameObject plat = new GameObject(name);
            plat.transform.position = pos;
            plat.transform.localScale = scale;

            var sr = plat.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.08f, 0.08f, 0.12f, 1.0f); // #0D0D15 Obsidian stone
            sr.sortingOrder = 0;

            var col = plat.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            return plat;
        }

        private static GameObject CreateMonolithObject(Sprite sprite, Vector3 pos, Vector2 size, float groundY, Transform sunTransform)
        {
            GameObject monolith = new GameObject("Monolith");
            monolith.transform.position = pos;
            monolith.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = monolith.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.05f, 0.05f, 0.08f, 1f); // #0D0D15 Dark Matter
            sr.sortingOrder = 2;

            var box = monolith.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;

            var shadowCollider = monolith.AddComponent<DynamicShadowCollider>();
            SerializedObject so = new SerializedObject(shadowCollider);
            so.FindProperty("customDimensions").vector2Value = size;
            so.FindProperty("renderShadowMesh").boolValue = true;
            so.FindProperty("generatePolygonCollider").boolValue = true;
            so.FindProperty("fallbackGroundY").floatValue = groundY;
            if (sunTransform != null) so.FindProperty("sunTransform").objectReferenceValue = sunTransform;
            so.ApplyModifiedProperties();

            return monolith;
        }

        private static void CreateSanctuaryArch(Sprite sprite, Vector3 pos)
        {
            GameObject arch = new GameObject("Sanctuary_Arch");
            arch.transform.position = pos;
            arch.transform.localScale = new Vector3(2.5f, 3.5f, 1f);

            var sr = arch.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(1f, 0.85f, 0.4f, 0.9f); // Radiant Sanctuary Gold (#FFD166)
            sr.sortingOrder = -5;

            var trigger = arch.AddComponent<BoxCollider2D>();
            trigger.size = Vector2.one;
            trigger.isTrigger = true;
        }

        private static void EnsureLevelsDirectory()
        {
            if (!Directory.Exists(LevelsFolder)) Directory.CreateDirectory(LevelsFolder);
        }

        private static Sprite GetOrCreateSquareSprite()
        {
            string path = $"{SpriteFolder}/Square_32.png";
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

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
#endif
