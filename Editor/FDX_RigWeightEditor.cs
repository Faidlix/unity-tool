using System;
using System.Collections.Generic;
using System.IO;
using Faidlix.UnityTools;
using UnityEditor;
using UnityEngine;

namespace Faidlix.UnityTools.Editor
{
#pragma warning disable UDR0004
    public sealed class FDX_RigWeightEditor : EditorWindow
    {
        private enum BrushMode
        {
            Add,
            Subtract,
            Replace,
            Smooth
        }

        private FDX_SecondaryMotion motion;
        private SkinnedMeshRenderer rendererTarget;
        private int selectedBoneIndex;
        private BrushMode brushMode = BrushMode.Add;
        private float brushRadiusPixels = 42f;
        private float brushStrength = 0.2f;
        private bool paintInScene;
        private bool showAllVertices;
        private Vector2 scroll;

        private sealed class AutoRigSegment
        {
            public FDX_SecondaryMotion.FlexibleEndPoint point;
            public Vector3 start;
            public Vector3 end;
            public int boneStart;
            public int boneCount;
        }

        [MenuItem("Tools/FDX/Attachment Motion/Rig & Weight Editor")]
        public static void Open()
        {
            GetWindow<FDX_RigWeightEditor>("FDX Rig & Weight").Show();
        }

        [MenuItem("Tools/FDX/Attachment Motion/Run Smoke Test")]
        public static void RunBatchSmokeTest()
        {
            GameObject root = null;
            string generatedAssetPath = null;
            FDX_RigWeightEditor window = null;
            try
            {
                root = new GameObject("FDX_Smoke_Root");
                FDX_SecondaryMotion testMotion = root.AddComponent<FDX_SecondaryMotion>();
                testMotion.Source = FDX_SecondaryMotion.MotionSource.AutomaticPivot;
                testMotion.RotationPivot = root.transform;
                testMotion.EnableAdvancedFlexible = true;

                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "FDX_Smoke_Mesh";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = new Vector3(0.1f, 0.1f, 1f);

                var tipObject = new GameObject("FDX_Smoke_Tip");
                tipObject.transform.SetParent(root.transform, false);
                tipObject.transform.localPosition = new Vector3(0.3f, 0f, 1f);
                var rootPoint = new FDX_SecondaryMotion.FlexibleEndPoint
                {
                    displayName = "SmokeTip",
                    tip = tipObject.transform,
                    detectionRadius = 2f,
                    generatedSegments = 3,
                    motionMultiplier = 1f,
                    liveMirror = true,
                    mirrorAxis = FDX_SecondaryMotion.MirrorAxis.X
                };
                var childObject = new GameObject("FDX_Smoke_ChildTip");
                childObject.transform.SetParent(tipObject.transform, false);
                childObject.transform.localPosition = new Vector3(0.2f, 0f, 0.5f);
                rootPoint.children.Add(new FDX_SecondaryMotion.FlexibleEndPoint
                {
                    displayName = "SmokeChildTip",
                    tip = childObject.transform,
                    detectionRadius = 1f,
                    generatedSegments = 2,
                    motionMultiplier = 0.8f
                });
                testMotion.EndPoints.Add(rootPoint);

                var secondPivotObject = new GameObject("FDX_Smoke_RightPivot");
                secondPivotObject.transform.SetParent(root.transform, false);
                secondPivotObject.transform.localPosition = Vector3.right * 0.3f;
                testMotion.PivotGroups.Add(new FDX_SecondaryMotion.PivotGroup
                {
                    displayName = "Right Pivot",
                    pivot = secondPivotObject.transform,
                    motionTarget = secondPivotObject.transform,
                    automaticSimulationAnchor = true
                });

                window = CreateInstance<FDX_RigWeightEditor>();
                window.motion = testMotion;
                window.GenerateAutomaticRigAndWeights();

                SkinnedMeshRenderer result = testMotion.DeformingRenderer;
                if (result == null || result.sharedMesh == null)
                    throw new InvalidOperationException("Automatic rig did not create a SkinnedMeshRenderer.");
                if (result.bones == null || result.bones.Length < 11)
                    throw new InvalidOperationException("Recursive mirrored automatic rig did not create the expected bones.");
                if (result.sharedMesh.boneWeights.Length != result.sharedMesh.vertexCount)
                    throw new InvalidOperationException("Generated bone weights do not match the vertex count.");

                generatedAssetPath = AssetDatabase.GetAssetPath(result.sharedMesh);
                testMotion.AddForce(Vector3.right);
                testMotion.AddImpulse(Vector3.up);
                testMotion.ClearForces();
                testMotion.RebuildSimulation();

                Animator primaryAnimator = root.AddComponent<Animator>();
                FDX_AttachmentManager manager = root.AddComponent<FDX_AttachmentManager>();
                var secondaryCharacter = new GameObject("FDX_Smoke_SecondaryCharacter");
                secondaryCharacter.transform.SetParent(root.transform, false);
                Animator secondaryAnimator = secondaryCharacter.AddComponent<Animator>();
                var managerObject = new SerializedObject(manager);
                SerializedProperty additional = managerObject.FindProperty("additionalAnimators");
                additional.arraySize = 1;
                additional.GetArrayElementAtIndex(0).objectReferenceValue = secondaryAnimator;
                managerObject.ApplyModifiedPropertiesWithoutUndo();
                manager.RefreshAutomaticAnimator();
                if (manager.Animator != primaryAnimator || manager.FindManagedAnimators().Count != 2)
                    throw new InvalidOperationException("Attachment Manager did not merge automatic and additional Animators.");
                var childManagerObject = new GameObject("FDX_Smoke_ChildManager");
                childManagerObject.transform.SetParent(root.transform, false);
                FDX_AttachmentManager childManager = childManagerObject.AddComponent<FDX_AttachmentManager>();
                childManager.RefreshAutomaticAnimator();
                if (childManager.Animator != primaryAnimator)
                    throw new InvalidOperationException("Attachment Manager did not find the parent Animator.");
                DestroyImmediate(childManagerObject);

                var externalManagerObject = new GameObject("FDX_Smoke_ExternalManager");
                FDX_AttachmentManager externalManager = externalManagerObject.AddComponent<FDX_AttachmentManager>();
                var externalManagerSerialized = new SerializedObject(externalManager);
                SerializedProperty externalAnimators = externalManagerSerialized.FindProperty("additionalAnimators");
                externalAnimators.arraySize = 1;
                externalAnimators.GetArrayElementAtIndex(0).objectReferenceValue = primaryAnimator;
                externalManagerSerialized.ApplyModifiedPropertiesWithoutUndo();
                externalManager.RefreshAutomaticAnimator();
                if (externalManager.Animator != null || externalManager.FindManagedAnimators().Count != 1)
                    throw new InvalidOperationException("Standalone Attachment Manager did not use its Animator list.");
                DestroyImmediate(externalManagerObject);
                if (manager.FindHierarchyMotionComponents().Count != 1 ||
                    manager.FindAllMotionComponents().Count != 1)
                    throw new InvalidOperationException("Attachment Manager did not detect Secondary Motion in its hierarchy.");

                FDX_AttachmentManager.AttachmentSlot searchSlot = manager.AddAttachment();
                searchSlot.anchorMode = FDX_AttachmentManager.AnchorMode.NameOrPath;
                searchSlot.searchRoot = root.transform;
                searchSlot.anchorNameOrPath = "FDX_Smoke_Tip";
                if (manager.ResolveAnchor(searchSlot) != tipObject.transform)
                    throw new InvalidOperationException("Name/path anchor lookup did not resolve the expected Transform.");
                manager.RemoveAttachment(searchSlot);

                var hipsObject = new GameObject("FDX_Smoke_Hips");
                hipsObject.transform.SetParent(root.transform, false);
                FDX_SecondaryMotion chainMotion = hipsObject.AddComponent<FDX_SecondaryMotion>();
                chainMotion.Source = FDX_SecondaryMotion.MotionSource.ExistingBones;
                chainMotion.SimulationAnchor = hipsObject.transform;
                var skirtRoot = new GameObject("Skirt_01.L");
                skirtRoot.transform.SetParent(hipsObject.transform, false);
                var skirtTip = new GameObject("Skirt_02.L_end");
                skirtTip.transform.SetParent(skirtRoot.transform, false);
                skirtTip.transform.localPosition = Vector3.forward * 0.5f;
                var chain = new FDX_SecondaryMotion.BoneChain
                {
                    displayName = "Smoke Skirt Chain",
                    root = skirtRoot.transform,
                    includeChildBones = true,
                    includeEndBone = true
                };
                chainMotion.BoneChains.Add(chain);
                if (FDX_SecondaryMotion.GetBoneChainDepth(chain) != 2)
                    throw new InvalidOperationException("Bone-chain depth detection did not include the root and child.");
                chain.allBonesSway = false;
                chain.boneMotionLevels = 1;
                chainMotion.RebuildSimulation();
                if (!chainMotion.HasValidExistingBones || chainMotion.ValidateSetup().Count != 0)
                    throw new InvalidOperationException("Existing bone-chain validation failed.");
                Quaternion chainStart = skirtRoot.transform.localRotation;
                for (int i = 0; i < 30; i++) chainMotion.PreviewStep(1f / 60f);
                if (Quaternion.Angle(chainStart, skirtRoot.transform.localRotation) < 0.001f)
                    throw new InvalidOperationException("Existing bone chain did not simulate.");
                chainMotion.StopPreview();

                manager.SetAllSimulation(false);
                if (testMotion.Simulate || chainMotion.Simulate)
                    throw new InvalidOperationException("Attachment Manager did not disable every detected motion component.");
                manager.SetAllSimulation(true);
                if (!testMotion.Simulate || !chainMotion.Simulate)
                    throw new InvalidOperationException("Attachment Manager did not enable every detected motion component.");

                FDX_SecondaryMotion.PivotGroup replicated = testMotion.PivotGroups[0];
                replicated.replicationMode = FDX_SecondaryMotion.PivotReplicationMode.Mirror;
                replicated.mirrorX = true;
                replicated.mirrorY = true;
                replicated.mirrorZ = false;
                var replicatedPositions = new List<Vector3>();
                testMotion.GetReplicatedPivotPositions(replicated, replicatedPositions);
                if (replicatedPositions.Count != 3)
                    throw new InvalidOperationException("Two-axis pivot mirroring did not create three additional positions.");
                chain.axisSettings.lockX = chain.axisSettings.lockY = chain.axisSettings.lockZ = true;
                chainMotion.RebuildSimulation();
                chainStart = skirtRoot.transform.localRotation;
                for (int i = 0; i < 30; i++) chainMotion.PreviewStep(1f / 60f);
                if (Quaternion.Angle(chainStart, skirtRoot.transform.localRotation) > 0.001f)
                    throw new InvalidOperationException("Fully locked bone chain still moved.");
                chainMotion.StopPreview();

                Quaternion previewStart = root.transform.localRotation;
                Quaternion secondPreviewStart = secondPivotObject.transform.localRotation;
                testMotion.Settings.previewAutoSway = true;
                for (int i = 0; i < 30; i++) manager.PreviewStep(1f / 60f);
                if (Quaternion.Angle(previewStart, root.transform.localRotation) < 0.001f)
                    throw new InvalidOperationException("Edit Mode Preview did not rotate the automatic motion target.");
                if (Quaternion.Angle(secondPreviewStart, secondPivotObject.transform.localRotation) < 0.001f)
                    throw new InvalidOperationException("Multi-pivot preview did not rotate the second motion target.");
                manager.StopPreview();

                previewStart = root.transform.localRotation;
                chainStart = skirtRoot.transform.localRotation;
                for (int i = 0; i < 30; i++) manager.PreviewStep(1f / 60f, testMotion);
                if (Quaternion.Angle(previewStart, root.transform.localRotation) < 0.001f)
                    throw new InvalidOperationException("Solo preview did not simulate the selected motion component.");
                if (Quaternion.Angle(chainStart, skirtRoot.transform.localRotation) > 0.001f)
                    throw new InvalidOperationException("Solo preview did not stop the other motion component.");
                manager.StopPreview();

                Debug.Log($"FDX_ATTACHMENT_MOTION_SMOKE_OK bones={result.bones.Length} vertices={result.sharedMesh.vertexCount} chains=1 animators=2 solo=1");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
            finally
            {
                if (window != null) DestroyImmediate(window);
                if (root != null) DestroyImmediate(root);
                if (!string.IsNullOrEmpty(generatedAssetPath)) AssetDatabase.DeleteAsset(generatedAssetPath);
                if (AssetDatabase.IsValidFolder("Assets/FDX_Generated") &&
                    AssetDatabase.FindAssets(string.Empty, new[] { "Assets/FDX_Generated" }).Length == 0)
                    AssetDatabase.DeleteAsset("Assets/FDX_Generated");
                AssetDatabase.SaveAssets();
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Tools/FDX/Attachment Motion/Run Regression Test")]
        public static void RunRegressionSmokeTest()
        {
            GameObject root = null;
            GameObject standaloneRoot = null;
            Mesh generatedMesh = null;
            try
            {
                root = new GameObject("FDX_Regression");
                root.AddComponent<Animator>();
                var manager = root.AddComponent<FDX_AttachmentManager>();
                var hips = new GameObject("Hips");
                hips.transform.SetParent(root.transform, false);
                var head = new GameObject("Head");
                head.transform.SetParent(hips.transform, false);
                var glasses = GameObject.CreatePrimitive(PrimitiveType.Cube);
                glasses.name = "Accessory_GlassesFrame";
                glasses.transform.SetParent(head.transform, false);
                foreach (string name in new[] { "Body", "Face" })
                {
                    var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    body.name = name;
                    body.transform.SetParent(root.transform, false);
                }
                var collision = new GameObject("PureCollider");
                collision.transform.SetParent(head.transform, false);
                collision.AddComponent<BoxCollider>();
                var effect = new GameObject("EquipmentEffect");
                effect.transform.SetParent(head.transform, false);
                effect.AddComponent<ParticleSystem>();
                var equipmentRig = new GameObject("IndependentRigEquipment");
                equipmentRig.transform.SetParent(head.transform, false);
                var equipmentBone = new GameObject("Base");
                equipmentBone.transform.SetParent(equipmentRig.transform, false);
                var rigMesh = new GameObject("RigMesh");
                rigMesh.transform.SetParent(equipmentRig.transform, false);
                var rigRenderer = rigMesh.AddComponent<SkinnedMeshRenderer>();
                rigRenderer.bones = new[] { equipmentBone.transform };
                rigRenderer.rootBone = equipmentBone.transform;
                List<GameObject> candidates = FDX_AttachmentManagerBilingualInspector.FindUnconfiguredEquipment(manager);
                if (!candidates.Contains(glasses) || !candidates.Contains(effect) || !candidates.Contains(equipmentRig) || candidates.Contains(rigMesh) || candidates.Contains(collision) ||
                    candidates.Exists(item => item.name == "Body" || item.name == "Face" || item == hips || item == head))
                    throw new InvalidOperationException("Nested visible equipment scan failed.");

                if (!FDX_AttachmentManagerBilingualInspector.AutoRegisterMountedEquipment(manager) || manager.Attachments.Count != 3)
                    throw new InvalidOperationException("Equipment already mounted under character bones was not registered automatically.");
                FDX_AttachmentManager.AttachmentSlot slot = null;
                foreach (FDX_AttachmentManager.AttachmentSlot current in manager.Attachments)
                    if (current.sceneSources.Exists(source => source != null && source.source == glasses)) slot = current;
                if (slot == null || manager.ResolveAnchor(slot) != head.transform || !slot.useIndividualTransforms)
                    throw new InvalidOperationException("Automatically registered equipment did not preserve its bone anchor and transform mode.");
                slot.sceneSources[0].localPosition = Vector3.right * 0.2f;
                glasses.transform.SetParent(root.transform, false);
                manager.RebindAndRebuild();
                if (glasses.transform.parent != head.transform || glasses.transform.localPosition != slot.sceneSources[0].localPosition)
                    throw new InvalidOperationException("Edit-mode equipment attachment failed.");

                var headAccessory = new GameObject("Accessory_Head");
                headAccessory.transform.SetParent(root.transform, false);
                var headAccessoryRenderer = headAccessory.AddComponent<SkinnedMeshRenderer>();
                var headAccessoryRig = new GameObject("Accessory_HeadBone");
                headAccessoryRig.transform.SetParent(root.transform, false);
                var headAccessoryBase = new GameObject("Base");
                headAccessoryBase.transform.SetParent(headAccessoryRig.transform, false);
                var headAccessoryMotion = headAccessoryRig.AddComponent<FDX_SecondaryMotion>();
                headAccessoryRenderer.rootBone = headAccessoryBase.transform;
                headAccessoryRenderer.bones = new[] { headAccessoryBase.transform };
                var meshSlot = manager.AddAttachment();
                meshSlot.sourceMode = FDX_AttachmentManager.AttachmentSourceMode.ExistingSceneObject;
                meshSlot.sceneSources.Add(new FDX_AttachmentManager.AttachmentSource { source = headAccessory });
                var rigSlot = manager.AddAttachment();
                rigSlot.sourceMode = FDX_AttachmentManager.AttachmentSourceMode.ExistingSceneObject;
                rigSlot.sceneSources.Add(new FDX_AttachmentManager.AttachmentSource { source = headAccessoryRig });
                manager.RebindAndRebuild();
                FDX_AttachmentManager.AttachmentSlot mergedSlot = null;
                foreach (FDX_AttachmentManager.AttachmentSlot current in manager.Attachments)
                    if (current.sceneSources.Exists(source => source != null && source.source == headAccessory)) mergedSlot = current;
                if (mergedSlot == null || mergedSlot.sceneSources.Count != 2 || manager.Attachments.Count != 4 ||
                    !manager.FindMotionComponents(mergedSlot).Contains(headAccessoryMotion) ||
                    headAccessory.transform.parent != head.transform || headAccessoryRig.transform.parent != head.transform)
                    throw new InvalidOperationException("Bound mesh and equipment rig were not consolidated and attached together.");

                Vector3 releasedWorldPosition = glasses.transform.position;
                if (!manager.RemoveAttachment(slot) || glasses.transform.parent != manager.transform ||
                    Vector3.Distance(glasses.transform.position, releasedWorldPosition) > 0.0001f)
                    throw new InvalidOperationException("Released equipment did not move to the default unequipped root while preserving world position.");

                var ear = new GameObject("StandaloneEarring");
                ear.transform.SetParent(head.transform, false);
                ear.transform.localPosition = Vector3.right * 0.2f;
                var motion = ear.AddComponent<FDX_SecondaryMotion>();
                if (motion.Source != FDX_SecondaryMotion.MotionSource.ExistingBones)
                    throw new InvalidOperationException("New motion did not default to existing bones.");
                motion.Source = FDX_SecondaryMotion.MotionSource.AutomaticPivot;
                motion.PivotGroups[0].pivot = ear.transform;
                motion.Settings.gravityStrength = 0f;
                motion.Settings.constantWind = Vector3.zero;
                motion.Settings.enableDistanceSimulation = false;
                motion.RebuildSimulation();
                motion.PreviewStep(1f / 60f);
                for (int i = 1; i <= 30; i++)
                {
                    head.transform.localRotation = Quaternion.Euler(0f, i * 2f, 0f);
                    motion.PreviewStep(1f / 60f);
                }
                if (Quaternion.Angle(ear.transform.localRotation, Quaternion.identity) < 0.01f)
                    throw new InvalidOperationException("Rotating head did not produce earring inertia.");
                motion.Simulate = false;
                if (Quaternion.Angle(ear.transform.localRotation, Quaternion.identity) > 0.001f)
                    throw new InvalidOperationException("Disabling simulation did not restore the pose.");
                motion.BoneChainGroups.Add(new FDX_SecondaryMotion.BoneChainGroup { id = "parent" });
                motion.BoneChainGroups.Add(new FDX_SecondaryMotion.BoneChainGroup { id = "child", parentId = "parent" });
                if (!FDX_SecondaryMotionBilingualInspector.PruneEmptyGroups(motion) || motion.BoneChainGroups.Count != 0)
                    throw new InvalidOperationException("Empty nested groups did not disappear.");
                if (!FDX_SecondaryMotionBilingualInspector.ParentIdsEqual(null, string.Empty))
                    throw new InvalidOperationException("Root bone-chain groups did not normalize null and empty parent IDs.");
                standaloneRoot = new GameObject("FDX_StandalonePreview");
                var standalone = standaloneRoot.AddComponent<FDX_SecondaryMotion>();
                standalone.Source = FDX_SecondaryMotion.MotionSource.AutomaticPivot;
                standalone.PivotGroups[0].pivot = standaloneRoot.transform;
                standalone.Settings.gravityStrength = 0f;
                standalone.Settings.previewAutoSway = true;
                standalone.Settings.enableDistanceSimulation = false;
                standalone.RebuildSimulation();
                motion.Simulate = true;
                motion.Settings.previewAutoSway = true;
                var updatePreview = typeof(FDX_EditModePreviewDriver).GetMethod("Update",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                for (int i = 0; i < 30; i++) updatePreview.Invoke(null, null);
                if (Quaternion.Angle(standaloneRoot.transform.localRotation, Quaternion.identity) < 0.001f)
                    throw new InvalidOperationException("Standalone editor preview driver did not advance.");
                if (Quaternion.Angle(ear.transform.localRotation, Quaternion.identity) > 0.001f)
                    throw new InvalidOperationException("Disabled Manager preview was bypassed by standalone preview.");
                standalone.Settings.previewAutoSway = false;
                updatePreview.Invoke(null, null);
                if (Quaternion.Angle(standaloneRoot.transform.localRotation, Quaternion.identity) > 0.001f)
                    throw new InvalidOperationException("Standalone preview did not stop.");

                var individualMotionRoot = new GameObject("IndividualChainMotion");
                individualMotionRoot.transform.SetParent(root.transform, false);
                var individualBone = new GameObject("TailChainRoot");
                individualBone.transform.SetParent(individualMotionRoot.transform, false);
                var individualMotion = individualMotionRoot.AddComponent<FDX_SecondaryMotion>();
                individualMotion.Source = FDX_SecondaryMotion.MotionSource.ExistingBones;
                individualMotion.Settings.gravityStrength = 0f;
                individualMotion.Settings.constantWind = Vector3.zero;
                individualMotion.Settings.enableDistanceSimulation = false;
                var individualChain = new FDX_SecondaryMotion.BoneChain
                {
                    displayName = "Tail",
                    root = individualBone.transform,
                    includeChildBones = false,
                    useIndividualMotionSettings = true
                };
                individualChain.motionSettings.gravityStrength = 0f;
                individualChain.motionSettings.constantWind = Vector3.right * 4f;
                individualChain.motionSettings.enableDistanceSimulation = false;
                individualMotion.BoneChains.Add(individualChain);
                individualMotion.RebuildSimulation();
                for (int i = 0; i < 30; i++) individualMotion.PreviewStep(1f / 60f);
                if (Quaternion.Angle(individualBone.transform.localRotation, Quaternion.identity) < 0.01f)
                    throw new InvalidOperationException("Individual bone-chain motion settings were not used by the simulation.");

                var collisionRig = new GameObject("CollisionRegression");
                collisionRig.transform.SetParent(root.transform, false);
                collisionRig.transform.localPosition = Vector3.right * 4f;
                var collisionBone = new GameObject("CollisionBone");
                collisionBone.transform.SetParent(collisionRig.transform, false);
                var collisionTip = new GameObject("CollisionTip");
                collisionTip.transform.SetParent(collisionBone.transform, false);
                collisionTip.transform.localPosition = Vector3.right;
                var animatedColliderObject = new GameObject("AnimatedCapsuleCollider");
                animatedColliderObject.transform.SetParent(root.transform, false);
                animatedColliderObject.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
                animatedColliderObject.transform.localScale = new Vector3(1.2f, 0.9f, 1.1f);
                var animatedCapsule = animatedColliderObject.AddComponent<CapsuleCollider>();
                animatedCapsule.direction = 1;
                animatedCapsule.height = 0.4f;
                animatedCapsule.radius = 0.12f;
                var collisionMotion = collisionRig.AddComponent<FDX_SecondaryMotion>();
                collisionMotion.Source = FDX_SecondaryMotion.MotionSource.ExistingBones;
                collisionMotion.Settings.gravityStrength = 0f;
                collisionMotion.Settings.constantWind = Vector3.zero;
                collisionMotion.Settings.enableDistanceSimulation = false;
                collisionMotion.Settings.enableCollision = true;
                collisionMotion.Settings.collisionRadius = 0.08f;
                collisionMotion.Settings.maxAngle = 120f;
                collisionMotion.Settings.collisionFriction = 1f;
                collisionMotion.BoneChains.Add(new FDX_SecondaryMotion.BoneChain
                {
                    displayName = "Collision Chain",
                    root = collisionBone.transform,
                    includeChildBones = false
                });
                var colliderField = typeof(FDX_SecondaryMotion).GetField("explicitColliders",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var colliderList = (List<Collider>)colliderField.GetValue(collisionMotion);
                colliderList.Add(animatedCapsule);
                animatedColliderObject.transform.position = collisionRig.transform.position + Vector3.right * 3f;
                collisionMotion.RebuildSimulation();
                collisionMotion.PreviewStep(1f / 60f);
                animatedColliderObject.transform.position = collisionRig.transform.position + new Vector3(0.75f, 0.12f, 0f);
                collisionMotion.PreviewStep(1f / 60f);
                Vector3 collisionEnd = collisionBone.transform.position + collisionBone.transform.right;
                float clearance = DistanceSegmentToCapsuleAxis(collisionBone.transform.position, collisionEnd, animatedCapsule);
                Vector3 colliderScale = animatedCapsule.transform.lossyScale;
                float capsuleRadius = animatedCapsule.radius * Mathf.Max(Mathf.Abs(colliderScale.x), Mathf.Abs(colliderScale.z));
                if (clearance < capsuleRadius + collisionMotion.Settings.collisionRadius - 0.02f)
                    throw new InvalidOperationException("Animated capsule still penetrated the full bone collision radius.");
                if (Quaternion.Angle(collisionBone.transform.localRotation, Quaternion.identity) < 0.01f)
                    throw new InvalidOperationException("Animated collider did not produce a hard collision response.");

                var meshObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                meshObject.name = "MirroredEquipment";
                meshObject.transform.SetParent(root.transform, false);
                var mirrored = meshObject.AddComponent<FDX_SecondaryMotion>();
                mirrored.Source = FDX_SecondaryMotion.MotionSource.AutomaticPivot;
                var pivot = new GameObject("MirrorSource_FDX_Pivot");
                pivot.transform.SetParent(meshObject.transform, false);
                pivot.transform.localPosition = Vector3.right * 0.2f;
                var group = mirrored.PivotGroups[0];
                group.pivot = pivot.transform;
                group.mirrorCenter = meshObject.transform;
                group.replicationMode = FDX_SecondaryMotion.PivotReplicationMode.Mirror;
                group.influenceRadius = 2f;
                MeshFilter filter = meshObject.GetComponent<MeshFilter>();
                generatedMesh = Instantiate(filter.sharedMesh);
                DestroyImmediate(meshObject.GetComponent<MeshRenderer>());
                var skin = meshObject.AddComponent<SkinnedMeshRenderer>();
                skin.sharedMesh = generatedMesh;
                mirrored.OriginalMeshFilter = filter;
                mirrored.AutomaticMultiPivotRenderer = skin;
                mirrored.DeformingRenderer = skin;
                mirrored.AutomaticMultiPivotSkinning = true;
                FDX_SecondaryMotionBilingualInspector.UpdateAutomaticMultiPivotSkinning(mirrored);
                if (group.replicatedPivots.Count != 1 || skin.bones.Length != 3 || generatedMesh.bindposes.Length != 3)
                    throw new InvalidOperationException("Mirror pivot did not join skinning.");
                mirrored.Settings.gravityStrength = 0f;
                mirrored.Settings.previewAutoSway = true;
                mirrored.Settings.enableDistanceSimulation = false;
                mirrored.RebuildSimulation();
                for (int i = 0; i < 30; i++) mirrored.PreviewStep(1f / 60f);
                if (Quaternion.Angle(pivot.transform.localRotation, Quaternion.identity) < 0.01f ||
                    Quaternion.Angle(group.replicatedPivots[0].localRotation, Quaternion.identity) < 0.01f)
                    throw new InvalidOperationException("Both mirrored pivots did not simulate.");
                FDX_SecondaryMotionBilingualInspector.ConvertToStatic(mirrored);
                if (meshObject.GetComponent<FDX_SecondaryMotion>() != null || meshObject.GetComponent<MeshRenderer>() == null ||
                    meshObject.GetComponent<SkinnedMeshRenderer>() != null)
                    throw new InvalidOperationException("Static conversion did not restore the renderer.");
                Debug.Log("FDX_REGRESSION_SMOKE_OK scan=1 auto_mount=1 attachment=1 release=1 bound_group=1 rotation=1 individual=1 collision=1 mirror=1 static=1 standalone=1 groups=1");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
            finally
            {
                if (root != null) DestroyImmediate(root);
                if (standaloneRoot != null) DestroyImmediate(standaloneRoot);
                if (generatedMesh != null) DestroyImmediate(generatedMesh);
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static float DistanceSegmentToCapsuleAxis(Vector3 segmentStart, Vector3 segmentEnd,
            CapsuleCollider capsule)
        {
            Vector3 scale = capsule.transform.lossyScale;
            Vector3 localAxis = capsule.direction == 0 ? Vector3.right : capsule.direction == 1 ? Vector3.up : Vector3.forward;
            float axisScale = capsule.direction == 0 ? Mathf.Abs(scale.x) :
                capsule.direction == 1 ? Mathf.Abs(scale.y) : Mathf.Abs(scale.z);
            float radialScale = capsule.direction == 0 ? Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)) :
                capsule.direction == 1 ? Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) :
                Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            float worldRadius = capsule.radius * radialScale;
            float halfLine = Mathf.Max(0f, capsule.height * axisScale * 0.5f - worldRadius);
            Vector3 center = capsule.transform.TransformPoint(capsule.center);
            Vector3 axis = capsule.transform.TransformDirection(localAxis).normalized;
            ClosestPointsOnSegments(segmentStart, segmentEnd, center - axis * halfLine, center + axis * halfLine,
                out Vector3 first, out Vector3 second);
            return Vector3.Distance(first, second);
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

        [MenuItem("Tools/FDX/Attachment Motion/Export Package 1.7.0")]
        public static void RunBatchExportPackage()
        {
            try
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                if (string.IsNullOrEmpty(projectRoot))
                    throw new InvalidOperationException("Unable to resolve the Unity project root.");

                string releaseDirectory = Path.Combine(projectRoot, "Releases");
                Directory.CreateDirectory(releaseDirectory);
                string outputPath = Path.Combine(releaseDirectory, "FDX_AttachmentMotion-1.7.0.unitypackage");
                AssetDatabase.ExportPackage(
                    "Assets/Scripts/Custom/FDX_AttachmentMotion",
                    outputPath,
                    ExportPackageOptions.Recurse);
                if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
                    throw new InvalidOperationException("Unity package export did not create a valid file.");
                Debug.Log($"FDX_ATTACHMENT_MOTION_PACKAGE_OK path={outputPath}");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += DuringSceneGUI;
            AdoptSelection();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
        }

        private void OnSelectionChange()
        {
            AdoptSelection();
            Repaint();
        }

        private void AdoptSelection()
        {
            if (Selection.activeGameObject == null) return;
            FDX_SecondaryMotion selected = Selection.activeGameObject.GetComponent<FDX_SecondaryMotion>();
            if (selected == null) selected = Selection.activeGameObject.GetComponentInParent<FDX_SecondaryMotion>();
            if (selected == null) return;
            motion = selected;
            rendererTarget = motion.DeformingRenderer != null
                ? motion.DeformingRenderer
                : motion.GetComponentInChildren<SkinnedMeshRenderer>();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField(new GUIContent("FDX 骨架與權重編輯器", "Rig & Weight Editor"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "一般物件可先用單一旋轉軸心。只有需要局部彎曲時才建立尾端控制點與自動權重；複雜模型再使用下方筆刷修正。",
                MessageType.Info);

            motion = (FDX_SecondaryMotion)EditorGUILayout.ObjectField(new GUIContent("動態元件", "Secondary Motion"), motion,
                typeof(FDX_SecondaryMotion), true);
            rendererTarget = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(new GUIContent("蒙皮網格", "Skinned Mesh"), rendererTarget,
                typeof(SkinnedMeshRenderer), true);

            using (new EditorGUI.DisabledScope(motion == null))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField(new GUIContent("快速建立", "Quick Setup"), EditorStyles.boldLabel);
                if (GUILayout.Button(new GUIContent("建立／選取旋轉軸心", "Create / Select Pivot"))) EnsurePivot();
                if (GUILayout.Button(new GUIContent("新增尾端控制點", "Add End Point"))) AddEndPoint();

                using (new EditorGUI.DisabledScope(motion == null || motion.EndPoints.Count == 0))
                {
                    if (GUILayout.Button(new GUIContent("產生彎曲骨架並自動計算權重", "Generate Rig & Weights"))) GenerateAutomaticRigAndWeights();
                }
            }

            EditorGUILayout.Space(10f);
            DrawWeightPaintingGUI();
            EditorGUILayout.EndScrollView();
        }

        private void DrawWeightPaintingGUI()
        {
            EditorGUILayout.LabelField(new GUIContent("精細權重", "Precise Weights"), EditorStyles.boldLabel);
            if (rendererTarget == null || rendererTarget.sharedMesh == null || rendererTarget.bones == null ||
                rendererTarget.bones.Length == 0)
            {
                EditorGUILayout.HelpBox("請先指定含骨頭的 SkinnedMeshRenderer，或先產生自動彎曲骨架。", MessageType.Warning);
                return;
            }

            string[] names = new string[rendererTarget.bones.Length];
            for (int i = 0; i < names.Length; i++)
                names[i] = rendererTarget.bones[i] != null ? rendererTarget.bones[i].name : $"Missing Bone {i}";
            selectedBoneIndex = Mathf.Clamp(selectedBoneIndex, 0, names.Length - 1);
            selectedBoneIndex = EditorGUILayout.Popup(new GUIContent("目前骨頭", "Current Bone"), selectedBoneIndex, names);
            string[] brushNames = { "增加", "減少", "取代", "平滑" };
            brushMode = (BrushMode)EditorGUILayout.Popup(new GUIContent("筆刷模式", "Brush Mode"), (int)brushMode, brushNames);
            brushRadiusPixels = EditorGUILayout.Slider(new GUIContent("筆刷畫面半徑", "Brush Radius"), brushRadiusPixels, 5f, 160f);
            brushStrength = EditorGUILayout.Slider(new GUIContent("筆刷強度", "Brush Strength"), brushStrength, 0.01f, 1f);
            paintInScene = EditorGUILayout.Toggle(new GUIContent("在場景視窗繪製", "Paint In Scene"), paintInScene);
            showAllVertices = EditorGUILayout.Toggle(new GUIContent("顯示全部頂點", "Show All Vertices"), showAllVertices);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("正規化全部權重", "Normalize Weights"))) NormalizeAllWeights();
            if (GUILayout.Button(new GUIContent("儲存目前網格", "Save Mesh"))) SaveMesh();
            EditorGUILayout.EndHorizontal();

            if (paintInScene)
                EditorGUILayout.HelpBox("在 Scene 視窗按住滑鼠左鍵塗抹。Alt 操作視角時不會修改權重。", MessageType.None);
        }

        private void EnsurePivot()
        {
            if (motion == null) return;
            if (motion.RotationPivot != null)
            {
                Selection.activeTransform = motion.RotationPivot;
                return;
            }

            Transform item = motion.transform;
            var go = new GameObject($"{item.name}_FDX_Pivot");
            Undo.RegisterCreatedObjectUndo(go, "Create FDX Pivot");
            Transform pivot = go.transform;
            Undo.SetTransformParent(pivot, item, "Parent FDX Pivot To Item");
            pivot.localPosition = Vector3.zero;
            pivot.localRotation = Quaternion.identity;
            pivot.localScale = Vector3.one;
            Undo.RecordObject(motion, "Assign FDX Pivot");
            motion.RotationPivot = pivot;
            motion.Source = FDX_SecondaryMotion.MotionSource.AutomaticPivot;
            EditorUtility.SetDirty(motion);
            Selection.activeTransform = pivot;
        }

        private void AddEndPoint()
        {
            if (motion == null) return;
            EnsurePivot();
            Transform pivot = motion.RotationPivot;
            var go = new GameObject($"FDX_EndPoint_{motion.EndPoints.Count + 1}");
            Undo.RegisterCreatedObjectUndo(go, "Create FDX End Point");
            go.transform.SetParent(pivot, false);
            go.transform.localPosition = Vector3.forward * 0.5f;

            Undo.RecordObject(motion, "Add FDX End Point");
            motion.EndPoints.Add(new FDX_SecondaryMotion.FlexibleEndPoint
            {
                displayName = go.name,
                tip = go.transform,
                detectionRadius = 0.15f,
                motionMultiplier = 1f,
                generatedSegments = 4
            });
            motion.EnableAdvancedFlexible = true;
            EditorUtility.SetDirty(motion);
            Selection.activeGameObject = go;
        }

        private void GenerateAutomaticRigAndWeights()
        {
            if (motion == null) return;
            EnsurePivot();
            if (!TryResolveSourceRenderer(out Mesh sourceMesh, out Transform meshTransform, out Material[] materials,
                    out Renderer oldRenderer))
            {
                EditorUtility.DisplayDialog("FDX", "找不到 MeshFilter 或 SkinnedMeshRenderer。", "確定");
                return;
            }

            Mesh generatedMesh;
            try
            {
                generatedMesh = Instantiate(sourceMesh);
                generatedMesh.name = $"{sourceMesh.name}_FDX_Deformed";
                _ = generatedMesh.vertices;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("FDX", "無法讀取網格頂點。請在模型 Import Settings 開啟 Read/Write。", "確定");
                return;
            }

            Transform pivot = motion.RotationPivot != null ? motion.RotationPivot : motion.transform;
            var bones = new List<Transform> { motion.transform };
            var rigSegments = new List<AutoRigSegment>();

            Undo.RecordObject(motion, "Generate FDX Rig");
            foreach (FDX_SecondaryMotion.PivotGroup group in motion.PivotGroups)
            {
                if (group == null || !group.enabled) continue;
                Transform groupPivot = group.pivot != null ? group.pivot : pivot;
                if (!bones.Contains(groupPivot)) bones.Add(groupPivot);
                foreach (FDX_SecondaryMotion.FlexibleEndPoint point in group.endPoints)
                    ClearGeneratedBones(point);
                foreach (FDX_SecondaryMotion.FlexibleEndPoint point in group.endPoints)
                    BuildAutomaticRigBranch(point, groupPivot, groupPivot, groupPivot.position, groupPivot.rotation, false,
                        point.mirrorAxis, true, bones, rigSegments);
            }

            if (rigSegments.Count == 0)
            {
                EditorUtility.DisplayDialog("FDX", "沒有有效的尾端控制點。", "確定");
                DestroyImmediate(generatedMesh);
                return;
            }

            string assetPath = CreateGeneratedMeshAsset(generatedMesh);
            generatedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            Matrix4x4[] bindPoses = new Matrix4x4[bones.Count];
            for (int i = 0; i < bones.Count; i++)
                bindPoses[i] = bones[i].worldToLocalMatrix * meshTransform.localToWorldMatrix;
            generatedMesh.bindposes = bindPoses;
            generatedMesh.boneWeights = CalculateAutomaticWeights(generatedMesh, meshTransform, rigSegments);
            generatedMesh.RecalculateBounds();
            EditorUtility.SetDirty(generatedMesh);

            SkinnedMeshRenderer skinned = meshTransform.GetComponent<SkinnedMeshRenderer>();
            if (skinned == null) skinned = Undo.AddComponent<SkinnedMeshRenderer>(meshTransform.gameObject);
            Undo.RecordObject(skinned, "Assign FDX Skinned Mesh");
            skinned.sharedMesh = generatedMesh;
            skinned.sharedMaterials = materials;
            skinned.rootBone = motion.transform;
            skinned.bones = bones.ToArray();
            skinned.updateWhenOffscreen = true;
            if (oldRenderer != null && oldRenderer != skinned)
            {
                Undo.RecordObject(oldRenderer, "Disable Original Renderer");
                oldRenderer.enabled = false;
            }

            motion.DeformingRenderer = skinned;
            motion.Source = FDX_SecondaryMotion.MotionSource.AutomaticPivot;
            motion.EnableAdvancedFlexible = true;
            rendererTarget = skinned;
            motion.RebuildSimulation();
            EditorUtility.SetDirty(motion);
            AssetDatabase.SaveAssets();
            Selection.activeObject = motion;
            SceneView.RepaintAll();
        }

        private static void ClearGeneratedBones(FDX_SecondaryMotion.FlexibleEndPoint point)
        {
            if (point == null) return;
            point.generatedBones.Clear();
            foreach (FDX_SecondaryMotion.FlexibleEndPoint child in point.children) ClearGeneratedBones(child);
        }

        private void BuildAutomaticRigBranch(FDX_SecondaryMotion.FlexibleEndPoint point, Transform controlParent,
            Transform boneParent, Vector3 start, Quaternion startRotation, bool mirrored,
            FDX_SecondaryMotion.MirrorAxis mirrorAxis, bool allowLiveMirror, List<Transform> bones,
            List<AutoRigSegment> rigSegments)
        {
            if (point == null || point.tip == null) return;
            Vector3 end;
            Quaternion endRotation;
            if (mirrored)
            {
                Vector3 local = controlParent.InverseTransformPoint(point.tip.position);
                end = start + startRotation * FDX_SecondaryMotion.MirrorLocalPoint(local, mirrorAxis);
                endRotation = startRotation * MirrorLocalRotation(point.tip.localRotation, mirrorAxis);
            }
            else
            {
                end = point.tip.position;
                endRotation = point.tip.rotation;
            }

            Vector3 direction = end - start;
            int segmentCount = Mathf.Clamp(point.generatedSegments, 1, 12);
            int boneStart = bones.Count;
            Transform previous = boneParent;
            for (int i = 1; i <= segmentCount; i++)
            {
                string mirrorSuffix = mirrored ? "_Mirror" : string.Empty;
                var boneObject = new GameObject($"FDX_AutoBone_{point.displayName}{mirrorSuffix}_{i:00}");
                Undo.RegisterCreatedObjectUndo(boneObject, "Create FDX Auto Bone");
                if (mirrored) boneObject.hideFlags = HideFlags.HideInHierarchy;
                Transform bone = boneObject.transform;
                bone.position = Vector3.Lerp(start, end, i / (float)segmentCount);
                bone.rotation = direction.sqrMagnitude > 0.000001f
                    ? Quaternion.LookRotation(direction.normalized, startRotation * Vector3.up)
                    : startRotation;
                bone.localScale = Vector3.one;
                bone.SetParent(previous, true);
                previous = bone;
                point.generatedBones.Add(bone);
                bones.Add(bone);
            }
            rigSegments.Add(new AutoRigSegment
            {
                point = point,
                start = start,
                end = end,
                boneStart = boneStart,
                boneCount = segmentCount
            });

            foreach (FDX_SecondaryMotion.FlexibleEndPoint child in point.children)
                BuildAutomaticRigBranch(child, point.tip, previous, end, endRotation, mirrored, mirrorAxis,
                    false, bones, rigSegments);

            if (allowLiveMirror && point.liveMirror)
                BuildAutomaticRigBranch(point, controlParent, boneParent, start, startRotation, true,
                    point.mirrorAxis, false, bones, rigSegments);
        }

        private static Quaternion MirrorLocalRotation(Quaternion rotation, FDX_SecondaryMotion.MirrorAxis axis)
        {
            Vector3 euler = rotation.eulerAngles;
            if (axis == FDX_SecondaryMotion.MirrorAxis.X) { euler.y = -euler.y; euler.z = -euler.z; }
            else if (axis == FDX_SecondaryMotion.MirrorAxis.Y) { euler.x = -euler.x; euler.z = -euler.z; }
            else { euler.x = -euler.x; euler.y = -euler.y; }
            return Quaternion.Euler(euler);
        }

        private BoneWeight[] CalculateAutomaticWeights(Mesh mesh, Transform meshTransform,
            IReadOnlyList<AutoRigSegment> rigSegments)
        {
            Vector3[] vertices = mesh.vertices;
            var weights = new BoneWeight[vertices.Length];

            for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
            {
                Vector3 world = meshTransform.TransformPoint(vertices[vertexIndex]);
                float bestScore = 0f;
                int bestBone = 0;
                float bestAlong = 0f;

                foreach (AutoRigSegment rigSegment in rigSegments)
                {
                    FDX_SecondaryMotion.FlexibleEndPoint point = rigSegment.point;
                    Vector3 start = rigSegment.start;
                    Vector3 end = rigSegment.end;
                    Vector3 segment = end - start;
                    float lengthSquared = segment.sqrMagnitude;
                    if (lengthSquared < 0.000001f) continue;
                    float along = Mathf.Clamp01(Vector3.Dot(world - start, segment) / lengthSquared);
                    Vector3 closest = start + segment * along;
                    float radial = Vector3.Distance(world, closest);
                    if (radial > point.detectionRadius) continue;
                    float radialWeight = 1f - radial / Mathf.Max(0.0001f, point.detectionRadius);
                    float score = radialWeight * Mathf.Clamp01(point.weightFalloff.Evaluate(along));
                    if (score <= bestScore) continue;

                    bestScore = score;
                    bestAlong = along;
                    int segmentIndex = Mathf.Clamp(Mathf.CeilToInt(along * rigSegment.boneCount) - 1,
                        0, rigSegment.boneCount - 1);
                    bestBone = rigSegment.boneStart + segmentIndex;
                }

                float flexibleWeight = Mathf.Clamp01(bestScore * bestAlong);
                weights[vertexIndex] = new BoneWeight
                {
                    boneIndex0 = bestBone,
                    weight0 = flexibleWeight,
                    boneIndex1 = 0,
                    weight1 = 1f - flexibleWeight
                };
            }
            return weights;
        }

        private bool TryResolveSourceRenderer(out Mesh mesh, out Transform meshTransform, out Material[] materials,
            out Renderer oldRenderer)
        {
            SkinnedMeshRenderer skinned = rendererTarget != null
                ? rendererTarget
                : motion.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinned != null && skinned.sharedMesh != null)
            {
                mesh = skinned.sharedMesh;
                meshTransform = skinned.transform;
                materials = skinned.sharedMaterials;
                oldRenderer = skinned;
                return true;
            }

            MeshFilter filter = motion.GetComponentInChildren<MeshFilter>();
            MeshRenderer meshRenderer = filter != null ? filter.GetComponent<MeshRenderer>() : null;
            if (filter != null && filter.sharedMesh != null && meshRenderer != null)
            {
                mesh = filter.sharedMesh;
                meshTransform = filter.transform;
                materials = meshRenderer.sharedMaterials;
                oldRenderer = meshRenderer;
                return true;
            }

            mesh = null;
            meshTransform = null;
            materials = null;
            oldRenderer = null;
            return false;
        }

        private static string CreateGeneratedMeshAsset(Mesh mesh)
        {
            const string folder = "Assets/FDX_Generated";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "FDX_Generated");
            string safeName = string.Join("_", mesh.name.Split(Path.GetInvalidFileNameChars()));
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safeName}.asset");
            AssetDatabase.CreateAsset(mesh, path);
            return path;
        }

        private void DuringSceneGUI(SceneView sceneView)
        {
            if (!paintInScene || rendererTarget == null || rendererTarget.sharedMesh == null) return;
            Mesh mesh = rendererTarget.sharedMesh;
            Vector3[] vertices;
            BoneWeight[] weights;
            try
            {
                vertices = mesh.vertices;
                weights = mesh.boneWeights;
            }
            catch
            {
                return;
            }
            if (vertices.Length == 0 || weights.Length != vertices.Length) return;

            Event current = Event.current;
            Vector2 mouse = current.mousePosition;
            var affected = new List<int>();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 world = rendererTarget.transform.TransformPoint(vertices[i]);
                Vector2 gui = HandleUtility.WorldToGUIPoint(world);
                float distance = Vector2.Distance(gui, mouse);
                if (distance <= brushRadiusPixels) affected.Add(i);

                if (showAllVertices || GetBoneWeight(weights[i], selectedBoneIndex) > 0.001f)
                {
                    float value = GetBoneWeight(weights[i], selectedBoneIndex);
                    Handles.color = Color.Lerp(new Color(0f, 0.2f, 1f, 0.3f), new Color(1f, 0.1f, 0f, 0.9f), value);
                    float size = HandleUtility.GetHandleSize(world) * 0.018f;
                    Handles.DotHandleCap(0, world, Quaternion.identity, size, EventType.Repaint);
                }
            }

            Handles.BeginGUI();
            Color previous = GUI.color;
            GUI.color = new Color(0.2f, 0.85f, 1f, 0.7f);
            GUI.DrawTexture(new Rect(mouse.x - brushRadiusPixels, mouse.y - brushRadiusPixels,
                brushRadiusPixels * 2f, brushRadiusPixels * 2f), EditorGUIUtility.whiteTexture);
            GUI.color = previous;
            Handles.EndGUI();

            if (current.alt || current.button != 0 ||
                (current.type != EventType.MouseDown && current.type != EventType.MouseDrag)) return;
            if (affected.Count == 0) return;

            Undo.RecordObject(mesh, "Paint FDX Bone Weights");
            float smoothTarget = 0f;
            if (brushMode == BrushMode.Smooth)
            {
                foreach (int index in affected) smoothTarget += GetBoneWeight(weights[index], selectedBoneIndex);
                smoothTarget /= affected.Count;
            }

            foreach (int index in affected)
            {
                float oldValue = GetBoneWeight(weights[index], selectedBoneIndex);
                float nextValue = oldValue;
                switch (brushMode)
                {
                    case BrushMode.Add: nextValue = oldValue + brushStrength; break;
                    case BrushMode.Subtract: nextValue = oldValue - brushStrength; break;
                    case BrushMode.Replace: nextValue = brushStrength; break;
                    case BrushMode.Smooth: nextValue = Mathf.Lerp(oldValue, smoothTarget, brushStrength); break;
                }
                BoneWeight boneWeight = weights[index];
                SetBoneWeight(ref boneWeight, selectedBoneIndex, Mathf.Clamp01(nextValue));
                weights[index] = boneWeight;
            }

            mesh.boneWeights = weights;
            EditorUtility.SetDirty(mesh);
            current.Use();
            sceneView.Repaint();
        }

        private void NormalizeAllWeights()
        {
            if (rendererTarget == null || rendererTarget.sharedMesh == null) return;
            Mesh mesh = rendererTarget.sharedMesh;
            BoneWeight[] weights = mesh.boneWeights;
            Undo.RecordObject(mesh, "Normalize FDX Bone Weights");
            for (int i = 0; i < weights.Length; i++)
            {
                BoneWeight value = weights[i];
                float total = value.weight0 + value.weight1 + value.weight2 + value.weight3;
                if (total <= 0.000001f)
                {
                    value.boneIndex0 = 0;
                    value.weight0 = 1f;
                }
                else
                {
                    value.weight0 /= total;
                    value.weight1 /= total;
                    value.weight2 /= total;
                    value.weight3 /= total;
                }
                weights[i] = value;
            }
            mesh.boneWeights = weights;
            EditorUtility.SetDirty(mesh);
            SceneView.RepaintAll();
        }

        private void SaveMesh()
        {
            if (rendererTarget == null || rendererTarget.sharedMesh == null) return;
            EditorUtility.SetDirty(rendererTarget.sharedMesh);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static float GetBoneWeight(BoneWeight value, int boneIndex)
        {
            if (value.boneIndex0 == boneIndex) return value.weight0;
            if (value.boneIndex1 == boneIndex) return value.weight1;
            if (value.boneIndex2 == boneIndex) return value.weight2;
            if (value.boneIndex3 == boneIndex) return value.weight3;
            return 0f;
        }

        private static void SetBoneWeight(ref BoneWeight value, int boneIndex, float newWeight)
        {
            int slot;
            if (value.boneIndex0 == boneIndex) slot = 0;
            else if (value.boneIndex1 == boneIndex) slot = 1;
            else if (value.boneIndex2 == boneIndex) slot = 2;
            else if (value.boneIndex3 == boneIndex) slot = 3;
            else
            {
                float minimum = Mathf.Min(value.weight0, value.weight1, value.weight2, value.weight3);
                slot = Mathf.Approximately(minimum, value.weight0) ? 0 :
                    Mathf.Approximately(minimum, value.weight1) ? 1 :
                    Mathf.Approximately(minimum, value.weight2) ? 2 : 3;
            }

            float oldSelected = slot == 0 ? value.weight0 : slot == 1 ? value.weight1 : slot == 2 ? value.weight2 : value.weight3;
            float remainingOld = Mathf.Max(0.000001f, 1f - oldSelected);
            float remainingNew = 1f - newWeight;
            float scale = remainingNew / remainingOld;

            value.weight0 *= scale;
            value.weight1 *= scale;
            value.weight2 *= scale;
            value.weight3 *= scale;
            if (slot == 0) { value.boneIndex0 = boneIndex; value.weight0 = newWeight; }
            else if (slot == 1) { value.boneIndex1 = boneIndex; value.weight1 = newWeight; }
            else if (slot == 2) { value.boneIndex2 = boneIndex; value.weight2 = newWeight; }
            else { value.boneIndex3 = boneIndex; value.weight3 = newWeight; }
        }
    }

    [InitializeOnLoad]
    internal static class FDX_EditModePreviewDriver
    {
        private static readonly HashSet<FDX_AttachmentManager> activeManagers =
            new HashSet<FDX_AttachmentManager>();
        private static readonly Dictionary<FDX_AttachmentManager, HashSet<FDX_SecondaryMotion>> managerMotions =
            new Dictionary<FDX_AttachmentManager, HashSet<FDX_SecondaryMotion>>();
        private static readonly HashSet<FDX_SecondaryMotion> managedMotions = new HashSet<FDX_SecondaryMotion>();
        private static double previousTime;
        private static double nextActiveRefreshTime;
        private static bool discoveryDirty = true;
        private static readonly HashSet<FDX_SecondaryMotion> activeStandalone = new HashSet<FDX_SecondaryMotion>();
        private static readonly List<FDX_AttachmentManager> inactiveManagers = new List<FDX_AttachmentManager>();
        private static readonly List<FDX_SecondaryMotion> inactiveStandalone = new List<FDX_SecondaryMotion>();

        static FDX_EditModePreviewDriver()
        {
            FDX_SecondaryMotion.EditModeDistanceReferenceProvider = () =>
                SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null
                    ? SceneView.lastActiveSceneView.camera.transform
                    : null;
            previousTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Update;
            EditorApplication.hierarchyChanged += InvalidateDiscovery;
            Undo.undoRedoPerformed += InvalidateDiscovery;
            AssemblyReloadEvents.beforeAssemblyReload += StopAll;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void Update()
        {
            double now = EditorApplication.timeSinceStartup;
            float deltaTime = Mathf.Clamp((float)(now - previousTime), 1f / 240f, 1f / 20f);
            previousTime = now;

            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (discoveryDirty || ((activeManagers.Count > 0 || activeStandalone.Count > 0) &&
                                   now >= nextActiveRefreshTime)) DiscoverPreviewTargets(now);
            if (activeManagers.Count == 0 && activeStandalone.Count == 0) return;

            inactiveManagers.Clear();
            foreach (FDX_AttachmentManager manager in activeManagers)
            {
                if (manager == null || !manager.PreviewInEditMode || !manager.enabled ||
                    !manager.gameObject.scene.IsValid())
                {
                    if (manager != null && managerMotions.TryGetValue(manager, out HashSet<FDX_SecondaryMotion> stopped))
                        foreach (FDX_SecondaryMotion motion in stopped) if (motion != null) motion.StopPreview();
                    inactiveManagers.Add(manager);
                    continue;
                }
                if (!managerMotions.TryGetValue(manager, out HashSet<FDX_SecondaryMotion> motions)) continue;
                FDX_SecondaryMotion solo = null;
                if (Selection.activeGameObject != null)
                {
                    FDX_SecondaryMotion selected = Selection.activeGameObject.GetComponent<FDX_SecondaryMotion>();
                    if (selected == null) selected = Selection.activeGameObject.GetComponentInParent<FDX_SecondaryMotion>();
                    if (selected != null && motions.Contains(selected)) solo = selected;
                }
                foreach (FDX_SecondaryMotion motion in motions)
                {
                    if (motion == null || !motion.gameObject.scene.IsValid()) continue;
                    if (solo == null || motion == solo) motion.PreviewStep(deltaTime);
                    else motion.StopPreview();
                }
            }
            foreach (FDX_AttachmentManager manager in inactiveManagers) activeManagers.Remove(manager);
            inactiveStandalone.Clear();
            foreach (FDX_SecondaryMotion motion in activeStandalone)
            {
                if (motion != null && motion.gameObject.scene.IsValid() && motion.isActiveAndEnabled &&
                    motion.Simulate && motion.Settings.previewAutoSway)
                {
                    motion.PreviewStep(deltaTime);
                    continue;
                }
                if (motion != null) motion.StopPreview();
                inactiveStandalone.Add(motion);
            }
            foreach (FDX_SecondaryMotion motion in inactiveStandalone) activeStandalone.Remove(motion);
            if (activeManagers.Count > 0 || activeStandalone.Count > 0) SceneView.RepaintAll();
        }

        private static void DiscoverPreviewTargets(double now)
        {
            discoveryDirty = false;
            nextActiveRefreshTime = now + 1d;
            var previousManagers = new HashSet<FDX_AttachmentManager>(activeManagers);
            var previousStandalone = new HashSet<FDX_SecondaryMotion>(activeStandalone);
            activeManagers.Clear();
            activeStandalone.Clear();
            managerMotions.Clear();
            managedMotions.Clear();

            foreach (FDX_AttachmentManager manager in Resources.FindObjectsOfTypeAll<FDX_AttachmentManager>())
            {
                if (manager == null || !manager.isActiveAndEnabled || !manager.gameObject.scene.IsValid()) continue;
                var motions = new HashSet<FDX_SecondaryMotion>(manager.FindAllMotionComponents());
                managerMotions[manager] = motions;
                managedMotions.UnionWith(motions);
                if (manager.PreviewInEditMode) activeManagers.Add(manager);
            }
            foreach (FDX_AttachmentManager previous in previousManagers)
                if (previous != null && !activeManagers.Contains(previous)) previous.StopPreview();

            foreach (FDX_SecondaryMotion motion in Resources.FindObjectsOfTypeAll<FDX_SecondaryMotion>())
                if (motion != null && motion.gameObject.scene.IsValid() && !managedMotions.Contains(motion) &&
                    motion.isActiveAndEnabled && motion.Simulate && motion.Settings.previewAutoSway)
                    activeStandalone.Add(motion);
            foreach (FDX_SecondaryMotion previous in previousStandalone)
                if (previous != null && !activeStandalone.Contains(previous) && !managedMotions.Contains(previous))
                    previous.StopPreview();
        }

        internal static void InvalidateDiscovery()
        {
            discoveryDirty = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) StopAll();
        }

        internal static void Deactivate(FDX_AttachmentManager manager)
        {
            if (manager != null && managerMotions.TryGetValue(manager, out HashSet<FDX_SecondaryMotion> motions))
                foreach (FDX_SecondaryMotion motion in motions) if (motion != null) motion.StopPreview();
            activeManagers.Remove(manager);
            managerMotions.Remove(manager);
            discoveryDirty = true;
            SceneView.RepaintAll();
        }

        private static void StopAll()
        {
            foreach (FDX_AttachmentManager manager in activeManagers)
                if (manager != null) manager.StopPreview();
            activeManagers.Clear();
            managerMotions.Clear();
            managedMotions.Clear();
            foreach (FDX_SecondaryMotion motion in activeStandalone)
                if (motion != null) motion.StopPreview();
            activeStandalone.Clear();
            discoveryDirty = true;
        }
    }

#pragma warning restore UDR0004
}
