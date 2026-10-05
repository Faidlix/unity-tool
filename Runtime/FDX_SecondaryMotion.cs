using System;
using System.Collections.Generic;
using UnityEngine;

namespace Faidlix.UnityTools
{
    [Serializable]
    public sealed class FDX_MotionSettings
    {
        [Min(0f)] public float inertia = 1.2f;
        [Min(0f)] public float spring = 38f;
        [Min(0f)] public float damping = 8f;
        [Range(0f, 180f)] public float maxAngle = 35f;
        [Min(0f)] public float gravityStrength = 1f;
        public Vector3 gravityDirection = Vector3.down;
        public Vector3 constantWind = Vector3.zero;
        [Min(0f)] public float windMultiplier = 1f;
        [Min(0.001f)] public float teleportDistance = 2f;
        [Min(0.001f)] public float maxDeltaTime = 0.0333f;

        [Header("碰撞")]
        public bool enableCollision;
        public LayerMask collisionLayers = ~0;
        [Min(0.001f)] public float collisionRadius = 0.03f;
        [Min(0f)] public float collisionStrength = 20f;
        [Range(0f, 1f)] public float collisionFriction = 0.2f;

        public void CopyFrom(FDX_MotionSettings other)
        {
            if (other == null) return;
            inertia = other.inertia;
            spring = other.spring;
            damping = other.damping;
            maxAngle = other.maxAngle;
            gravityStrength = other.gravityStrength;
            gravityDirection = other.gravityDirection;
            constantWind = other.constantWind;
            windMultiplier = other.windMultiplier;
            teleportDistance = other.teleportDistance;
            maxDeltaTime = other.maxDeltaTime;
            enableCollision = other.enableCollision;
            collisionLayers = other.collisionLayers;
            collisionRadius = other.collisionRadius;
            collisionStrength = other.collisionStrength;
            collisionFriction = other.collisionFriction;
        }
    }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    [AddComponentMenu("FDX/Attachment Motion/Secondary Motion")]
    public sealed class FDX_SecondaryMotion : MonoBehaviour
    {
        public enum MotionSource
        {
            AutomaticPivot,
            ExistingBones
        }

        [Serializable]
        public sealed class BoneEntry
        {
            public Transform transform;
            [Range(0f, 2f)] public float influence = 1f;
            [Tooltip("由骨頭本地座標指出末端方向。")]
            public Vector3 localAxis = Vector3.down;
            [Min(0.001f)] public float length = 0.1f;
        }

        [Serializable]
        public sealed class FlexibleEndPoint
        {
            public string displayName = "End Point";
            public Transform tip;
            [Min(0.001f)] public float detectionRadius = 0.15f;
            [Range(0f, 2f)] public float motionMultiplier = 1f;
            [Range(1, 12)] public int generatedSegments = 4;
            public AnimationCurve weightFalloff = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            [HideInInspector] public List<Transform> generatedBones = new List<Transform>();
        }

        [Header("運作來源")]
        [SerializeField] private MotionSource motionSource = MotionSource.AutomaticPivot;
        [SerializeField] private bool simulate = true;
        [SerializeField] private FDX_MotionSettings settings = new FDX_MotionSettings();

        [Header("現有骨架")]
        [SerializeField] private List<BoneEntry> existingBones = new List<BoneEntry>();

        [Header("預設旋轉軸心")]
        [SerializeField] private bool autoCreatePivot = true;
        [SerializeField] private Transform rotationPivot;

        [Header("進階柔性設定")]
        [SerializeField] private bool enableAdvancedFlexible;
        [SerializeField] private List<FlexibleEndPoint> endPoints = new List<FlexibleEndPoint>();

        [Header("精細權重設定")]
        [SerializeField] private bool enablePreciseWeights;
        [SerializeField] private SkinnedMeshRenderer deformingRenderer;

        [Header("碰撞來源")]
        [SerializeField] private List<Collider> explicitColliders = new List<Collider>();

        [Header("Gizmo")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color pivotColor = new Color(0.1f, 0.85f, 1f, 0.9f);
        [SerializeField] private Color endPointColor = new Color(1f, 0.55f, 0.1f, 0.9f);
        [SerializeField, Min(0.001f)] private float pivotGizmoRadius = 0.08f;
        [SerializeField, Min(0.001f)] private float endPointGizmoRadius = 0.045f;

        private sealed class NodeState
        {
            public Transform target;
            public Quaternion restLocalRotation;
            public Vector3 localAxis;
            public float length;
            public float influence;
            public Vector3 angle;
            public Vector3 angularVelocity;
        }

        private readonly List<NodeState> nodeStates = new List<NodeState>();
        private readonly Collider[] collisionBuffer = new Collider[32];
        private Vector3 previousAnchorPosition;
        private Vector3 previousAnchorVelocity;
        private Vector3 continuousForce;
        private Vector3 impulseVelocity;
        private bool initialized;

        public MotionSource Source { get => motionSource; set => motionSource = value; }
        public FDX_MotionSettings Settings => settings;
        public Transform RotationPivot { get => rotationPivot; set => rotationPivot = value; }
        public bool EnableAdvancedFlexible { get => enableAdvancedFlexible; set => enableAdvancedFlexible = value; }
        public bool EnablePreciseWeights { get => enablePreciseWeights; set => enablePreciseWeights = value; }
        public List<FlexibleEndPoint> EndPoints => endPoints;
        public List<BoneEntry> ExistingBones => existingBones;
        public SkinnedMeshRenderer DeformingRenderer { get => deformingRenderer; set => deformingRenderer = value; }
        public bool HasValidExistingBones => existingBones.Exists(entry => entry != null && entry.transform != null);
        public bool HasUsableMotionSource => motionSource == MotionSource.AutomaticPivot || HasValidExistingBones;

        private void Reset()
        {
            settings = new FDX_MotionSettings();
            motionSource = MotionSource.AutomaticPivot;
            autoCreatePivot = true;
        }

        private void OnEnable()
        {
            initialized = false;
        }

        private void Start()
        {
            EnsureRuntimePivot();
            RebuildSimulation();
        }

        private void OnDisable()
        {
            RestoreRestPose();
            initialized = false;
        }

        private void LateUpdate()
        {
            if (!simulate) return;
            if (!initialized)
            {
                EnsureRuntimePivot();
                RebuildSimulation();
            }
            if (nodeStates.Count == 0) return;

            float dt = Mathf.Min(Time.deltaTime, settings.maxDeltaTime);
            if (dt <= 0f) return;

            Transform anchor = GetMotionAnchor();
            Vector3 anchorPosition = anchor != null ? anchor.position : transform.position;
            if (Vector3.Distance(previousAnchorPosition, anchorPosition) > settings.teleportDistance)
            {
                ResetSimulation();
                return;
            }

            Vector3 anchorVelocity = (anchorPosition - previousAnchorPosition) / dt;
            Vector3 anchorAcceleration = (anchorVelocity - previousAnchorVelocity) / dt;
            previousAnchorPosition = anchorPosition;
            previousAnchorVelocity = anchorVelocity;

            Vector3 gravity = settings.gravityDirection.sqrMagnitude > 0.0001f
                ? settings.gravityDirection.normalized * (9.81f * settings.gravityStrength)
                : Vector3.zero;
            Vector3 worldForce = gravity + settings.constantWind * settings.windMultiplier + continuousForce + impulseVelocity;
            worldForce -= anchorAcceleration * settings.inertia;
            impulseVelocity = Vector3.MoveTowards(impulseVelocity, Vector3.zero, dt * settings.damping);

            foreach (NodeState state in nodeStates)
            {
                if (state.target == null) continue;
                Transform parent = state.target.parent;
                Vector3 localForce = parent != null ? parent.InverseTransformDirection(worldForce) : worldForce;
                Vector3 axis = state.localAxis.sqrMagnitude > 0.0001f ? state.localAxis.normalized : Vector3.down;
                Vector3 targetAngle = Vector3.Cross(axis, localForce) * (state.influence * 3.5f);

                Vector3 collisionForce = CalculateCollisionForce(state);
                if (collisionForce.sqrMagnitude > 0.0001f)
                {
                    Vector3 localCollision = parent != null
                        ? parent.InverseTransformDirection(collisionForce)
                        : collisionForce;
                    targetAngle += Vector3.Cross(axis, localCollision) * settings.collisionStrength;
                    state.angularVelocity *= 1f - settings.collisionFriction;
                }

                if (targetAngle.magnitude > settings.maxAngle)
                    targetAngle = targetAngle.normalized * settings.maxAngle;

                Vector3 acceleration = (targetAngle - state.angle) * settings.spring;
                state.angularVelocity += acceleration * dt;
                state.angularVelocity *= Mathf.Exp(-settings.damping * dt);
                state.angle += state.angularVelocity * dt;
                if (state.angle.magnitude > settings.maxAngle)
                    state.angle = state.angle.normalized * settings.maxAngle;

                state.target.localRotation = state.restLocalRotation * Quaternion.Euler(state.angle);
            }
        }

        public void ApplySettings(FDX_MotionSettings source, bool resetSimulation)
        {
            settings.CopyFrom(source);
            if (resetSimulation) ResetSimulation();
        }

        public void AddForce(Vector3 worldForce)
        {
            continuousForce += worldForce;
        }

        public void RemoveForce(Vector3 worldForce)
        {
            continuousForce -= worldForce;
        }

        public void ClearForces()
        {
            continuousForce = Vector3.zero;
            impulseVelocity = Vector3.zero;
        }

        public void AddImpulse(Vector3 worldImpulse)
        {
            impulseVelocity += worldImpulse;
        }

        [ContextMenu("Rebuild Simulation")]
        public void RebuildSimulation()
        {
            RestoreRestPose();
            nodeStates.Clear();
            var used = new HashSet<Transform>();

            if (motionSource == MotionSource.ExistingBones)
            {
                foreach (BoneEntry entry in existingBones)
                {
                    if (entry == null || entry.transform == null || !used.Add(entry.transform)) continue;
                    AddState(entry.transform, entry.localAxis, entry.length, entry.influence);
                }
            }
            else
            {
                if (rotationPivot != null && used.Add(rotationPivot))
                    AddState(rotationPivot, Vector3.down, Mathf.Max(0.05f, pivotGizmoRadius * 2f), 1f);

                if (enableAdvancedFlexible)
                {
                    foreach (FlexibleEndPoint point in endPoints)
                    {
                        if (point == null) continue;
                        if (point.generatedBones != null && point.generatedBones.Count > 0)
                        {
                            for (int i = 0; i < point.generatedBones.Count; i++)
                            {
                                Transform bone = point.generatedBones[i];
                                if (bone == null || !used.Add(bone)) continue;
                                float influence = point.motionMultiplier * (i + 1f) / point.generatedBones.Count;
                                AddState(bone, Vector3.forward, GetChildDistance(bone), influence);
                            }
                        }
                        else if (point.tip != null && used.Add(point.tip))
                        {
                            Transform pivot = rotationPivot != null ? rotationPivot : transform;
                            Vector3 axis = point.tip.parent != null
                                ? point.tip.parent.InverseTransformDirection(point.tip.position - pivot.position)
                                : Vector3.forward;
                            AddState(point.tip, axis, Mathf.Max(0.05f, Vector3.Distance(pivot.position, point.tip.position)),
                                point.motionMultiplier);
                        }
                    }
                }
            }

            initialized = true;
            ResetSimulation();
        }

        [ContextMenu("Reset Motion")]
        public void ResetSimulation()
        {
            foreach (NodeState state in nodeStates)
            {
                state.angle = Vector3.zero;
                state.angularVelocity = Vector3.zero;
                if (state.target != null) state.target.localRotation = state.restLocalRotation;
            }

            Transform anchor = GetMotionAnchor();
            previousAnchorPosition = anchor != null ? anchor.position : transform.position;
            previousAnchorVelocity = Vector3.zero;
            impulseVelocity = Vector3.zero;
        }

        private void AddState(Transform target, Vector3 localAxis, float length, float influence)
        {
            nodeStates.Add(new NodeState
            {
                target = target,
                restLocalRotation = target.localRotation,
                localAxis = localAxis.sqrMagnitude > 0.0001f ? localAxis.normalized : Vector3.down,
                length = Mathf.Max(0.001f, length),
                influence = influence
            });
        }

        private void EnsureRuntimePivot()
        {
            if (motionSource != MotionSource.AutomaticPivot || rotationPivot != null || !autoCreatePivot) return;

            Transform oldParent = transform.parent;
            var pivotObject = new GameObject($"{name}_FDX_Pivot");
            Transform createdPivot = pivotObject.transform;
            createdPivot.SetPositionAndRotation(transform.position, transform.rotation);
            createdPivot.localScale = Vector3.one;
            createdPivot.SetParent(oldParent, true);
            transform.SetParent(createdPivot, true);
            rotationPivot = createdPivot;
        }

        private Transform GetMotionAnchor()
        {
            if (motionSource == MotionSource.AutomaticPivot && rotationPivot != null)
                return rotationPivot.parent != null ? rotationPivot.parent : rotationPivot;
            if (nodeStates.Count > 0 && nodeStates[0].target != null)
                return nodeStates[0].target.parent != null ? nodeStates[0].target.parent : nodeStates[0].target;
            return transform.parent != null ? transform.parent : transform;
        }

        private Vector3 CalculateCollisionForce(NodeState state)
        {
            if (!settings.enableCollision || state.target == null) return Vector3.zero;
            Vector3 tip = state.target.TransformPoint(state.localAxis * state.length);
            Vector3 correction = Vector3.zero;

            foreach (Collider col in explicitColliders)
                correction += GetColliderCorrection(col, tip, settings.collisionRadius);

            int count = Physics.OverlapSphereNonAlloc(tip, settings.collisionRadius, collisionBuffer,
                settings.collisionLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider col = collisionBuffer[i];
                if (col == null || explicitColliders.Contains(col) || col.transform.IsChildOf(transform)) continue;
                correction += GetColliderCorrection(col, tip, settings.collisionRadius);
            }
            return correction;
        }

        private static Vector3 GetColliderCorrection(Collider col, Vector3 point, float radius)
        {
            if (col == null || !col.enabled) return Vector3.zero;
            Vector3 closest = col.ClosestPoint(point);
            Vector3 delta = point - closest;
            float distance = delta.magnitude;
            if (distance >= radius) return Vector3.zero;
            if (distance < 0.0001f)
            {
                delta = point - col.bounds.center;
                if (delta.sqrMagnitude < 0.0001f) delta = Vector3.up;
                distance = 0f;
            }
            return delta.normalized * (radius - distance);
        }

        private void RestoreRestPose()
        {
            foreach (NodeState state in nodeStates)
                if (state.target != null) state.target.localRotation = state.restLocalRotation;
        }

        private static float GetChildDistance(Transform bone)
        {
            if (bone.childCount == 0) return 0.1f;
            return Mathf.Max(0.001f, Vector3.Distance(bone.position, bone.GetChild(0).position));
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            Transform pivot = rotationPivot != null ? rotationPivot : transform;
            Gizmos.color = pivotColor;
            Gizmos.DrawWireSphere(pivot.position, pivotGizmoRadius);

            if (!enableAdvancedFlexible) return;
            foreach (FlexibleEndPoint point in endPoints)
            {
                if (point == null || point.tip == null) continue;
                Gizmos.color = endPointColor;
                Gizmos.DrawWireSphere(point.tip.position, endPointGizmoRadius);
                DrawCylinderWire(pivot.position, point.tip.position, point.detectionRadius);
            }
        }

        private static void DrawCylinderWire(Vector3 start, Vector3 end, float radius)
        {
            Vector3 axis = end - start;
            if (axis.sqrMagnitude < 0.000001f) return;
            Vector3 sideA = Vector3.Cross(axis.normalized, Vector3.up);
            if (sideA.sqrMagnitude < 0.001f) sideA = Vector3.Cross(axis.normalized, Vector3.right);
            sideA.Normalize();
            Vector3 sideB = Vector3.Cross(axis.normalized, sideA).normalized;
            const int segments = 16;
            Vector3 previousStart = start + sideA * radius;
            Vector3 previousEnd = end + sideA * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 radial = (sideA * Mathf.Cos(angle) + sideB * Mathf.Sin(angle)) * radius;
                Vector3 currentStart = start + radial;
                Vector3 currentEnd = end + radial;
                Gizmos.DrawLine(previousStart, currentStart);
                Gizmos.DrawLine(previousEnd, currentEnd);
                if (i % 4 == 0) Gizmos.DrawLine(currentStart, currentEnd);
                previousStart = currentStart;
                previousEnd = currentEnd;
            }
        }
    }

}
