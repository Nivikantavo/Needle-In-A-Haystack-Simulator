using Needle.Haystack;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Needle.Haystack.Editor
{
    [CustomEditor(typeof(HaystackSystem))]
    public class HaystackSystemEditor : UnityEditor.Editor
    {
        SerializedProperty _strawMesh;
        SerializedProperty _strawMaterial;
        SerializedProperty _needleMesh;
        SerializedProperty _needleMaterial;
        SerializedProperty _poseAsset;
        SerializedProperty _presets;
        SerializedProperty _presetIndex;
        SerializedProperty _shape;
        SerializedProperty _diameter;
        SerializedProperty _height;
        SerializedProperty _strawCount;
        SerializedProperty _seed;
        SerializedProperty _horizontalFraction;
        SerializedProperty _strawLength;
        SerializedProperty _strawRadius;
        SerializedProperty _bakeSeconds;
        SerializedProperty _pickRadiusMultiplier;
        SerializedProperty _hoverColor;
        SerializedProperty _hotPoolSize;
        SerializedProperty _settleSpeed;
        SerializedProperty _settleAngular;
        SerializedProperty _settleHold;
        SerializedProperty _settleTimeout;
        SerializedProperty _onNeedleFound;

        void OnEnable()
        {
            _strawMesh = serializedObject.FindProperty("_strawMesh");
            _strawMaterial = serializedObject.FindProperty("_strawMaterial");
            _needleMesh = serializedObject.FindProperty("_needleMesh");
            _needleMaterial = serializedObject.FindProperty("_needleMaterial");
            _poseAsset = serializedObject.FindProperty("_poseAsset");
            _presets = serializedObject.FindProperty("_presets");
            _presetIndex = serializedObject.FindProperty("_presetIndex");
            _shape = serializedObject.FindProperty("_shape");
            _diameter = serializedObject.FindProperty("_diameter");
            _height = serializedObject.FindProperty("_height");
            _strawCount = serializedObject.FindProperty("_strawCount");
            _seed = serializedObject.FindProperty("_seed");
            _horizontalFraction = serializedObject.FindProperty("_horizontalFraction");
            _strawLength = serializedObject.FindProperty("_strawLength");
            _strawRadius = serializedObject.FindProperty("_strawRadius");
            _bakeSeconds = serializedObject.FindProperty("_bakeSeconds");
            _pickRadiusMultiplier = serializedObject.FindProperty("_pickRadiusMultiplier");
            _hoverColor = serializedObject.FindProperty("_hoverColor");
            _hotPoolSize = serializedObject.FindProperty("_hotPoolSize");
            _settleSpeed = serializedObject.FindProperty("_settleSpeed");
            _settleAngular = serializedObject.FindProperty("_settleAngular");
            _settleHold = serializedObject.FindProperty("_settleHold");
            _settleTimeout = serializedObject.FindProperty("_settleTimeout");
            _onNeedleFound = serializedObject.FindProperty("_onNeedleFound");

            var system = (HaystackSystem)target;
            if (system != null &&
                (system.NeedleMesh == null ||
                 system.NeedleMaterial == null ||
                 system.Presets == null ||
                 system.Presets.Length == 0))
            {
                HaystackAssetUtility.BindDefaultAssets(system);
                EditorUtility.SetDirty(system);
                serializedObject.Update();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Render", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_strawMesh);
            EditorGUILayout.PropertyField(_strawMaterial);
            EditorGUILayout.PropertyField(_needleMesh);
            EditorGUILayout.PropertyField(_needleMaterial);
            EditorGUILayout.PropertyField(_poseAsset, new GUIContent("Pose Asset", "Optional. Ignored when a difficulty preset is selected — Play generates the pile instead of loading baked matrices."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Difficulty", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_shape, new GUIContent("Shape"));
            EditorGUILayout.PropertyField(_presets, new GUIContent("Presets", "Each preset stores radius, height, straw count, and seed. Shape is chosen on this component. Presets do not store straw matrices."));
            DrawDifficultyPopup();

            HaystackPreset active = ((HaystackSystem)target).ActivePreset;
            if (active != null)
            {
                EditorGUILayout.HelpBox(
                    $"{active.DisplayName}: radius {active.Radius:0.##} m, height {active.Height:0.##} m, {active.StrawCount} straws.\n" +
                    "Edit size on the preset asset. Shape is the field above. Generate / Play builds the pile in memory.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.PropertyField(_diameter, new GUIContent("Diameter"));
                EditorGUILayout.PropertyField(_height, new GUIContent("Height"));
                EditorGUILayout.PropertyField(_strawCount, new GUIContent("Straw Count", "No upper cap. Very large counts increase Generate time and DrawMeshInstanced batches."));
                EditorGUILayout.PropertyField(_seed, new GUIContent("Seed", "RNG start. Same seed rebuilds the same pile."));
            }

            EditorGUILayout.PropertyField(_horizontalFraction, new GUIContent("Horizontal Fraction", "How many straws lie flat. The rest stay closer to vertical."));
            EditorGUILayout.PropertyField(_strawLength, new GUIContent("Straw Length"));
            EditorGUILayout.PropertyField(_strawRadius, new GUIContent("Straw Radius"));
            EditorGUILayout.PropertyField(_bakeSeconds, new GUIContent("Physics Bake Seconds", "Optional pose-settle quality. Player collision uses PileVolume, not this bake."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pick / Grab", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_pickRadiusMultiplier, new GUIContent("Pick Radius Multiplier", "Inflates the pick capsule so a first-person reticle can hit a thin straw."));
            EditorGUILayout.PropertyField(_hoverColor, new GUIContent("Hover Color"));
            EditorGUILayout.PropertyField(_hotPoolSize, new GUIContent("Hot Pool Size", "Held straw plus recently thrown ones. ADR cap is 8."));
            EditorGUILayout.PropertyField(_settleSpeed, new GUIContent("Settle Speed"));
            EditorGUILayout.PropertyField(_settleAngular, new GUIContent("Settle Angular"));
            EditorGUILayout.PropertyField(_settleHold, new GUIContent("Settle Hold"));
            EditorGUILayout.PropertyField(_settleTimeout, new GUIContent("Settle Timeout"));
            EditorGUILayout.PropertyField(_onNeedleFound, new GUIContent("On Needle Found"));

            serializedObject.ApplyModifiedProperties();

            var system = (HaystackSystem)target;
            HaystackPreset preset = system.ActivePreset;
            EditorGUILayout.HelpBox(
                "Difficulty presets store radius, height, straw count, and seed — not shape, matrices, or flags.\n" +
                "Generate builds the pile in memory. Play does the same from the selected preset.\n" +
                "Physics Bake optionally settles those poses so the pile looks rested. It does not make the pile walkable.\n" +
                "The player collides with a single PileVolume collider (layer HayPileVolume).\n" +
                "Play mode: look at a reachable straw (it highlights), LMB or RMB picks it up if the hand is empty, click again throws.\n" +
                "One straw is the needle: metal mesh from the start, spawned near the volume axis. Dig to expose it, then grab.",
                MessageType.Info);
            if (preset != null)
            {
                EditorGUILayout.HelpBox(
                    $"Active preset '{preset.DisplayName}': {system.StrawCount} straws, draw calls ≈ {(system.StrawCount + HaystackSystem.DrawBatchSize - 1) / HaystackSystem.DrawBatchSize}.",
                    MessageType.None);
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("Generate", GUILayout.Height(28f)))
                    Generate(system);

                if (GUILayout.Button("Physics Bake", GUILayout.Height(28f)))
                    PhysicsBake(system);
            }
        }

        void DrawDifficultyPopup()
        {
            int count = _presets != null ? _presets.arraySize : 0;
            if (count <= 0)
                return;

            var names = new string[count];
            for (int i = 0; i < count; i++)
            {
                var preset = _presets.GetArrayElementAtIndex(i).objectReferenceValue as HaystackPreset;
                names[i] = preset != null ? preset.DisplayName : $"Empty {i}";
            }

            int current = Mathf.Clamp(_presetIndex.intValue, 0, count - 1);
            int next = EditorGUILayout.Popup("Selected", current, names);
            if (next != current)
                _presetIndex.intValue = next;
        }

        void OnSceneGUI()
        {
            var system = (HaystackSystem)target;
            Transform t = system.transform;
            Handles.color = new Color(0.9f, 0.75f, 0.2f, 0.9f);
            Handles.matrix = t.localToWorldMatrix;

            float diameter = Application.isPlaying ? system.CollisionDiameter : system.Diameter;
            float height = Application.isPlaying ? system.CollisionHeight : system.Height;
            if (height <= 0.001f)
            {
                Handles.matrix = Matrix4x4.identity;
                return;
            }

            float radius = diameter * 0.5f;

            switch (system.Shape)
            {
                case HaystackShape.Box:
                    Handles.DrawWireCube(new Vector3(0f, height * 0.5f, 0f), new Vector3(diameter, height, diameter));
                    break;
                case HaystackShape.Cylinder:
                    DrawCylinderHandles(radius, height);
                    break;
                case HaystackShape.Hemisphere:
                    DrawHemisphereHandles(radius, height);
                    break;
                case HaystackShape.Pyramid:
                    DrawPyramidHandles(radius, height);
                    break;
            }

            Handles.matrix = Matrix4x4.identity;
        }

        static void DrawCylinderHandles(float radius, float height)
        {
            Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius);
            Handles.DrawWireDisc(new Vector3(0f, height, 0f), Vector3.up, radius);
            Handles.DrawLine(new Vector3(radius, 0f, 0f), new Vector3(radius, height, 0f));
            Handles.DrawLine(new Vector3(-radius, 0f, 0f), new Vector3(-radius, height, 0f));
            Handles.DrawLine(new Vector3(0f, 0f, radius), new Vector3(0f, height, radius));
            Handles.DrawLine(new Vector3(0f, 0f, -radius), new Vector3(0f, height, -radius));
        }

        static void DrawHemisphereHandles(float radius, float height)
        {
            Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius);
            Handles.DrawWireArc(Vector3.zero, Vector3.forward, Vector3.right, 180f, radius);
            Handles.DrawWireArc(Vector3.zero, Vector3.right, Vector3.forward, -180f, radius);
            Handles.DrawLine(new Vector3(0f, 0f, 0f), new Vector3(0f, height, 0f));
            if (!Mathf.Approximately(height, radius))
            {
                const int rings = 4;
                for (int i = 1; i < rings; i++)
                {
                    float t = i / (float)rings;
                    float y = height * t;
                    float scale = Mathf.Sqrt(Mathf.Max(0f, 1f - t * t));
                    Handles.DrawWireDisc(new Vector3(0f, y, 0f), Vector3.up, radius * scale);
                }
            }
        }

        static void DrawPyramidHandles(float half, float height)
        {
            Vector3 apex = new Vector3(0f, height, 0f);
            Vector3 a = new Vector3(-half, 0f, -half);
            Vector3 b = new Vector3(half, 0f, -half);
            Vector3 c = new Vector3(half, 0f, half);
            Vector3 d = new Vector3(-half, 0f, half);
            Handles.DrawLine(a, b);
            Handles.DrawLine(b, c);
            Handles.DrawLine(c, d);
            Handles.DrawLine(d, a);
            Handles.DrawLine(a, apex);
            Handles.DrawLine(b, apex);
            Handles.DrawLine(c, apex);
            Handles.DrawLine(d, apex);
        }

        static void Generate(HaystackSystem system)
        {
            Undo.RecordObject(system, "Generate Haystack");
            HaystackAssetUtility.BindDefaultAssets(system);
            Straw[] straws = HaystackLayout.Generate(system.GetLayoutSettings());
            system.ApplyGenerated(straws);
            EditorUtility.SetDirty(system);
            EditorSceneManager.MarkSceneDirty(system.gameObject.scene);
        }

        static void PhysicsBake(HaystackSystem system)
        {
            if (system.StrawCount >= 8000 &&
                !EditorUtility.DisplayDialog(
                    "Haystack Physics Bake",
                    $"Settling {system.StrawCount} straw poses can take a while. This only changes how the pile looks, not player collision. Continue?",
                    "Settle Poses",
                    "Cancel"))
                return;

            Undo.RecordObject(system, "Bake Haystack");
            HaystackAssetUtility.BindDefaultAssets(system);
            HaystackLayoutSettings settings = system.GetLayoutSettings();
            Straw[] initial = HaystackLayout.Generate(settings);
            Straw[] straws = HaystackPhysicsBaker.Bake(settings, initial, system.BakeSeconds);
            system.ApplyGenerated(straws);
            EditorUtility.SetDirty(system);
            EditorSceneManager.MarkSceneDirty(system.gameObject.scene);
        }

        [MenuItem("GameObject/Haystack", false, 12)]
        static void CreateHaystack(MenuCommand command)
        {
            var go = new GameObject("Haystack");
            Undo.RegisterCreatedObjectUndo(go, "Create Haystack");
            var system = go.AddComponent<HaystackSystem>();
            HaystackAssetUtility.BindDefaultAssets(system);
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            Selection.activeGameObject = go;
        }

        [MenuItem("Tools/Haystack/Generate Poses For Selected")]
        static void GenerateSelected()
        {
            var system = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<HaystackSystem>()
                : null;
            if (system == null)
            {
                EditorUtility.DisplayDialog("Haystack", "Select a GameObject with HaystackSystem.", "OK");
                return;
            }

            Generate(system);
        }

        [MenuItem("Tools/Haystack/Bind Default Assets")]
        static void BindDefaultAssetsMenu()
        {
            HaystackAssetUtility.EnsureMesh();
            HaystackAssetUtility.EnsureMaterial();
            HaystackAssetUtility.EnsureNeedleMesh();
            HaystackAssetUtility.EnsureNeedleMaterial();
            var systems = UnityEngine.Object.FindObjectsByType<HaystackSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < systems.Length; i++)
            {
                HaystackAssetUtility.BindDefaultAssets(systems[i]);
                EditorUtility.SetDirty(systems[i]);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
