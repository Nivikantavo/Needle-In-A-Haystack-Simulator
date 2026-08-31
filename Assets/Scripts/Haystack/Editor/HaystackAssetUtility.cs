using Needle.Haystack;
using UnityEditor;
using UnityEngine;

namespace Needle.Haystack.Editor
{
    public static class HaystackAssetUtility
    {
        public const string ArtFolder = "Assets/Art/Haystack";
        public const string DataFolder = "Assets/Data/Haystack";
        public const string MeshPath = ArtFolder + "/Straw.mesh";
        public const string MaterialPath = ArtFolder + "/Straw.mat";
        public const string NeedleMeshPath = ArtFolder + "/Needle.mesh";
        public const string NeedleMaterialPath = ArtFolder + "/Needle.mat";
        public const string DefaultPosePath = DataFolder + "/DefaultHaystack.asset";

        public static void EnsureFolders()
        {
            EnsureFolder("Assets/Art", "Haystack");
            EnsureFolder("Assets/Data", "Haystack");
        }

        public static Mesh EnsureMesh()
        {
            EnsureFolders();
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (mesh != null)
                return mesh;

            mesh = StrawMeshFactory.CreateUnitCylinder();
            AssetDatabase.CreateAsset(mesh, MeshPath);
            return mesh;
        }

        public static Material EnsureMaterial()
        {
            EnsureFolders();
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                material.enableInstancing = true;
                return material;
            }

            Shader shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Diffuse");

            material = new Material(shader)
            {
                name = "Straw",
                color = new Color(0.76f, 0.62f, 0.28f, 1f),
                enableInstancing = true
            };
            material.SetFloat("_Glossiness", 0.08f);
            material.SetFloat("_Metallic", 0f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        public static Mesh EnsureNeedleMesh()
        {
            EnsureFolders();
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(NeedleMeshPath);
            if (mesh != null)
                return mesh;

            mesh = NeedleMeshFactory.Create();
            AssetDatabase.CreateAsset(mesh, NeedleMeshPath);
            return mesh;
        }

        public static Material EnsureNeedleMaterial()
        {
            EnsureFolders();
            var material = AssetDatabase.LoadAssetAtPath<Material>(NeedleMaterialPath);
            if (material != null)
                return material;

            Shader shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Diffuse");

            material = new Material(shader)
            {
                name = "Needle",
                color = new Color(0.72f, 0.74f, 0.78f, 1f),
                enableInstancing = false
            };
            material.SetFloat("_Glossiness", 0.62f);
            material.SetFloat("_Metallic", 0.85f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            AssetDatabase.CreateAsset(material, NeedleMaterialPath);
            return material;
        }

        public static HaystackPoseAsset EnsurePoseAsset()
        {
            EnsureFolders();
            var asset = AssetDatabase.LoadAssetAtPath<HaystackPoseAsset>(DefaultPosePath);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<HaystackPoseAsset>();
            AssetDatabase.CreateAsset(asset, DefaultPosePath);
            return asset;
        }

        public static HaystackPreset[] EnsureDifficultyPresets()
        {
            EnsureFolders();
            EnsureFolder(DataFolder, "Presets");
            return new[]
            {
                EnsurePreset("Easy.asset", "Easy", 1.25f, 1.2f, 2500, 1),
                EnsurePreset("Medium.asset", "Medium", 2f, 1.8f, 8000, 2),
                EnsurePreset("Hard.asset", "Hard", 3f, 2.5f, 18000, 3),
                EnsurePreset("Expert.asset", "Expert", 3.5f, 3.2f, 25000, 4)
            };
        }

        static HaystackPreset EnsurePreset(
            string fileName,
            string displayName,
            float radius,
            float height,
            int strawCount,
            int seed)
        {
            string path = DataFolder + "/Presets/" + fileName;
            var asset = AssetDatabase.LoadAssetAtPath<HaystackPreset>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<HaystackPreset>();
            AssetDatabase.CreateAsset(asset, path);
            var so = new SerializedObject(asset);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_radius").floatValue = radius;
            so.FindProperty("_height").floatValue = height;
            so.FindProperty("_strawCount").intValue = strawCount;
            so.FindProperty("_seed").intValue = seed;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        public static void BindDefaultAssets(HaystackSystem system)
        {
            if (system.StrawMesh == null)
                system.StrawMesh = EnsureMesh();
            if (system.StrawMaterial == null)
                system.StrawMaterial = EnsureMaterial();
            if (system.NeedleMesh == null)
                system.NeedleMesh = EnsureNeedleMesh();
            if (system.NeedleMaterial == null)
                system.NeedleMaterial = EnsureNeedleMaterial();

            HaystackPreset[] presets = EnsureDifficultyPresets();
            if (system.Presets == null || system.Presets.Length == 0)
            {
                system.Presets = presets;
                system.SelectPreset(1);
            }
        }

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent))
            {
                int slash = parent.LastIndexOf('/');
                string grand = parent.Substring(0, slash);
                string parentName = parent.Substring(slash + 1);
                AssetDatabase.CreateFolder(grand, parentName);
            }

            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
