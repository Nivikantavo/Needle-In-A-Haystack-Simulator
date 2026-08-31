using UnityEngine;

namespace Needle.Haystack
{
    public sealed class HaystackPileVolumeCollider
    {
        public const string ChildName = "PileVolume";

        GameObject _root;
        Mesh _mesh;
        HaystackShape _shape;
        float _diameter;
        float _height;

        public Collider Collider => _root != null ? _root.GetComponent<Collider>() : null;

        public void Sync(Transform parent, HaystackShape shape, float diameter, float height)
        {
            if (parent == null)
                return;

            diameter = Mathf.Max(0.1f, diameter);

            if (height <= 0.001f)
            {
                if (_root != null)
                    _root.SetActive(false);
                _shape = shape;
                _diameter = diameter;
                _height = 0f;
                return;
            }

            height = Mathf.Max(HaystackPileOccupancy.HeightStep, height);

            if (_root != null &&
                _root.activeSelf &&
                _shape == shape &&
                Mathf.Approximately(_diameter, diameter) &&
                Mathf.Approximately(_height, height))
                return;

            EnsureRoot(parent);
            if (!_root.activeSelf)
                _root.SetActive(true);

            if (shape == HaystackShape.Box)
            {
                ApplyBox(diameter, height);
            }
            else if (_root != null &&
                     _shape == shape &&
                     Mathf.Approximately(_diameter, diameter) &&
                     !Mathf.Approximately(_height, height) &&
                     _mesh != null)
            {
                RebuildConvexMesh(shape, diameter, height);
            }
            else
            {
                ClearColliders();
                DestroyMesh();
                _mesh = HaystackVolumeMesh.Create(shape, diameter, height);
                var meshCollider = _root.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = _mesh;
                meshCollider.convex = true;
            }

            int layer = LayerMask.NameToLayer("HayPileVolume");
            _root.layer = layer >= 0 ? layer : 0;
            _root.transform.localPosition = Vector3.zero;
            _root.transform.localRotation = Quaternion.identity;
            _root.transform.localScale = Vector3.one;

            _shape = shape;
            _diameter = diameter;
            _height = height;
        }

        public void Release()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
                _root = null;
            }

            DestroyMesh();
            _diameter = 0f;
            _height = 0f;
        }

        void ApplyBox(float diameter, float height)
        {
            BoxCollider box = _root.GetComponent<BoxCollider>();
            if (box == null)
            {
                ClearColliders();
                DestroyMesh();
                box = _root.AddComponent<BoxCollider>();
            }

            box.center = new Vector3(0f, height * 0.5f, 0f);
            box.size = new Vector3(diameter, height, diameter);
        }

        void RebuildConvexMesh(HaystackShape shape, float diameter, float height)
        {
            DestroyMesh();
            _mesh = HaystackVolumeMesh.Create(shape, diameter, height);
            MeshCollider meshCollider = _root.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                ClearColliders();
                meshCollider = _root.AddComponent<MeshCollider>();
                meshCollider.convex = true;
            }

            meshCollider.sharedMesh = _mesh;
            meshCollider.convex = true;
        }

        void EnsureRoot(Transform parent)
        {
            if (_root != null)
                return;

            Transform existing = parent.Find(ChildName);
            if (existing != null)
            {
                _root = existing.gameObject;
                _root.hideFlags = HideFlags.DontSave;
                return;
            }

            _root = new GameObject(ChildName);
            _root.transform.SetParent(parent, false);
            _root.hideFlags = HideFlags.DontSave;
        }

        void ClearColliders()
        {
            if (_root == null)
                return;

            Collider[] colliders = _root.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
                Object.DestroyImmediate(colliders[i]);
        }

        void DestroyMesh()
        {
            if (_mesh == null)
                return;

            Object.DestroyImmediate(_mesh);
            _mesh = null;
        }
    }
}
