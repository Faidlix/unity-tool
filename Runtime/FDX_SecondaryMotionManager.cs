using System.Collections.Generic;
using UnityEngine;

namespace Faidlix.UnityTools
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(8990)]
    [AddComponentMenu("FDX/Attachment Motion/Secondary Motion Manager")]
    public sealed class FDX_SecondaryMotionManager : MonoBehaviour
    {
        [SerializeField] private bool simulateAll = true;
        [SerializeField, HideInInspector] private bool motionListExpanded = true;

        public bool SimulateAll
        {
            get => simulateAll;
            set
            {
                simulateAll = value;
                SetAllSimulation(value);
            }
        }

        public bool MotionListExpanded
        {
            get => motionListExpanded;
            set => motionListExpanded = value;
        }

        private void OnEnable()
        {
            if (Application.isPlaying) SetAllSimulation(simulateAll);
        }

        public List<FDX_SecondaryMotion> FindMotionComponents()
        {
            var results = new List<FDX_SecondaryMotion>();
            gameObject.GetComponentsInChildren(true, results);
            results.RemoveAll(motion => motion == null || !motion.gameObject.scene.IsValid());
            return results;
        }

        public void SetAllSimulation(bool value)
        {
            simulateAll = value;
            foreach (FDX_SecondaryMotion motion in FindMotionComponents()) motion.Simulate = value;
        }

        public void PreviewStep(float deltaTime)
        {
            if (!simulateAll) return;
            foreach (FDX_SecondaryMotion motion in FindMotionComponents())
                if (motion.isActiveAndEnabled && motion.Simulate) motion.PreviewStep(deltaTime);
        }

        public void PreviewStep(float deltaTime, FDX_SecondaryMotion solo)
        {
            foreach (FDX_SecondaryMotion motion in FindMotionComponents())
            {
                if (!simulateAll || !motion.isActiveAndEnabled || !motion.Simulate || (solo != null && motion != solo))
                    motion.StopPreview();
                else
                    motion.PreviewStep(deltaTime);
            }
        }

        public void StopPreview()
        {
            foreach (FDX_SecondaryMotion motion in FindMotionComponents()) motion.StopPreview();
        }
    }
}
