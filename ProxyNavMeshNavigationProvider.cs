using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SailwindVirtualCrew
{
    internal sealed class ProxyNavMeshNavigationProvider
    {
        private const string Phase = "Phase05";
        private readonly ProxyBoat _proxy;
        private readonly List<NavMeshBuildSource> _sources = new List<NavMeshBuildSource>();
        private NavMeshData _navMeshData;
        private NavMeshDataInstance _navMeshDataInstance;

        internal ProxyNavMeshNavigationProvider(ProxyBoat proxy)
        {
            _proxy = proxy;
        }

        internal bool IsBaked => _navMeshData != null && _navMeshDataInstance.valid;
        internal ProxyBoat Proxy => _proxy;

        internal bool Bake()
        {
            Clear();

            if (_proxy == null || !_proxy.IsValid)
            {
                CrewDebugLog.Warn(Phase, "No proxy boat exists; create one before baking.");
                return false;
            }

            int layerMask = 1 << _proxy.Root.layer;
            var markups = new List<NavMeshBuildMarkup>();
            NavMeshBuilder.CollectSources(
                _proxy.Root.transform,
                layerMask,
                NavMeshCollectGeometry.PhysicsColliders,
                0,
                markups,
                _sources);

            CrewDebugLog.Ok(Phase, "Build sources=" + _sources.Count);
            if (_sources.Count == 0)
            {
                CrewDebugLog.Fail(Phase, "No NavMesh build sources were collected from the proxy.");
                return false;
            }

            var settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = 0.18f;
            settings.agentHeight = 1.7f;
            settings.agentClimb = 0.45f;
            settings.agentSlope = 50f;
            settings.minRegionArea = 0.05f;

            Bounds localBounds = GetExpandedLocalBounds(_proxy);
            _navMeshData = NavMeshBuilder.BuildNavMeshData(
                settings,
                _sources,
                localBounds,
                _proxy.Root.transform.position,
                _proxy.Root.transform.rotation);

            CrewDebugLog.Ok(Phase, "NavMeshData created=" + (_navMeshData != null));
            if (_navMeshData == null)
            {
                CrewDebugLog.Fail(Phase, "NavMeshBuilder.BuildNavMeshData returned null.");
                return false;
            }

            _navMeshDataInstance = NavMesh.AddNavMeshData(_navMeshData);
            CrewDebugLog.Ok(Phase, "NavMeshDataInstance valid=" + _navMeshDataInstance.valid);
            return _navMeshDataInstance.valid;
        }

        internal void Clear()
        {
            if (_navMeshDataInstance.valid)
                _navMeshDataInstance.Remove();

            _navMeshDataInstance = new NavMeshDataInstance();
            _navMeshData = null;
            _sources.Clear();
        }

        internal bool SampleLocal(Vector3 localPosition, float maxDistance, out NavMeshHit hit)
        {
            hit = new NavMeshHit();
            if (_proxy == null || !_proxy.IsValid)
            {
                CrewDebugLog.Warn(Phase, "No proxy boat exists; cannot sample NavMesh.");
                return false;
            }

            Vector3 world = _proxy.Root.transform.TransformPoint(localPosition);
            bool success = NavMesh.SamplePosition(world, out hit, maxDistance, NavMesh.AllAreas);
            CrewDebugLog.Ok(Phase,
                "SamplePosition local=" + Format(localPosition)
                + " success=" + success
                + " hit=" + Format(hit.position));
            return success;
        }

        internal bool TryGetWorldOnNavMesh(Vector3 localPosition, float maxDistance, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;
            if (!SampleLocal(localPosition, maxDistance, out var hit))
                return false;

            worldPosition = hit.position;
            return true;
        }

        internal bool TryGetWorldOnNavMeshQuiet(Vector3 localPosition, float maxDistance, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;
            if (_proxy == null || !_proxy.IsValid)
                return false;

            Vector3 world = _proxy.Root.transform.TransformPoint(localPosition);
            if (!NavMesh.SamplePosition(world, out var hit, maxDistance, NavMesh.AllAreas))
                return false;

            worldPosition = hit.position;
            return true;
        }

        internal Vector3 WorldToProxyLocal(Vector3 worldPosition)
        {
            if (_proxy == null || !_proxy.IsValid)
                return worldPosition;

            return _proxy.Root.transform.InverseTransformPoint(worldPosition);
        }

        internal bool CalculateTestPath(Vector3 fromLocal, Vector3 toLocal)
        {
            if (_proxy == null || !_proxy.IsValid)
            {
                CrewDebugLog.Warn(Phase, "No proxy boat exists; cannot calculate path.");
                return false;
            }

            if (!SampleLocal(fromLocal, 6f, out var fromHit) || !SampleLocal(toLocal, 6f, out var toHit))
            {
                CrewDebugLog.Warn(Phase, "Test path endpoints could not be sampled onto the NavMesh.");
                return false;
            }

            var path = new NavMeshPath();
            bool success = NavMesh.CalculatePath(fromHit.position, toHit.position, NavMesh.AllAreas, path);
            CrewDebugLog.Ok(Phase,
                "Test path success=" + success
                + " status=" + path.status
                + " corners=" + path.corners.Length);
            return success && path.status == NavMeshPathStatus.PathComplete;
        }

        private static Bounds GetExpandedLocalBounds(ProxyBoat proxy)
        {
            Vector3 center = proxy.Root.transform.InverseTransformPoint(proxy.Bounds.center);
            Vector3 size = proxy.Bounds.size;
            size.x += 4f;
            size.y += 4f;
            size.z += 4f;
            return new Bounds(center, size);
        }

        private static string Format(Vector3 value)
        {
            return "(" + value.x.ToString("0.000") + ", " + value.y.ToString("0.000") + ", " + value.z.ToString("0.000") + ")";
        }
    }
}
