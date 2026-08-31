using System;
using System.Collections.Generic;
using Needle.Haystack;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Needle.Haystack.Editor
{
    public static class HaystackPhysicsBaker
    {
        const float StepDt = 0.02f;
        const int DepenetrateSteps = 6;
        const HideFlags BakeHideFlags = HideFlags.HideAndDontSave;

        public static Straw[] Bake(HaystackLayoutSettings settings, float bakeSeconds)
        {
            Straw[] initial = HaystackLayout.Generate(settings);
            return Bake(settings, initial, bakeSeconds);
        }

        public static Straw[] Bake(HaystackLayoutSettings settings, Straw[] initial, float bakeSeconds)
        {
            if (initial == null || initial.Length == 0)
                return Array.Empty<Straw>();

            bakeSeconds = Mathf.Max(0.5f, bakeSeconds);
            int count = initial.Length;
            int batchSize = ChooseBatchSize(count);
            int foundationCount = Mathf.Min(count, Mathf.Max(24, batchSize));
            int remaining = Mathf.Max(0, count - foundationCount);
            int batchCount = Mathf.Max(1, Mathf.CeilToInt(remaining / (float)batchSize));
            int settleSteps = Mathf.Clamp(Mathf.RoundToInt(12f + bakeSeconds * 4f), 16, 48);

            Scene bakeScene = default;
            bool previewScene = false;
            SimulationMode previousSimulationMode = Physics.simulationMode;
            int previousSolverIterations = Physics.defaultSolverIterations;
            int previousSolverVelocityIterations = Physics.defaultSolverVelocityIterations;
            float previousMaxDepenetration = Physics.defaultMaxDepenetrationVelocity;
            Rigidbody[] frozenBodies = null;
            bool[] frozenKinematic = null;
            bool[] ignoredLayers = null;
            var baked = new Straw[count];
            bool wrote = false;
            try
            {
                bakeScene = EditorSceneManager.NewPreviewScene();
                previewScene = bakeScene.IsValid();
                PhysicsScene physicsScene = bakeScene.GetPhysicsScene();

                bool isolated = physicsScene.IsValid() && physicsScene != Physics.defaultPhysicsScene;
                if (!isolated)
                {
                    physicsScene = Physics.defaultPhysicsScene;
                    Physics.simulationMode = SimulationMode.Script;
                    FreezeGameplayRigidbodies(bakeScene, out frozenBodies, out frozenKinematic);
                }

                IsolateBakeLayer(out ignoredLayers);
                Physics.defaultSolverIterations = 12;
                Physics.defaultSolverVelocityIterations = 4;
                Physics.defaultMaxDepenetrationVelocity = 2f;

                PhysicsMaterial strawMaterial = CreateBakeMaterial();
                BuildContainer(bakeScene, settings, strawMaterial);

                var bodies = new Rigidbody[count];
                var colliders = new CapsuleCollider[count];
                for (int i = 0; i < count; i++)
                {
                    if (i % 256 == 0 &&
                        EditorUtility.DisplayCancelableProgressBar(
                            "Haystack Physics Bake",
                            $"Spawning {i}/{count}",
                            i / (float)count * 0.08f))
                        return initial;

                    bodies[i] = CreateStrawBody(bakeScene, initial[i], settings, strawMaterial);
                    colliders[i] = bodies[i].GetComponent<CapsuleCollider>();
                }

                int[] order = SortedIndicesByHeight(bodies);

                for (int i = 0; i < foundationCount; i++)
                    EnableCollider(colliders[order[i]], true);

                Physics.SyncTransforms();
                physicsScene.Simulate(StepDt);

                int cursor = foundationCount;
                int batchIndex = 0;
                while (cursor < count)
                {
                    int end = Mathf.Min(cursor + batchSize, count);
                    float progress = 0.08f + 0.9f * (batchIndex / (float)batchCount);
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Haystack Physics Bake",
                            $"Settling layer {batchIndex + 1}/{batchCount} ({end}/{count})",
                            progress))
                        return initial;

                    for (int i = cursor; i < end; i++)
                        ActivateDynamic(bodies[order[i]], colliders[order[i]], useGravity: false);

                    Physics.SyncTransforms();
                    SimulateSteps(physicsScene, DepenetrateSteps);

                    for (int i = cursor; i < end; i++)
                    {
                        Rigidbody body = bodies[order[i]];
                        body.useGravity = true;
                        body.WakeUp();
                    }

                    SimulateSteps(physicsScene, settleSteps);

                    for (int i = cursor; i < end; i++)
                        FreezeKinematic(bodies[order[i]]);

                    cursor = end;
                    batchIndex++;
                }

                Vector3 scale = new Vector3(settings.StrawRadius * 2f, settings.StrawLength, settings.StrawRadius * 2f);
                for (int i = 0; i < count; i++)
                {
                    Rigidbody body = bodies[i];
                    Vector3 position;
                    Quaternion rotation;
                    if (body != null)
                    {
                        position = body.position;
                        rotation = body.rotation;
                    }
                    else
                    {
                        position = initial[i].LocalToWorld.GetPosition();
                        rotation = initial[i].LocalToWorld.rotation;
                    }

                    if (!IsFinite(position) || !IsFinite(rotation))
                    {
                        position = initial[i].LocalToWorld.GetPosition();
                        rotation = initial[i].LocalToWorld.rotation;
                    }

                    position = ClampToVolume(settings, position);
                    baked[i] = new Straw
                    {
                        LocalToWorld = Matrix4x4.TRS(position, rotation, scale),
                        Flags = (byte)(initial[i].Flags & ~(byte)StrawFlags.IsNeedle),
                        Variant = initial[i].Variant
                    };
                }

                StrawNeedle.StampInDepth(baked, settings);
                wrote = true;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                RestoreBakeLayer(ignoredLayers);
                RestoreGameplayRigidbodies(frozenBodies, frozenKinematic);
                Physics.simulationMode = previousSimulationMode;
                Physics.defaultSolverIterations = previousSolverIterations;
                Physics.defaultSolverVelocityIterations = previousSolverVelocityIterations;
                Physics.defaultMaxDepenetrationVelocity = previousMaxDepenetration;
                if (previewScene && bakeScene.IsValid())
                    EditorSceneManager.ClosePreviewScene(bakeScene);
            }

            return wrote ? baked : initial;
        }

        static int ChooseBatchSize(int count)
        {
            if (count <= 400)
                return 32;
            if (count <= 2000)
                return 48;
            if (count <= 8000)
                return 80;
            return 128;
        }

        static int[] SortedIndicesByHeight(Rigidbody[] bodies)
        {
            var order = new int[bodies.Length];
            for (int i = 0; i < order.Length; i++)
                order[i] = i;

            Array.Sort(order, (a, b) =>
            {
                float ya = bodies[a] != null ? bodies[a].position.y : 0f;
                float yb = bodies[b] != null ? bodies[b].position.y : 0f;
                int cmp = ya.CompareTo(yb);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });
            return order;
        }

        static void SimulateSteps(PhysicsScene physicsScene, int steps)
        {
            for (int i = 0; i < steps; i++)
                physicsScene.Simulate(StepDt);
        }

        static void EnableCollider(CapsuleCollider collider, bool enabled)
        {
            if (collider != null)
                collider.enabled = enabled;
        }

        static void ActivateDynamic(Rigidbody body, CapsuleCollider collider, bool useGravity)
        {
            if (body == null)
                return;

            EnableCollider(collider, true);
            body.isKinematic = false;
            body.useGravity = useGravity;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.WakeUp();
        }

        static void FreezeKinematic(Rigidbody body)
        {
            if (body == null)
                return;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.useGravity = false;
            body.isKinematic = true;
        }

        static Vector3 ClampToVolume(HaystackLayoutSettings settings, Vector3 position)
        {
            float radius = Mathf.Max(0.001f, settings.StrawRadius);
            position.y = Mathf.Clamp(position.y, radius * 0.5f, settings.Height + settings.StrawLength * 0.5f);

            if (HaystackVolume.Contains(settings.Shape, settings.Diameter * 1.2f, settings.Height * 1.35f, position))
                return position;

            for (int pull = 0; pull < 8; pull++)
            {
                position.x *= 0.7f;
                position.z *= 0.7f;
                if (HaystackVolume.Contains(settings.Shape, settings.Diameter, settings.Height, position))
                    return position;
            }

            position.x = 0f;
            position.z = 0f;
            position.y = Mathf.Clamp(position.y, radius, settings.Height);
            return position;
        }

        static bool IsFinite(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                     float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        }

        static bool IsFinite(Quaternion q)
        {
            return !(float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w) ||
                     float.IsInfinity(q.x) || float.IsInfinity(q.y) || float.IsInfinity(q.z) || float.IsInfinity(q.w));
        }

        static int BakeLayer()
        {
            int layer = LayerMask.NameToLayer("HayHot");
            return layer >= 0 ? layer : 0;
        }

        static void IsolateBakeLayer(out bool[] previousIgnore)
        {
            int bakeLayer = BakeLayer();
            previousIgnore = new bool[32];
            if (bakeLayer <= 0)
                return;

            for (int i = 0; i < 32; i++)
            {
                previousIgnore[i] = Physics.GetIgnoreLayerCollision(bakeLayer, i);
                Physics.IgnoreLayerCollision(bakeLayer, i, i != bakeLayer);
            }
        }

        static void RestoreBakeLayer(bool[] previousIgnore)
        {
            int bakeLayer = BakeLayer();
            if (previousIgnore == null || bakeLayer <= 0)
                return;

            for (int i = 0; i < previousIgnore.Length && i < 32; i++)
                Physics.IgnoreLayerCollision(bakeLayer, i, previousIgnore[i]);
        }

        static void FreezeGameplayRigidbodies(Scene exclude, out Rigidbody[] bodies, out bool[] wasKinematic)
        {
            var list = new List<Rigidbody>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded || scene == exclude)
                    continue;

                GameObject[] roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                    list.AddRange(roots[r].GetComponentsInChildren<Rigidbody>(true));
            }

            bodies = list.ToArray();
            wasKinematic = new bool[bodies.Length];
            for (int i = 0; i < bodies.Length; i++)
            {
                if (bodies[i] == null)
                    continue;
                wasKinematic[i] = bodies[i].isKinematic;
                bodies[i].isKinematic = true;
            }
        }

        static void RestoreGameplayRigidbodies(Rigidbody[] bodies, bool[] wasKinematic)
        {
            if (bodies == null || wasKinematic == null)
                return;

            int n = Mathf.Min(bodies.Length, wasKinematic.Length);
            for (int i = 0; i < n; i++)
            {
                if (bodies[i] != null)
                    bodies[i].isKinematic = wasKinematic[i];
            }
        }

        static PhysicsMaterial CreateBakeMaterial()
        {
            return new PhysicsMaterial("HaystackBake")
            {
                bounciness = 0.02f,
                staticFriction = 0.9f,
                dynamicFriction = 0.75f,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
        }

        static Rigidbody CreateStrawBody(Scene scene, Straw straw, HaystackLayoutSettings settings, PhysicsMaterial material)
        {
            var go = new GameObject("BakeStraw");
            go.hideFlags = BakeHideFlags;
            go.layer = BakeLayer();
            SceneManager.MoveGameObjectToScene(go, scene);

            Matrix4x4 m = straw.LocalToWorld;
            go.transform.SetPositionAndRotation(m.GetPosition(), m.rotation);

            float radius = Mathf.Max(0.001f, settings.StrawRadius);
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.radius = radius;
            capsule.height = Mathf.Max(settings.StrawLength, radius * 2f);
            capsule.sharedMaterial = material;
            capsule.contactOffset = Mathf.Clamp(radius * 0.12f, 0.001f, 0.003f);
            capsule.enabled = false;

            var body = go.AddComponent<Rigidbody>();
            body.mass = 0.02f;
            body.linearDamping = 0.8f;
            body.angularDamping = 1.2f;
            body.interpolation = RigidbodyInterpolation.None;
            body.sleepThreshold = 0.08f;
            body.solverIterations = 8;
            body.solverVelocityIterations = 4;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.useGravity = false;
            body.isKinematic = true;
            return body;
        }

        static void BuildContainer(Scene scene, HaystackLayoutSettings settings, PhysicsMaterial material)
        {
            float diameter = settings.Diameter;
            float height = settings.Height;
            float half = diameter * 0.5f;

            CreateStaticBox(scene, new Vector3(0f, -0.12f, 0f), Quaternion.identity, new Vector3(diameter + 1.5f, 0.24f, diameter + 1.5f), material);

            switch (settings.Shape)
            {
                case HaystackShape.Box:
                    CreateBoxWalls(scene, half, height, material);
                    CreateStaticBox(scene, new Vector3(0f, height + 0.1f, 0f), Quaternion.identity, new Vector3(diameter + 0.4f, 0.2f, diameter + 0.4f), material);
                    break;
                case HaystackShape.Cylinder:
                    CreateCylinderWalls(scene, half, height, material);
                    CreateStaticBox(scene, new Vector3(0f, height + 0.1f, 0f), Quaternion.identity, new Vector3(diameter + 0.4f, 0.2f, diameter + 0.4f), material);
                    break;
                case HaystackShape.Pyramid:
                    CreateMeshCollider(scene, BuildPyramidShell(half, height), material);
                    break;
                case HaystackShape.Hemisphere:
                    CreateMeshCollider(scene, BuildHemisphereShell(half, height), material);
                    break;
            }
        }

        static void CreateBoxWalls(Scene scene, float half, float height, PhysicsMaterial material)
        {
            const float thickness = 0.2f;
            float wallHeight = height + 0.4f;
            float y = wallHeight * 0.5f - 0.1f;
            CreateStaticBox(scene, new Vector3(half + thickness * 0.5f, y, 0f), Quaternion.identity, new Vector3(thickness, wallHeight, half * 2f + thickness * 2f), material);
            CreateStaticBox(scene, new Vector3(-half - thickness * 0.5f, y, 0f), Quaternion.identity, new Vector3(thickness, wallHeight, half * 2f + thickness * 2f), material);
            CreateStaticBox(scene, new Vector3(0f, y, half + thickness * 0.5f), Quaternion.identity, new Vector3(half * 2f, wallHeight, thickness), material);
            CreateStaticBox(scene, new Vector3(0f, y, -half - thickness * 0.5f), Quaternion.identity, new Vector3(half * 2f, wallHeight, thickness), material);
        }

        static void CreateCylinderWalls(Scene scene, float radius, float height, PhysicsMaterial material)
        {
            const int segments = 16;
            const float thickness = 0.18f;
            float wallLength = 2f * radius * Mathf.Tan(Mathf.PI / segments) + 0.08f;
            float wallHeight = height + 0.4f;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                Vector3 outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 pos = outward * (radius + thickness * 0.5f) + new Vector3(0f, wallHeight * 0.5f - 0.1f, 0f);
                Quaternion rot = Quaternion.LookRotation(outward);
                CreateStaticBox(scene, pos, rot, new Vector3(wallLength, wallHeight, thickness), material);
            }
        }

        static void CreateStaticBox(Scene scene, Vector3 position, Quaternion rotation, Vector3 size, PhysicsMaterial material)
        {
            var go = new GameObject("BakeWall");
            go.hideFlags = BakeHideFlags;
            go.layer = BakeLayer();
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetPositionAndRotation(position, rotation);
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.sharedMaterial = material;
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.interpolation = RigidbodyInterpolation.None;
        }

        static void CreateMeshCollider(Scene scene, Mesh mesh, PhysicsMaterial material)
        {
            var go = new GameObject("BakeShell");
            go.hideFlags = BakeHideFlags;
            go.layer = BakeLayer();
            SceneManager.MoveGameObjectToScene(go, scene);
            var collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = false;
            collider.sharedMaterial = material;
        }

        static Mesh BuildPyramidShell(float half, float height)
        {
            Vector3 apex = new Vector3(0f, height, 0f);
            Vector3 a = new Vector3(-half, 0f, -half);
            Vector3 b = new Vector3(half, 0f, -half);
            Vector3 c = new Vector3(half, 0f, half);
            Vector3 d = new Vector3(-half, 0f, half);

            var mesh = new Mesh { name = "PyramidShell" };
            mesh.vertices = new[] { apex, a, b, c, d };
            mesh.triangles = TwoSided(new[]
            {
                0, 2, 1,
                0, 3, 2,
                0, 4, 3,
                0, 1, 4
            });
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        static Mesh BuildHemisphereShell(float radius, float height)
        {
            const int lon = 20;
            const int lat = 10;
            var vertices = new List<Vector3>((lat + 1) * (lon + 1));
            var triangles = new List<int>();

            for (int y = 0; y <= lat; y++)
            {
                float v = y / (float)lat;
                float phi = v * Mathf.PI * 0.5f;
                float ringY = height * Mathf.Cos(phi);
                float ringR = radius * Mathf.Sin(phi);
                for (int x = 0; x <= lon; x++)
                {
                    float u = x / (float)lon;
                    float theta = u * Mathf.PI * 2f;
                    vertices.Add(new Vector3(Mathf.Cos(theta) * ringR, ringY, Mathf.Sin(theta) * ringR));
                }
            }

            for (int y = 0; y < lat; y++)
            {
                for (int x = 0; x < lon; x++)
                {
                    int i0 = y * (lon + 1) + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + (lon + 1);
                    int i3 = i2 + 1;
                    triangles.Add(i0);
                    triangles.Add(i2);
                    triangles.Add(i1);
                    triangles.Add(i1);
                    triangles.Add(i2);
                    triangles.Add(i3);
                }
            }

            var mesh = new Mesh { name = "HemisphereShell" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(TwoSided(triangles.ToArray()), 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        static int[] TwoSided(int[] triangles)
        {
            var twoSided = new int[triangles.Length * 2];
            Array.Copy(triangles, twoSided, triangles.Length);
            int offset = triangles.Length;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                twoSided[offset + i] = triangles[i];
                twoSided[offset + i + 1] = triangles[i + 2];
                twoSided[offset + i + 2] = triangles[i + 1];
            }

            return twoSided;
        }
    }
}
