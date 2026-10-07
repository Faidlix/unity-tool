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
        public bool perAxisSettings;
        public Vector3 inertiaPerAxis = Vector3.one * 1.2f;
        public Vector3 springPerAxis = Vector3.one * 38f;
        public Vector3 dampingPerAxis = Vector3.one * 8f;
        public Vector3 maxAnglePerAxis = Vector3.one * 35f;
        [Range(0f, 1f)] public float animationBlend = 1f;
        [Range(1, 4)] public int substeps = 1;
        [Min(0f)] public float gravityStrength = 1f;
        public Vector3 gravityDirection = Vector3.down;
        public Vector3 constantWind = Vector3.zero;
        [Min(0f)] public float windMultiplier = 1f;
        [Min(0.001f)] public float teleportDistance = 2f;
        [Min(0.001f)] public float maxDeltaTime = 0.0333f;
        public bool enableCollision;
        public LayerMask collisionLayers = ~0;
        [Min(0.001f)] public float collisionRadius = 0.03f;
        [Min(0f)] public float collisionStrength = 20f;
        [Range(0f, 1f)] public float collisionFriction = 0.2f;

        public Vector3 Inertia => perAxisSettings ? ClampPositive(inertiaPerAxis) : Vector3.one * Mathf.Max(0f, inertia);
        public Vector3 Spring => perAxisSettings ? ClampPositive(springPerAxis) : Vector3.one * Mathf.Max(0f, spring);
        public Vector3 Damping => perAxisSettings ? ClampPositive(dampingPerAxis) : Vector3.one * Mathf.Max(0f, damping);
        public Vector3 MaxAngle => perAxisSettings ? ClampPositive(maxAnglePerAxis) : Vector3.one * Mathf.Max(0f, maxAngle);

        public void CopyFrom(FDX_MotionSettings other)
        {
            if (other == null) return;
            inertia = other.inertia;
            spring = other.spring;
            damping = other.damping;
            maxAngle = other.maxAngle;
            perAxisSettings = other.perAxisSettings;
            inertiaPerAxis = other.inertiaPerAxis;
            springPerAxis = other.springPerAxis;
            dampingPerAxis = other.dampingPerAxis;
            maxAnglePerAxis = other.maxAnglePerAxis;
            animationBlend = other.animationBlend;
            substeps = other.substeps;
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

        private static Vector3 ClampPositive(Vector3 value) => new Vector3(
            Mathf.Max(0f, value.x), Mathf.Max(0f, value.y), Mathf.Max(0f, value.z));
    }

    [Serializable]
    public sealed class FDX_AxisControlSettings
    {
        public bool lockX;
        public bool lockY;
        public bool lockZ;
        public bool useIndividualDynamics;
        public bool perAxisSettings;
        [Min(0f)] public float inertia = 1.2f;
        [Min(0f)] public float spring = 38f;
        [Min(0f)] public float damping = 8f;
        [Range(0f, 180f)] public float maxAngle = 35f;
        public Vector3 inertiaPerAxis = Vector3.one * 1.2f;
        public Vector3 springPerAxis = Vector3.one * 38f;
        public Vector3 dampingPerAxis = Vector3.one * 8f;
        public Vector3 maxAnglePerAxis = Vector3.one * 35f;

        public bool AllLocked => lockX && lockY && lockZ;
        public Vector3 ResolveInertia(FDX_MotionSettings fallback) => Resolve(inertia, inertiaPerAxis, fallback.Inertia);
        public Vector3 ResolveSpring(FDX_MotionSettings fallback) => Resolve(spring, springPerAxis, fallback.Spring);
        public Vector3 ResolveDamping(FDX_MotionSettings fallback) => Resolve(damping, dampingPerAxis, fallback.Damping);
        public Vector3 ResolveMaxAngle(FDX_MotionSettings fallback) => Resolve(maxAngle, maxAnglePerAxis, fallback.MaxAngle);

        public Vector3 ApplyLocks(Vector3 value)
        {
            if (lockX) value.x = 0f;
            if (lockY) value.y = 0f;
            if (lockZ) value.z = 0f;
            return value;
        }

        public void CopyFrom(FDX_AxisControlSettings other)
        {
            if (other == null) return;
            lockX = other.lockX;
            lockY = other.lockY;
            lockZ = other.lockZ;
            useIndividualDynamics = other.useIndividualDynamics;
            perAxisSettings = other.perAxisSettings;
            inertia = other.inertia;
            spring = other.spring;
            damping = other.damping;
            maxAngle = other.maxAngle;
            inertiaPerAxis = other.inertiaPerAxis;
            springPerAxis = other.springPerAxis;
            dampingPerAxis = other.dampingPerAxis;
            maxAnglePerAxis = other.maxAnglePerAxis;
        }

        private Vector3 Resolve(float unified, Vector3 perAxis, Vector3 fallback)
        {
            if (!useIndividualDynamics) return fallback;
            if (!perAxisSettings) return Vector3.one * Mathf.Max(0f, unified);
            return new Vector3(Mathf.Max(0f, perAxis.x), Mathf.Max(0f, perAxis.y), Mathf.Max(0f, perAxis.z));
        }
    }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    [AddComponentMenu("FDX/Attachment Motion/Secondary Motion")]
    public sealed class FDX_SecondaryMotion : MonoBehaviour
    {
        public enum MotionSource { AutomaticPivot, ExistingBones }
        public enum MirrorAxis { X, Y, Z }
        public enum ForceSpace { World, AnchorLocal }

        [Serializable]
        public sealed class BoneEntry
        {
            public Transform transform;
            [Range(0f, 2f)] public float influence = 1f;
            public Vector3 localAxis = Vector3.down;
            [Min(0.001f)] public float length = 0.1f;
            public FDX_AxisControlSettings axisSettings = new FDX_AxisControlSettings();
        }

        [Serializable]
        public sealed class BoneChain
        {
            public string displayName = "Bone Chain";
            public bool enabled = true;
            public bool solo;
            public Transform root;
            public bool includeChildBones = true;
            public bool includeAllBranches = true;
            public Transform endBone;
            public bool includeEndBone = true;
            public List<Transform> excludedBones = new List<Transform>();
            [Range(0f, 2f)] public float influence = 1f;
            [Min(0.001f)] public float displayRadius = 0.04f;
            public FDX_AxisControlSettings axisSettings = new FDX_AxisControlSettings();
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
            public FDX_AxisControlSettings axisSettings = new FDX_AxisControlSettings();
            public bool liveMirror;
            public MirrorAxis mirrorAxis = MirrorAxis.X;
            public List<FlexibleEndPoint> children = new List<FlexibleEndPoint>();
            [HideInInspector] public List<Transform> generatedBones = new List<Transform>();
            [HideInInspector] public bool expanded = true;
        }

        [Serializable]
        public sealed class PivotGroup
        {
            public string displayName = "Rotation Pivot";
            public bool enabled = true;
            public bool solo;
            public Transform pivot;
            [Tooltip("The Transform actually rotated by the simulation. Leave empty to use the pivot.")]
            public Transform motionTarget;
            [Tooltip("Automatically use the pivot parent as the motion reference.")]
            public bool automaticSimulationAnchor = true;
            public Transform simulationAnchor;
            [Min(0.001f)] public float influenceRadius = 0.15f;
            public FDX_AxisControlSettings axisSettings = new FDX_AxisControlSettings();
            public bool liveMirror;
            public MirrorAxis mirrorAxis = MirrorAxis.X;
            public Transform mirrorCenter;
            public bool enableAdvancedFlexible;
            public List<FlexibleEndPoint> endPoints = new List<FlexibleEndPoint>();
            [HideInInspector] public bool expanded = true;
        }

        [SerializeField] private MotionSource motionSource = MotionSource.AutomaticPivot;
        [SerializeField] private bool simulate = true;
        [SerializeField] private ForceSpace forceSpace = ForceSpace.World;
        [SerializeField] private FDX_MotionSettings settings = new FDX_MotionSettings();
        [SerializeField] private Transform simulationAnchor;
        [SerializeField] private List<BoneChain> boneChains = new List<BoneChain>();
        [SerializeField] private List<BoneEntry> existingBones = new List<BoneEntry>();
        [SerializeField] private bool autoCreatePivot = true;
        [SerializeField] private Transform rotationPivot;
        [SerializeField, Min(0.001f)] private float pivotInfluenceRadius = 0.15f;
        [SerializeField] private FDX_AxisControlSettings pivotAxisSettings = new FDX_AxisControlSettings();
        [SerializeField] private List<PivotGroup> pivotGroups = new List<PivotGroup>();
        [SerializeField, HideInInspector] private int pivotGroupDataVersion;
        [SerializeField] private bool enableAdvancedFlexible;
        [SerializeField] private List<FlexibleEndPoint> endPoints = new List<FlexibleEndPoint>();
        [SerializeField] private bool enablePreciseWeights;
        [SerializeField] private SkinnedMeshRenderer deformingRenderer;
        [SerializeField] private List<Collider> explicitColliders = new List<Collider>();
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private bool showAllInfluenceRanges = true;
        [SerializeField] private Color pivotColor = new Color(0.1f, 0.85f, 1f, 0.9f);
        [SerializeField] private Color endPointColor = new Color(1f, 0.55f, 0.1f, 0.9f);
        [SerializeField] private Color mirrorColor = new Color(0.7f, 0.35f, 1f, 0.75f);
        [SerializeField, Min(0.001f)] private float pivotGizmoRadius = 0.08f;
        [SerializeField, Min(0.001f)] private float endPointGizmoRadius = 0.045f;

        private sealed class NodeState
        {
            public Transform target;
            public Quaternion restLocalRotation;
            public Quaternion lastAppliedLocalRotation;
            public Vector3 localAxis;
            public float length;
            public float influence;
            public FDX_AxisControlSettings axisSettings;
            public Transform simulationAnchor;
            public Vector3 angle;
            public Vector3 angularVelocity;
        }

        private sealed class AnchorState
        {
            public Vector3 previousPosition;
            public Vector3 previousVelocity;
            public Vector3 acceleration;
            public bool initialized;
        }

        private readonly List<NodeState> nodeStates = new List<NodeState>();
        private readonly Dictionary<Transform, AnchorState> anchorStates = new Dictionary<Transform, AnchorState>();
        private readonly Collider[] collisionBuffer = new Collider[32];
        private Vector3 previousAnchorPosition;
        private Vector3 previousAnchorVelocity;
        private Vector3 continuousForce;
        private Vector3 impulseVelocity;
        private float previewTime;
        private bool initialized;

        public MotionSource Source { get => motionSource; set => motionSource = value; }
        public FDX_MotionSettings Settings => settings;
        public Transform SimulationAnchor { get => simulationAnchor; set => simulationAnchor = value; }
        public Transform RotationPivot
        {
            get => pivotGroups != null && pivotGroups.Count > 0 && pivotGroups[0] != null
                ? pivotGroups[0].pivot
                : rotationPivot;
            set
            {
                rotationPivot = value;
                if (pivotGroups != null && pivotGroups.Count > 0 && pivotGroups[0] != null)
                    pivotGroups[0].pivot = value;
            }
        }
        public List<PivotGroup> PivotGroups => pivotGroups;
        public bool EnableAdvancedFlexible
        {
            get => pivotGroups != null && pivotGroups.Count > 0 && pivotGroups[0] != null
                ? pivotGroups[0].enableAdvancedFlexible
                : enableAdvancedFlexible;
            set
            {
                enableAdvancedFlexible = value;
                if (pivotGroups != null && pivotGroups.Count > 0 && pivotGroups[0] != null)
                    pivotGroups[0].enableAdvancedFlexible = value;
            }
        }
        public bool EnablePreciseWeights { get => enablePreciseWeights; set => enablePreciseWeights = value; }
        public List<FlexibleEndPoint> EndPoints => pivotGroups != null && pivotGroups.Count > 0 && pivotGroups[0] != null
            ? pivotGroups[0].endPoints
            : endPoints;
        public List<BoneEntry> ExistingBones => existingBones;
        public List<BoneChain> BoneChains => boneChains;
        public SkinnedMeshRenderer DeformingRenderer { get => deformingRenderer; set => deformingRenderer = value; }
        public bool HasValidExistingBones => existingBones.Exists(entry => entry != null && entry.transform != null) ||
                                             boneChains.Exists(chain => chain != null && chain.enabled && chain.root != null);
        public bool HasUsableMotionSource => motionSource == MotionSource.AutomaticPivot || HasValidExistingBones;

        private void Reset()
        {
            settings = new FDX_MotionSettings();
            motionSource = MotionSource.AutomaticPivot;
            autoCreatePivot = true;
        }

        private void OnEnable() { EnsureDataIntegrity(); initialized = false; }
        private void OnValidate() => EnsureDataIntegrity();
        private void Start() { EnsureDataIntegrity(); EnsureRuntimePivot(); RebuildSimulation(); }
        private void OnDisable() { RestoreRestPose(); initialized = false; }
        private void LateUpdate() { if (simulate) SimulateStep(Time.deltaTime, Vector3.zero); }

        private void SimulateStep(float deltaTime, Vector3 additionalWorldForce)
        {
            if (!initialized) { EnsureRuntimePivot(); RebuildSimulation(); }
            if (nodeStates.Count == 0) return;
            float frameDt = Mathf.Min(deltaTime, settings.maxDeltaTime);
            if (frameDt <= 0f) return;

            Vector3 gravityDirection = settings.gravityDirection;
            Vector3 wind = settings.constantWind;
            Transform anchor = GetMotionAnchor();
            if (forceSpace == ForceSpace.AnchorLocal && anchor != null)
            {
                gravityDirection = anchor.TransformDirection(gravityDirection);
                wind = anchor.TransformDirection(wind);
            }
            Vector3 gravity = gravityDirection.sqrMagnitude > 0.0001f
                ? gravityDirection.normalized * (9.81f * settings.gravityStrength)
                : Vector3.zero;
            Vector3 externalWorldForce = gravity + wind * settings.windMultiplier +
                                         continuousForce + impulseVelocity + additionalWorldForce;
            impulseVelocity = Vector3.MoveTowards(impulseVelocity, Vector3.zero, frameDt * settings.damping);
            UpdateAnchorStates(frameDt);
            int steps = Mathf.Clamp(settings.substeps, 1, 4);
            float dt = frameDt / steps;
            for (int step = 0; step < steps; step++) SimulateNodes(dt, externalWorldForce);
        }

        private void SimulateNodes(float dt, Vector3 externalWorldForce)
        {
            foreach (NodeState state in nodeStates)
            {
                FDX_AxisControlSettings axisSettings = state.axisSettings ?? pivotAxisSettings;
                if (state.target == null || axisSettings.AllLocked) continue;
                if (Quaternion.Angle(state.target.localRotation, state.lastAppliedLocalRotation) > 0.01f)
                    state.restLocalRotation = state.target.localRotation;

                Vector3 inertia = axisSettings.ResolveInertia(settings);
                Vector3 spring = axisSettings.ResolveSpring(settings);
                Vector3 damping = axisSettings.ResolveDamping(settings);
                Vector3 maxAngle = axisSettings.ResolveMaxAngle(settings);
                Vector3 localForce = state.target.InverseTransformDirection(externalWorldForce);
                Transform anchor = state.simulationAnchor != null ? state.simulationAnchor : GetMotionAnchor();
                Vector3 anchorAcceleration = anchor != null && anchorStates.TryGetValue(anchor, out AnchorState anchorState)
                    ? anchorState.acceleration
                    : Vector3.zero;
                Vector3 localAcceleration = state.target.InverseTransformDirection(anchorAcceleration);
                localForce -= Vector3.Scale(localAcceleration, inertia);
                Vector3 axis = state.localAxis.sqrMagnitude > 0.0001f ? state.localAxis.normalized : Vector3.down;
                Vector3 targetAngle = Vector3.Cross(axis, localForce) * (state.influence * 3.5f);

                Vector3 collisionForce = CalculateCollisionForce(state);
                if (collisionForce.sqrMagnitude > 0.0001f)
                {
                    Vector3 localCollision = state.target.InverseTransformDirection(collisionForce);
                    targetAngle += Vector3.Cross(axis, localCollision) * settings.collisionStrength;
                    state.angularVelocity *= 1f - settings.collisionFriction;
                }

                targetAngle = axisSettings.ApplyLocks(ClampComponents(targetAngle, maxAngle));
                Vector3 acceleration = Vector3.Scale(targetAngle - state.angle, spring);
                state.angularVelocity += acceleration * dt;
                state.angularVelocity = new Vector3(
                    state.angularVelocity.x * Mathf.Exp(-damping.x * dt),
                    state.angularVelocity.y * Mathf.Exp(-damping.y * dt),
                    state.angularVelocity.z * Mathf.Exp(-damping.z * dt));
                state.angularVelocity = axisSettings.ApplyLocks(state.angularVelocity);
                state.angle = axisSettings.ApplyLocks(ClampComponents(state.angle + state.angularVelocity * dt, maxAngle));
                state.lastAppliedLocalRotation = state.restLocalRotation *
                                                 Quaternion.Euler(state.angle * settings.animationBlend);
                state.target.localRotation = state.lastAppliedLocalRotation;
            }
        }

        private void UpdateAnchorStates(float frameDt)
        {
            var activeAnchors = new HashSet<Transform>();
            foreach (NodeState state in nodeStates)
            {
                Transform anchor = state.simulationAnchor != null ? state.simulationAnchor : GetMotionAnchor();
                if (anchor == null || !activeAnchors.Add(anchor)) continue;
                if (!anchorStates.TryGetValue(anchor, out AnchorState history))
                {
                    history = new AnchorState();
                    anchorStates.Add(anchor, history);
                }
                Vector3 position = anchor.position;
                if (!history.initialized || Vector3.Distance(history.previousPosition, position) > settings.teleportDistance)
                {
                    history.previousPosition = position;
                    history.previousVelocity = Vector3.zero;
                    history.acceleration = Vector3.zero;
                    history.initialized = true;
                    continue;
                }
                Vector3 velocity = (position - history.previousPosition) / Mathf.Max(0.0001f, frameDt);
                history.acceleration = (velocity - history.previousVelocity) / Mathf.Max(0.0001f, frameDt);
                history.previousPosition = position;
                history.previousVelocity = velocity;
            }
        }

        public void PreviewStep(float deltaTime)
        {
            if (!simulate || Application.isPlaying) return;
            previewTime += deltaTime;
            Vector3 previewForce = new Vector3(Mathf.Sin(previewTime * 2.3f), 0f,
                Mathf.Cos(previewTime * 1.7f)) * 2f;
            SimulateStep(deltaTime, previewForce);
        }

        public void StopPreview()
        {
            if (Application.isPlaying) return;
            RestoreRestPose();
            previewTime = 0f;
            initialized = false;
        }

        public void ApplySettings(FDX_MotionSettings source, bool resetSimulation)
        {
            settings.CopyFrom(source);
            if (resetSimulation) ResetSimulation();
        }

        public void AddForce(Vector3 worldForce) => continuousForce += worldForce;
        public void RemoveForce(Vector3 worldForce) => continuousForce -= worldForce;
        public void ClearForces() { continuousForce = Vector3.zero; impulseVelocity = Vector3.zero; }
        public void AddImpulse(Vector3 worldImpulse) => impulseVelocity += worldImpulse;

        [ContextMenu("Rebuild Simulation")]
        public void RebuildSimulation()
        {
            EnsureDataIntegrity();
            RestoreRestPose();
            nodeStates.Clear();
            anchorStates.Clear();
            var used = new HashSet<Transform>();
            if (motionSource == MotionSource.ExistingBones)
            {
                bool hasSolo = boneChains.Exists(chain => chain != null && chain.enabled && chain.solo);
                foreach (BoneChain chain in boneChains)
                    if (!hasSolo || (chain != null && chain.solo)) AddBoneChain(chain, used);
                foreach (BoneEntry entry in existingBones)
                {
                    if (entry == null || entry.transform == null || !used.Add(entry.transform)) continue;
                    AddState(entry.transform, entry.localAxis, entry.length, entry.influence, entry.axisSettings);
                }
            }
            else
            {
                bool hasSolo = pivotGroups.Exists(group => group != null && group.enabled && group.solo);
                foreach (PivotGroup group in pivotGroups)
                {
                    if (group == null || !group.enabled || (hasSolo && !group.solo)) continue;
                    Transform pivot = group.pivot != null ? group.pivot : transform;
                    Transform motionTarget = ResolveMotionTarget(group);
                    Transform anchor = ResolveSimulationAnchor(group, motionTarget);
                    if (motionTarget != null && used.Add(motionTarget))
                        AddState(motionTarget, Vector3.down, Mathf.Max(0.05f, pivotGizmoRadius * 2f), 1f,
                            group.axisSettings, anchor);
                    if (group.enableAdvancedFlexible)
                        foreach (FlexibleEndPoint point in group.endPoints)
                            AddEndPointStates(point, pivot, used, anchor);
                }
            }
            initialized = true;
            ResetSimulation();
        }

        private void AddBoneChain(BoneChain chain, HashSet<Transform> used)
        {
            if (chain == null || !chain.enabled || chain.root == null) return;
            AddBoneRecursive(chain.root, chain, used);
        }

        private void EnsureDataIntegrity()
        {
            if (settings == null) settings = new FDX_MotionSettings();
            if (pivotAxisSettings == null) pivotAxisSettings = new FDX_AxisControlSettings();
            if (pivotGroups == null) pivotGroups = new List<PivotGroup>();
            if (boneChains == null) boneChains = new List<BoneChain>();
            if (existingBones == null) existingBones = new List<BoneEntry>();
            if (endPoints == null) endPoints = new List<FlexibleEndPoint>();
            if (explicitColliders == null) explicitColliders = new List<Collider>();
            foreach (BoneChain chain in boneChains)
            {
                if (chain == null) continue;
                if (chain.excludedBones == null) chain.excludedBones = new List<Transform>();
                if (chain.axisSettings == null) chain.axisSettings = new FDX_AxisControlSettings();
            }
            foreach (BoneEntry entry in existingBones)
                if (entry != null && entry.axisSettings == null) entry.axisSettings = new FDX_AxisControlSettings();
            foreach (FlexibleEndPoint point in endPoints) EnsureEndPointData(point);
            MigrateLegacyPivot();
            foreach (PivotGroup group in pivotGroups)
            {
                if (group == null) continue;
                if (group.axisSettings == null) group.axisSettings = new FDX_AxisControlSettings();
                if (group.endPoints == null) group.endPoints = new List<FlexibleEndPoint>();
                foreach (FlexibleEndPoint point in group.endPoints) EnsureEndPointData(point);
            }
        }

        private void MigrateLegacyPivot()
        {
            if (pivotGroupDataVersion > 0) return;
            if (pivotGroups.Count == 0)
            {
                pivotGroups.Add(new PivotGroup
                {
                    displayName = string.IsNullOrWhiteSpace(name) ? "旋轉軸心 1" : name + " 軸心",
                    pivot = rotationPivot,
                    influenceRadius = pivotInfluenceRadius,
                    axisSettings = pivotAxisSettings ?? new FDX_AxisControlSettings(),
                    automaticSimulationAnchor = simulationAnchor == null,
                    simulationAnchor = simulationAnchor,
                    enableAdvancedFlexible = enableAdvancedFlexible,
                    endPoints = endPoints ?? new List<FlexibleEndPoint>()
                });
            }
            pivotGroupDataVersion = 1;
        }

        private static void EnsureEndPointData(FlexibleEndPoint point)
        {
            if (point == null) return;
            if (point.axisSettings == null) point.axisSettings = new FDX_AxisControlSettings();
            if (point.children == null) point.children = new List<FlexibleEndPoint>();
            if (point.generatedBones == null) point.generatedBones = new List<Transform>();
            if (point.weightFalloff == null) point.weightFalloff = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            foreach (FlexibleEndPoint child in point.children) EnsureEndPointData(child);
        }

        private bool AddBoneRecursive(Transform bone, BoneChain chain, HashSet<Transform> used)
        {
            if (bone == null || chain.excludedBones.Contains(bone)) return false;
            bool isEnd = chain.endBone != null && bone == chain.endBone;
            if ((!isEnd || chain.includeEndBone) && used.Add(bone))
            {
                Transform directionChild = FindFirstUsableChild(bone, chain);
                Vector3 axis = directionChild != null
                    ? bone.InverseTransformDirection(directionChild.position - bone.position)
                    : Vector3.down;
                float length = directionChild != null ? Vector3.Distance(bone.position, directionChild.position) : 0.1f;
                AddState(bone, axis, length, chain.influence, chain.axisSettings);
            }
            if (isEnd || !chain.includeChildBones) return isEnd;
            int childLimit = chain.includeAllBranches ? bone.childCount : Mathf.Min(1, bone.childCount);
            for (int i = 0; i < childLimit; i++)
                if (AddBoneRecursive(bone.GetChild(i), chain, used) && chain.endBone != null) return true;
            return false;
        }

        private static Transform FindFirstUsableChild(Transform bone, BoneChain chain)
        {
            for (int i = 0; i < bone.childCount; i++)
            {
                Transform child = bone.GetChild(i);
                if (!chain.excludedBones.Contains(child)) return child;
            }
            return null;
        }

        private void AddEndPointStates(FlexibleEndPoint point, Transform parentPoint, HashSet<Transform> used,
            Transform anchor)
        {
            if (point == null) return;
            if (point.generatedBones != null && point.generatedBones.Count > 0)
            {
                for (int i = 0; i < point.generatedBones.Count; i++)
                {
                    Transform bone = point.generatedBones[i];
                    if (bone == null || !used.Add(bone)) continue;
                    float influence = point.motionMultiplier * (i + 1f) / point.generatedBones.Count;
                    AddState(bone, Vector3.forward, GetChildDistance(bone), influence, point.axisSettings, anchor);
                }
            }
            else if (point.tip != null && used.Add(point.tip))
            {
                Vector3 axis = point.tip.InverseTransformDirection(point.tip.position - parentPoint.position);
                AddState(point.tip, axis, Mathf.Max(0.05f, Vector3.Distance(parentPoint.position, point.tip.position)),
                    point.motionMultiplier, point.axisSettings, anchor);
            }
            Transform nextParent = point.tip != null ? point.tip : parentPoint;
            foreach (FlexibleEndPoint child in point.children) AddEndPointStates(child, nextParent, used, anchor);
        }

        [ContextMenu("Reset Motion")]
        public void ResetSimulation()
        {
            foreach (NodeState state in nodeStates)
            {
                state.angle = Vector3.zero;
                state.angularVelocity = Vector3.zero;
                if (state.target == null) continue;
                state.target.localRotation = state.restLocalRotation;
                state.lastAppliedLocalRotation = state.restLocalRotation;
            }
            Transform anchor = GetMotionAnchor();
            previousAnchorPosition = anchor != null ? anchor.position : transform.position;
            previousAnchorVelocity = Vector3.zero;
            impulseVelocity = Vector3.zero;
            anchorStates.Clear();
        }

        [ContextMenu("Capture Current Rest Pose")]
        public void CaptureCurrentRestPose()
        {
            foreach (NodeState state in nodeStates)
            {
                if (state.target == null) continue;
                state.restLocalRotation = state.target.localRotation;
                state.lastAppliedLocalRotation = state.target.localRotation;
                state.angle = Vector3.zero;
                state.angularVelocity = Vector3.zero;
            }
            ResetSimulation();
        }

        public List<string> ValidateSetup()
        {
            var issues = new List<string>();
            if (motionSource == MotionSource.ExistingBones)
            {
                if (!HasValidExistingBones) issues.Add("尚未指定有效的骨架鏈（No valid bone chain）。");
                var seen = new HashSet<Transform>();
                foreach (BoneChain chain in boneChains)
                {
                    if (chain == null || !chain.enabled) continue;
                    if (chain.root == null)
                    {
                        issues.Add($"{chain.displayName}: 缺少骨架鏈起點（Missing chain root）。");
                        continue;
                    }
                    if (!seen.Add(chain.root)) issues.Add($"{chain.displayName}: 骨架鏈起點重複（Duplicate chain root）。");
                    if (chain.endBone != null && !chain.endBone.IsChildOf(chain.root) && chain.endBone != chain.root)
                        issues.Add($"{chain.displayName}: 結束骨頭不在起點之下（End bone is outside chain）。");
                }
            }
            else if (!pivotGroups.Exists(group => group != null && group.enabled && group.pivot != null) && !autoCreatePivot)
                issues.Add("缺少旋轉軸心（Missing rotation pivot）。");
            return issues;
        }

        private void AddState(Transform target, Vector3 localAxis, float length, float influence,
            FDX_AxisControlSettings axisSettings, Transform anchor = null)
        {
            nodeStates.Add(new NodeState
            {
                target = target,
                restLocalRotation = target.localRotation,
                lastAppliedLocalRotation = target.localRotation,
                localAxis = localAxis.sqrMagnitude > 0.0001f ? localAxis.normalized : Vector3.down,
                length = Mathf.Max(0.001f, length),
                influence = influence,
                axisSettings = axisSettings ?? new FDX_AxisControlSettings(),
                simulationAnchor = anchor
            });
        }

        private void EnsureRuntimePivot()
        {
            if (motionSource != MotionSource.AutomaticPivot || !autoCreatePivot) return;
            MigrateLegacyPivot();
            PivotGroup group = pivotGroups.Count > 0 ? pivotGroups[0] : null;
            if (group != null && group.pivot != null) return;
            var pivotObject = new GameObject($"{name}_FDX_Pivot");
            rotationPivot = pivotObject.transform;
            rotationPivot.SetParent(transform, false);
            rotationPivot.localPosition = Vector3.zero;
            rotationPivot.localRotation = Quaternion.identity;
            rotationPivot.localScale = Vector3.one;
            if (group != null) group.pivot = rotationPivot;
        }

        private Transform ResolveMotionTarget(PivotGroup group)
        {
            if (group.motionTarget != null) return group.motionTarget;
            if (pivotGroups.Count > 1 && group.pivot != null) return group.pivot;
            if (group.pivot == null) return transform;
            return group.pivot.IsChildOf(transform) ? transform : group.pivot;
        }

        private Transform ResolveSimulationAnchor(PivotGroup group, Transform motionTarget)
        {
            if (!group.automaticSimulationAnchor && group.simulationAnchor != null) return group.simulationAnchor;
            Transform pivot = group.pivot != null ? group.pivot : motionTarget;
            return pivot != null && pivot.parent != null ? pivot.parent : pivot;
        }

        private Transform GetAutomaticMotionTarget()
        {
            if (rotationPivot == null) return transform;
            return rotationPivot.IsChildOf(transform) ? transform : rotationPivot;
        }

        private Transform GetMotionAnchor()
        {
            if (simulationAnchor != null) return simulationAnchor;
            if (motionSource == MotionSource.AutomaticPivot)
            {
                Transform motionTarget = GetAutomaticMotionTarget();
                return motionTarget.parent != null ? motionTarget.parent : motionTarget;
            }
            if (nodeStates.Count > 0 && nodeStates[0].target != null)
                return nodeStates[0].target.parent != null ? nodeStates[0].target.parent : nodeStates[0].target;
            return transform.parent != null ? transform.parent : transform;
        }

        private Vector3 CalculateCollisionForce(NodeState state)
        {
            if (!settings.enableCollision || state.target == null) return Vector3.zero;
            Vector3 worldAxis = state.target.TransformDirection(state.localAxis);
            if (worldAxis.sqrMagnitude < 0.0001f) worldAxis = -state.target.up;
            Vector3 tip = state.target.position + worldAxis.normalized * state.length;
            Vector3 correction = Vector3.zero;
            foreach (Collider col in explicitColliders) correction += GetColliderCorrection(col, tip, settings.collisionRadius);
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
            {
                if (state.target == null) continue;
                state.target.localRotation = state.restLocalRotation;
                state.lastAppliedLocalRotation = state.restLocalRotation;
            }
        }

        private static Vector3 ClampComponents(Vector3 value, Vector3 maximum)
        {
            value.x = Mathf.Clamp(value.x, -maximum.x, maximum.x);
            value.y = Mathf.Clamp(value.y, -maximum.y, maximum.y);
            value.z = Mathf.Clamp(value.z, -maximum.z, maximum.z);
            return value;
        }

        private static float GetChildDistance(Transform bone) => bone.childCount == 0
            ? 0.1f
            : Mathf.Max(0.001f, Vector3.Distance(bone.position, bone.GetChild(0).position));

        public static Vector3 MirrorLocalPoint(Vector3 localPoint, MirrorAxis axis)
        {
            if (axis == MirrorAxis.X) localPoint.x = -localPoint.x;
            else if (axis == MirrorAxis.Y) localPoint.y = -localPoint.y;
            else localPoint.z = -localPoint.z;
            return localPoint;
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            if (motionSource == MotionSource.ExistingBones) { DrawBoneChainGizmos(); return; }
            foreach (PivotGroup group in pivotGroups)
            {
                if (group == null || !group.enabled) continue;
                Transform pivot = group.pivot != null ? group.pivot : transform;
                Gizmos.color = pivotColor;
                Gizmos.DrawWireSphere(pivot.position, pivotGizmoRadius);
                if (showAllInfluenceRanges) Gizmos.DrawWireSphere(pivot.position, group.influenceRadius);
                if (group.enableAdvancedFlexible)
                    foreach (FlexibleEndPoint point in group.endPoints)
                        DrawEndPointGizmos(point, pivot, false, pivot.position, pivot.rotation);
                if (group.liveMirror) DrawMirroredPivotGizmo(group, pivot);
            }
        }

        private void DrawMirroredPivotGizmo(PivotGroup group, Transform pivot)
        {
            Transform center = group.mirrorCenter != null ? group.mirrorCenter : transform;
            Vector3 mirrored = center.TransformPoint(MirrorLocalPoint(center.InverseTransformPoint(pivot.position), group.mirrorAxis));
            Gizmos.color = mirrorColor;
            Gizmos.DrawWireSphere(mirrored, pivotGizmoRadius);
            if (showAllInfluenceRanges) Gizmos.DrawWireSphere(mirrored, group.influenceRadius);
        }

        private void DrawBoneChainGizmos()
        {
            foreach (BoneChain chain in boneChains)
                if (chain != null && chain.enabled && chain.root != null) DrawBoneGizmoRecursive(chain.root, chain);
        }

        private void DrawBoneGizmoRecursive(Transform bone, BoneChain chain)
        {
            if (bone == null || chain.excludedBones.Contains(bone)) return;
            Gizmos.color = endPointColor;
            Gizmos.DrawWireSphere(bone.position, chain.displayRadius);
            if (chain.endBone != null && bone == chain.endBone) return;
            int limit = chain.includeAllBranches ? bone.childCount : Mathf.Min(1, bone.childCount);
            for (int i = 0; i < limit; i++)
            {
                Transform child = bone.GetChild(i);
                if (chain.excludedBones.Contains(child)) continue;
                DrawCylinderWire(bone.position, child.position, chain.displayRadius);
                DrawBoneGizmoRecursive(child, chain);
            }
        }

        private void DrawEndPointGizmos(FlexibleEndPoint point, Transform actualParent, bool virtualBranch,
            Vector3 virtualParentPosition, Quaternion virtualParentRotation)
        {
            if (point == null || point.tip == null) return;
            Vector3 position = virtualBranch
                ? virtualParentPosition + virtualParentRotation * actualParent.InverseTransformPoint(point.tip.position)
                : point.tip.position;
            Quaternion rotation = virtualBranch
                ? virtualParentRotation * point.tip.localRotation
                : point.tip.rotation;
            Vector3 parentPosition = virtualBranch ? virtualParentPosition : actualParent.position;
            Gizmos.color = virtualBranch ? mirrorColor : endPointColor;
            Gizmos.DrawWireSphere(position, endPointGizmoRadius);
            if (showAllInfluenceRanges) Gizmos.DrawWireSphere(position, point.detectionRadius);
            DrawCylinderWire(parentPosition, position, point.detectionRadius);
            foreach (FlexibleEndPoint child in point.children)
                DrawEndPointGizmos(child, point.tip, virtualBranch, position, rotation);

            if (!virtualBranch && point.liveMirror)
            {
                Vector3 local = actualParent.InverseTransformPoint(point.tip.position);
                Vector3 mirroredPosition = actualParent.TransformPoint(MirrorLocalPoint(local, point.mirrorAxis));
                Quaternion mirroredRotation = actualParent.rotation * MirrorLocalRotation(point.tip.localRotation, point.mirrorAxis);
                Gizmos.color = mirrorColor;
                Gizmos.DrawWireSphere(mirroredPosition, endPointGizmoRadius);
                if (showAllInfluenceRanges) Gizmos.DrawWireSphere(mirroredPosition, point.detectionRadius);
                DrawCylinderWire(actualParent.position, mirroredPosition, point.detectionRadius);
                foreach (FlexibleEndPoint child in point.children)
                    DrawEndPointGizmos(child, point.tip, true, mirroredPosition, mirroredRotation);
            }
        }

        private static Quaternion MirrorLocalRotation(Quaternion rotation, MirrorAxis axis)
        {
            Vector3 euler = rotation.eulerAngles;
            if (axis == MirrorAxis.X) { euler.y = -euler.y; euler.z = -euler.z; }
            else if (axis == MirrorAxis.Y) { euler.x = -euler.x; euler.z = -euler.z; }
            else { euler.x = -euler.x; euler.y = -euler.y; }
            return Quaternion.Euler(euler);
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
