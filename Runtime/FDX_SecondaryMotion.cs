using System;
using System.Collections.Generic;
using UnityEngine;

namespace Faidlix.UnityTools
{
    public enum FDX_DistanceUpdateMode { FixedFrameInterval, FixedUpdateFrequency }
    public enum FDX_DistanceQuality { Low, Medium, High, Custom }

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
        public bool previewAutoSway;
        [Min(0.001f)] public float teleportDistance = 2f;
        [Min(0.001f)] public float maxDeltaTime = 0.0333f;
        public bool enableCollision;
        public LayerMask collisionLayers = ~0;
        [Min(0.001f)] public float collisionRadius = 0.03f;
        [Min(0f)] public float collisionStrength = 20f;
        [Range(0f, 1f)] public float collisionFriction = 0.2f;
        public List<Collider> explicitColliders = new List<Collider>();
        [HideInInspector] public bool collidersExpanded;
        public bool enableDistanceSimulation = true;
        public bool useSceneViewCameraInEditMode = true;
        public Transform distanceReference;
        public FDX_DistanceUpdateMode distanceUpdateMode = FDX_DistanceUpdateMode.FixedFrameInterval;
        public FDX_DistanceQuality distanceQuality = FDX_DistanceQuality.High;
        [Min(0f)] public float fullSimulationDistance = 15f;
        [Min(0f)] public float reducedSimulationDistance = 25f;
        [Min(0f)] public float minimalSimulationDistance = 35f;
        public Vector3Int customFrameIntervals = new Vector3Int(1, 2, 4);
        public Vector3 customUpdateFrequencies = new Vector3(60f, 30f, 15f);

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
            previewAutoSway = other.previewAutoSway;
            teleportDistance = other.teleportDistance;
            maxDeltaTime = other.maxDeltaTime;
            enableCollision = other.enableCollision;
            collisionLayers = other.collisionLayers;
            collisionRadius = other.collisionRadius;
            collisionStrength = other.collisionStrength;
            collisionFriction = other.collisionFriction;
            explicitColliders = other.explicitColliders != null
                ? new List<Collider>(other.explicitColliders)
                : new List<Collider>();
            collidersExpanded = other.collidersExpanded;
            enableDistanceSimulation = other.enableDistanceSimulation;
            useSceneViewCameraInEditMode = other.useSceneViewCameraInEditMode;
            distanceReference = other.distanceReference;
            distanceUpdateMode = other.distanceUpdateMode;
            distanceQuality = other.distanceQuality;
            fullSimulationDistance = other.fullSimulationDistance;
            reducedSimulationDistance = other.reducedSimulationDistance;
            minimalSimulationDistance = other.minimalSimulationDistance;
            customFrameIntervals = other.customFrameIntervals;
            customUpdateFrequencies = other.customUpdateFrequencies;
        }

        public int GetFrameInterval(int stage)
        {
            Vector3Int values = distanceQuality == FDX_DistanceQuality.Low
                ? new Vector3Int(2, 4, 8)
                : distanceQuality == FDX_DistanceQuality.Medium
                    ? new Vector3Int(1, 3, 6)
                    : distanceQuality == FDX_DistanceQuality.High
                        ? new Vector3Int(1, 2, 4)
                        : customFrameIntervals;
            return Mathf.Max(1, stage == 0 ? values.x : stage == 1 ? values.y : values.z);
        }

        public float GetUpdateFrequency(int stage)
        {
            Vector3 values = distanceQuality == FDX_DistanceQuality.Low
                ? new Vector3(20f, 10f, 5f)
                : distanceQuality == FDX_DistanceQuality.Medium
                    ? new Vector3(30f, 20f, 10f)
                    : distanceQuality == FDX_DistanceQuality.High
                        ? new Vector3(60f, 30f, 15f)
                        : customUpdateFrequencies;
            return Mathf.Max(1f, stage == 0 ? values.x : stage == 1 ? values.y : values.z);
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
        public enum PivotReplicationMode { None, Mirror, Radial }

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
            [HideInInspector] public string editorId;
            [HideInInspector] public int dataVersion;
            [HideInInspector] public bool expanded = true;
            public string displayName = "Bone Chain";
            public bool enabled = true;
            public bool solo;
            public Transform root;
            public bool includeChildBones = true;
            public bool includeAllBranches = true;
            public bool allBonesSway = true;
            [Min(1)] public int boneMotionLevels = 1;
            public Transform endBone;
            public bool includeEndBone = true;
            public List<Transform> excludedBones = new List<Transform>();
            [Range(0f, 2f)] public float influence = 1f;
            [Min(0.001f)] public float displayRadius = 0.04f;
            public bool scaleGizmoByDepth;
            public FDX_AxisControlSettings axisSettings = new FDX_AxisControlSettings();
            public bool useIndividualMotionSettings;
            [HideInInspector] public bool motionSettingsExpanded = true;
            public FDX_MotionSettings motionSettings = new FDX_MotionSettings();
        }

        [Serializable]
        public sealed class BoneChainGroup
        {
            public string id;
            public string parentId;
            public string displayName;
            public bool enabled = true;
            public bool soloPreview;
            public bool expanded = true;
            public bool commonSettingsExpanded;
            public bool useMotionSettings;
            [HideInInspector] public bool motionSettingsExpanded = true;
            public FDX_MotionSettings motionSettings = new FDX_MotionSettings();
            public List<string> chainIds = new List<string>();
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
            [HideInInspector] public int dataVersion;
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
            public PivotReplicationMode replicationMode;
            public bool mirrorX = true;
            public bool mirrorY;
            public bool mirrorZ;
            [Range(1, 32)] public int radialCopies = 1;
            public MirrorAxis radialAxis = MirrorAxis.Y;
            public bool enableAdvancedFlexible;
            public List<FlexibleEndPoint> endPoints = new List<FlexibleEndPoint>();
            [HideInInspector] public List<Transform> replicatedPivots = new List<Transform>();
            [HideInInspector] public bool expanded = true;
        }

        [SerializeField] private MotionSource motionSource = MotionSource.ExistingBones;
        [SerializeField] private bool simulate = true;
        [SerializeField] private ForceSpace forceSpace = ForceSpace.World;
        [SerializeField] private FDX_MotionSettings settings = new FDX_MotionSettings();
        [SerializeField] private Transform simulationAnchor;
        [SerializeField] private List<BoneChain> boneChains = new List<BoneChain>();
        [SerializeField, HideInInspector] private List<BoneChainGroup> boneChainGroups = new List<BoneChainGroup>();
        [SerializeField, HideInInspector] private bool boneCandidatesExpanded = true;
        [SerializeField, HideInInspector] private bool boneChainsExpanded = true;
        [SerializeField, HideInInspector] private bool pivotGroupsExpanded = true;
        [SerializeField, HideInInspector] private bool motionSettingsExpanded = true;
        [SerializeField, HideInInspector] private bool globalMotionSettingsExpanded = true;
        [SerializeField, HideInInspector] private bool simulationAnchorAutomaticallyAssigned;
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
        [HideInInspector, SerializeField] private bool automaticMultiPivotSkinning;
        [HideInInspector, SerializeField] private MeshRenderer originalMeshRenderer;
        [HideInInspector, SerializeField] private MeshFilter originalMeshFilter;
        [HideInInspector, SerializeField] private SkinnedMeshRenderer automaticMultiPivotRenderer;
        [HideInInspector, SerializeField] private Material[] originalMeshMaterials = Array.Empty<Material>();
        [HideInInspector, SerializeField] private bool originalMeshRendererEnabled = true;
        [SerializeField, HideInInspector] private List<Collider> explicitColliders = new List<Collider>();
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private bool showAllInfluenceRanges = true;
        [SerializeField] private bool showCollisionRadiusGizmos = true;
        [SerializeField] private Color pivotColor = new Color(0.1f, 0.85f, 1f, 0.9f);
        [SerializeField] private Color endPointColor = new Color(1f, 0.55f, 0.1f, 0.9f);
        [SerializeField] private Color mirrorColor = new Color(0.7f, 0.35f, 1f, 0.75f);
        [SerializeField] private Color boneRootColor = new Color(0.2f, 1f, 0.45f, 0.95f);
        [SerializeField] private Color collisionRadiusColor = new Color(1f, 0.25f, 0.05f, 0.95f);
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
            public FDX_MotionSettings motionSettings;
            public Transform simulationAnchor;
            public Vector3 angle;
            public Vector3 angularVelocity;
            public Vector3 previousTipPosition;
            public bool collisionTipInitialized;
        }

        private sealed class AnchorState
        {
            public Vector3 previousPosition;
            public Vector3 previousVelocity;
            public Vector3 acceleration;
            public Quaternion previousRotation;
            public Vector3 angularVelocity;
            public Vector3 angularAcceleration;
            public bool initialized;
        }

        private readonly List<NodeState> nodeStates = new List<NodeState>();
        private readonly Dictionary<Transform, AnchorState> anchorStates = new Dictionary<Transform, AnchorState>();
        private readonly Collider[] collisionBuffer = new Collider[128];
        private readonly HashSet<Collider> collisionCandidates = new HashSet<Collider>();
        private Vector3 previousAnchorPosition;
        private Vector3 previousAnchorVelocity;
        private Vector3 continuousForce;
        private Vector3 impulseVelocity;
        private float previewTime;
        private float scheduledDeltaTime;
        private int scheduledFrameCount;
        private bool distanceSleeping;
        private bool initialized;
        private Transform attachmentAnchor;

        public static Func<Transform> EditModeDistanceReferenceProvider;

        public MotionSource Source { get => motionSource; set => motionSource = value; }
        public bool Simulate { get => simulate; set { simulate = value; if (!value) { RestoreRestPose(); initialized = false; } } }
        public ForceSpace Forces { get => forceSpace; set => forceSpace = value; }
        public FDX_MotionSettings Settings => settings;
        public Transform SimulationAnchor { get => simulationAnchor; set => simulationAnchor = value; }
        public void SetAutomaticAttachmentReference(Transform anchor) => attachmentAnchor = anchor;
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
        public List<BoneChainGroup> BoneChainGroups => boneChainGroups;
        public bool BoneCandidatesExpanded { get => boneCandidatesExpanded; set => boneCandidatesExpanded = value; }
        public bool BoneChainsExpanded { get => boneChainsExpanded; set => boneChainsExpanded = value; }
        public bool PivotGroupsExpanded { get => pivotGroupsExpanded; set => pivotGroupsExpanded = value; }
        public bool MotionSettingsExpanded { get => motionSettingsExpanded; set => motionSettingsExpanded = value; }
        public bool GlobalMotionSettingsExpanded { get => globalMotionSettingsExpanded; set => globalMotionSettingsExpanded = value; }
        public bool SimulationAnchorAutomaticallyAssigned
        {
            get => simulationAnchorAutomaticallyAssigned;
            set => simulationAnchorAutomaticallyAssigned = value;
        }
        public SkinnedMeshRenderer DeformingRenderer { get => deformingRenderer; set => deformingRenderer = value; }
        public bool AutomaticMultiPivotSkinning { get => automaticMultiPivotSkinning; set => automaticMultiPivotSkinning = value; }
        public MeshRenderer OriginalMeshRenderer { get => originalMeshRenderer; set => originalMeshRenderer = value; }
        public MeshFilter OriginalMeshFilter { get => originalMeshFilter; set => originalMeshFilter = value; }
        public SkinnedMeshRenderer AutomaticMultiPivotRenderer { get => automaticMultiPivotRenderer; set => automaticMultiPivotRenderer = value; }
        public Material[] OriginalMeshMaterials { get => originalMeshMaterials; set => originalMeshMaterials = value ?? Array.Empty<Material>(); }
        public bool OriginalMeshRendererEnabled { get => originalMeshRendererEnabled; set => originalMeshRendererEnabled = value; }
        public bool HasValidExistingBones => existingBones.Exists(entry => entry != null && entry.transform != null) ||
                                             boneChains.Exists(chain => chain != null && chain.enabled && chain.root != null);
        public bool HasUsableMotionSource => motionSource == MotionSource.AutomaticPivot || HasValidExistingBones;

        private void Reset()
        {
            settings = new FDX_MotionSettings();
            motionSource = MotionSource.ExistingBones;
            autoCreatePivot = true;
        }

        private void OnEnable() { EnsureDataIntegrity(); initialized = false; }
        private void OnValidate() { EnsureDataIntegrity(); if (!simulate) StopPreview(); else initialized = false; }
        private void Start() { EnsureDataIntegrity(); EnsureRuntimePivot(); RebuildSimulation(); }
        private void OnDisable() { RestoreRestPose(); initialized = false; }
        private void LateUpdate() { if (simulate) AdvanceScheduledSimulation(Time.deltaTime, Vector3.zero); }

        private void AdvanceScheduledSimulation(float deltaTime, Vector3 additionalWorldForce)
        {
            if (!settings.enableDistanceSimulation)
            {
                SimulateStep(deltaTime, additionalWorldForce);
                return;
            }

            int stage = GetDistanceStage();
            if (stage >= 3)
            {
                if (!distanceSleeping) ResetSimulation();
                distanceSleeping = true;
                scheduledDeltaTime = 0f;
                scheduledFrameCount = 0;
                return;
            }
            if (distanceSleeping)
            {
                ResetSimulation();
                distanceSleeping = false;
            }

            scheduledDeltaTime += Mathf.Max(0f, deltaTime);
            if (settings.distanceUpdateMode == FDX_DistanceUpdateMode.FixedFrameInterval)
            {
                scheduledFrameCount++;
                if (scheduledFrameCount < settings.GetFrameInterval(stage)) return;
                scheduledFrameCount = 0;
            }
            else
            {
                float interval = 1f / settings.GetUpdateFrequency(stage);
                if (scheduledDeltaTime < interval) return;
            }

            float stepDelta = scheduledDeltaTime;
            scheduledDeltaTime = 0f;
            SimulateStep(stepDelta, additionalWorldForce);
        }

        private int GetDistanceStage()
        {
            Transform reference = !Application.isPlaying && settings.useSceneViewCameraInEditMode &&
                                  EditModeDistanceReferenceProvider != null
                ? EditModeDistanceReferenceProvider()
                : null;
            if (reference == null) reference = settings.distanceReference;
            if (reference == null && Camera.main != null) reference = Camera.main.transform;
            if (reference == null) return 0;
            float first = Mathf.Max(0f, settings.fullSimulationDistance);
            float second = Mathf.Max(first, settings.reducedSimulationDistance);
            float third = Mathf.Max(second, settings.minimalSimulationDistance);
            float distance = Vector3.Distance(transform.position, reference.position);
            if (distance < first) return 0;
            if (distance < second) return 1;
            if (distance < third) return 2;
            return 3;
        }

        private void SimulateStep(float deltaTime, Vector3 additionalWorldForce)
        {
            if (!initialized) { EnsureRuntimePivot(); RebuildSimulation(); }
            if (nodeStates.Count == 0) return;
            float frameDt = Mathf.Min(deltaTime, settings.maxDeltaTime);
            if (frameDt <= 0f) return;

            if (nodeStates.Exists(state => (state.motionSettings ?? settings).enableCollision))
                Physics.SyncTransforms();

            impulseVelocity = Vector3.MoveTowards(impulseVelocity, Vector3.zero, frameDt * settings.damping);
            UpdateAnchorStates(frameDt);
            int steps = Mathf.Clamp(settings.substeps, 1, 4);
            float dt = frameDt / steps;
            for (int step = 0; step < steps; step++) SimulateNodes(dt, additionalWorldForce);
        }

        private void SimulateNodes(float dt, Vector3 additionalWorldForce)
        {
            foreach (NodeState state in nodeStates)
            {
                FDX_MotionSettings activeSettings = state.motionSettings ?? settings;
                FDX_AxisControlSettings axisSettings = state.axisSettings ?? pivotAxisSettings;
                if (state.target == null || axisSettings.AllLocked) continue;
                if (Quaternion.Angle(state.target.localRotation, state.lastAppliedLocalRotation) > 0.01f)
                    state.restLocalRotation = state.target.localRotation;

                Vector3 inertia = axisSettings.ResolveInertia(activeSettings);
                Vector3 spring = axisSettings.ResolveSpring(activeSettings);
                Vector3 damping = axisSettings.ResolveDamping(activeSettings);
                Vector3 maxAngle = axisSettings.ResolveMaxAngle(activeSettings);
                Transform forceAnchor = state.simulationAnchor != null ? state.simulationAnchor : GetMotionAnchor();
                Vector3 gravityDirection = activeSettings.gravityDirection;
                Vector3 wind = activeSettings.constantWind;
                if (forceSpace == ForceSpace.AnchorLocal && forceAnchor != null)
                {
                    gravityDirection = forceAnchor.TransformDirection(gravityDirection);
                    wind = forceAnchor.TransformDirection(wind);
                }
                Vector3 gravity = gravityDirection.sqrMagnitude > 0.0001f
                    ? gravityDirection.normalized * (9.81f * activeSettings.gravityStrength)
                    : Vector3.zero;
                Vector3 externalWorldForce = gravity + wind * activeSettings.windMultiplier +
                                             continuousForce + impulseVelocity + additionalWorldForce;
                Vector3 localForce = state.target.InverseTransformDirection(externalWorldForce);
                Transform anchor = state.simulationAnchor != null ? state.simulationAnchor : GetMotionAnchor();
                Vector3 anchorAcceleration = anchor != null && anchorStates.TryGetValue(anchor, out AnchorState anchorState)
                    ? anchorState.acceleration
                    : Vector3.zero;
                if (anchor != null && anchorStates.TryGetValue(anchor, out AnchorState rotationState))
                {
                    Vector3 restAxis = state.target.parent != null
                        ? state.target.parent.rotation * state.restLocalRotation * state.localAxis.normalized
                        : state.restLocalRotation * state.localAxis.normalized;
                    Vector3 offset = state.target.position + restAxis * state.length - anchor.position;
                    anchorAcceleration += Vector3.Cross(rotationState.angularAcceleration, offset) +
                        Vector3.Cross(rotationState.angularVelocity, Vector3.Cross(rotationState.angularVelocity, offset));
                }
                Vector3 localAcceleration = state.target.InverseTransformDirection(anchorAcceleration);
                localForce -= Vector3.Scale(localAcceleration, inertia);
                Vector3 axis = state.localAxis.sqrMagnitude > 0.0001f ? state.localAxis.normalized : Vector3.down;
                Vector3 targetAngle = Vector3.Cross(axis, localForce) * (state.influence * 3.5f);

                Vector3 collisionForce = CalculateCollisionForce(state, activeSettings);
                if (collisionForce.sqrMagnitude > 0.0001f)
                {
                    Vector3 localCollision = state.target.InverseTransformDirection(collisionForce);
                    targetAngle += Vector3.Cross(axis, localCollision) * activeSettings.collisionStrength;
                    state.angularVelocity *= 1f - activeSettings.collisionFriction;
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
                                                 Quaternion.Euler(state.angle * activeSettings.animationBlend);
                state.target.localRotation = state.lastAppliedLocalRotation;
                ResolveCollisionConstraint(state, activeSettings, axisSettings, maxAngle);
            }
        }

        private void UpdateAnchorStates(float frameDt)
        {
            var activeAnchors = new HashSet<Transform>();
            foreach (NodeState state in nodeStates)
            {
                Transform anchor = state.simulationAnchor != null ? state.simulationAnchor : GetMotionAnchor();
                if (state.target == null || anchor == null || anchor == state.target || anchor.IsChildOf(state.target) || !activeAnchors.Add(anchor)) continue;
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
                    history.previousRotation = anchor.rotation;
                    history.angularVelocity = Vector3.zero;
                    history.angularAcceleration = Vector3.zero;
                    history.initialized = true;
                    continue;
                }
                Vector3 velocity = (position - history.previousPosition) / Mathf.Max(0.0001f, frameDt);
                history.acceleration = (velocity - history.previousVelocity) / Mathf.Max(0.0001f, frameDt);
                history.previousPosition = position;
                history.previousVelocity = velocity;
                Quaternion deltaRotation = anchor.rotation * Quaternion.Inverse(history.previousRotation);
                if (deltaRotation.w < 0f) deltaRotation = new Quaternion(-deltaRotation.x, -deltaRotation.y, -deltaRotation.z, -deltaRotation.w);
                deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
                Vector3 angularVelocity = angle > 0.001f && axis.sqrMagnitude > 0.0001f
                    ? axis.normalized * (angle * Mathf.Deg2Rad / Mathf.Max(0.0001f, frameDt)) : Vector3.zero;
                history.angularAcceleration = (angularVelocity - history.angularVelocity) / Mathf.Max(0.0001f, frameDt);
                history.angularVelocity = angularVelocity;
                history.previousRotation = anchor.rotation;
            }
        }

        public void PreviewStep(float deltaTime)
        {
            if (!simulate || !isActiveAndEnabled || Application.isPlaying) return;
            previewTime += deltaTime;
            Vector3 previewForce = settings.previewAutoSway
                ? new Vector3(Mathf.Sin(previewTime * 2.3f), 0f, Mathf.Cos(previewTime * 1.7f)) * 2f
                : Vector3.zero;
            // Explicit edit-mode sway is a preview command. Keep it visible even when the distance LOD
            // would normally enter its final "stop simulation" band.
            if (settings.previewAutoSway && settings.enableDistanceSimulation && GetDistanceStage() >= 3)
                SimulateStep(deltaTime, previewForce);
            else
                AdvanceScheduledSimulation(deltaTime, previewForce);
        }

        public void StopPreview()
        {
            if (Application.isPlaying) return;
            RestoreRestPose();
            previewTime = 0f;
            scheduledDeltaTime = 0f;
            scheduledFrameCount = 0;
            distanceSleeping = false;
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
                bool hasGroupSolo = boneChainGroups.Exists(group => group != null && group.soloPreview &&
                    IsGroupHierarchyEnabled(group));
                bool hasChainSolo = !hasGroupSolo && boneChains.Exists(chain => chain != null && chain.enabled &&
                    chain.solo && IsChainGroupEnabled(chain));
                foreach (BoneChain chain in boneChains)
                    if (IsChainGroupEnabled(chain) &&
                        (!hasGroupSolo || IsChainInSoloGroup(chain)) &&
                        (!hasChainSolo || (chain != null && chain.solo))) AddBoneChain(chain, used);
                if (!hasGroupSolo && !hasChainSolo)
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
                    if (group.replicatedPivots != null)
                        foreach (Transform replica in group.replicatedPivots)
                            if (replica != null && used.Add(replica))
                                AddState(replica, Vector3.down, Mathf.Max(0.05f, pivotGizmoRadius * 2f), 1f,
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
            AddBoneRecursive(chain.root, chain, used, 0, ResolveMotionSettings(chain));
        }

        private FDX_MotionSettings ResolveMotionSettings(BoneChain chain)
        {
            if (chain == null) return null;
            if (chain.useIndividualMotionSettings) return chain.motionSettings;
            BoneChainGroup group = FindOwningGroup(chain);
            var visited = new HashSet<string>();
            while (group != null)
            {
                if (group.useMotionSettings) return group.motionSettings;
                if (string.IsNullOrEmpty(group.parentId) || !visited.Add(group.parentId)) break;
                string parentId = group.parentId;
                group = boneChainGroups.Find(candidate => candidate != null && candidate.id == parentId);
            }
            return null;
        }

        private BoneChainGroup FindOwningGroup(BoneChain chain)
        {
            if (chain == null || string.IsNullOrEmpty(chain.editorId)) return null;
            return boneChainGroups.Find(group => group != null && group.chainIds != null &&
                group.chainIds.Contains(chain.editorId));
        }

        private bool IsGroupHierarchyEnabled(BoneChainGroup group)
        {
            var visited = new HashSet<string>();
            while (group != null)
            {
                if (!group.enabled) return false;
                if (string.IsNullOrEmpty(group.parentId) || !visited.Add(group.parentId)) break;
                string parentId = group.parentId;
                group = boneChainGroups.Find(candidate => candidate != null && candidate.id == parentId);
            }
            return true;
        }

        private bool IsChainGroupEnabled(BoneChain chain)
        {
            BoneChainGroup group = FindOwningGroup(chain);
            return group == null || IsGroupHierarchyEnabled(group);
        }

        private bool IsChainInSoloGroup(BoneChain chain)
        {
            BoneChainGroup group = FindOwningGroup(chain);
            var visited = new HashSet<string>();
            while (group != null)
            {
                if (group.soloPreview && IsGroupHierarchyEnabled(group)) return true;
                if (string.IsNullOrEmpty(group.parentId) || !visited.Add(group.parentId)) break;
                string parentId = group.parentId;
                group = boneChainGroups.Find(candidate => candidate != null && candidate.id == parentId);
            }
            return false;
        }

        private void EnsureDataIntegrity()
        {
            if (settings == null) settings = new FDX_MotionSettings();
            if (pivotAxisSettings == null) pivotAxisSettings = new FDX_AxisControlSettings();
            if (pivotGroups == null) pivotGroups = new List<PivotGroup>();
            if (boneChains == null) boneChains = new List<BoneChain>();
            if (boneChainGroups == null) boneChainGroups = new List<BoneChainGroup>();
            if (existingBones == null) existingBones = new List<BoneEntry>();
            if (endPoints == null) endPoints = new List<FlexibleEndPoint>();
            if (explicitColliders == null) explicitColliders = new List<Collider>();
            if (settings.explicitColliders == null) settings.explicitColliders = new List<Collider>();
            if (explicitColliders.Count > 0)
            {
                foreach (Collider collider in explicitColliders)
                    if (collider != null && !settings.explicitColliders.Contains(collider))
                        settings.explicitColliders.Add(collider);
                explicitColliders.Clear();
            }
            foreach (BoneChain chain in boneChains)
            {
                if (chain == null) continue;
                MigrateBoneChain(chain);
                if (string.IsNullOrWhiteSpace(chain.editorId)) chain.editorId = Guid.NewGuid().ToString("N");
                if (chain.excludedBones == null) chain.excludedBones = new List<Transform>();
                if (chain.axisSettings == null) chain.axisSettings = new FDX_AxisControlSettings();
                if (chain.motionSettings == null) chain.motionSettings = new FDX_MotionSettings();
                if (chain.motionSettings.explicitColliders == null)
                    chain.motionSettings.explicitColliders = new List<Collider>();
            }
            foreach (BoneChainGroup group in boneChainGroups)
            {
                if (group == null) continue;
                if (group.chainIds == null) group.chainIds = new List<string>();
                if (group.motionSettings == null) group.motionSettings = new FDX_MotionSettings();
                if (group.motionSettings.explicitColliders == null)
                    group.motionSettings.explicitColliders = new List<Collider>();
            }
            foreach (BoneEntry entry in existingBones)
                if (entry != null && entry.axisSettings == null) entry.axisSettings = new FDX_AxisControlSettings();
            foreach (FlexibleEndPoint point in endPoints) EnsureEndPointData(point);
            MigrateLegacyPivot();
            foreach (PivotGroup group in pivotGroups)
            {
                if (group == null) continue;
                MigratePivotReplication(group);
                if (group.axisSettings == null) group.axisSettings = new FDX_AxisControlSettings();
                if (group.endPoints == null) group.endPoints = new List<FlexibleEndPoint>();
                foreach (FlexibleEndPoint point in group.endPoints) EnsureEndPointData(point);
            }
            settings.fullSimulationDistance = Mathf.Max(0f, settings.fullSimulationDistance);
            settings.reducedSimulationDistance = Mathf.Max(settings.fullSimulationDistance, settings.reducedSimulationDistance);
            settings.minimalSimulationDistance = Mathf.Max(settings.reducedSimulationDistance, settings.minimalSimulationDistance);
            settings.customFrameIntervals = new Vector3Int(
                Mathf.Max(1, settings.customFrameIntervals.x),
                Mathf.Max(1, settings.customFrameIntervals.y),
                Mathf.Max(1, settings.customFrameIntervals.z));
            settings.customUpdateFrequencies = new Vector3(
                Mathf.Max(1f, settings.customUpdateFrequencies.x),
                Mathf.Max(1f, settings.customUpdateFrequencies.y),
                Mathf.Max(1f, settings.customUpdateFrequencies.z));
        }

        public void RefreshAutomaticSimulationAnchor(bool force)
        {
            if (!force && simulationAnchor != null && !simulationAnchorAutomaticallyAssigned) return;
            var roots = new List<Transform>();
            foreach (BoneChain chain in boneChains)
                if (chain != null && chain.enabled && chain.root != null) roots.Add(chain.root);
            if (roots.Count == 0) return;
            Transform common = roots[0].parent != null ? roots[0].parent : roots[0];
            while (common != null)
            {
                bool containsAll = true;
                foreach (Transform root in roots)
                {
                    if (root == common || root.IsChildOf(common)) continue;
                    containsAll = false;
                    break;
                }
                if (containsAll) break;
                common = common.parent;
            }
            simulationAnchor = common != null ? common : transform;
            simulationAnchorAutomaticallyAssigned = true;
        }

        public void ResetToDefaults()
        {
            StopPreview();
            motionSource = MotionSource.ExistingBones;
            simulate = true;
            forceSpace = ForceSpace.World;
            settings = new FDX_MotionSettings();
            simulationAnchor = null;
            simulationAnchorAutomaticallyAssigned = false;
            boneChains = new List<BoneChain>();
            boneChainGroups = new List<BoneChainGroup>();
            existingBones = new List<BoneEntry>();
            autoCreatePivot = true;
            rotationPivot = null;
            pivotInfluenceRadius = 0.15f;
            pivotAxisSettings = new FDX_AxisControlSettings();
            pivotGroups = new List<PivotGroup> { new PivotGroup { displayName = name + " 軸心" } };
            pivotGroupDataVersion = 1;
            enableAdvancedFlexible = false;
            endPoints = new List<FlexibleEndPoint>();
            enablePreciseWeights = false;
            deformingRenderer = null;
            automaticMultiPivotSkinning = false;
            originalMeshRenderer = null;
            originalMeshFilter = null;
            automaticMultiPivotRenderer = null;
            originalMeshMaterials = Array.Empty<Material>();
            originalMeshRendererEnabled = true;
            explicitColliders = new List<Collider>();
            showGizmos = true;
            showAllInfluenceRanges = true;
            showCollisionRadiusGizmos = true;
            pivotColor = new Color(0.1f, 0.85f, 1f, 0.9f);
            boneRootColor = new Color(0.2f, 1f, 0.45f, 0.95f);
            endPointColor = new Color(1f, 0.55f, 0.1f, 0.9f);
            mirrorColor = new Color(0.7f, 0.35f, 1f, 0.75f);
            collisionRadiusColor = new Color(1f, 0.25f, 0.05f, 0.95f);
            pivotGizmoRadius = 0.08f;
            endPointGizmoRadius = 0.045f;
            boneCandidatesExpanded = true;
            boneChainsExpanded = true;
            pivotGroupsExpanded = true;
            motionSettingsExpanded = true;
            globalMotionSettingsExpanded = true;
            initialized = false;
        }

        private static void MigrateBoneChain(BoneChain chain)
        {
            if (chain.dataVersion > 0) return;
            if (chain.endBone != null && chain.root != null)
            {
                int levels = 1;
                Transform current = chain.endBone;
                while (current != null && current != chain.root)
                {
                    levels++;
                    current = current.parent;
                }
                if (current == chain.root)
                {
                    chain.allBonesSway = false;
                    chain.boneMotionLevels = Mathf.Max(1, levels - (chain.includeEndBone ? 0 : 1));
                }
            }
            chain.dataVersion = 1;
        }

        private static void MigratePivotReplication(PivotGroup group)
        {
            if (group.dataVersion > 0) return;
            if (group.liveMirror)
            {
                group.replicationMode = PivotReplicationMode.Mirror;
                group.mirrorX = group.mirrorAxis == MirrorAxis.X;
                group.mirrorY = group.mirrorAxis == MirrorAxis.Y;
                group.mirrorZ = group.mirrorAxis == MirrorAxis.Z;
            }
            group.radialCopies = Mathf.Clamp(group.radialCopies, 1, 32);
            group.dataVersion = 1;
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

        private bool AddBoneRecursive(Transform bone, BoneChain chain, HashSet<Transform> used, int depth,
            FDX_MotionSettings motionSettings)
        {
            if (bone == null || chain.excludedBones.Contains(bone)) return false;
            if (used.Add(bone))
            {
                Transform directionChild = FindFirstUsableChild(bone, chain);
                Vector3 axis = directionChild != null
                    ? bone.InverseTransformDirection(directionChild.position - bone.position)
                    : Vector3.down;
                float length = directionChild != null ? Vector3.Distance(bone.position, directionChild.position) : 0.1f;
                AddState(bone, axis, length, chain.influence, chain.axisSettings, null, motionSettings);
            }
            if (!chain.includeChildBones || (!chain.allBonesSway && depth + 1 >= Mathf.Max(1, chain.boneMotionLevels)))
                return true;
            int childLimit = chain.includeAllBranches ? bone.childCount : Mathf.Min(1, bone.childCount);
            for (int i = 0; i < childLimit; i++)
                AddBoneRecursive(bone.GetChild(i), chain, used, depth + 1, motionSettings);
            return true;
        }

        public static int GetBoneChainDepth(BoneChain chain)
        {
            if (chain == null || chain.root == null) return 1;
            return GetBoneDepthRecursive(chain.root, chain, 1);
        }

        private static int GetBoneDepthRecursive(Transform bone, BoneChain chain, int level)
        {
            int maximum = level;
            int childLimit = chain.includeAllBranches ? bone.childCount : Mathf.Min(1, bone.childCount);
            for (int i = 0; i < childLimit; i++)
            {
                Transform child = bone.GetChild(i);
                if (chain.excludedBones.Contains(child)) continue;
                maximum = Mathf.Max(maximum, GetBoneDepthRecursive(child, chain, level + 1));
            }
            return maximum;
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
                state.previousTipPosition = GetNodeTip(state);
                state.collisionTipInitialized = false;
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
            FDX_AxisControlSettings axisSettings, Transform anchor = null, FDX_MotionSettings motionSettingsOverride = null)
        {
            var state = new NodeState
            {
                target = target,
                restLocalRotation = target.localRotation,
                lastAppliedLocalRotation = target.localRotation,
                localAxis = localAxis.sqrMagnitude > 0.0001f ? localAxis.normalized : Vector3.down,
                length = Mathf.Max(0.001f, length),
                influence = influence,
                axisSettings = axisSettings ?? new FDX_AxisControlSettings(),
                motionSettings = motionSettingsOverride,
                simulationAnchor = anchor
            };
            state.previousTipPosition = GetNodeTip(state);
            nodeStates.Add(state);
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
            if ((pivotGroups.Count > 1 || automaticMultiPivotSkinning || group.replicationMode != PivotReplicationMode.None) && group.pivot != null) return group.pivot;
            if (group.pivot == null) return transform;
            return group.pivot.IsChildOf(transform) ? transform : group.pivot;
        }

        private Transform ResolveSimulationAnchor(PivotGroup group, Transform motionTarget)
        {
            if (!group.automaticSimulationAnchor && group.simulationAnchor != null) return group.simulationAnchor;
            if (attachmentAnchor != null) return attachmentAnchor;
            if (automaticMultiPivotSkinning && motionTarget != null && motionTarget.IsChildOf(transform))
                return transform.parent != null ? transform.parent : transform;
            if (motionTarget != null && motionTarget.parent != null) return motionTarget.parent;
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
            if (simulationAnchor != null && !simulationAnchorAutomaticallyAssigned) return simulationAnchor;
            if (attachmentAnchor != null) return attachmentAnchor;
            if (motionSource == MotionSource.AutomaticPivot)
            {
                Transform motionTarget = GetAutomaticMotionTarget();
                return motionTarget.parent != null ? motionTarget.parent : motionTarget;
            }
            if (nodeStates.Count > 0 && nodeStates[0].target != null)
                return nodeStates[0].target.parent != null ? nodeStates[0].target.parent : nodeStates[0].target;
            return transform.parent != null ? transform.parent : transform;
        }

        private Vector3 CalculateCollisionForce(NodeState state, FDX_MotionSettings activeSettings)
        {
            if (activeSettings == null || !activeSettings.enableCollision || state.target == null) return Vector3.zero;
            Vector3 root = GetCollisionSegmentStart(state);
            Vector3 tip = GetNodeTip(state);
            CollectCollisionCandidates(root, tip, state, activeSettings);
            return CalculateSegmentCollisionCorrection(root, tip, activeSettings.collisionRadius);
        }

        private void ResolveCollisionConstraint(NodeState state, FDX_MotionSettings activeSettings,
            FDX_AxisControlSettings axisSettings, Vector3 maxAngle)
        {
            if (activeSettings == null || !activeSettings.enableCollision || state.target == null) return;
            float blend = Mathf.Abs(activeSettings.animationBlend);
            if (blend < 0.0001f) return;

            for (int iteration = 0; iteration < 3; iteration++)
            {
                Vector3 root = GetCollisionSegmentStart(state);
                Vector3 tip = GetNodeTip(state);
                CollectCollisionCandidates(root, tip, state, activeSettings);
                Vector3 correction = CalculateSegmentCollisionCorrection(root, tip, activeSettings.collisionRadius);
                if (state.collisionTipInitialized)
                {
                    Vector3 sweep = CalculateSegmentCollisionCorrection(state.previousTipPosition, tip,
                        activeSettings.collisionRadius);
                    if (sweep.sqrMagnitude > correction.sqrMagnitude) correction = sweep;
                }
                if (correction.sqrMagnitude < 0.00000001f) break;
                Vector3 currentDirection = tip - state.target.position;
                Vector3 desiredDirection = tip + correction - state.target.position;
                if (currentDirection.sqrMagnitude < 0.000001f || desiredDirection.sqrMagnitude < 0.000001f) break;
                Quaternion worldDelta = Quaternion.FromToRotation(currentDirection, desiredDirection);
                Quaternion desiredWorld = worldDelta * state.target.rotation;
                Quaternion desiredLocal = state.target.parent != null
                    ? Quaternion.Inverse(state.target.parent.rotation) * desiredWorld
                    : desiredWorld;
                Vector3 desiredAngle = ToSignedEuler(Quaternion.Inverse(state.restLocalRotation) * desiredLocal) / blend;
                state.angle = axisSettings.ApplyLocks(ClampComponents(desiredAngle, maxAngle));
                state.angularVelocity *= Mathf.Clamp01(1f - activeSettings.collisionFriction);
                state.lastAppliedLocalRotation = state.restLocalRotation *
                                                 Quaternion.Euler(state.angle * activeSettings.animationBlend);
                state.target.localRotation = state.lastAppliedLocalRotation;
            }
            state.previousTipPosition = GetNodeTip(state);
            state.collisionTipInitialized = true;
        }

        private Vector3 GetCollisionSegmentStart(NodeState state)
        {
            Vector3 root = state.target.position;
            return Vector3.Lerp(root, GetNodeTip(state), 0.05f);
        }

        private static Vector3 GetNodeTip(NodeState state)
        {
            if (state == null || state.target == null) return Vector3.zero;
            Vector3 worldAxis = state.target.TransformDirection(state.localAxis);
            if (worldAxis.sqrMagnitude < 0.0001f) worldAxis = -state.target.up;
            return state.target.position + worldAxis.normalized * state.length;
        }

        private void CollectCollisionCandidates(Vector3 start, Vector3 end, NodeState state,
            FDX_MotionSettings activeSettings)
        {
            collisionCandidates.Clear();
            foreach (Collider col in activeSettings.explicitColliders)
                if (IsUsableCollider(col)) collisionCandidates.Add(col);
            int count = Physics.OverlapCapsuleNonAlloc(start, end, activeSettings.collisionRadius, collisionBuffer,
                activeSettings.collisionLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (IsUsableCollider(collisionBuffer[i])) collisionCandidates.Add(collisionBuffer[i]);
            if (!state.collisionTipInitialized) return;
            count = Physics.OverlapCapsuleNonAlloc(state.previousTipPosition, end, activeSettings.collisionRadius,
                collisionBuffer, activeSettings.collisionLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (IsUsableCollider(collisionBuffer[i])) collisionCandidates.Add(collisionBuffer[i]);
        }

        private bool IsUsableCollider(Collider col)
        {
            if (col == null || !col.enabled || col.isTrigger) return false;
            foreach (NodeState state in nodeStates)
            {
                if (state == null || state.target == null) continue;
                if (col.transform == state.target || col.transform.IsChildOf(state.target)) return false;
            }
            return true;
        }

        private Vector3 CalculateSegmentCollisionCorrection(Vector3 start, Vector3 end, float radius)
        {
            Vector3 best = Vector3.zero;
            foreach (Collider col in collisionCandidates)
            {
                Vector3 correction = GetColliderSegmentCorrection(col, start, end, radius);
                if (correction.sqrMagnitude > best.sqrMagnitude) best = correction;
            }
            return best;
        }

        private static Vector3 GetColliderSegmentCorrection(Collider col, Vector3 start, Vector3 end, float radius)
        {
            if (col is SphereCollider sphere)
            {
                Vector3 center = sphere.transform.TransformPoint(sphere.center);
                Vector3 scale = Abs(sphere.transform.lossyScale);
                float colliderRadius = sphere.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
                Vector3 point = ClosestPointOnSegment(start, end, center);
                return GetRadialCorrection(point - center, colliderRadius + radius, sphere.transform.up);
            }
            if (col is CapsuleCollider capsule)
            {
                GetCapsuleAxis(capsule, out Vector3 capsuleStart, out Vector3 capsuleEnd, out float colliderRadius);
                ClosestPointsOnSegments(start, end, capsuleStart, capsuleEnd, out Vector3 point, out Vector3 capsulePoint);
                return GetRadialCorrection(point - capsulePoint, colliderRadius + radius, capsule.transform.up);
            }
            Vector3 best = Vector3.zero;
            const int samples = 24;
            for (int i = 0; i <= samples; i++)
            {
                Vector3 correction = GetColliderCorrection(col, Vector3.Lerp(start, end, i / (float)samples), radius);
                if (correction.sqrMagnitude > best.sqrMagnitude) best = correction;
            }
            return best;
        }

        private static Vector3 GetColliderCorrection(Collider col, Vector3 point, float radius)
        {
            if (col == null || !col.enabled) return Vector3.zero;
            if (col is SphereCollider sphere) return GetSphereCorrection(sphere, point, radius);
            if (col is CapsuleCollider capsule) return GetCapsuleCorrection(capsule, point, radius);
            if (col is BoxCollider box) return GetBoxCorrection(box, point, radius);
            Vector3 closest = col.ClosestPoint(point);
            Vector3 delta = point - closest;
            float distance = delta.magnitude;
            if (distance >= radius) return Vector3.zero;
            if (distance < 0.0001f)
            {
                delta = point - col.bounds.center;
                if (delta.sqrMagnitude < 0.0001f) delta = Vector3.up;
            }
            return delta.normalized * (radius - distance);
        }

        private static Vector3 GetSphereCorrection(SphereCollider sphere, Vector3 point, float radius)
        {
            Vector3 center = sphere.transform.TransformPoint(sphere.center);
            Vector3 scale = Abs(sphere.transform.lossyScale);
            float colliderRadius = sphere.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
            return GetRadialCorrection(point - center, colliderRadius + radius, sphere.transform.up);
        }

        private static Vector3 GetCapsuleCorrection(CapsuleCollider capsule, Vector3 point, float radius)
        {
            GetCapsuleAxis(capsule, out Vector3 start, out Vector3 end, out float colliderRadius);
            Vector3 closest = ClosestPointOnSegment(start, end, point);
            return GetRadialCorrection(point - closest, colliderRadius + radius, capsule.transform.up);
        }

        private static void GetCapsuleAxis(CapsuleCollider capsule, out Vector3 start, out Vector3 end,
            out float colliderRadius)
        {
            Vector3 scale = Abs(capsule.transform.lossyScale);
            Vector3 localAxis = capsule.direction == 0 ? Vector3.right : capsule.direction == 1 ? Vector3.up : Vector3.forward;
            float axisScale = capsule.direction == 0 ? scale.x : capsule.direction == 1 ? scale.y : scale.z;
            float radialScale = capsule.direction == 0 ? Mathf.Max(scale.y, scale.z) :
                capsule.direction == 1 ? Mathf.Max(scale.x, scale.z) : Mathf.Max(scale.x, scale.y);
            colliderRadius = capsule.radius * radialScale;
            float halfLine = Mathf.Max(0f, capsule.height * axisScale * 0.5f - colliderRadius);
            Vector3 center = capsule.transform.TransformPoint(capsule.center);
            Vector3 axis = capsule.transform.TransformDirection(localAxis).normalized;
            start = center - axis * halfLine;
            end = center + axis * halfLine;
        }

        private static Vector3 GetBoxCorrection(BoxCollider box, Vector3 point, float radius)
        {
            Vector3 local = box.transform.InverseTransformPoint(point) - box.center;
            Vector3 half = box.size * 0.5f;
            Vector3 clamped = new Vector3(Mathf.Clamp(local.x, -half.x, half.x),
                Mathf.Clamp(local.y, -half.y, half.y), Mathf.Clamp(local.z, -half.z, half.z));
            Vector3 closestWorld = box.transform.TransformPoint(box.center + clamped);
            Vector3 delta = point - closestWorld;
            if (delta.sqrMagnitude > 0.00000001f)
            {
                float distance = delta.magnitude;
                return distance < radius ? delta.normalized * (radius - distance) : Vector3.zero;
            }
            Vector3 gaps = half - Abs(local);
            int axis = gaps.x <= gaps.y && gaps.x <= gaps.z ? 0 : gaps.y <= gaps.z ? 1 : 2;
            Vector3 localNormal = axis == 0 ? Vector3.right * (local.x >= 0f ? 1f : -1f) :
                axis == 1 ? Vector3.up * (local.y >= 0f ? 1f : -1f) : Vector3.forward * (local.z >= 0f ? 1f : -1f);
            Vector3 surfaceLocal = local;
            if (axis == 0) surfaceLocal.x = localNormal.x * half.x;
            else if (axis == 1) surfaceLocal.y = localNormal.y * half.y;
            else surfaceLocal.z = localNormal.z * half.z;
            Vector3 surfaceWorld = box.transform.TransformPoint(box.center + surfaceLocal);
            Vector3 outward = surfaceWorld - point;
            return outward.sqrMagnitude > 0.00000001f
                ? outward + outward.normalized * radius
                : box.transform.TransformDirection(localNormal) * radius;
        }

        private static Vector3 GetRadialCorrection(Vector3 delta, float requiredDistance, Vector3 fallback)
        {
            float distance = delta.magnitude;
            if (distance >= requiredDistance) return Vector3.zero;
            Vector3 direction = distance > 0.0001f ? delta / distance : fallback.normalized;
            return direction * (requiredDistance - distance);
        }

        private static Vector3 ClosestPointOnSegment(Vector3 start, Vector3 end, Vector3 point)
        {
            Vector3 segment = end - start;
            float denominator = segment.sqrMagnitude;
            if (denominator < 0.000001f) return start;
            return start + segment * Mathf.Clamp01(Vector3.Dot(point - start, segment) / denominator);
        }

        private static void ClosestPointsOnSegments(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2,
            out Vector3 first, out Vector3 second)
        {
            Vector3 d1 = q1 - p1;
            Vector3 d2 = q2 - p2;
            Vector3 r = p1 - p2;
            float a = Vector3.Dot(d1, d1);
            float e = Vector3.Dot(d2, d2);
            float f = Vector3.Dot(d2, r);
            float s;
            float t;
            if (a <= 0.000001f && e <= 0.000001f) { first = p1; second = p2; return; }
            if (a <= 0.000001f) { s = 0f; t = Mathf.Clamp01(f / e); }
            else
            {
                float c = Vector3.Dot(d1, r);
                if (e <= 0.000001f) { t = 0f; s = Mathf.Clamp01(-c / a); }
                else
                {
                    float b = Vector3.Dot(d1, d2);
                    float denominator = a * e - b * b;
                    s = denominator != 0f ? Mathf.Clamp01((b * f - c * e) / denominator) : 0f;
                    t = (b * s + f) / e;
                    if (t < 0f) { t = 0f; s = Mathf.Clamp01(-c / a); }
                    else if (t > 1f) { t = 1f; s = Mathf.Clamp01((b - c) / a); }
                }
            }
            first = p1 + d1 * s;
            second = p2 + d2 * t;
        }

        private static Vector3 Abs(Vector3 value) =>
            new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

        private static Vector3 ToSignedEuler(Quaternion rotation)
        {
            Vector3 value = rotation.eulerAngles;
            if (value.x > 180f) value.x -= 360f;
            if (value.y > 180f) value.y -= 360f;
            if (value.z > 180f) value.z -= 360f;
            return value;
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
            if (motionSource == MotionSource.ExistingBones)
            {
                DrawBoneChainGizmos();
                DrawCollisionRadiusGizmos();
                return;
            }
            foreach (PivotGroup group in pivotGroups)
            {
                if (group == null || !group.enabled) continue;
                Transform pivot = group.pivot != null ? group.pivot : transform;
                Color solidPivot = pivotColor;
                solidPivot.a = Mathf.Min(solidPivot.a, 0.3f);
                Gizmos.color = solidPivot;
                Gizmos.DrawSphere(pivot.position, pivotGizmoRadius * 0.3f);
                DrawPivotOrientation(pivot);
                Gizmos.color = pivotColor;
                if (showAllInfluenceRanges) Gizmos.DrawWireSphere(pivot.position, group.influenceRadius);
                if (group.enableAdvancedFlexible)
                    foreach (FlexibleEndPoint point in group.endPoints)
                        DrawEndPointGizmos(point, pivot, false, pivot.position, pivot.rotation);
                DrawReplicatedPivotGizmos(group, pivot);
            }
            DrawCollisionRadiusGizmos();
        }

        private void DrawCollisionRadiusGizmos()
        {
            if (!showCollisionRadiusGizmos) return;
            Gizmos.color = collisionRadiusColor;
            foreach (NodeState state in nodeStates)
            {
                FDX_MotionSettings activeSettings = state.motionSettings ?? settings;
                if (state.target == null || activeSettings == null || !activeSettings.enableCollision) continue;
                Vector3 start = GetCollisionSegmentStart(state);
                Vector3 end = GetNodeTip(state);
                Gizmos.DrawWireSphere(start, activeSettings.collisionRadius);
                Gizmos.DrawWireSphere(end, activeSettings.collisionRadius);
                DrawCylinderWire(start, end, activeSettings.collisionRadius);
            }
        }

        private void DrawPivotOrientation(Transform pivot)
        {
            NodeState state = nodeStates.Find(item => item.target == pivot);
            Quaternion rest = state != null
                ? (pivot.parent != null ? pivot.parent.rotation : Quaternion.identity) * state.restLocalRotation
                : pivot.rotation;
            float length = Mathf.Max(0.02f, pivotGizmoRadius * 4f);
            Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
            Color[] colors = { Color.red, Color.green, Color.blue };
            for (int i = 0; i < axes.Length; i++)
            {
                Gizmos.color = new Color(colors[i].r, colors[i].g, colors[i].b, 0.22f);
                Gizmos.DrawLine(pivot.position, pivot.position + rest * axes[i] * length);
                Gizmos.color = colors[i];
                Vector3 direction = pivot.rotation * axes[i];
                Vector3 end = pivot.position + direction * length;
                Gizmos.DrawLine(pivot.position, end);
                Vector3 side = pivot.rotation * axes[(i + 1) % 3] * (length * 0.12f);
                Gizmos.DrawLine(end, end - direction * (length * 0.2f) + side);
                Gizmos.DrawLine(end, end - direction * (length * 0.2f) - side);
            }
        }

        private void DrawReplicatedPivotGizmos(PivotGroup group, Transform pivot)
        {
            if (group.replicatedPivots != null && group.replicatedPivots.Count > 0)
            {
                foreach (Transform replica in group.replicatedPivots)
                {
                    if (replica == null) continue;
                    DrawPivotOrientation(replica);
                    Gizmos.color = mirrorColor;
                    Gizmos.DrawSphere(replica.position, pivotGizmoRadius * 0.3f);
                    if (showAllInfluenceRanges) Gizmos.DrawWireSphere(replica.position, group.influenceRadius);
                }
                return;
            }
            var positions = new List<Vector3>();
            GetReplicatedPivotPositions(group, positions);
            foreach (Vector3 position in positions)
            {
                Color solidMirror = mirrorColor;
                solidMirror.a = Mathf.Min(solidMirror.a, 0.3f);
                Gizmos.color = solidMirror;
                Gizmos.DrawSphere(position, pivotGizmoRadius);
                Gizmos.color = mirrorColor;
                if (showAllInfluenceRanges) Gizmos.DrawWireSphere(position, group.influenceRadius);
            }
        }

        public void GetReplicatedPivotPositions(PivotGroup group, List<Vector3> results)
        {
            results.Clear();
            if (group == null || group.pivot == null || group.replicationMode == PivotReplicationMode.None) return;
            Transform center = group.mirrorCenter != null ? group.mirrorCenter : transform;
            Vector3 local = center.InverseTransformPoint(group.pivot.position);
            if (group.replicationMode == PivotReplicationMode.Mirror)
            {
                int axisMask = (group.mirrorX ? 1 : 0) | (group.mirrorY ? 2 : 0) | (group.mirrorZ ? 4 : 0);
                for (int mask = 1; mask < 8; mask++)
                {
                    if ((mask & ~axisMask) != 0) continue;
                    Vector3 mirrored = local;
                    if ((mask & 1) != 0) mirrored.x = -mirrored.x;
                    if ((mask & 2) != 0) mirrored.y = -mirrored.y;
                    if ((mask & 4) != 0) mirrored.z = -mirrored.z;
                    results.Add(center.TransformPoint(mirrored));
                }
                return;
            }

            int copies = Mathf.Clamp(group.radialCopies, 1, 32);
            float spacing = 360f / (copies + 1f);
            Vector3 axis = group.radialAxis == MirrorAxis.X ? Vector3.right :
                group.radialAxis == MirrorAxis.Y ? Vector3.up : Vector3.forward;
            for (int i = 1; i <= copies; i++)
                results.Add(center.TransformPoint(Quaternion.AngleAxis(spacing * i, axis) * local));
        }

        private void DrawBoneChainGizmos()
        {
            foreach (BoneChain chain in boneChains)
                if (chain != null && chain.enabled && chain.root != null && IsChainGroupEnabled(chain))
                    DrawBoneGizmoRecursive(chain.root, chain, 0);
        }

        private void DrawBoneGizmoRecursive(Transform bone, BoneChain chain, int depth)
        {
            if (bone == null || chain.excludedBones.Contains(bone)) return;
            float radius = chain.displayRadius * (chain.scaleGizmoByDepth ? Mathf.Pow(0.8f, depth) : 1f);
            Gizmos.color = depth == 0 ? boneRootColor : endPointColor;
            Gizmos.DrawWireSphere(bone.position, radius);
            if (!chain.includeChildBones || (!chain.allBonesSway && depth + 1 >= Mathf.Max(1, chain.boneMotionLevels))) return;
            int limit = chain.includeAllBranches ? bone.childCount : Mathf.Min(1, bone.childCount);
            for (int i = 0; i < limit; i++)
            {
                Transform child = bone.GetChild(i);
                if (chain.excludedBones.Contains(child)) continue;
                float childRadius = chain.displayRadius * (chain.scaleGizmoByDepth ? Mathf.Pow(0.8f, depth + 1) : 1f);
                DrawCylinderWire(bone.position, child.position, Mathf.Min(radius, childRadius));
                DrawBoneGizmoRecursive(child, chain, depth + 1);
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
