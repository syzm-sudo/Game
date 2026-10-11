using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace TheLastPatient.Editor
{
    [InitializeOnLoad]
    public static class PrototypeGenerator
    {
        public const string Root = "Assets/TheLastPatient/Generated";
        public const string ScenePath = Root + "/Scenes/Chapter01.unity";
        private static Material wall, floor, metal, wood, paper, lightMaterial, shadow, eye;
        private static readonly List<Light> HospitalLights = new List<Light>();
        private static Transform environment;

        static PrototypeGenerator() => EditorApplication.update += GenerateOnFirstOpen;

        private static void GenerateOnFirstOpen()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= GenerateOnFirstOpen;
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(ScenePath)) return;
            // Never replace unsaved user work during automatic first-open setup.
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.Log("The Last Patient: Speichere deine Szene und wähle Tools > The Last Patient > Generate Game Prototype.");
                return;
            }
            GenerateGamePrototype();
        }

        [MenuItem("Tools/The Last Patient/Generate Game Prototype")]
        public static void GenerateGamePrototype()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath) && !Application.isBatchMode && !EditorUtility.DisplayDialog("Greybox neu erzeugen?",
                "Die generierte Szene und ihre Story-Daten werden neu erstellt. Manuelle Änderungen an Generated-Inhalten zuerst außerhalb dieses Ordners sichern. Andere Szenen und Scripts bleiben erhalten.", "Neu erzeugen", "Abbrechen")) return;
            try
            {
                EditorUtility.DisplayProgressBar("The Last Patient", "Pakete, Schrift und Renderpipeline vorbereiten …", 0.1f);
                Folders(); EnsureTmpResources(); ConfigureRendering(); CreateMaterials();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                HospitalLights.Clear();
                environment = new GameObject("Blackwood Hospital · Chapter 1").transform;
                BuildHospital();
                EditorUtility.DisplayProgressBar("The Last Patient", "Story und Spieler verbinden …", 0.6f);
                var chapter = CreateStory(out var entryEvent, out var watcherEvent, out var endEvent,
                    out var intro, out var blackout, out var apparition, out var tape, out var ending);
                var player = CreatePlayer();
                var game = CreateSystems(player, chapter);
                game.introEvent = Event("chapter01_intro", "intro", Array.Empty<string>(), Array.Empty<string>());
                game.cutscenes.intro = intro; game.cutscenes.blackout = blackout;
                game.cutscenes.watcherDialogue = apparition; game.cutscenes.ending = ending;
                AddInteractions(game, tape, entryEvent, watcherEvent, endEvent);
                game.ambience.hospitalLights = HospitalLights.ToArray();
                SavePrefabs(player, game.cutscenes.watcher);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
                var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
                foreach (var item in EditorBuildSettings.scenes) if (item.path != ScenePath) scenes.Add(item);
                EditorBuildSettings.scenes = scenes.ToArray();
                AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
                Selection.activeGameObject = player.gameObject;
                Debug.Log("THE LAST PATIENT: Kapitel 1 erzeugt. Öffne das Game-Fenster, drücke Play und klicke ins Spiel. Keine Inspector-Zuweisungen nötig.");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem("Tools/The Last Patient/Open Chapter 1")]
        public static void OpenChapter()
        {
            if (!File.Exists(ScenePath)) { GenerateGamePrototype(); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Tools/The Last Patient/Build Windows Player")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath)) GenerateGamePrototype();
            if (!File.Exists(ScenePath)) throw new InvalidOperationException("Kapitel 1 wurde noch nicht erzeugt.");
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = "Builds/Windows/TheLastPatient.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Windows-Build fehlgeschlagen. Details stehen in der Unity-Console.");
            Debug.Log("Windows-Spiel: Builds/Windows/TheLastPatient.exe");
        }

        // CI entry point: Unity -batchmode -quit -projectPath ... -executeMethod TheLastPatient.Editor.PrototypeGenerator.GenerateForCI
        public static void GenerateForCI()
        {
            GenerateGamePrototype();
            if (!File.Exists(ScenePath)) throw new InvalidOperationException("Die Szene wurde nicht gespeichert.");
        }

        private static void Folders()
        {
            foreach (string path in new[] { Root, Root + "/Scenes", Root + "/Story", Root + "/Materials", Root + "/Prefabs", Root + "/Rendering" })
                Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }

        private static void EnsureTmpResources()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return;
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            if (package == null) throw new InvalidOperationException("TextMeshPro/Unity UI ist noch nicht installiert. Warte auf den Package Manager.");
            var archives = Directory.GetFiles(package.resolvedPath, "TMP Essential Resources.unitypackage", SearchOption.AllDirectories);
            if (archives.Length == 0) throw new FileNotFoundException("Importiere Window > TextMeshPro > Import TMP Essential Resources und starte den Generator erneut.");
            UnityEditor.AssetPackage.Package.Import(archives[0], false);
            AssetDatabase.Refresh();
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
                throw new InvalidOperationException("TMP-Import noch nicht abgeschlossen. Starte den Generator nach dem Import erneut.");
        }

        private static void ConfigureRendering()
        {
            var renderer = Asset<UniversalRendererData>(Root + "/Rendering/BlackwoodRenderer.asset");
            string pipelinePath = Root + "/Rendering/BlackwoodURP.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            var settings = new SerializedObject(pipeline);
            var renderers = settings.FindProperty("m_RendererDataList");
            if (renderers == null) throw new InvalidOperationException("Diese URP-Version unterstützt die Renderer-Konfiguration nicht.");
            renderers.arraySize = 1; renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            var additionalShadows = settings.FindProperty("m_AdditionalLightShadowsSupported");
            if (additionalShadows == null) throw new InvalidOperationException("Die URP-Schatteneinstellung wurde nicht gefunden.");
            additionalShadows.boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            pipeline.renderScale = 1; pipeline.shadowDistance = 35;
            pipeline.maxAdditionalLightsCount = 8;
            pipeline.msaaSampleCount = 2;
            GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
            PlayerSettings.companyName = "BlackwoodStudio"; PlayerSettings.productName = "The Last Patient";
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = false;
            EditorUtility.SetDirty(pipeline);
        }

        private static T Asset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        private static Material Material(string name, Color color, Color emission = default)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit fehlt. Warte auf den Package Manager.");
                material = new Material(shader); AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", 0.15f);
            if (emission.maxColorComponent > 0) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", emission); }
            EditorUtility.SetDirty(material); return material;
        }
        private static void CreateMaterials()
        {
            wall = Material("HospitalPlaster", new Color(0.35f, 0.4f, 0.37f));
            floor = Material("FloorTile", new Color(0.2f, 0.25f, 0.25f));
            metal = Material("WornMetal", new Color(0.12f, 0.18f, 0.19f));
            wood = Material("DarkWood", new Color(0.22f, 0.15f, 0.11f));
            paper = Material("PatientFile", new Color(0.68f, 0.66f, 0.47f), new Color(0.04f, 0.04f, 0.02f));
            lightMaterial = Material("EmergencyGreen", new Color(0.14f, 0.47f, 0.35f), new Color(0.18f, 0.75f, 0.49f));
            shadow = Material("WatcherShadow", new Color(0.018f, 0.015f, 0.02f));
            eye = Material("WatcherEyes", Color.white, new Color(1.6f, 1.35f, 0.8f));
        }
        private static GameObject Box(string name, Vector3 position, Vector3 size, Material material, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent != null ? parent : environment, false);
            go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        private static void Sign(string text, Vector3 position, float size = 0.27f, Quaternion? rotation = null)
        {
            var go = new GameObject("Sign · " + text); go.transform.SetParent(environment, false);
            go.transform.position = position; go.transform.rotation = rotation ?? Quaternion.identity;
            var label = go.AddComponent<TextMeshPro>();
            label.text = text; label.fontSize = size * 10; label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.63f, 0.79f, 0.71f); label.rectTransform.sizeDelta = new Vector2(7, 1.2f);
        }
        private static Light PointLight(string name, Vector3 position, Color color, float intensity, float range, bool hospital)
        {
            var go = new GameObject(name); go.transform.SetParent(environment, false); go.transform.position = position;
            var light = go.AddComponent<Light>(); light.type = LightType.Point; light.color = color;
            light.intensity = intensity; light.range = range; light.shadows = LightShadows.None;
            if (hospital) HospitalLights.Add(light); return light;
        }
        private static void BuildHospital()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.16f, 0.18f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.035f, 0.055f, 0.065f); RenderSettings.fogDensity = 0.025f;
            Box("Outside ground", new Vector3(0, -0.2f, -8), new Vector3(18, 0.4f, 20), floor);
            Box("Left outside boundary", new Vector3(-9, 1.5f, -8), new Vector3(0.3f, 3, 20), metal);
            Box("Right outside boundary", new Vector3(9, 1.5f, -8), new Vector3(0.3f, 3, 20), metal);
            Box("Outside back boundary", new Vector3(0, 1.5f, -18), new Vector3(18, 3, 0.3f), metal);
            Box("Hospital floor", new Vector3(0, -0.2f, 20), new Vector3(8, 0.4f, 40), floor);
            Box("Left wall", new Vector3(-4, 1.8f, 20), new Vector3(0.3f, 3.6f, 40), wall);
            Box("Right wall", new Vector3(4, 1.8f, 20), new Vector3(0.3f, 3.6f, 40), wall);
            Box("Roof", new Vector3(0, 3.7f, 20), new Vector3(8, 0.25f, 40), wall);
            Box("Ward end wall", new Vector3(0, 1.8f, 40), new Vector3(8, 3.6f, 0.3f), wall);
            Box("Facade above entrance", new Vector3(0, 4.2f, 0), new Vector3(8.3f, 1.2f, 0.5f), metal);
            foreach (float z in new[] { 0f, 29f })
            {
                Box("Door wall left", new Vector3(-2.65f, 1.8f, z), new Vector3(2.7f, 3.6f, 0.3f), wall);
                Box("Door wall right", new Vector3(2.65f, 1.8f, z), new Vector3(2.7f, 3.6f, 0.3f), wall);
                Box("Door lintel", new Vector3(0, 3.25f, z), new Vector3(2.6f, 0.7f, 0.3f), wall);
            }
            Box("Reception desk", new Vector3(-2.55f, 0.55f, 8), new Vector3(2.3f, 1.1f, 3.4f), wood);
            Box("Flashlight trolley", new Vector3(2.45f, 0.45f, 3.3f), new Vector3(1.5f, 0.9f, 1.2f), metal);
            Box("Battery trolley", new Vector3(2.5f, 0.45f, 12.5f), new Vector3(1.4f, 0.9f, 1.2f), metal);
            for (int i = 0; i < 12; i++)
            {
                float z = 1 + i * 3.1f;
                Box("Wall rail L", new Vector3(-3.82f, 0.92f, z), new Vector3(0.08f, 0.15f, 2.95f), metal);
                Box("Wall rail R", new Vector3(3.82f, 0.92f, z), new Vector3(0.08f, 0.15f, 2.95f), metal);
                if (i % 2 == 0)
                {
                    Box("Ceiling fixture", new Vector3(0, 3.48f, z), new Vector3(1.2f, 0.08f, 0.36f), lightMaterial);
                    PointLight("Fluorescent", new Vector3(0, 3.1f, z), new Color(0.65f, 0.85f, 0.76f), 1.6f, 7, true);
                }
            }
            // Surface tile seams have no colliders and cannot snag the controller.
            for (int z = 1; z < 40; z += 2)
            {
                var seam = Box("Floor seam", new Vector3(0, 0.006f, z), new Vector3(7.7f, 0.004f, 0.02f), metal);
                UnityEngine.Object.DestroyImmediate(seam.GetComponent<Collider>());
            }
            for (int i = 0; i < 3; i++)
            {
                float z = 16 + i * 4;
                Box("Patient room door", new Vector3(3.8f, 1.3f, z), new Vector3(0.1f, 2.6f, 1.4f), wood);
                Sign("ZIMMER " + (5 + i), new Vector3(3.71f, 2.8f, z), 0.18f, Quaternion.Euler(0, 90, 0));
            }
            Box("Hospital bed", new Vector3(-2.5f, 0.65f, 35), new Vector3(1.3f, 0.45f, 2.8f), metal);
            Box("Mattress", new Vector3(-2.5f, 0.94f, 35), new Vector3(1.25f, 0.22f, 2.7f), paper);
            Box("Exit sign", new Vector3(0, 3.05f, -0.25f), new Vector3(2.2f, 0.46f, 0.1f), metal);
            Sign("BLACKWOOD\nKRANKENHAUS", new Vector3(0, 4.35f, -0.25f), 0.4f);
            Sign("EMPFANG", new Vector3(-2.2f, 2.55f, 10), 0.26f);
            Sign("PATIENTENSTATION", new Vector3(0, 3.21f, 28.78f), 0.25f);
            Sign("BATTERIEN / NOT-DYNAMO", new Vector3(2.3f, 1.75f, 13.05f), 0.17f);
            PointLight("Outside lamp", new Vector3(0, 4, -6), new Color(0.38f, 0.52f, 0.66f), 2.5f, 15, false);
            PointLight("Reception emergency", new Vector3(-2.7f, 2.1f, 7.6f), new Color(0.34f, 0.56f, 0.43f), 0.55f, 7, false);
            PointLight("Pickup glow", new Vector3(2.45f, 1.7f, 3.3f), new Color(0.25f, 0.61f, 0.43f), 0.65f, 5, false);
            PointLight("Ward emergency", new Vector3(0, 2.7f, 28), new Color(0.56f, 0.16f, 0.12f), 0.85f, 8, false);
        }

        private static DialogueDefinition Dialogue(string id, params DialogueLine[] lines)
        {
            var asset = Asset<DialogueDefinition>(Root + "/Story/" + id + ".asset");
            asset.id = id; asset.lines = lines; EditorUtility.SetDirty(asset); return asset;
        }
        private static DialogueLine Line(string speaker, string text) => new DialogueLine { speaker = speaker, text = text, holdSeconds = 2.4f };
        private static StoryEventDefinition Event(string id, string cutscene, string[] flags, string[] goals, params string[] required)
        {
            var asset = Asset<StoryEventDefinition>(Root + "/Story/event." + id + ".asset");
            asset.id = id; asset.cutsceneId = cutscene; asset.setFlags = flags; asset.completeObjectives = goals;
            asset.requiredFlags = required; asset.once = true; EditorUtility.SetDirty(asset); return asset;
        }
        private static ChapterDefinition CreateStory(out StoryEventDefinition entryEvent, out StoryEventDefinition watcherEvent,
            out StoryEventDefinition endEvent, out DialogueDefinition intro, out DialogueDefinition blackout,
            out DialogueDefinition apparition, out DialogueDefinition tape, out DialogueDefinition ending)
        {
            var chapter = Asset<ChapterDefinition>(Root + "/Story/Chapter01.asset");
            chapter.id = StoryIds.Chapter; chapter.title = "Kapitel 1 · Die Ankunft";
            string[] ids = { StoryIds.Enter, StoryIds.Flashlight, StoryIds.Reception, StoryIds.Key, StoryIds.Tape, StoryIds.Ward };
            string[] goals = { "Finde einen Weg in das Krankenhaus.", "Finde eine Taschenlampe im Empfangsbereich.",
                "Untersuche die Patientenakte am Empfang.", "Finde den Schlüssel zur Patientenstation.",
                "Höre das erste Tonband am Empfang an.", "Öffne die Patientenstation und betrete den Trakt." };
            chapter.objectives = new ObjectiveDefinition[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                var objective = Asset<ObjectiveDefinition>(Root + "/Story/" + ids[i] + ".asset");
                objective.id = ids[i]; objective.description = goals[i]; chapter.objectives[i] = objective; EditorUtility.SetDirty(objective);
            }
            intro = Dialogue("dialogue.intro", Line("DANIEL CARTER · 2013", "Fünfzehn Jahre. So lange habe ich versucht, diesen Ort zu vergessen."),
                Line("DANIEL", "Dann kam die Kassette. Emilys Stimme. Und diese Adresse."),
                Line("EMILY · AUF DER KASSETTE", "Daniel ... wenn du mich hören kannst, komm nach Blackwood. Bitte."),
                Line("DANIEL", "Ich finde dich. Diesmal gehe ich nicht ohne Antworten."));
            blackout = Dialogue("dialogue.blackout", Line("DANIEL", "Die Tür ... sie hat sich verriegelt."),
                Line("EMILY · LAUTSPRECHER", "Daniel ... du darfst ihnen nicht vertrauen."),
                Line("DANIEL", "Emily? Bist du das?"), Line("EMILY", "Nicht hier. Geh zur Patientenstation. Der Schlüssel liegt noch am Empfang."));
            apparition = Dialogue("dialogue.watcher", Line("DANIEL", "Da steht jemand. Am Ende des Flurs."),
                Line("EMILY · LAUTSPRECHER", "Er erinnert sich an dich."), Line("DANIEL", "Wer erinnert sich an mich?"),
                Line("EMILY", "Der, den sie zurückgelassen haben."));
            tape = Dialogue("dialogue.tape01", Line("TONBAND · 12. NOVEMBER 1998", "Aufnahme Blackwood. Station B. Die Unterlagen der Geschwister bleiben unter Verschluss."),
                Line("EMILY · FLÜSTERND", "Daniel, hör mir zu. Wenn sie sagen, du sollst dich erinnern: Tu es nicht."),
                Line("DANIEL", "Das ist ihre Stimme. Aber sie klingt ... wie damals."),
                Line("EMILY", "Nimm den Stationsschlüssel aus der Akte. Zimmer sieben. Dort haben wir etwas versteckt."));
            ending = Dialogue("dialogue.chapter_end", Line("DANIEL", "Patientenstation. Genau wie auf der Kassette."),
                Line("EMILY · FERNE STIMME", "Zimmer sieben, Daniel. Du warst nicht der Einzige."));
            chapter.intro = intro; EditorUtility.SetDirty(chapter);
            entryEvent = Event("hospital_entry", "blackout", new[] { StoryIds.Inside }, new[] { StoryIds.Enter });
            watcherEvent = Event("first_watcher", "watcher", new[] { StoryIds.WatcherSeen }, Array.Empty<string>(), StoryIds.TapeHeard);
            endEvent = Event("chapter01_end", "chapter_end", new[] { StoryIds.SliceComplete }, new[] { StoryIds.Ward }, StoryIds.TapeHeard, StoryIds.WardKey);
            return chapter;
        }

        private static FirstPersonController CreatePlayer()
        {
            var go = new GameObject("Daniel Carter"); go.layer = 2; go.transform.position = new Vector3(0, 0.1f, -12);
            var controller = go.AddComponent<CharacterController>();
            controller.height = 1.8f; controller.radius = 0.3f; controller.center = Vector3.up * 0.9f;
            controller.stepOffset = 0.25f; controller.skinWidth = 0.04f;
            var input = go.AddComponent<PlayerInputReader>();
            var cameraGo = new GameObject("First Person Camera"); cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(go.transform, false); cameraGo.transform.localPosition = Vector3.up * 1.62f;
            var camera = cameraGo.AddComponent<Camera>(); camera.fieldOfView = 72;
            camera.nearClipPlane = 0.05f; camera.farClipPlane = 90;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.035f, 0.055f, 0.065f);
            cameraGo.AddComponent<AudioListener>();
            var player = go.AddComponent<FirstPersonController>(); player.input = input; player.cameraPivot = cameraGo.transform;
            var beamGo = new GameObject("Flashlight Beam"); beamGo.transform.SetParent(cameraGo.transform, false);
            beamGo.transform.localPosition = new Vector3(0.15f, -0.1f, 0.1f);
            var beam = beamGo.AddComponent<Light>(); beam.type = LightType.Spot;
            beam.color = new Color(0.97f, 0.94f, 0.78f); beam.range = 24; beam.spotAngle = 52;
            beam.innerSpotAngle = 24; beam.intensity = 7; beam.shadows = LightShadows.Soft; beam.enabled = false;
            var flashlight = go.AddComponent<FlashlightController>(); flashlight.beam = beam; flashlight.input = input;
            var interactor = go.AddComponent<PlayerInteractor>(); interactor.view = camera; interactor.input = input;
            return player;
        }
        private static GameDirector CreateSystems(FirstPersonController player, ChapterDefinition chapter)
        {
            var root = new GameObject("Game Systems");
            var game = root.AddComponent<GameDirector>();
            game.story = root.AddComponent<StoryManager>(); game.story.chapter = chapter;
            game.ui = root.AddComponent<GameUI>(); game.missions = root.AddComponent<MissionManager>();
            game.missions.story = game.story; game.missions.ui = game.ui;
            game.player = player; game.input = player.GetComponent<PlayerInputReader>();
            game.flashlight = player.GetComponent<FlashlightController>();
            game.dialogue = root.AddComponent<DialogueManager>(); game.dialogue.ui = game.ui; game.dialogue.input = game.input;
            var voice = new GameObject("Dialogue Audio"); voice.transform.SetParent(root.transform, false);
            game.dialogue.voice = voice.AddComponent<AudioSource>(); game.dialogue.voice.playOnAwake = false;
            game.cutscenes = root.AddComponent<CutsceneManager>();
            game.ambience = root.AddComponent<HospitalAmbience>();
            var hum = new GameObject("Ambient Audio"); hum.transform.SetParent(root.transform, false);
            game.ambience.ambience = hum.AddComponent<AudioSource>(); game.ambience.ambience.playOnAwake = false;
            var effects = new GameObject("Effects Audio"); effects.transform.SetParent(root.transform, false);
            game.ambience.effects = effects.AddComponent<AudioSource>(); game.ambience.effects.playOnAwake = false;
            return game;
        }
        private static StoryDoor Door(string id, float z, string label)
        {
            var root = new GameObject("Door · " + id); root.transform.SetParent(environment, false);
            root.transform.position = new Vector3(-1.28f, 0, z);
            var hinge = new GameObject("Hinge"); hinge.transform.SetParent(root.transform, false);
            Box("Door leaf", new Vector3(1.28f, 1.42f, 0), new Vector3(2.55f, 2.84f, 0.16f), wood, hinge.transform);
            Box("Handle", new Vector3(2.2f, 1.22f, -0.13f), new Vector3(0.22f, 0.05f, 0.1f), lightMaterial, hinge.transform);
            var door = root.AddComponent<StoryDoor>(); door.doorId = id; door.hinge = hinge.transform; door.label = label;
            return door;
        }
        private static void Trigger(string name, float z, StoryEventDefinition storyEvent)
        {
            var go = new GameObject("Story Trigger · " + name); go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(0, 1.5f, z);
            var trigger = go.AddComponent<BoxCollider>(); trigger.isTrigger = true; trigger.size = new Vector3(7.5f, 3, 1);
            go.AddComponent<StoryTrigger>().storyEvent = storyEvent;
        }
        private static void AddInteractions(GameDirector game, DialogueDefinition tape, StoryEventDefinition entry,
            StoryEventDefinition watcherEvent, StoryEventDefinition end)
        {
            var entryDoor = Door("entrance", 0, "Krankenhaus betreten"); entryDoor.lockAfterEntry = true;
            game.cutscenes.entrance = entryDoor;
            var wardDoor = Door("ward", 29, "Patientenstation aufschließen");
            wardDoor.requiredFlags = new[] { StoryIds.WardKey, StoryIds.TapeHeard };
            wardDoor.lockedMessage = "Du brauchst den Stationsschlüssel und einen Hinweis vom Tonband am Empfang.";
            var flashlight = Box("Collectible flashlight", new Vector3(2.3f, 1.05f, 3.22f), new Vector3(0.55f, 0.22f, 0.23f), lightMaterial);
            var pickup = flashlight.AddComponent<PickupItem>(); pickup.label = "Taschenlampe nehmen";
            pickup.itemId = StoryIds.HasFlashlight; pickup.objectiveId = StoryIds.Flashlight;
            pickup.requiredFlags = new[] { StoryIds.Inside };
            var battery = Box("Battery supplies", new Vector3(2.4f, 1.05f, 12.35f), new Vector3(0.45f, 0.23f, 0.35f), lightMaterial);
            var batteries = battery.AddComponent<PickupItem>(); batteries.battery = true; batteries.label = "Batterien einsetzen";
            batteries.requiredFlags = new[] { StoryIds.HasFlashlight }; batteries.lockedMessage = "Finde zuerst die Taschenlampe.";
            var file = Box("Reception patient file", new Vector3(-1.7f, 1.15f, 7.1f), new Vector3(0.65f, 0.06f, 0.47f), paper);
            var document = file.AddComponent<DocumentItem>(); document.label = "Empfangsakte untersuchen";
            document.documentId = StoryIds.ReceptionFile; document.title = "BLACKWOOD · AUFNAHMEPROTOKOLL / 1998";
            document.text = "12. November 1998 · Station B\n\nZwei Geschwister aufgenommen. Der Junge reagiert auf vertraute Stimmen. Die ältere Schwester weigert sich, den Raum zu verlassen.\n\nDr. R. Voss: „Kontakt zwischen den Patienten nur unter Aufsicht. Originalaufnahmen nicht vernichten.“\n\nHandschriftlicher Vermerk:\n„Den Schlüssel zur Patientenstation unter der Rückseite dieser Akte lassen. Das Tonband liegt daneben.“\n\nUnter dem Papier ertastest du einen kleinen, kalten Schlüssel.";
            document.objectiveId = StoryIds.Reception; document.hiddenItem = StoryIds.WardKey; document.hiddenItemObjective = StoryIds.Key;
            document.requiredFlags = new[] { StoryIds.HasFlashlight }; document.lockedMessage = "Es ist zu dunkel zum Lesen. Auf dem Wagen rechts liegt eine Taschenlampe.";
            var recorder = Box("Tape recorder", new Vector3(-1.7f, 1.25f, 8.85f), new Vector3(0.7f, 0.3f, 0.5f), metal);
            var tapePlayer = recorder.AddComponent<TapePlayer>(); tapePlayer.label = "Tonband abspielen";
            tapePlayer.tapeId = StoryIds.FirstTape; tapePlayer.dialogue = tape; tapePlayer.objectiveId = StoryIds.Tape;
            tapePlayer.requiredFlags = new[] { StoryIds.WardKey }; tapePlayer.lockedMessage = "Untersuche zuerst die Akte und nimm den Stationsschlüssel.";
            PrefabUtility.SaveAsPrefabAssetAndConnect(entryDoor.gameObject, Root + "/Prefabs/EntranceDoor.prefab", InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAssetAndConnect(wardDoor.gameObject, Root + "/Prefabs/WardDoor.prefab", InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAssetAndConnect(flashlight, Root + "/Prefabs/FlashlightPickup.prefab", InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAssetAndConnect(file, Root + "/Prefabs/PatientFile.prefab", InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAssetAndConnect(recorder, Root + "/Prefabs/TapeRecorder.prefab", InteractionMode.AutomatedAction);
            for (int i = 0; i < 2; i++)
            {
                var reel = Box("Tape reel", new Vector3(-1.88f + i * 0.36f, 1.42f, 8.85f), new Vector3(0.2f, 0.04f, 0.2f), paper);
                UnityEngine.Object.DestroyImmediate(reel.GetComponent<Collider>());
            }
            game.cutscenes.watcher = CreateWatcher();
            Trigger("hospital entry", 2, entry);
            Trigger("watcher appearance", 17, watcherEvent);
            Trigger("chapter end", 32, end);
        }
        private static GameObject CreateWatcher()
        {
            var root = new GameObject("THE WATCHER · First Appearance"); root.transform.SetParent(environment, false);
            root.transform.position = new Vector3(0, 0, 25);
            Box("Body", new Vector3(0, 1.05f, 0), new Vector3(0.7f, 1.8f, 0.4f), shadow, root.transform);
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere); head.name = "Head"; head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0, 2.1f, 0); head.transform.localScale = new Vector3(0.36f, 0.44f, 0.36f);
            head.GetComponent<Renderer>().sharedMaterial = shadow;
            Box("Left eye", new Vector3(-0.08f, 2.15f, -0.17f), new Vector3(0.04f, 0.026f, 0.026f), eye, root.transform);
            Box("Right eye", new Vector3(0.08f, 2.15f, -0.17f), new Vector3(0.04f, 0.026f, 0.026f), eye, root.transform);
            Box("Left arm", new Vector3(-0.48f, 1.03f, 0), new Vector3(0.18f, 1.65f, 0.22f), shadow, root.transform);
            Box("Right arm", new Vector3(0.48f, 1.03f, 0), new Vector3(0.18f, 1.65f, 0.22f), shadow, root.transform);
            foreach (var collider in root.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            root.SetActive(false); return root;
        }
        private static void SavePrefabs(FirstPersonController player, GameObject watcher)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(player.gameObject, Root + "/Prefabs/Player.prefab", InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAssetAndConnect(watcher, Root + "/Prefabs/Watcher.prefab", InteractionMode.AutomatedAction);
        }
    }
}
