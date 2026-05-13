using System.Collections.Generic;
using UnityEngine;

namespace SailwindVirtualCrew
{
    internal static class ProxyBoatBuilder
    {
        private const string Phase = "Phase04";
        private static readonly Vector3 ProxyOrigin = new Vector3(10000f, -5000f, 10000f);
        private const int ProxyLayer = 2;

        internal static ProxyBoat Create(CrewBoatContext context)
        {
            var root = new GameObject("VC_Proxy_Boat_" + context.SaveSceneIndex);
            root.transform.position = ProxyOrigin;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            root.layer = ProxyLayer;
            root.isStatic = true;

            var transformMap = BuildTransformMirror(context.WalkCol, root.transform);
            var sourceColliders = context.WalkCol.GetComponentsInChildren<Collider>(true);
            int copied = 0;
            int skipped = 0;

            foreach (var source in sourceColliders)
            {
                if (!ShouldCopy(source))
                {
                    skipped++;
                    continue;
                }

                if (!transformMap.TryGetValue(source.transform, out var targetTransform))
                {
                    skipped++;
                    continue;
                }

                if (CopyCollider(source, targetTransform.gameObject))
                    copied++;
                else
                    skipped++;
            }

            SetStaticAndLayer(root.transform);
            Bounds bounds = CalculateBounds(root);

            var proxy = new ProxyBoat(root, bounds, sourceColliders.Length, copied, skipped);
            LogCreated(proxy);
            return proxy;
        }

        internal static void Destroy(ProxyBoat proxy)
        {
            if (proxy != null && proxy.Root)
                Object.Destroy(proxy.Root);
        }

        internal static void LogProxy(ProxyBoat proxy)
        {
            if (proxy == null || !proxy.IsValid)
            {
                CrewDebugLog.Warn(Phase, "No proxy boat exists.");
                return;
            }

            CrewDebugLog.Ok(Phase, "Proxy root='" + proxy.Root.name + "'");
            CrewDebugLog.Ok(Phase, "Proxy origin=" + Format(proxy.Root.transform.position));
            CrewDebugLog.Ok(Phase, "Source colliders=" + proxy.SourceColliderCount + ", copied=" + proxy.CopiedColliderCount + ", skipped=" + proxy.SkippedColliderCount);
            CrewDebugLog.Ok(Phase, "Proxy bounds center=" + Format(proxy.Bounds.center) + ", size=" + Format(proxy.Bounds.size));
        }

        internal static void ShowProxyMarkers(ProxyBoat proxy)
        {
            if (proxy == null || !proxy.IsValid)
            {
                CrewDebugLog.Warn(Phase, "No proxy boat exists; create one before showing proxy markers.");
                return;
            }

            CreateMarker(proxy.Root.transform, "VC_Proxy_Marker_Origin", Vector3.zero, Color.magenta, 1.5f);
            Vector3 localCenter = proxy.Root.transform.InverseTransformPoint(proxy.Bounds.center);
            CreateMarker(proxy.Root.transform, "VC_Proxy_Marker_BoundsCenter", localCenter, Color.yellow, 1f);
            CrewDebugLog.Ok(Phase, "Created proxy markers at origin and bounds center.");
        }

        private static Dictionary<Transform, Transform> BuildTransformMirror(Transform sourceRoot, Transform proxyRoot)
        {
            var map = new Dictionary<Transform, Transform>();
            map[sourceRoot] = proxyRoot;

            var sourceTransforms = sourceRoot.GetComponentsInChildren<Transform>(true);
            foreach (var source in sourceTransforms)
            {
                if (source == sourceRoot)
                    continue;

                if (!source.gameObject.activeInHierarchy)
                    continue;

                if (!map.TryGetValue(source.parent, out var targetParent))
                    continue;

                var copy = new GameObject("VC_Proxy_" + source.name);
                copy.transform.SetParent(targetParent, false);
                copy.transform.localPosition = source.localPosition;
                copy.transform.localRotation = source.localRotation;
                copy.transform.localScale = source.localScale;
                copy.layer = ProxyLayer;
                copy.isStatic = true;
                map[source] = copy.transform;
            }

            return map;
        }

        private static bool ShouldCopy(Collider source)
        {
            if (!source || !source.enabled || source.isTrigger || !source.gameObject.activeInHierarchy)
                return false;

            // Skip colliders owned by ship items so they don't carve holes in the NavMesh.
            // ItemRigidbody.EnterBoat() reparents the physics body directly to walkCol, so
            // there is no ShipItem in its parent chain — check both types.
            if (source.GetComponentInParent<ShipItem>() != null)
                return false;
            if (source.GetComponentInParent<ItemRigidbody>() != null)
                return false;

            return true;
        }

        private static bool CopyCollider(Collider source, GameObject target)
        {
            if (source is BoxCollider box)
            {
                var copy = target.AddComponent<BoxCollider>();
                copy.center = box.center;
                copy.size = box.size;
                copy.isTrigger = false;
                return true;
            }

            if (source is CapsuleCollider capsule)
            {
                var copy = target.AddComponent<CapsuleCollider>();
                copy.center = capsule.center;
                copy.radius = capsule.radius;
                copy.height = capsule.height;
                copy.direction = capsule.direction;
                copy.isTrigger = false;
                return true;
            }

            if (source is SphereCollider sphere)
            {
                var copy = target.AddComponent<SphereCollider>();
                copy.center = sphere.center;
                copy.radius = sphere.radius;
                copy.isTrigger = false;
                return true;
            }

            if (source is MeshCollider mesh)
            {
                if (!mesh.sharedMesh)
                    return false;

                var copy = target.AddComponent<MeshCollider>();
                copy.sharedMesh = mesh.sharedMesh;
                copy.convex = mesh.convex;
                copy.isTrigger = false;
                return true;
            }

            return false;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            if (colliders.Length == 0)
                return new Bounds(root.transform.position, Vector3.zero);

            var bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                bounds.Encapsulate(colliders[i].bounds);
            return bounds;
        }

        private static void SetStaticAndLayer(Transform transform)
        {
            transform.gameObject.layer = ProxyLayer;
            transform.gameObject.isStatic = true;
            foreach (Transform child in transform)
                SetStaticAndLayer(child);
        }

        private static void CreateMarker(Transform parent, string name, Vector3 localPosition, Color color, float size)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;
            marker.transform.localRotation = Quaternion.identity;
            marker.transform.localScale = Vector3.one * size;
            marker.layer = ProxyLayer;

            var collider = marker.GetComponent<Collider>();
            if (collider)
                Object.Destroy(collider);

            var renderer = marker.GetComponent<Renderer>();
            if (renderer)
                renderer.material.color = color;
        }

        private static void LogCreated(ProxyBoat proxy)
        {
            CrewDebugLog.Ok(Phase, "Created proxy root='" + proxy.Root.name + "'");
            CrewDebugLog.Ok(Phase, "Proxy origin=" + Format(proxy.Root.transform.position));
            CrewDebugLog.Ok(Phase, "Source colliders copied=" + proxy.CopiedColliderCount + ", skipped=" + proxy.SkippedColliderCount + ", total=" + proxy.SourceColliderCount);
            CrewDebugLog.Ok(Phase, "Proxy bounds center=" + Format(proxy.Bounds.center) + ", size=" + Format(proxy.Bounds.size));
        }

        private static string Format(Vector3 value)
        {
            return "(" + value.x.ToString("0.000") + ", " + value.y.ToString("0.000") + ", " + value.z.ToString("0.000") + ")";
        }
    }
}
