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

        [Serializable]
        public sealed class AttachmentSlot
        {
            public string displayName = "New Attachment";
            public bool enabled = true;
            [Tooltip("Prefab 資產或場景中的現有物件。")]
            public GameObject source;
            [Tooltip("開啟後使用 Humanoid Animator 的標準骨頭；關閉則使用自訂掛點。")]
            public bool useHumanoidBone = true;
            public HumanBodyBones humanoidBone = HumanBodyBones.Head;
            public Transform customAnchor;
            [Tooltip("Prefab 在執行時會自動建立；場景物件可選擇直接重新掛接。")]
            public bool instantiateAtRuntime = true;
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
            public Vector3 localScale = Vector3.one;
            [Tooltip("在個別設定模式下，勾選後不會套用管理器的統一物理設定。")]
            public bool useIndividualMotionSettings;

            [NonSerialized] public GameObject runtimeInstance;
        }

        [Header("角色")]
        [SerializeField] private Animator animator;

        [Header("掛載清單")]
        [SerializeField] private List<AttachmentSlot> attachments = new List<AttachmentSlot>();

        [Header("動態設定同步")]
        [SerializeField] private MotionSettingsMode settingsMode = MotionSettingsMode.Unified;
        [SerializeField] private FDX_MotionSettings sharedMotionSettings = new FDX_MotionSettings();
        [SerializeField] private bool applySharedSettingsOnAttach = true;

        private readonly List<GameObject> ownedRuntimeInstances = new List<GameObject>();

        public Animator Animator => animator;
        public IReadOnlyList<AttachmentSlot> Attachments => attachments;
        public MotionSettingsMode SettingsMode => settingsMode;
        public FDX_MotionSettings SharedMotionSettings => sharedMotionSettings;

        private void Reset()
        {
            animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
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
            if (!slot.useHumanoidBone) return slot.customAnchor;
            if (animator == null || !animator.isHuman) return null;
            return animator.GetBoneTransform(slot.humanoidBone);
        }

        public List<FDX_SecondaryMotion> FindMotionComponents(AttachmentSlot slot)
        {
            var results = new List<FDX_SecondaryMotion>();
            if (slot == null) return results;
            GameObject root = slot.runtimeInstance != null ? slot.runtimeInstance : slot.source;
            if (root != null) root.GetComponentsInChildren(true, results);
            return results;
        }

        public void ApplySharedSettingsToAll()
        {
            foreach (AttachmentSlot slot in attachments)
            {
                if (slot == null || !slot.enabled || slot.useIndividualMotionSettings) continue;
                GameObject root = slot.runtimeInstance != null ? slot.runtimeInstance : slot.source;
                if (root != null) ApplySettings(root, sharedMotionSettings);
            }
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

        private static void ApplySettings(GameObject root, FDX_MotionSettings settings)
        {
            FDX_SecondaryMotion[] motions = root.GetComponentsInChildren<FDX_SecondaryMotion>(true);
            foreach (FDX_SecondaryMotion motion in motions) motion.ApplySettings(settings, true);
        }
    }

}
