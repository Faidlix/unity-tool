using System;
using System.Collections.Generic;
using UnityEngine;

namespace Faidlix.UnityTools
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(9000)]
    [AddComponentMenu("FDX/Attachment Motion/Attachment Manager")]
    [RequireComponent(typeof(FDX_SecondaryMotionManager))]
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

        public enum AttachmentSourceMode
        {
            RuntimePrefab,
            ExistingSceneObject
        }

        [Serializable]
        public sealed class AttachmentSource
        {
            public GameObject source;
            public bool useSourceMotionSettings;
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
            public Vector3 localScale = Vector3.one;
        }

        [Serializable]
        public sealed class AttachmentSlot
        {
            public string displayName = "New Attachment";
            public bool enabled = true;
            [HideInInspector] public bool expanded = true;
            [HideInInspector] public bool positionExpanded;
            [Tooltip("Prefab 資產或場景中的現有物件。")]
            [HideInInspector]
            public GameObject source;
            public AttachmentSourceMode sourceMode = AttachmentSourceMode.RuntimePrefab;
            public List<AttachmentSource> prefabSources = new List<AttachmentSource>();
            public List<AttachmentSource> sceneSources = new List<AttachmentSource>();
            public AnchorMode anchorMode = AnchorMode.HumanoidBone;
            [HideInInspector] public int dataVersion = 2;
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
            [HideInInspector]
            public bool instantiateAtRuntime = true;
            public bool useIndividualTransforms;
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
            public Vector3 localScale = Vector3.one;
            [Tooltip("在個別設定模式下，勾選後不會套用管理器的統一物理設定。")]
            [HideInInspector]
            public bool useIndividualMotionSettings;

            [NonSerialized] public GameObject runtimeInstance;
            [NonSerialized] public List<GameObject> runtimeInstances = new List<GameObject>();
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
        [SerializeField] private List<Animator> additionalAnimators = new List<Animator>();

        [Header("裝備設定")]
        [SerializeField] private List<AttachmentSlot> attachments = new List<AttachmentSlot>();
        [SerializeField] private Transform defaultUnequippedParent;

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
        [HideInInspector, SerializeField] private int equipmentStatusTab;
        [HideInInspector, SerializeField] private int equipmentMotionTab;

        private readonly List<GameObject> ownedRuntimeInstances = new List<GameObject>();
        private readonly HashSet<FDX_SecondaryMotion> sourceSettingsMotions = new HashSet<FDX_SecondaryMotion>();

        public Animator Animator => animator;
        public IReadOnlyList<Animator> AdditionalAnimators => additionalAnimators;
        public IReadOnlyList<AttachmentSlot> Attachments => attachments;
        public Transform DefaultUnequippedParent
        {
            get => defaultUnequippedParent != null ? defaultUnequippedParent : transform;
            set => defaultUnequippedParent = value;
        }
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
        public int EquipmentStatusTab { get => equipmentStatusTab; set => equipmentStatusTab = Mathf.Clamp(value, 0, 2); }
        public int EquipmentMotionTab { get => equipmentMotionTab; set => equipmentMotionTab = Mathf.Clamp(value, 0, 1); }
        public bool PreviewInEditMode
        {
            get => previewInEditMode;
            set => previewInEditMode = value;
        }

        private void Reset()
        {
            EnsureDataIntegrity();
            RefreshAutomaticAnimator();
        }

        private void OnValidate() => EnsureDataIntegrity();

        private void Awake()
        {
            EnsureDataIntegrity();
            RefreshAutomaticAnimator();
        }

        private void EnsureDataIntegrity()
        {
            if (attachments == null) attachments = new List<AttachmentSlot>();
            if (sharedMotionSettings == null) sharedMotionSettings = new FDX_MotionSettings();
            if (motionOverrides == null) motionOverrides = new List<MotionOverride>();
            if (additionalAnimators == null) additionalAnimators = new List<Animator>();
            if (defaultUnequippedParent == null) defaultUnequippedParent = transform;
            additionalAnimators.RemoveAll(item => item == null);
            foreach (AttachmentSlot slot in attachments)
            {
                if (slot == null) continue;
                if (slot.runtimeInstances == null) slot.runtimeInstances = new List<GameObject>();
                if (slot.prefabSources == null) slot.prefabSources = new List<AttachmentSource>();
                if (slot.sceneSources == null) slot.sceneSources = new List<AttachmentSource>();
                bool hasNewSource = slot.prefabSources.Exists(item => item != null && item.source != null) ||
                                    slot.sceneSources.Exists(item => item != null && item.source != null);
                if (slot.dataVersion < 2 || (slot.source != null && !hasNewSource))
                {
                    if (slot.source != null)
                    {
                        var migrated = new AttachmentSource { source = slot.source };
                        if (slot.source.scene.IsValid())
                        {
                            slot.sceneSources.Add(migrated);
                            slot.sourceMode = AttachmentSourceMode.ExistingSceneObject;
                        }
                        else
                        {
                            slot.prefabSources.Add(migrated);
                            slot.sourceMode = AttachmentSourceMode.RuntimePrefab;
                        }
                    }
                    slot.dataVersion = 2;
                }
                slot.prefabSources.RemoveAll(item => item == null);
                slot.sceneSources.RemoveAll(item => item == null);
            }
        }

        public void RefreshAutomaticAnimator()
        {
            Animator automatic = GetComponent<Animator>();
            if (automatic == null) automatic = GetComponentInParent<Animator>();
            if (automatic == null && animator != null && !additionalAnimators.Contains(animator))
                additionalAnimators.Add(animator);
            animator = automatic;
        }

        public List<Animator> FindManagedAnimators()
        {
            RefreshAutomaticAnimator();
            var results = new List<Animator>();
            var found = new HashSet<Animator>();
            if (animator != null && found.Add(animator)) results.Add(animator);
            foreach (Animator item in additionalAnimators)
                if (item != null && found.Add(item)) results.Add(item);
            return results;
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
            for (int i = 0; i < attachments.Count; i++)
            {
                Detach(i);
                foreach (Animator targetAnimator in FindManagedAnimators()) Attach(i, targetAnimator);
            }
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
            if (!Application.isPlaying) ReleaseSceneAttachment(slot);
            Detach(index);
            attachments.RemoveAt(index);
            return true;
        }

        private void ReleaseSceneAttachment(AttachmentSlot slot)
        {
            if (slot == null || slot.sceneSources == null) return;
            var released = new HashSet<Transform>();
            foreach (AttachmentSource source in slot.sceneSources)
            {
                if (source?.source == null || !source.source.scene.IsValid()) continue;
                Transform item = source.source.transform;
                if (item.parent != null && item.parent.name.IndexOf("_FDX_Pivot", StringComparison.OrdinalIgnoreCase) >= 0)
                    item = item.parent;
                if (!released.Add(item)) continue;
                Transform destination = DefaultUnequippedParent;
                if (destination == item || destination.IsChildOf(item)) destination = transform;
                if (item.parent == destination) continue;
#if UNITY_EDITOR
                UnityEditor.Undo.SetTransformParent(item, destination, "Release FDX Scene Equipment");
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(item);
#else
                item.SetParent(destination, true);
#endif
            }
        }

        public bool IsValidUnequippedParent(Transform candidate)
        {
            if (candidate == null) return false;
            foreach (Animator targetAnimator in FindManagedAnimators())
            {
                if (targetAnimator == null) continue;
                if (candidate == targetAnimator.transform) return true;
                if (targetAnimator.isHuman)
                {
                    for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
                    {
                        Transform bone = targetAnimator.GetBoneTransform((HumanBodyBones)i);
                        if (bone != null && (candidate == bone || candidate.IsChildOf(bone))) return false;
                    }
                }
                foreach (Transform item in targetAnimator.GetComponentsInChildren<Transform>(true))
                    if (item != null && item.name.Equals("Hips", StringComparison.OrdinalIgnoreCase) &&
                        (candidate == item || candidate.IsChildOf(item))) return false;
            }
            return true;
        }

        public bool ConsolidateBoundEquipmentSlots()
        {
            bool changed = false;
            foreach (AttachmentSlot slot in attachments)
            {
                if (slot == null || slot.sourceMode != AttachmentSourceMode.ExistingSceneObject) continue;
                var sources = new List<AttachmentSource>(slot.sceneSources);
                foreach (AttachmentSource source in sources)
                {
                    if (source?.source == null) continue;
                    foreach (Animator targetAnimator in FindManagedAnimators())
                    {
                        if (targetAnimator == null) continue;
                        for (int childIndex = 0; childIndex < targetAnimator.transform.childCount; childIndex++)
                        {
                            GameObject sibling = targetAnimator.transform.GetChild(childIndex).gameObject;
                            if (sibling == source.source || !AreBoundEquipmentRelated(source.source, sibling)) continue;
                            if (slot.sceneSources.Exists(item => item != null && item.source == sibling)) continue;
                            slot.sceneSources.Add(new AttachmentSource { source = sibling });
                            changed = true;
                        }
                    }
                }
            }
            for (int i = 0; i < attachments.Count; i++)
            {
                AttachmentSlot target = attachments[i];
                if (target == null || target.sourceMode != AttachmentSourceMode.ExistingSceneObject) continue;
                for (int j = attachments.Count - 1; j > i; j--)
                {
                    AttachmentSlot candidate = attachments[j];
                    if (candidate == null || candidate.sourceMode != AttachmentSourceMode.ExistingSceneObject ||
                        !SlotsShareBoundRig(target, candidate)) continue;
                    foreach (AttachmentSource source in candidate.sceneSources)
                        if (source != null && source.source != null &&
                            !target.sceneSources.Exists(item => item != null && item.source == source.source))
                            target.sceneSources.Add(source);
                    target.enabled |= candidate.enabled;
                    if (!SlotContainsRenderer(target) && SlotContainsRenderer(candidate))
                        target.displayName = candidate.displayName;
                    attachments.RemoveAt(j);
                    changed = true;
                }
            }
            return changed;
        }

        private bool SlotsShareBoundRig(AttachmentSlot first, AttachmentSlot second)
        {
            foreach (AttachmentSource a in first.sceneSources)
                foreach (AttachmentSource b in second.sceneSources)
                    if (a?.source != null && b?.source != null && AreBoundEquipmentRelated(a.source, b.source))
                        return true;
            return false;
        }

        private static bool SlotContainsRenderer(AttachmentSlot slot) => slot != null &&
            slot.sceneSources.Exists(source => source?.source != null &&
                source.source.GetComponentInChildren<Renderer>(true) != null);

        public bool AreBoundEquipmentRelated(GameObject first, GameObject second)
        {
            if (first == null || second == null || first == second) return first == second;
            Transform firstRoot = GetManagedTopLevel(first.transform);
            Transform secondRoot = GetManagedTopLevel(second.transform);
            if (firstRoot == null || secondRoot == null || firstRoot == secondRoot) return firstRoot == secondRoot;
            foreach (Animator targetAnimator in FindManagedAnimators())
            {
                if (targetAnimator == null) continue;
                foreach (SkinnedMeshRenderer renderer in targetAnimator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    Transform rendererRoot = GetManagedTopLevel(renderer.transform);
                    if (rendererRoot != firstRoot && rendererRoot != secondRoot) continue;
                    Transform otherRoot = rendererRoot == firstRoot ? secondRoot : firstRoot;
                    if (renderer.rootBone != null && IsSameManagedRoot(renderer.rootBone, otherRoot)) return true;
                    foreach (Transform bone in renderer.bones)
                        if (bone != null && IsSameManagedRoot(bone, otherRoot)) return true;
                }
            }
            return false;
        }

        private Transform GetManagedTopLevel(Transform item)
        {
            if (item == null) return null;
            foreach (Animator targetAnimator in FindManagedAnimators())
            {
                if (targetAnimator == null || (item != targetAnimator.transform && !item.IsChildOf(targetAnimator.transform))) continue;
                Transform current = item;
                while (current.parent != null && current.parent != targetAnimator.transform &&
                    !IsHumanoidAnchor(targetAnimator, current.parent)) current = current.parent;
                return current;
            }
            Transform fallback = item;
            while (fallback.parent != null && fallback.parent != transform) fallback = fallback.parent;
            return fallback;
        }

        private bool IsSameManagedRoot(Transform item, Transform expected) =>
            item != null && expected != null && GetManagedTopLevel(item) == expected;

        private static bool IsHumanoidAnchor(Animator animator, Transform item)
        {
            if (animator == null || item == null) return false;
            if (animator.isHuman)
                for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
                    if (animator.GetBoneTransform((HumanBodyBones)i) == item) return true;
            return Enum.TryParse(item.name, true, out HumanBodyBones parsed) && parsed != HumanBodyBones.LastBone;
        }

        public GameObject Attach(int index)
        {
            List<Animator> targets = FindManagedAnimators();
            return targets.Count > 0 ? Attach(index, targets[0]) : null;
        }

        public GameObject Attach(int index, Animator targetAnimator)
        {
            if (index < 0 || index >= attachments.Count) return null;
            AttachmentSlot slot = attachments[index];
            if (!slot.enabled) return null;

            Transform anchor = ResolveAnchor(slot, targetAnimator);
            if (anchor == null)
            {
                Debug.LogWarning($"[FDX Attachment] '{slot.displayName}' 找不到有效掛點。", this);
                return null;
            }

            List<AttachmentSource> sources = GetActiveSources(slot);
            GameObject first = null;
            foreach (AttachmentSource source in sources)
            {
                if (source == null || source.source == null) continue;
                GameObject instance = ResolveInstance(source.source,
                    slot.sourceMode == AttachmentSourceMode.RuntimePrefab, FindManagedAnimators().Count > 1);
                if (instance == null) continue;
                instance.transform.SetParent(anchor, false);
                Vector3 position = slot.useIndividualTransforms ? source.localPosition : slot.localPosition;
                Vector3 rotation = slot.useIndividualTransforms ? source.localEulerAngles : slot.localEulerAngles;
                Vector3 scale = slot.useIndividualTransforms ? source.localScale : slot.localScale;
                instance.transform.localPosition = position;
                instance.transform.localRotation = Quaternion.Euler(rotation);
                instance.transform.localScale = scale;
                foreach (FDX_SecondaryMotion motion in instance.GetComponentsInChildren<FDX_SecondaryMotion>(true))
                    motion.SetAutomaticAttachmentReference(anchor);
                if (first == null) first = instance;
                slot.runtimeInstance = instance;
                if (slot.runtimeInstances == null) slot.runtimeInstances = new List<GameObject>();
                if (!slot.runtimeInstances.Contains(instance)) slot.runtimeInstances.Add(instance);
                if (slot.sourceMode == AttachmentSourceMode.RuntimePrefab && source.useSourceMotionSettings)
                    foreach (FDX_SecondaryMotion motion in instance.GetComponentsInChildren<FDX_SecondaryMotion>(true))
                        if (motion != null) sourceSettingsMotions.Add(motion);
                if (applySharedSettingsOnAttach && settingsMode == MotionSettingsMode.Unified &&
                    !(slot.sourceMode == AttachmentSourceMode.RuntimePrefab && source.useSourceMotionSettings))
                    ApplySettingsUnlessOverridden(instance, sharedMotionSettings);
            }
            return first;
        }

        public void Detach(int index, bool destroyOwnedInstance = true)
        {
            if (index < 0 || index >= attachments.Count) return;
            AttachmentSlot slot = attachments[index];
            var instances = slot.runtimeInstances != null
                ? new List<GameObject>(slot.runtimeInstances)
                : new List<GameObject>();
            if (instances.Count == 0 && slot.runtimeInstance != null) instances.Add(slot.runtimeInstance);
            slot.runtimeInstance = null;
            if (slot.runtimeInstances != null) slot.runtimeInstances.Clear();
            foreach (GameObject instance in instances)
            {
                if (instance == null) continue;
                foreach (FDX_SecondaryMotion motion in instance.GetComponentsInChildren<FDX_SecondaryMotion>(true))
                    if (motion != null) { sourceSettingsMotions.Remove(motion); motion.SetAutomaticAttachmentReference(null); }
                bool isOwned = ownedRuntimeInstances.Remove(instance);
                if (isOwned && destroyOwnedInstance)
                {
                    if (Application.isPlaying) Destroy(instance); else DestroyImmediate(instance);
                }
                else instance.transform.SetParent(null, true);
            }
        }

        public Transform ResolveAnchor(AttachmentSlot slot)
        {
            List<Animator> targets = FindManagedAnimators();
            return ResolveAnchor(slot, targets.Count > 0 ? targets[0] : null);
        }

        public Transform ResolveAnchor(AttachmentSlot slot, Animator targetAnimator)
        {
            if (slot == null) return null;
            AnchorMode mode = slot.dataVersion == 0
                ? (slot.useHumanoidBone ? AnchorMode.HumanoidBone : AnchorMode.DirectTransform)
                : slot.anchorMode;
            if (mode == AnchorMode.DirectTransform) return slot.customAnchor;
            if (mode == AnchorMode.NameOrPath) return ResolveAnchorByNameOrPath(slot, targetAnimator);
            if (targetAnimator == null) return null;
            if (targetAnimator.isHuman) return targetAnimator.GetBoneTransform(slot.humanoidBone);
            // Imported scene rigs can have an Animator without an assigned humanoid Avatar.
            foreach (Transform bone in targetAnimator.GetComponentsInChildren<Transform>(true))
                if (string.Equals(NormalizeName(bone.name, true), slot.humanoidBone.ToString(), StringComparison.OrdinalIgnoreCase))
                    return bone;
            return null;
        }

        public Transform ResolveAnchorByNameOrPath(AttachmentSlot slot)
        {
            List<Animator> targets = FindManagedAnimators();
            return ResolveAnchorByNameOrPath(slot, targets.Count > 0 ? targets[0] : null);
        }

        public Transform ResolveAnchorByNameOrPath(AttachmentSlot slot, Animator targetAnimator)
        {
            if (slot == null || string.IsNullOrWhiteSpace(slot.anchorNameOrPath)) return null;
            Transform root = slot.searchRoot != null
                ? slot.searchRoot
                : targetAnimator != null ? targetAnimator.transform : transform;
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
            var found = new HashSet<FDX_SecondaryMotion>();
            if (slot.runtimeInstances != null)
            {
                foreach (GameObject instance in slot.runtimeInstances)
                {
                    if (instance == null) continue;
                    var current = new List<FDX_SecondaryMotion>();
                    instance.GetComponentsInChildren(true, current);
                    AddUnique(current, found, results);
                }
            }
            foreach (AttachmentSource source in GetActiveSources(slot))
            {
                GameObject root = source != null ? source.source : null;
                if (root == null) continue;
                var current = new List<FDX_SecondaryMotion>();
                root.GetComponentsInChildren(true, current);
                AddUnique(current, found, results);
            }
            return results;
        }

        public List<FDX_SecondaryMotion> FindHierarchyMotionComponents()
        {
            var results = new List<FDX_SecondaryMotion>();
            var found = new HashSet<FDX_SecondaryMotion>();
            foreach (Animator targetAnimator in FindManagedAnimators())
            {
                var current = new List<FDX_SecondaryMotion>();
                targetAnimator.gameObject.GetComponentsInChildren(true, current);
                AddUnique(current, found, results);
            }
            if (results.Count == 0) gameObject.GetComponentsInChildren(true, results);
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
            foreach (FDX_SecondaryMotion motion in FindAllMotionComponents())
                if (motion != null && motion.gameObject.scene.IsValid() && !UsesIndividualSettings(motion))
                {
                    motion.Simulate = sharedSimulate;
                    motion.Forces = sharedForceSpace;
                    motion.ApplySettings(sharedMotionSettings, true);
                }
        }

        public bool UsesIndividualSettings(FDX_SecondaryMotion motion)
        {
            if (motion == null) return false;
            if (sourceSettingsMotions.Contains(motion)) return true;
            foreach (MotionOverride entry in motionOverrides)
                if (entry != null && entry.motion == motion) return entry.useIndividualSettings;
            foreach (AttachmentSlot slot in attachments)
                if (slot != null && slot.useIndividualMotionSettings && IsMotionWithinSlot(slot, motion))
                    return true;
            if (TryGetPrefabMotionSettings(motion, out bool usePrefabSettings)) return usePrefabSettings;
            return !SharedSettingsEnabled || settingsMode == MotionSettingsMode.PerAttachment;
        }

        private bool IsMotionWithinSlot(AttachmentSlot slot, FDX_SecondaryMotion motion)
        {
            if (slot == null || motion == null) return false;
            Transform target = motion.transform;
            if (slot.runtimeInstances != null)
                foreach (GameObject instance in slot.runtimeInstances)
                    if (instance != null && (target == instance.transform || target.IsChildOf(instance.transform)))
                        return true;
            foreach (AttachmentSource source in GetActiveSources(slot))
                if (source?.source != null &&
                    (target == source.source.transform || target.IsChildOf(source.source.transform))) return true;
            return false;
        }

        public void SetIndividualSettings(FDX_SecondaryMotion motion, bool value)
        {
            if (motion == null) return;
            foreach (AttachmentSlot slot in attachments)
                if (slot != null && slot.prefabSources != null)
                    foreach (AttachmentSource source in slot.prefabSources)
                    {
                        if (source == null || source.source == null) continue;
                        FDX_SecondaryMotion[] sourceMotions = source.source.GetComponentsInChildren<FDX_SecondaryMotion>(true);
                        for (int i = 0; i < sourceMotions.Length; i++)
                            if (sourceMotions[i] == motion) source.useSourceMotionSettings = value;
                    }
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
                if (motion != null && motion.gameObject.scene.IsValid()) motion.Simulate = value;
        }

        public void PreviewStep(float deltaTime)
        {
            foreach (FDX_SecondaryMotion motion in FindAllMotionComponents())
                if (motion != null && motion.gameObject.scene.IsValid()) motion.PreviewStep(deltaTime);
        }

        public void PreviewStep(float deltaTime, FDX_SecondaryMotion solo)
        {
            foreach (FDX_SecondaryMotion motion in FindAllMotionComponents())
            {
                if (motion == null) continue;
                if (!motion.gameObject.scene.IsValid()) continue;
                if (solo == null || motion == solo) motion.PreviewStep(deltaTime);
                else motion.StopPreview();
            }
        }

        public void StopPreview()
        {
            foreach (FDX_SecondaryMotion motion in FindAllMotionComponents())
                if (motion != null && motion.gameObject.scene.IsValid()) motion.StopPreview();
        }

        public void RebindAndRebuild()
        {
            RefreshAutomaticAnimator();
            ConsolidateBoundEquipmentSlots();
            if (Application.isPlaying) AttachAll();
            else AttachExistingSceneObjects();
            foreach (FDX_SecondaryMotion motion in FindAllMotionComponents())
                if (motion != null && motion.gameObject.scene.IsValid()) motion.RebuildSimulation();
        }

        public void AttachExistingSceneObjects()
        {
            foreach (AttachmentSlot slot in attachments)
            {
                if (slot == null || !slot.enabled || slot.sourceMode != AttachmentSourceMode.ExistingSceneObject) continue;
                Transform anchor = ResolveAnchor(slot);
                if (anchor == null) continue;
                foreach (AttachmentSource source in GetActiveSources(slot))
                {
                    if (source?.source == null || !source.source.scene.IsValid()) continue;
                    Transform item = source.source.transform;
                    // A generated single-pivot wrapper must travel with its mesh.
                    if (item.parent != null && item.parent.name.IndexOf("_FDX_Pivot", StringComparison.OrdinalIgnoreCase) >= 0)
                        item = item.parent;
                    if (anchor == item || anchor.IsChildOf(item)) continue;
                    Vector3 position = slot.useIndividualTransforms ? source.localPosition : slot.localPosition;
                    Quaternion rotation = Quaternion.Euler(slot.useIndividualTransforms ? source.localEulerAngles : slot.localEulerAngles);
                    Vector3 scale = slot.useIndividualTransforms ? source.localScale : slot.localScale;
#if UNITY_EDITOR
                    bool firstAttach = item.parent != anchor;
                    if (firstAttach) UnityEditor.Undo.SetTransformParent(item, anchor, "Attach FDX Scene Equipment");
                    if (firstAttach && position == Vector3.zero && rotation == Quaternion.identity && scale == Vector3.one)
                    {
                        source.localPosition = item.localPosition;
                        source.localEulerAngles = item.localEulerAngles;
                        source.localScale = item.localScale;
                        slot.useIndividualTransforms = true;
                        position = source.localPosition;
                        rotation = Quaternion.Euler(source.localEulerAngles);
                        scale = source.localScale;
                    }
                    if (item.localPosition != position || item.localRotation != rotation || item.localScale != scale)
                        UnityEditor.Undo.RecordObject(item, "Adjust FDX Scene Equipment");
#else
                    item.SetParent(anchor, false);
#endif
                    item.localPosition = position;
                    item.localRotation = rotation;
                    item.localScale = scale;
                    foreach (FDX_SecondaryMotion motion in source.source.GetComponentsInChildren<FDX_SecondaryMotion>(true))
                        {
                            motion.SetAutomaticAttachmentReference(anchor);
                        }
#if UNITY_EDITOR
                    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(item);
#endif
                }
            }
        }

        public bool HasInvalidAttachments()
        {
            bool hasEnabledAttachment = false;
            foreach (AttachmentSlot slot in attachments)
                if (slot != null && slot.enabled) { hasEnabledAttachment = true; break; }
            if (!hasEnabledAttachment) return false;
            List<Animator> targets = FindManagedAnimators();
            if (targets.Count == 0) return true;
            foreach (AttachmentSlot slot in attachments)
            {
                if (slot == null || !slot.enabled) continue;
                bool hasSource = GetActiveSources(slot).Exists(item => item != null && item.source != null);
                if (!hasSource) return true;
                foreach (Animator targetAnimator in targets)
                    if (ResolveAnchor(slot, targetAnimator) == null) return true;
            }
            return false;
        }

        private static void AddUnique(IEnumerable<FDX_SecondaryMotion> source,
            HashSet<FDX_SecondaryMotion> found, List<FDX_SecondaryMotion> results)
        {
            foreach (FDX_SecondaryMotion motion in source)
                if (motion != null && found.Add(motion)) results.Add(motion);
        }

        public List<AttachmentSource> GetActiveSources(AttachmentSlot slot)
        {
            if (slot == null) return new List<AttachmentSource>();
            List<AttachmentSource> active = slot.sourceMode == AttachmentSourceMode.RuntimePrefab
                ? slot.prefabSources : slot.sceneSources;
            if ((active == null || active.Count == 0) && slot.source != null)
                return new List<AttachmentSource> { new AttachmentSource { source = slot.source } };
            return active ?? new List<AttachmentSource>();
        }

        public bool IsPrefabMotion(FDX_SecondaryMotion motion)
        {
            return TryGetPrefabMotionSettings(motion, out _);
        }

        private bool TryGetPrefabMotionSettings(FDX_SecondaryMotion motion, out bool useSourceSettings)
        {
            useSourceSettings = false;
            if (motion == null) return false;
            foreach (AttachmentSlot slot in attachments)
                if (slot != null && slot.prefabSources != null)
                    foreach (AttachmentSource source in slot.prefabSources)
                    {
                        if (source == null || source.source == null) continue;
                        foreach (FDX_SecondaryMotion item in source.source.GetComponentsInChildren<FDX_SecondaryMotion>(true))
                            if (item == motion)
                            {
                                useSourceSettings = source.useSourceMotionSettings;
                                return true;
                            }
                    }
            return false;
        }

        private GameObject ResolveInstance(GameObject source, bool instantiate, bool forceInstantiate)
        {
            if (source == null) return null;
            bool sourceIsSceneObject = source.scene.IsValid();
            if (sourceIsSceneObject && !instantiate && !forceInstantiate) return source;

            GameObject instance = Instantiate(source);
            instance.name = source.name;
            ownedRuntimeInstances.Add(instance);
            return instance;
        }

        private void ApplySettingsUnlessOverridden(GameObject root, FDX_MotionSettings settings)
        {
            FDX_SecondaryMotion[] motions = root.GetComponentsInChildren<FDX_SecondaryMotion>(true);
            foreach (FDX_SecondaryMotion motion in motions)
            {
                if (UsesIndividualSettings(motion)) continue;
                motion.Simulate = sharedSimulate;
                motion.Forces = sharedForceSpace;
                motion.ApplySettings(settings, true);
            }
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
