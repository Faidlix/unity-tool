using System;
using System.Collections.Generic;
using UnityEngine;

namespace Faidlix.UnityTools
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(9000)]
    [AddComponentMenu("FDX/Attachment Motion/Attachment Manager")]
    public sealed class FDX_AttachmentManager : MonoBehaviour
    {
        public enum MotionSettingsMode
        {
            Unified,
            PerAttachment
        }

        public enum AnchorMode
        {
            HumanoidBone,
            DirectTransform,
            NameOrPath
        }

        [Serializable]
        public sealed class AttachmentSlot
        {
            public string displayName = "New Attachment";
            public bool enabled = true;
            [Tooltip("Prefab 資產或場景中的現有物件。")]
            public GameObject source;
            public AnchorMode anchorMode = AnchorMode.HumanoidBone;
            [HideInInspector] public int dataVersion = 1;
            [Tooltip("開啟後使用 Humanoid Animator 的標準骨頭；關閉則使用自訂掛點。")]
            [HideInInspector]
            public bool useHumanoidBone = true;
            public HumanBodyBones humanoidBone = HumanBodyBones.Head;
            public Transform customAnchor;
            public Transform searchRoot;
            public string anchorNameOrPath;
            public bool ignoreCase = true;
            public bool ignoreNamespace = true;
            [Tooltip("Prefab 在執行時會自動建立；場景物件可選擇直接重新掛接。")]
            public bool instantiateAtRuntime = true;
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
            public Vector3 localScale = Vector3.one;
            [Tooltip("在個別設定模式下，勾選後不會套用管理器的統一物理設定。")]
            public bool useIndividualMotionSettings;

            [NonSerialized] public GameObject runtimeInstance;
        }

        [Serializable]
        private sealed class MotionOverride
        {
            public FDX_SecondaryMotion motion;
            public bool useIndividualSettings;
            public bool expanded;
        }

        [Header("角色")]
        [SerializeField] private Animator animator;

        [Header("骨架掛物件設定")]
        [SerializeField] private List<AttachmentSlot> attachments = new List<AttachmentSlot>();

        [Header("動態設定同步")]
        [SerializeField] private MotionSettingsMode settingsMode = MotionSettingsMode.Unified;
        [SerializeField] private FDX_MotionSettings sharedMotionSettings = new FDX_MotionSettings();
        [SerializeField] private bool applySharedSettingsOnAttach = true;
        [SerializeField] private bool sharedSimulate = true;
        [SerializeField] private FDX_SecondaryMotion.ForceSpace sharedForceSpace = FDX_SecondaryMotion.ForceSpace.World;
        [SerializeField] private List<MotionOverride> motionOverrides = new List<MotionOverride>();

        [HideInInspector, SerializeField] private bool previewInEditMode;
        [HideInInspector, SerializeField] private bool attachmentsExpanded = true;
        [HideInInspector, SerializeField] private bool detectedMotionsExpanded = true;
        [HideInInspector, SerializeField] private bool sharedSettingsExpanded = true;

        private readonly List<GameObject> ownedRuntimeInstances = new List<GameObject>();

        public Animator Animator => animator;
        public IReadOnlyList<AttachmentSlot> Attachments => attachments;
        public MotionSettingsMode SettingsMode => settingsMode;
        public FDX_MotionSettings SharedMotionSettings => sharedMotionSettings;
        public bool SharedSimulate { get => sharedSimulate; set => sharedSimulate = value; }
        public FDX_SecondaryMotion.ForceSpace SharedForceSpace { get => sharedForceSpace; set => sharedForceSpace = value; }
        public bool SharedSettingsEnabled
        {
            get => applySharedSettingsOnAttach && settingsMode == MotionSettingsMode.Unified;
            set
            {
                applySharedSettingsOnAttach = value;
                settingsMode = value ? MotionSettingsMode.Unified : MotionSettingsMode.PerAttachment;
            }
        }
        public bool AttachmentsExpanded { get => attachmentsExpanded; set => attachmentsExpanded = value; }
        public bool DetectedMotionsExpanded { get => detectedMotionsExpanded; set => detectedMotionsExpanded = value; }
        public bool SharedSettingsExpanded { get => sharedSettingsExpanded; set => sharedSettingsExpanded = value; }
        public bool PreviewInEditMode
        {
            get => previewInEditMode;
            set => previewInEditMode = value;
        }

        private void Reset()
        {
            EnsureDataIntegrity();
            animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void OnValidate() => EnsureDataIntegrity();

        private void Awake()
        {
            EnsureDataIntegrity();
            if (animator == null) animator = GetComponent<Animator>();
        }

        private void EnsureDataIntegrity()
        {
            if (attachments == null) attachments = new List<AttachmentSlot>();
            if (sharedMotionSettings == null) sharedMotionSettings = new FDX_MotionSettings();
            if (motionOverrides == null) motionOverrides = new List<MotionOverride>();
        }

        private void Start()
        {
            AttachAll();
        }

        private void OnDestroy()
        {
            for (int i = ownedRuntimeInstances.Count - 1; i >= 0; i--)
            {
                if (ownedRuntimeInstances[i] != null) Destroy(ownedRuntimeInstances[i]);
            }
            ownedRuntimeInstances.Clear();
        }

        [ContextMenu("Attach All")]
        public void AttachAll()
        {
            for (int i = 0; i < attachments.Count; i++) Attach(i);
        }

        public AttachmentSlot AddAttachment()
        {
            var slot = new AttachmentSlot();
            attachments.Add(slot);
            return slot;
        }

        public bool RemoveAttachment(AttachmentSlot slot)
        {
            if (slot == null) return false;
            int index = attachments.IndexOf(slot);
            if (index < 0) return false;
            Detach(index);
            attachments.RemoveAt(index);
            return true;
        }

        public GameObject Attach(int index)
        {
            if (index < 0 || index >= attachments.Count) return null;
            AttachmentSlot slot = attachments[index];
            if (!slot.enabled || slot.source == null) return null;

            Transform anchor = ResolveAnchor(slot);
            if (anchor == null)
            {
                Debug.LogWarning($"[FDX Attachment] '{slot.displayName}' 找不到有效掛點。", this);
                return null;
            }

            GameObject instance = ResolveInstance(slot);
            if (instance == null) return null;

            instance.transform.SetParent(anchor, false);
            instance.transform.localPosition = slot.localPosition;
            instance.transform.localRotation = Quaternion.Euler(slot.localEulerAngles);
            instance.transform.localScale = slot.localScale;
            slot.runtimeInstance = instance;

            if (applySharedSettingsOnAttach && settingsMode == MotionSettingsMode.Unified &&
                !slot.useIndividualMotionSettings)
            {
                ApplySettings(instance, sharedMotionSettings);
            }

            return instance;
        }

        public void Detach(int index, bool destroyOwnedInstance = true)
        {
            if (index < 0 || index >= attachments.Count) return;
            AttachmentSlot slot = attachments[index];
            GameObject instance = slot.runtimeInstance;
            slot.runtimeInstance = null;
            if (instance == null) return;

            bool isOwned = ownedRuntimeInstances.Remove(instance);
            if (isOwned && destroyOwnedInstance)
                Destroy(instance);
            else
                instance.transform.SetParent(null, true);
        }

        public Transform ResolveAnchor(AttachmentSlot slot)
        {
            if (slot == null) return null;
            AnchorMode mode = slot.dataVersion == 0
                ? (slot.useHumanoidBone ? AnchorMode.HumanoidBone : AnchorMode.DirectTransform)
                : slot.anchorMode;
            if (mode == AnchorMode.DirectTransform) return slot.customAnchor;
            if (mode == AnchorMode.NameOrPath) return ResolveAnchorByNameOrPath(slot);
            if (animator == null || !animator.isHuman) return null;
            return animator.GetBoneTransform(slot.humanoidBone);
        }

        public Transform ResolveAnchorByNameOrPath(AttachmentSlot slot)
        {
            if (slot == null || string.IsNullOrWhiteSpace(slot.anchorNameOrPath)) return null;
            Transform root = slot.searchRoot != null
                ? slot.searchRoot
                : animator != null ? animator.transform : transform;
            string query = slot.anchorNameOrPath.Trim().Replace('\\', '/');
            Transform byPath = root.Find(query);
            if (byPath != null) return byPath;

            string targetName = NormalizeName(query.Contains("/") ? query.Substring(query.LastIndexOf('/') + 1) : query,
                slot.ignoreNamespace);
            Transform match = null;
            var stack = new Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                Transform current = stack.Pop();
                string candidate = NormalizeName(current.name, slot.ignoreNamespace);
                StringComparison comparison = slot.ignoreCase
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;
                if (string.Equals(candidate, targetName, comparison))
                {
                    if (match != null)
                    {
                        Debug.LogWarning($"[FDX Attachment] '{slot.displayName}' 找到多個同名掛點 '{targetName}'，請改用完整相對路徑。", this);
                        return null;
                    }
                    match = current;
                }
                for (int i = 0; i < current.childCount; i++) stack.Push(current.GetChild(i));
            }
            return match;
        }

        private static string NormalizeName(string value, bool ignoreNamespace)
        {
            if (!ignoreNamespace || string.IsNullOrEmpty(value)) return value;
            int colon = value.LastIndexOf(':');
            return colon >= 0 ? value.Substring(colon + 1) : value;
        }

        public List<FDX_SecondaryMotion> FindMotionComponents(AttachmentSlot slot)
        {
            var results = new List<FDX_SecondaryMotion>();
            if (slot == null) return results;
            GameObject root = slot.runtimeInstance != null ? slot.runtimeInstance : slot.source;
            if (root != null) root.GetComponentsInChildren(true, results);
            return results;
        }

        public List<FDX_SecondaryMotion> FindHierarchyMotionComponents()
        {
            var results = new List<FDX_SecondaryMotion>();
            GameObject root = animator != null ? animator.gameObject : gameObject;
            root.GetComponentsInChildren(true, results);
            return results;
        }

        public List<FDX_SecondaryMotion> FindAllMotionComponents()
        {
            var results = new List<FDX_SecondaryMotion>();
            var found = new HashSet<FDX_SecondaryMotion>();

            AddUnique(FindHierarchyMotionComponents(), found, results);
            foreach (AttachmentSlot slot in attachments)
                AddUnique(FindMotionComponents(slot), found, results);

            return results;
        }

        public void ApplySharedSettingsToAll()
        {
            var individualMotions = new HashSet<FDX_SecondaryMotion>();
            foreach (AttachmentSlot slot in attachments)
            {
                if (slot == null || !slot.enabled || !slot.useIndividualMotionSettings) continue;
                foreach (FDX_SecondaryMotion motion in FindMotionComponents(slot))
                    if (motion != null) individualMotions.Add(motion);
            }

            foreach (MotionOverride entry in motionOverrides)
                if (entry != null && entry.motion != null && entry.useIndividualSettings)
                    individualMotions.Add(entry.motion);

            foreach (FDX_SecondaryMotion motion in FindAllMotionComponents())
                if (motion != null && !individualMotions.Contains(motion))
                {
                    motion.Simulate = sharedSimulate;
                    motion.Forces = sharedForceSpace;
                    motion.ApplySettings(sharedMotionSettings, true);
                }
        }

        public bool UsesIndividualSettings(FDX_SecondaryMotion motion)
        {
            if (motion == null) return false;
            foreach (MotionOverride entry in motionOverrides)
                if (entry != null && entry.motion == motion) return entry.useIndividualSettings;
            foreach (AttachmentSlot slot in attachments)
            {
                if (slot == null || !slot.useIndividualMotionSettings) continue;
                if (FindMotionComponents(slot).Contains(motion)) return true;
            }
            return !SharedSettingsEnabled || settingsMode == MotionSettingsMode.PerAttachment;
        }

        public void SetIndividualSettings(FDX_SecondaryMotion motion, bool value)
        {
            if (motion == null) return;
            foreach (MotionOverride entry in motionOverrides)
            {
                if (entry == null || entry.motion != motion) continue;
                entry.useIndividualSettings = value;
                return;
            }
            motionOverrides.Add(new MotionOverride { motion = motion, useIndividualSettings = value });
        }

        public bool IsMotionExpanded(FDX_SecondaryMotion motion)
        {
            if (motion == null) return false;
            foreach (MotionOverride entry in motionOverrides)
                if (entry != null && entry.motion == motion) return entry.expanded;
            return false;
        }

        public void SetMotionExpanded(FDX_SecondaryMotion motion, bool value)
        {
            if (motion == null) return;
            foreach (MotionOverride entry in motionOverrides)
            {
                if (entry == null || entry.motion != motion) continue;
                entry.expanded = value;
                return;
            }
            motionOverrides.Add(new MotionOverride { motion = motion, expanded = value });
        }

        public void SetAllSimulation(bool value)
        {
            sharedSimulate = value;
            foreach (FDX_SecondaryMotion motion in FindAllMotionComponents())
                if (motion != null) motion.Simulate = value;
        }

        public void PreviewStep(float deltaTime)
        {
            foreach (FDX_SecondaryMotion motion in FindAllMotionComponents())
                if (motion != null) motion.PreviewStep(deltaTime);
        }

        public void StopPreview()
        {
            foreach (FDX_SecondaryMotion motion in FindAllMotionComponents())
                if (motion != null) motion.StopPreview();
        }

        public void RebindAndRebuild()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            foreach (FDX_SecondaryMotion motion in FindAllMotionComponents())
                if (motion != null) motion.RebuildSimulation();
        }

        private static void AddUnique(IEnumerable<FDX_SecondaryMotion> source,
            HashSet<FDX_SecondaryMotion> found, List<FDX_SecondaryMotion> results)
        {
            foreach (FDX_SecondaryMotion motion in source)
                if (motion != null && found.Add(motion)) results.Add(motion);
        }

        private GameObject ResolveInstance(AttachmentSlot slot)
        {
            bool sourceIsSceneObject = slot.source.scene.IsValid();
            if (sourceIsSceneObject && !slot.instantiateAtRuntime) return slot.source;

            GameObject instance = Instantiate(slot.source);
            instance.name = slot.source.name;
            ownedRuntimeInstances.Add(instance);
            return instance;
        }

        private void ApplySettings(GameObject root, FDX_MotionSettings settings)
        {
            FDX_SecondaryMotion[] motions = root.GetComponentsInChildren<FDX_SecondaryMotion>(true);
            foreach (FDX_SecondaryMotion motion in motions)
            {
                motion.Simulate = sharedSimulate;
                motion.Forces = sharedForceSpace;
                motion.ApplySettings(settings, true);
            }
        }
    }

}
