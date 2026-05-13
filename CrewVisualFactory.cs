using UnityEngine;

namespace SailwindVirtualCrew
{
    internal static class CrewVisualFactory
    {
        private const string Phase = "Phase03";
        private static readonly Vector3 NpcBodyScale = Vector3.one;
        private static GameObject _cachedNpcVisualTemplate;

        internal static CrewAgent SpawnTestCrewVisual(CrewBoatContext context, Vector3 localPosition, Quaternion localRotation, string id = "test-crew-001")
        {
            var root = new GameObject("VC_VisualCrew_" + id);
            root.transform.SetParent(context.WorldBoat, false);
            root.transform.localPosition = localPosition;
            root.transform.localRotation = localRotation;
            root.transform.localScale = Vector3.one;

            if (!TryCreateNpcBody(root.transform))
                CreateBody(root.transform);

            var agent = new CrewAgent(id, root);
            CrewDebugLog.Ok(Phase, "Spawned visual crew id='" + agent.Id + "'");
            CrewDebugLog.Ok(Phase, "Parent worldBoat='" + context.WorldBoat.name + "'");
            LogPose(agent);
            return agent;
        }

        internal static void LogPose(CrewAgent agent)
        {
            if (agent == null || !agent.VisualRoot)
            {
                CrewDebugLog.Warn(Phase, "No test crew visual exists.");
                return;
            }

            var t = agent.VisualRoot.transform;
            CrewDebugLog.Ok(Phase,
                "Local pose=pos" + Format(t.localPosition)
                + ", rot" + Format(t.localEulerAngles)
                + ", parent='" + (t.parent ? t.parent.name : "null") + "'");
        }

        private static void CreateBody(Transform root)
        {
            var torso = CreatePrimitive("Torso", PrimitiveType.Capsule, root, new Vector3(0f, 0.9f, 0f), Quaternion.identity, new Vector3(0.35f, 0.75f, 0.35f), new Color(0.12f, 0.32f, 0.75f));
            var head = CreatePrimitive("Head", PrimitiveType.Sphere, root, new Vector3(0f, 1.62f, 0f), Quaternion.identity, new Vector3(0.34f, 0.34f, 0.34f), new Color(0.82f, 0.66f, 0.48f));
            var leftArm = CreatePrimitive("LeftArm", PrimitiveType.Capsule, root, new Vector3(-0.34f, 1.02f, 0f), Quaternion.Euler(0f, 0f, 16f), new Vector3(0.14f, 0.45f, 0.14f), new Color(0.12f, 0.32f, 0.75f));
            var rightArm = CreatePrimitive("RightArm", PrimitiveType.Capsule, root, new Vector3(0.34f, 1.02f, 0f), Quaternion.Euler(0f, 0f, -16f), new Vector3(0.14f, 0.45f, 0.14f), new Color(0.12f, 0.32f, 0.75f));
            var leftLeg = CreatePrimitive("LeftLeg", PrimitiveType.Capsule, root, new Vector3(-0.13f, 0.28f, 0f), Quaternion.identity, new Vector3(0.16f, 0.35f, 0.16f), new Color(0.12f, 0.12f, 0.16f));
            var rightLeg = CreatePrimitive("RightLeg", PrimitiveType.Capsule, root, new Vector3(0.13f, 0.28f, 0f), Quaternion.identity, new Vector3(0.16f, 0.35f, 0.16f), new Color(0.12f, 0.12f, 0.16f));

            torso.name = "VC_VisualCrew_Torso";
            head.name = "VC_VisualCrew_Head";
            leftArm.name = "VC_VisualCrew_LeftArm";
            rightArm.name = "VC_VisualCrew_RightArm";
            leftLeg.name = "VC_VisualCrew_LeftLeg";
            rightLeg.name = "VC_VisualCrew_RightLeg";
        }

        private static bool TryCreateNpcBody(Transform root)
        {
            var template = GetNpcVisualTemplate();
            if (!template)
                return false;

            var body = Object.Instantiate(template);
            body.name = "VC_VisualCrew_NpcBody";
            body.transform.SetParent(root, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = NpcBodyScale;

            StripGameplayComponents(body);
            EnableRenderers(body);
            CrewDebugLog.Ok(Phase, "Using in-game NPC visual template='" + template.name + "'");
            return true;
        }

        private static GameObject GetNpcVisualTemplate()
        {
            if (_cachedNpcVisualTemplate && HasVisibleRenderers(_cachedNpcVisualTemplate))
                return _cachedNpcVisualTemplate;

            foreach (var anims in Object.FindObjectsOfType<NPCAnimations>())
            {
                if (!anims || !anims.gameObject.activeInHierarchy || IsVirtualCrewObject(anims.transform))
                    continue;

                if (HasVisibleRenderers(anims.gameObject))
                {
                    _cachedNpcVisualTemplate = anims.gameObject;
                    return _cachedNpcVisualTemplate;
                }
            }

            foreach (var dude in Object.FindObjectsOfType<PortDude>())
            {
                var visualRoot = FindRendererRoot(dude ? dude.transform : null);
                if (visualRoot)
                {
                    _cachedNpcVisualTemplate = visualRoot.gameObject;
                    return _cachedNpcVisualTemplate;
                }
            }

            foreach (var dude in Object.FindObjectsOfType<CargoTransportDude>())
            {
                var visualRoot = FindRendererRoot(dude ? dude.transform : null);
                if (visualRoot)
                {
                    _cachedNpcVisualTemplate = visualRoot.gameObject;
                    return _cachedNpcVisualTemplate;
                }
            }

            CrewDebugLog.Warn(Phase, "No in-game NPC visual template found; using fallback mannequin.");
            return null;
        }

        private static Transform FindRendererRoot(Transform candidate)
        {
            if (!candidate || IsVirtualCrewObject(candidate))
                return null;

            foreach (var renderer in candidate.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer || !renderer.gameObject.activeInHierarchy || !renderer.enabled)
                    continue;

                return renderer.transform.parent ? renderer.transform.parent : renderer.transform;
            }

            return null;
        }

        private static bool HasVisibleRenderers(GameObject candidate)
        {
            if (!candidate || IsVirtualCrewObject(candidate.transform))
                return false;

            foreach (var renderer in candidate.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer && renderer.enabled && renderer.gameObject.activeInHierarchy)
                    return true;
            }

            return false;
        }

        private static bool IsVirtualCrewObject(Transform transform)
        {
            while (transform)
            {
                if (transform.name.StartsWith("VC_"))
                    return true;

                transform = transform.parent;
            }

            return false;
        }

        private static void StripGameplayComponents(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);

            foreach (var rigidbody in root.GetComponentsInChildren<Rigidbody>(true))
                Object.Destroy(rigidbody);

            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                Object.Destroy(behaviour);
        }

        private static void EnableRenderers(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = true;
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType primitiveType, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Color color)
        {
            var obj = GameObject.CreatePrimitive(primitiveType);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = localRotation;
            obj.transform.localScale = localScale;

            var collider = obj.GetComponent<Collider>();
            if (collider)
                Object.Destroy(collider);

            var renderer = obj.GetComponent<Renderer>();
            if (renderer)
                renderer.material.color = color;

            return obj;
        }

        private static string Format(Vector3 value)
        {
            return "(" + value.x.ToString("0.000") + ", " + value.y.ToString("0.000") + ", " + value.z.ToString("0.000") + ")";
        }
    }
}
