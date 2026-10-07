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

                FDX_AttachmentManager manager = root.AddComponent<FDX_AttachmentManager>();
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
                for (int i = 0; i < 30; i++) manager.PreviewStep(1f / 60f);
                if (Quaternion.Angle(previewStart, root.transform.localRotation) < 0.001f)
                    throw new InvalidOperationException("Edit Mode Preview did not rotate the automatic motion target.");
                if (Quaternion.Angle(secondPreviewStart, secondPivotObject.transform.localRotation) < 0.001f)
                    throw new InvalidOperationException("Multi-pivot preview did not rotate the second motion target.");
                manager.StopPreview();

                Debug.Log($"FDX_ATTACHMENT_MOTION_SMOKE_OK bones={result.bones.Length} vertices={result.sharedMesh.vertexCount} chains=1");
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

        [MenuItem("Tools/FDX/Attachment Motion/Export Package 1.4.0")]
        public static void RunBatchExportPackage()
        {
            try
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                if (string.IsNullOrEmpty(projectRoot))
                    throw new InvalidOperationException("Unable to resolve the Unity project root.");

                string releaseDirectory = Path.Combine(projectRoot, "Releases");
                Directory.CreateDirectory(releaseDirectory);
                string outputPath = Path.Combine(releaseDirectory, "FDX_AttachmentMotion-1.4.0.unitypackage");
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
        private static double previousTime;
        private static double nextDiscoveryTime;

        static FDX_EditModePreviewDriver()
        {
            previousTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload += StopAll;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void Update()
        {
            double now = EditorApplication.timeSinceStartup;
            float deltaTime = Mathf.Clamp((float)(now - previousTime), 1f / 240f, 1f / 20f);
            previousTime = now;

            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (now >= nextDiscoveryTime)
            {
                nextDiscoveryTime = now + 0.5d;
                foreach (FDX_AttachmentManager manager in Resources.FindObjectsOfTypeAll<FDX_AttachmentManager>())
                    if (manager != null && manager.PreviewInEditMode && manager.enabled &&
                        manager.gameObject.scene.IsValid()) activeManagers.Add(manager);
            }

            var inactive = new List<FDX_AttachmentManager>();
            foreach (FDX_AttachmentManager manager in activeManagers)
            {
                if (manager == null || !manager.PreviewInEditMode || !manager.enabled ||
                    !manager.gameObject.scene.IsValid())
                {
                    if (manager != null) manager.StopPreview();
                    inactive.Add(manager);
                    continue;
                }
                manager.PreviewStep(deltaTime);
            }
            foreach (FDX_AttachmentManager manager in inactive) activeManagers.Remove(manager);
            if (activeManagers.Count > 0) SceneView.RepaintAll();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) StopAll();
        }

        private static void StopAll()
        {
            foreach (FDX_AttachmentManager manager in activeManagers)
                if (manager != null) manager.StopPreview();
            activeManagers.Clear();
        }
    }

    internal sealed class FDX_AttachmentManagerEditor : UnityEditor.Editor
    {
        private readonly Dictionary<int, UnityEditor.Editor> motionEditors = new Dictionary<int, UnityEditor.Editor>();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            var manager = (FDX_AttachmentManager)target;
            EditorGUILayout.Space(8f);
            EditorGUI.BeginChangeCheck();
            bool preview = EditorGUILayout.Toggle(
                new GUIContent("編輯模式預覽（Edit Mode Preview）", "不進入 Play Mode，直接在 Scene View 預覽擺動效果。"),
                manager.PreviewInEditMode);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(manager, "Toggle FDX Edit Mode Preview");
                manager.PreviewInEditMode = preview;
                if (!preview) manager.StopPreview();
                EditorUtility.SetDirty(manager);
                SceneView.RepaintAll();
            }

            EditorGUILayout.LabelField("偵測到的動態元件（Detected Motion Components）", EditorStyles.boldLabel);

            if (manager.SettingsMode == FDX_AttachmentManager.MotionSettingsMode.Unified &&
                GUILayout.Button("將統一設定套用到全部物件（Apply Shared Settings To All）"))
            {
                RecordAllMotionUndo(manager);
                manager.ApplySharedSettingsToAll();
                EditorUtility.SetDirty(manager);
            }

            bool foundAny = false;
            var displayedMotions = new HashSet<FDX_SecondaryMotion>();
            foreach (FDX_AttachmentManager.AttachmentSlot slot in manager.Attachments)
            {
                if (slot == null || slot.source == null) continue;
                List<FDX_SecondaryMotion> motions = manager.FindMotionComponents(slot);
                motions.RemoveAll(motion => motion == null || !displayedMotions.Add(motion));
                if (motions.Count == 0) continue;
                foundAny = true;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(slot.displayName) ? slot.source.name : slot.displayName,
                    EditorStyles.boldLabel);

                bool showIndividual = manager.SettingsMode == FDX_AttachmentManager.MotionSettingsMode.PerAttachment ||
                                      slot.useIndividualMotionSettings;
                if (!showIndividual)
                {
                    EditorGUILayout.HelpBox($"偵測到 {motions.Count} 個 FDX_SecondaryMotion，目前使用全員統一設定（Unified Settings）。",
                        MessageType.Info);
                }
                else
                {
                    DrawMotionEditors(motions);
                }
                EditorGUILayout.EndVertical();
            }

            List<FDX_SecondaryMotion> hierarchyMotions = manager.FindHierarchyMotionComponents();
            hierarchyMotions.RemoveAll(motion => motion == null || !displayedMotions.Add(motion));
            if (hierarchyMotions.Count > 0)
            {
                foundAny = true;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Animator 現有子階層（Animator Hierarchy）", EditorStyles.boldLabel);
                if (manager.SettingsMode == FDX_AttachmentManager.MotionSettingsMode.Unified)
                {
                    EditorGUILayout.HelpBox(
                        $"自動偵測到 {hierarchyMotions.Count} 個 FDX_SecondaryMotion，目前使用全員統一設定（Unified Settings）。",
                        MessageType.Info);
                    foreach (FDX_SecondaryMotion motion in hierarchyMotions)
                        EditorGUILayout.ObjectField(motion.name, motion, typeof(FDX_SecondaryMotion), true);
                }
                else
                {
                    DrawMotionEditors(hierarchyMotions);
                }
                EditorGUILayout.EndVertical();
            }

            if (!foundAny)
                EditorGUILayout.HelpBox("掛載來源與 Animator 子階層內都沒有偵測到 FDX_SecondaryMotion（No motion component detected）。",
                    MessageType.None);
        }

        private void DrawMotionEditors(List<FDX_SecondaryMotion> motions)
        {
            foreach (FDX_SecondaryMotion motion in motions)
            {
                if (motion == null) continue;
                int id = motion.GetInstanceID();
                motionEditors.TryGetValue(id, out UnityEditor.Editor childEditor);
                CreateCachedEditor(motion, null, ref childEditor);
                motionEditors[id] = childEditor;
                EditorGUILayout.LabelField(motion.name, EditorStyles.miniBoldLabel);
                childEditor.OnInspectorGUI();
            }
        }

        private static void RecordAllMotionUndo(FDX_AttachmentManager manager)
        {
            var objects = new List<UnityEngine.Object>();
            objects.AddRange(manager.FindAllMotionComponents());
            if (objects.Count > 0) Undo.RecordObjects(objects.ToArray(), "Apply FDX Motion Settings");
        }

        private void OnDisable()
        {
            foreach (UnityEditor.Editor editor in motionEditors.Values)
                if (editor != null) DestroyImmediate(editor);
            motionEditors.Clear();
        }
    }

    internal sealed class FDX_SecondaryMotionEditor : UnityEditor.Editor
    {
        private SerializedProperty motionSource;
        private SerializedProperty simulate;
        private SerializedProperty settings;
        private SerializedProperty existingBones;
        private SerializedProperty autoCreatePivot;
        private SerializedProperty rotationPivot;
        private SerializedProperty enableAdvancedFlexible;
        private SerializedProperty endPoints;
        private SerializedProperty enablePreciseWeights;
        private SerializedProperty deformingRenderer;
        private SerializedProperty explicitColliders;
        private SerializedProperty showGizmos;
        private SerializedProperty pivotColor;
        private SerializedProperty endPointColor;
        private SerializedProperty pivotGizmoRadius;
        private SerializedProperty endPointGizmoRadius;

        private void OnEnable()
        {
            motionSource = serializedObject.FindProperty("motionSource");
            simulate = serializedObject.FindProperty("simulate");
            settings = serializedObject.FindProperty("settings");
            existingBones = serializedObject.FindProperty("existingBones");
            autoCreatePivot = serializedObject.FindProperty("autoCreatePivot");
            rotationPivot = serializedObject.FindProperty("rotationPivot");
            enableAdvancedFlexible = serializedObject.FindProperty("enableAdvancedFlexible");
            endPoints = serializedObject.FindProperty("endPoints");
            enablePreciseWeights = serializedObject.FindProperty("enablePreciseWeights");
            deformingRenderer = serializedObject.FindProperty("deformingRenderer");
            explicitColliders = serializedObject.FindProperty("explicitColliders");
            showGizmos = serializedObject.FindProperty("showGizmos");
            pivotColor = serializedObject.FindProperty("pivotColor");
            endPointColor = serializedObject.FindProperty("endPointColor");
            pivotGizmoRadius = serializedObject.FindProperty("pivotGizmoRadius");
            endPointGizmoRadius = serializedObject.FindProperty("endPointGizmoRadius");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(simulate);
            EditorGUILayout.PropertyField(motionSource);
            EditorGUILayout.PropertyField(settings, true);

            if ((FDX_SecondaryMotion.MotionSource)motionSource.enumValueIndex == FDX_SecondaryMotion.MotionSource.ExistingBones)
            {
                EditorGUILayout.PropertyField(existingBones, true);
                if (existingBones.arraySize == 0)
                    EditorGUILayout.HelpBox("尚未指定有效骨架。可以切換為「Automatic Pivot」讓無骨架物件直接擺動。",
                        MessageType.Warning);
            }
            else
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("預設旋轉軸心", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(autoCreatePivot);
                EditorGUILayout.PropertyField(rotationPivot);
                if (rotationPivot.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("預設會在執行時建立空物件作為旋轉軸心。也可以現在建立以便在場景中調整位置。",
                        MessageType.Info);
                    if (GUILayout.Button("現在建立旋轉軸心")) CreatePivotWrapper();
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.PropertyField(enableAdvancedFlexible, new GUIContent("啟用進階彎曲設定"));
                if (enableAdvancedFlexible.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(endPoints, true);
                    if (GUILayout.Button("新增尾端控制點")) AddEndPoint();

                    EditorGUILayout.Space(4f);
                    EditorGUILayout.PropertyField(enablePreciseWeights, new GUIContent("啟用精細權重設定"));
                    if (enablePreciseWeights.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(deformingRenderer);
                        if (GUILayout.Button("開啟 FDX 骨架與權重編輯器")) OpenWeightEditor();
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.PropertyField(explicitColliders, true);
            EditorGUILayout.Space(6f);
            EditorGUILayout.PropertyField(showGizmos);
            if (showGizmos.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(pivotColor);
                EditorGUILayout.PropertyField(endPointColor);
                EditorGUILayout.PropertyField(pivotGizmoRadius);
                EditorGUILayout.PropertyField(endPointGizmoRadius);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space(8f);
            if (GUILayout.Button("重新建立模擬快取"))
            {
                foreach (UnityEngine.Object item in targets)
                    ((FDX_SecondaryMotion)item).RebuildSimulation();
            }
        }

        private void CreatePivotWrapper()
        {
            var motion = (FDX_SecondaryMotion)target;
            Transform item = motion.transform;
            var go = new GameObject($"{item.name}_FDX_Pivot");
            Undo.RegisterCreatedObjectUndo(go, "Create FDX Pivot");
            Transform pivot = go.transform;
            Undo.SetTransformParent(pivot, item, "Parent FDX Pivot To Item");
            pivot.localPosition = Vector3.zero;
            pivot.localRotation = Quaternion.identity;
            pivot.localScale = Vector3.one;
            rotationPivot.objectReferenceValue = pivot;
            serializedObject.ApplyModifiedProperties();
            Selection.activeTransform = pivot;
        }

        private void AddEndPoint()
        {
            var motion = (FDX_SecondaryMotion)target;
            Transform pivot = motion.RotationPivot != null ? motion.RotationPivot : motion.transform;
            var go = new GameObject($"FDX_EndPoint_{endPoints.arraySize + 1}");
            Undo.RegisterCreatedObjectUndo(go, "Create FDX End Point");
            go.transform.SetParent(pivot, false);
            go.transform.localPosition = Vector3.forward * 0.5f;

            int index = endPoints.arraySize;
            endPoints.InsertArrayElementAtIndex(index);
            SerializedProperty element = endPoints.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("displayName").stringValue = go.name;
            element.FindPropertyRelative("tip").objectReferenceValue = go.transform;
            element.FindPropertyRelative("detectionRadius").floatValue = 0.15f;
            element.FindPropertyRelative("motionMultiplier").floatValue = 1f;
            element.FindPropertyRelative("generatedSegments").intValue = 4;
            serializedObject.ApplyModifiedProperties();
            Selection.activeGameObject = go;
        }

        private void OpenWeightEditor()
        {
            Selection.activeObject = target;
            foreach (Type type in TypeCache.GetTypesDerivedFrom<EditorWindow>())
            {
                if (type.Name != "FDX_RigWeightEditor") continue;
                EditorWindow window = EditorWindow.GetWindow(type);
                window.Show();
                return;
            }
            EditorUtility.DisplayDialog("FDX", "找不到 FDX_RigWeightEditor。請確認 Editor 檔案已安裝。", "確定");
        }
    }
#pragma warning restore UDR0004
}
