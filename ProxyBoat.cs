using UnityEngine;

namespace SailwindVirtualCrew
{
    internal sealed class ProxyBoat
    {
        internal GameObject Root { get; private set; }
        internal Bounds Bounds { get; private set; }
        internal int SourceColliderCount { get; private set; }
        internal int CopiedColliderCount { get; private set; }
        internal int SkippedColliderCount { get; private set; }

        internal ProxyBoat(GameObject root, Bounds bounds, int sourceColliderCount, int copiedColliderCount, int skippedColliderCount)
        {
            Root = root;
            Bounds = bounds;
            SourceColliderCount = sourceColliderCount;
            CopiedColliderCount = copiedColliderCount;
            SkippedColliderCount = skippedColliderCount;
        }

        internal bool IsValid => Root != null;
    }
}
