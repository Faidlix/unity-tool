using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Faidlix.UnityTools.Editor
{
    internal static class FDX_InspectorGUI
    {
        private static GUIContent C(string chinese, string english) => new GUIContent(chinese, english);

        public static void DrawMotionSettings(SerializedProperty property)
        {
            SerializedProperty perAxis = property.FindPropertyRelative("perAxisSettings");
            EditorGUILayout.PropertyField(perAxis, C("各軸獨立設定", "Per-Axis Settings"));
            if (perAxis.boolValue)
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("inertiaPerAxis"), C("慣性 XYZ", "Inertia"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("springPerAxis"), C("彈力 XYZ", "Spring"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("dampingPerAxis"), C("阻尼 XYZ", "Damping"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("maxAnglePerAxis"), C("最大角度 XYZ", "Max Angle"));
            }
            else
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("inertia"), C("慣性", "Inertia"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("spring"), C("彈力", "Spring"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("damping"), C("阻尼", "Damping"));
                EditorGUILayout.Slider(property.FindPropertyRelative("maxAngle"), 0f, 180f, C("最大角度", "Max Angle"));
            }

            EditorGUILayout.Slider(property.FindPropertyRelative("animationBlend"), 0f, 1f,
                C("動畫混合", "Animation Blend"));
            EditorGUILayout.IntSlider(property.FindPropertyRelative("substeps"), 1, 4,
                C("模擬子步進", "Substeps"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("gravityStrength"), C("重力強度", "Gravity Strength"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("gravityDirection"), C("重力方向", "Gravity Direction"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("constantWind"), C("固定風力", "Constant Wind"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("windMultiplier"), C("風力倍率", "Wind Multiplier"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("teleportDistance"), C("瞬移判定距離", "Teleport Distance"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("maxDeltaTime"), C("最大更新間隔", "Max Delta Time"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(C("碰撞", "Collision"), EditorStyles.boldLabel);
            SerializedProperty enableCollision = property.FindPropertyRelative("enableCollision");
            EditorGUILayout.PropertyField(enableCollision, C("啟用碰撞", "Enable Collision"));
            if (enableCollision.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(property.FindPropertyRelative("collisionLayers"), C("碰撞圖層", "Collision Layers"));
                EditorGUILayout.Slider(property.FindPropertyRelative("collisionRadius"), 0.001f, 2f,
                    C("碰撞半徑", "Collision Radius"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("collisionStrength"), C("碰撞強度", "Collision Strength"));
                EditorGUILayout.Slider(property.FindPropertyRelative("collisionFriction"), 0f, 1f,
                    C("碰撞摩擦力", "Collision Friction"));
                EditorGUI.indentLevel--;
            }
        }

        public static void DrawAxisSettings(SerializedProperty property)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(C("啟用軸向", "Enabled Axes"));
            DrawEnabledAxis(property.FindPropertyRelative("lockX"), "X");
            DrawEnabledAxis(property.FindPropertyRelative("lockY"), "Y");
            DrawEnabledAxis(property.FindPropertyRelative("lockZ"), "Z");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            SerializedProperty individual = property.FindPropertyRelative("useIndividualDynamics");
            EditorGUILayout.PropertyField(individual, C("使用個別動態設定", "Individual Dynamics"));
            if (!individual.boolValue) return;
            EditorGUI.indentLevel++;
            SerializedProperty perAxis = property.FindPropertyRelative("perAxisSettings");
            EditorGUILayout.PropertyField(perAxis, C("各軸獨立設定", "Per-Axis Settings"));
            if (perAxis.boolValue)
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("inertiaPerAxis"), C("慣性 XYZ", "Inertia"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("springPerAxis"), C("彈力 XYZ", "Spring"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("dampingPerAxis"), C("阻尼 XYZ", "Damping"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("maxAnglePerAxis"), C("最大角度 XYZ", "Max Angle"));
            }
            else
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("inertia"), C("慣性", "Inertia"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("spring"), C("彈力", "Spring"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("damping"), C("阻尼", "Damping"));
                EditorGUILayout.Slider(property.FindPropertyRelative("maxAngle"), 0f, 180f, C("最大角度", "Max Angle"));
            }
            EditorGUI.indentLevel--;
        }

        private static void DrawEnabledAxis(SerializedProperty lockProperty, string axis)
        {
            bool enabled = !lockProperty.boolValue;
            EditorGUI.BeginChangeCheck();
            enabled = EditorGUILayout.ToggleLeft(new GUIContent(axis, "Enable " + axis + " Axis"), enabled, GUILayout.Width(36f));
            if (EditorGUI.EndChangeCheck()) lockProperty.boolValue = !enabled;
        }
    }

    [CustomEditor(typeof(FDX_AttachmentManager))]
    internal sealed class FDX_AttachmentManagerBilingualInspector : UnityEditor.Editor
    {
        private readonly Dictionary<int, UnityEditor.Editor> motionEditors = new Dictionary<int, UnityEditor.Editor>();
        private SerializedProperty animator;
        private SerializedProperty attachments;
        private SerializedProperty settingsMode;
        private SerializedProperty sharedMotionSettings;
        private SerializedProperty applySharedSettingsOnAttach;
        private SerializedProperty previewInEditMode;

        private void OnEnable()
        {
            animator = serializedObject.FindProperty("animator");
            attachments = serializedObject.FindProperty("attachments");
            settingsMode = serializedObject.FindProperty("settingsMode");
            sharedMotionSettings = serializedObject.FindProperty("sharedMotionSettings");
            applySharedSettingsOnAttach = serializedObject.FindProperty("applySharedSettingsOnAttach");
            previewInEditMode = serializedObject.FindProperty("previewInEditMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(animator, new GUIContent("角色動畫控制器", "Character Animator"));
            if (GUILayout.Button(new GUIContent("重新綁定與建立", "Rebind & Rebuild")))
            {
                serializedObject.ApplyModifiedProperties();
                var manager = (FDX_AttachmentManager)target;
                Undo.RecordObject(manager, "Rebind FDX Attachments");
                manager.RebindAndRebuild();
                EditorUtility.SetDirty(manager);
                serializedObject.Update();
            }
            EditorGUILayout.PropertyField(previewInEditMode, new GUIContent("編輯模式預覽", "Edit Mode Preview"));
            DrawAttachments();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(new GUIContent("動態設定同步", "Motion Settings Sync"), EditorStyles.boldLabel);
            string[] modeNames = { "全員統一", "各掛載物件" };
            settingsMode.enumValueIndex = EditorGUILayout.Popup(new GUIContent("設定模式", "Settings Mode"), settingsMode.enumValueIndex, modeNames);
            EditorGUILayout.PropertyField(applySharedSettingsOnAttach,
                new GUIContent("掛載時套用統一設定", "Apply Shared Settings On Attach"));
            if (settingsMode.enumValueIndex == (int)FDX_AttachmentManager.MotionSettingsMode.Unified)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(new GUIContent("統一動態設定", "Shared Motion Settings"), EditorStyles.boldLabel);
                FDX_InspectorGUI.DrawMotionSettings(sharedMotionSettings);
                EditorGUILayout.EndVertical();
            }
            serializedObject.ApplyModifiedProperties();

            DrawDetectedMotions();
        }

        private void DrawAttachments()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(new GUIContent($"掛載清單  {attachments.arraySize}", "Attachments"), EditorStyles.boldLabel);
            for (int i = 0; i < attachments.arraySize; i++)
            {
                SerializedProperty slot = attachments.GetArrayElementAtIndex(i);
                SerializedProperty name = slot.FindPropertyRelative("displayName");
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(slot.FindPropertyRelative("enabled"), GUIContent.none, GUILayout.Width(18f));
                name.stringValue = EditorGUILayout.TextField(name.stringValue);
                if (GUILayout.Button(new GUIContent("刪除", "Remove"), GUILayout.Width(105f)))
                {
                    attachments.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(slot.FindPropertyRelative("source"), new GUIContent("來源物件", "Source"));

                SerializedProperty dataVersion = slot.FindPropertyRelative("dataVersion");
                SerializedProperty anchorMode = slot.FindPropertyRelative("anchorMode");
                if (dataVersion.intValue == 0)
                {
                    bool legacyHumanoid = slot.FindPropertyRelative("useHumanoidBone").boolValue;
                    anchorMode.enumValueIndex = legacyHumanoid
                        ? (int)FDX_AttachmentManager.AnchorMode.HumanoidBone
                        : (int)FDX_AttachmentManager.AnchorMode.DirectTransform;
                    dataVersion.intValue = 1;
                }
                string[] anchorNames = { "人形骨架", "直接指定", "名稱／路徑" };
                anchorMode.enumValueIndex = EditorGUILayout.Popup(new GUIContent("掛點模式", "Anchor Mode"), anchorMode.enumValueIndex, anchorNames);
                if (anchorMode.enumValueIndex == (int)FDX_AttachmentManager.AnchorMode.HumanoidBone)
                    EditorGUILayout.PropertyField(slot.FindPropertyRelative("humanoidBone"), new GUIContent("人形骨頭", "Humanoid Bone"));
                else if (anchorMode.enumValueIndex == (int)FDX_AttachmentManager.AnchorMode.DirectTransform)
                    EditorGUILayout.PropertyField(slot.FindPropertyRelative("customAnchor"), new GUIContent("直接掛點", "Direct Anchor"));
                else
                {
                    EditorGUILayout.PropertyField(slot.FindPropertyRelative("searchRoot"), new GUIContent("搜尋根節點", "Search Root"));
                    EditorGUILayout.PropertyField(slot.FindPropertyRelative("anchorNameOrPath"), new GUIContent("名稱／相對路徑", "Name or Relative Path"));
                    EditorGUILayout.PropertyField(slot.FindPropertyRelative("ignoreCase"), new GUIContent("忽略大小寫", "Ignore Case"));
                    EditorGUILayout.PropertyField(slot.FindPropertyRelative("ignoreNamespace"), new GUIContent("忽略命名空間", "Ignore Namespace"));
                }
                EditorGUILayout.PropertyField(slot.FindPropertyRelative("instantiateAtRuntime"), new GUIContent("執行時建立", "Instantiate At Runtime"));
                EditorGUILayout.PropertyField(slot.FindPropertyRelative("localPosition"), new GUIContent("本地位置", "Local Position"));
                EditorGUILayout.PropertyField(slot.FindPropertyRelative("localEulerAngles"), new GUIContent("本地旋轉", "Local Rotation"));
                EditorGUILayout.PropertyField(slot.FindPropertyRelative("localScale"), new GUIContent("本地縮放", "Local Scale"));
                EditorGUILayout.PropertyField(slot.FindPropertyRelative("useIndividualMotionSettings"),
                    new GUIContent("使用個別動態設定", "Individual Motion Settings"));
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button(new GUIContent("新增掛載項目", "Add Attachment")))
            {
                serializedObject.ApplyModifiedProperties();
                var manager = (FDX_AttachmentManager)target;
                Undo.RecordObject(manager, "Add FDX Attachment");
                manager.AddAttachment();
                EditorUtility.SetDirty(manager);
                serializedObject.Update();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawDetectedMotions()
        {
            var manager = (FDX_AttachmentManager)target;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(new GUIContent("偵測到的動態元件", "Detected Motion Components"), EditorStyles.boldLabel);
            if (manager.SettingsMode == FDX_AttachmentManager.MotionSettingsMode.Unified &&
                GUILayout.Button(new GUIContent("將統一設定套用到全部物件", "Apply Shared Settings To All")))
            {
                List<FDX_SecondaryMotion> all = manager.FindAllMotionComponents();
                if (all.Count > 0) Undo.RecordObjects(all.ToArray(), "Apply FDX Motion Settings");
                manager.ApplySharedSettingsToAll();
            }

            List<FDX_SecondaryMotion> motions = manager.FindAllMotionComponents();
            if (motions.Count == 0)
            {
                EditorGUILayout.HelpBox("掛載來源與 Animator 子階層內都沒有偵測到 FDX_SecondaryMotion。", MessageType.None);
                return;
            }
            EditorGUILayout.HelpBox($"自動偵測到 {motions.Count} 個動態元件。", MessageType.Info);
            foreach (FDX_SecondaryMotion motion in motions)
            {
                if (motion == null) continue;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.ObjectField(motion.name, motion, typeof(FDX_SecondaryMotion), true);
                bool individual = manager.UsesIndividualSettings(motion);
                EditorGUI.BeginChangeCheck();
                individual = EditorGUILayout.Toggle(new GUIContent("使用個別設定", "Individual Settings"), individual);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(manager, "Change FDX Individual Settings");
                    manager.SetIndividualSettings(motion, individual);
                    EditorUtility.SetDirty(manager);
                }
                if (!individual)
                {
                    EditorGUILayout.HelpBox("目前使用管理器的統一設定。", MessageType.None);
                    EditorGUILayout.EndVertical();
                    continue;
                }
                int id = motion.GetInstanceID();
                motionEditors.TryGetValue(id, out UnityEditor.Editor child);
                CreateCachedEditor(motion, null, ref child);
                motionEditors[id] = child;
                child.OnInspectorGUI();
                EditorGUILayout.EndVertical();
            }
        }

        private void OnDisable()
        {
            foreach (UnityEditor.Editor editor in motionEditors.Values)
                if (editor != null) DestroyImmediate(editor);
            motionEditors.Clear();
        }
    }

    [CustomEditor(typeof(FDX_SecondaryMotion))]
    internal sealed class FDX_SecondaryMotionBilingualInspector : UnityEditor.Editor
    {
        private static FDX_MotionSettings copiedSettings;
        private readonly List<Transform> boneCandidates = new List<Transform>();
        private SerializedProperty motionSource;
        private SerializedProperty simulate;
        private SerializedProperty forceSpace;
        private SerializedProperty settings;
        private SerializedProperty simulationAnchor;
        private SerializedProperty boneChains;
        private SerializedProperty existingBones;
        private SerializedProperty autoCreatePivot;
        private SerializedProperty rotationPivot;
        private SerializedProperty pivotInfluenceRadius;
        private SerializedProperty pivotAxisSettings;
        private SerializedProperty pivotGroups;
        private SerializedProperty enableAdvancedFlexible;
        private SerializedProperty endPoints;
        private SerializedProperty enablePreciseWeights;
        private SerializedProperty deformingRenderer;
        private SerializedProperty explicitColliders;
        private SerializedProperty showGizmos;
        private SerializedProperty showAllInfluenceRanges;
        private SerializedProperty pivotColor;
        private SerializedProperty endPointColor;
        private SerializedProperty mirrorColor;
        private SerializedProperty pivotGizmoRadius;
        private SerializedProperty endPointGizmoRadius;
        private Transform editingTip;

        private void OnEnable()
        {
            motionSource = serializedObject.FindProperty("motionSource");
            simulate = serializedObject.FindProperty("simulate");
            forceSpace = serializedObject.FindProperty("forceSpace");
            settings = serializedObject.FindProperty("settings");
            simulationAnchor = serializedObject.FindProperty("simulationAnchor");
            boneChains = serializedObject.FindProperty("boneChains");
            existingBones = serializedObject.FindProperty("existingBones");
            autoCreatePivot = serializedObject.FindProperty("autoCreatePivot");
            rotationPivot = serializedObject.FindProperty("rotationPivot");
            pivotInfluenceRadius = serializedObject.FindProperty("pivotInfluenceRadius");
            pivotAxisSettings = serializedObject.FindProperty("pivotAxisSettings");
            pivotGroups = serializedObject.FindProperty("pivotGroups");
            enableAdvancedFlexible = serializedObject.FindProperty("enableAdvancedFlexible");
            endPoints = serializedObject.FindProperty("endPoints");
            enablePreciseWeights = serializedObject.FindProperty("enablePreciseWeights");
            deformingRenderer = serializedObject.FindProperty("deformingRenderer");
            explicitColliders = serializedObject.FindProperty("explicitColliders");
            showGizmos = serializedObject.FindProperty("showGizmos");
            showAllInfluenceRanges = serializedObject.FindProperty("showAllInfluenceRanges");
            pivotColor = serializedObject.FindProperty("pivotColor");
            endPointColor = serializedObject.FindProperty("endPointColor");
            mirrorColor = serializedObject.FindProperty("mirrorColor");
            pivotGizmoRadius = serializedObject.FindProperty("pivotGizmoRadius");
            endPointGizmoRadius = serializedObject.FindProperty("endPointGizmoRadius");
            SceneView.duringSceneGui += DuringSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
            editingTip = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(simulate, new GUIContent("模擬", "Simulate"));
            string[] forceSpaces = { "世界座標", "基準本地座標" };
            forceSpace.enumValueIndex = EditorGUILayout.Popup(new GUIContent("力場座標", "Force Space"), forceSpace.enumValueIndex, forceSpaces);
            string[] sources = { "自動旋轉軸心", "現有骨架鏈" };
            motionSource.enumValueIndex = EditorGUILayout.Popup(new GUIContent("運作來源", "Motion Source"), motionSource.enumValueIndex, sources);

            if ((FDX_SecondaryMotion.MotionSource)motionSource.enumValueIndex == FDX_SecondaryMotion.MotionSource.ExistingBones)
                DrawBoneChains();
            else
                DrawAutomaticPivot();

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(new GUIContent("動態設定", "Motion Settings"), EditorStyles.boldLabel);
            FDX_InspectorGUI.DrawMotionSettings(settings);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("複製設定", "Copy Settings")))
            {
                serializedObject.ApplyModifiedProperties();
                copiedSettings = new FDX_MotionSettings();
                copiedSettings.CopyFrom(((FDX_SecondaryMotion)target).Settings);
                serializedObject.Update();
            }
            using (new EditorGUI.DisabledScope(copiedSettings == null))
            {
                if (GUILayout.Button(new GUIContent("貼上設定", "Paste Settings")))
                {
                    serializedObject.ApplyModifiedProperties();
                    var motion = (FDX_SecondaryMotion)target;
                    Undo.RecordObject(motion, "Paste FDX Motion Settings");
                    motion.Settings.CopyFrom(copiedSettings);
                    motion.RebuildSimulation();
                    EditorUtility.SetDirty(motion);
                    serializedObject.Update();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6f);
            EditorGUILayout.PropertyField(explicitColliders, new GUIContent("指定碰撞器", "Explicit Colliders"), true);
            DrawGizmos();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("重新建立模擬", "Rebuild Simulation"))) ((FDX_SecondaryMotion)target).RebuildSimulation();
            if (GUILayout.Button(new GUIContent("重設姿勢", "Reset Pose"))) ((FDX_SecondaryMotion)target).ResetSimulation();
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button(new GUIContent("記錄目前預設姿勢", "Capture Current Rest Pose")))
                ((FDX_SecondaryMotion)target).CaptureCurrentRestPose();
            if (GUILayout.Button(new GUIContent("檢查設定", "Validate Setup"))) ShowValidation((FDX_SecondaryMotion)target);
        }

        private void DrawBoneChains()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(new GUIContent("現有骨架鏈", "Existing Bone Chains"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(simulationAnchor, new GUIContent("模擬基準", "Simulation Anchor"));
            EditorGUILayout.HelpBox("模擬基準只提供移動與旋轉參考，不會被加入擺動。每組只需指定第一根骨頭。", MessageType.Info);
            for (int i = 0; i < boneChains.arraySize; i++)
            {
                SerializedProperty chain = boneChains.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(chain.FindPropertyRelative("enabled"), GUIContent.none, GUILayout.Width(18f));
                EditorGUILayout.PropertyField(chain.FindPropertyRelative("displayName"), GUIContent.none);
                if (GUILayout.Button(new GUIContent("刪除", "Remove"), GUILayout.Width(105f)))
                {
                    boneChains.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(chain.FindPropertyRelative("root"), new GUIContent("骨架鏈起點", "Chain Root"));
                EditorGUILayout.PropertyField(chain.FindPropertyRelative("solo"), new GUIContent("單獨預覽", "Solo"));
                if (chain.FindPropertyRelative("root").objectReferenceValue != null &&
                    GUILayout.Button(new GUIContent("尋找並加入對側骨架", "Find Opposite Bone")))
                {
                    FindAndAddOppositeBone(i);
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.PropertyField(chain.FindPropertyRelative("includeChildBones"), new GUIContent("自動包含子骨頭", "Include Child Bones"));
                if (chain.FindPropertyRelative("includeChildBones").boolValue)
                {
                    EditorGUILayout.PropertyField(chain.FindPropertyRelative("includeAllBranches"), new GUIContent("包含全部分支", "Include All Branches"));
                    EditorGUILayout.PropertyField(chain.FindPropertyRelative("endBone"), new GUIContent("結束骨頭", "End Bone"));
                    EditorGUILayout.PropertyField(chain.FindPropertyRelative("includeEndBone"), new GUIContent("模擬結束骨頭", "Include End Bone"));
                }
                DrawObjectList(chain.FindPropertyRelative("excludedBones"), "排除骨頭", typeof(Transform));
                EditorGUILayout.Slider(chain.FindPropertyRelative("influence"), 0f, 2f, new GUIContent("影響倍率", "Influence"));
                EditorGUILayout.Slider(chain.FindPropertyRelative("displayRadius"), 0.001f, 1f, new GUIContent("顯示範圍", "Display Radius"));
                FDX_InspectorGUI.DrawAxisSettings(chain.FindPropertyRelative("axisSettings"));
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button(new GUIContent("新增骨架鏈組", "Add Bone Chain")))
            {
                AddBoneChain(null);
                GUIUtility.ExitGUI();
            }

            DrawBoneCandidateScanner();

            if (existingBones.arraySize > 0)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField(new GUIContent("舊版單骨頭設定", "Legacy Bone Entries"), EditorStyles.boldLabel);
                for (int i = 0; i < existingBones.arraySize; i++)
                {
                    SerializedProperty entry = existingBones.GetArrayElementAtIndex(i);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("transform"), new GUIContent("骨頭", "Bone"));
                    EditorGUILayout.Slider(entry.FindPropertyRelative("influence"), 0f, 2f, new GUIContent("影響倍率", "Influence"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("localAxis"), new GUIContent("本地方向", "Local Axis"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("length"), new GUIContent("長度", "Length"));
                    FDX_InspectorGUI.DrawAxisSettings(entry.FindPropertyRelative("axisSettings"));
                    if (GUILayout.Button(new GUIContent("移除舊版項目", "Remove Legacy Entry"))) { existingBones.DeleteArrayElementAtIndex(i); break; }
                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void DrawAutomaticPivot()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(new GUIContent("旋轉軸心組", "Rotation Pivot Groups"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoCreatePivot, new GUIContent("自動建立第一個旋轉軸心", "Auto Create First Rotation Pivot"));
            var motion = (FDX_SecondaryMotion)target;
            for (int i = 0; i < pivotGroups.arraySize && i < motion.PivotGroups.Count; i++)
                DrawPivotGroup(i, pivotGroups.GetArrayElementAtIndex(i), motion.PivotGroups[i]);
            if (GUILayout.Button(new GUIContent("新增旋轉軸心組", "Add Rotation Pivot Group"))) AddPivotGroup();

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(enablePreciseWeights, new GUIContent("啟用精細權重設定", "Precise Weight Editing"));
            if (enablePreciseWeights.boolValue)
            {
                EditorGUILayout.PropertyField(deformingRenderer, new GUIContent("變形網格", "Deforming Renderer"));
                if (GUILayout.Button(new GUIContent("開啟 FDX 骨架與權重編輯器", "Open Rig & Weight Editor"))) OpenWeightEditor();
            }
        }

        private void DrawPivotGroup(int index, SerializedProperty groupProperty, FDX_SecondaryMotion.PivotGroup group)
        {
            SerializedProperty expanded = groupProperty.FindPropertyRelative("expanded");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("enabled"), GUIContent.none, GUILayout.Width(18f));
            expanded.boolValue = EditorGUILayout.Foldout(expanded.boolValue,
                string.IsNullOrWhiteSpace(group.displayName) ? $"旋轉軸心 {index + 1}" : group.displayName, true);
            if (GUILayout.Button("刪除", GUILayout.Width(45f)))
            {
                RemovePivotGroup(index, group);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
            if (!expanded.boolValue) { EditorGUILayout.EndVertical(); return; }

            EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("displayName"), new GUIContent("顯示名稱", "Display Name"));
            EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("solo"), new GUIContent("單獨預覽", "Solo"));
            EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("pivot"), new GUIContent("旋轉軸心", "Rotation Pivot"));
            EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("motionTarget"),
                new GUIContent("擺動目標", "Motion Target; leave empty to use the pivot"));
            SerializedProperty automaticAnchor = groupProperty.FindPropertyRelative("automaticSimulationAnchor");
            EditorGUILayout.PropertyField(automaticAnchor, new GUIContent("自動取得模擬基準", "Automatic Simulation Anchor"));
            if (automaticAnchor.boolValue)
            {
                Transform resolved = group.pivot != null && group.pivot.parent != null ? group.pivot.parent : group.pivot;
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField(new GUIContent("目前模擬基準", "Resolved Simulation Anchor"), resolved, typeof(Transform), true);
            }
            else EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("simulationAnchor"), new GUIContent("模擬基準", "Simulation Anchor"));
            EditorGUILayout.Slider(groupProperty.FindPropertyRelative("influenceRadius"), 0.001f, 2f,
                new GUIContent("軸心影響範圍", "Pivot Influence Radius"));
            FDX_InspectorGUI.DrawAxisSettings(groupProperty.FindPropertyRelative("axisSettings"));

            SerializedProperty liveMirror = groupProperty.FindPropertyRelative("liveMirror");
            EditorGUILayout.PropertyField(liveMirror, new GUIContent("持續對稱編輯", "Live Mirror Editing"));
            if (liveMirror.boolValue)
            {
                string[] axes = { "X 軸", "Y 軸", "Z 軸" };
                SerializedProperty mirrorAxis = groupProperty.FindPropertyRelative("mirrorAxis");
                mirrorAxis.enumValueIndex = EditorGUILayout.Popup(new GUIContent("對稱軸向", "Mirror Axis"), mirrorAxis.enumValueIndex, axes);
                EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("mirrorCenter"),
                    new GUIContent("對稱中心", "Mirror Center; empty uses this component Transform"));
            }
            EditorGUILayout.BeginHorizontal();
            if (group.pivot == null && GUILayout.Button(new GUIContent("建立旋轉軸心", "Create Rotation Pivot"))) CreatePivot(index);
            if (group.pivot != null)
            {
                string editLabel = editingTip == group.pivot ? "結束編輯" : "編輯軸心位置";
                if (GUILayout.Button(new GUIContent(editLabel, "Edit Pivot Position"))) editingTip = editingTip == group.pivot ? null : group.pivot;
                if (GUILayout.Button(new GUIContent("複製鏡射", "Duplicate Mirrored")))
                {
                    DuplicateMirroredPivot(index);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();

            SerializedProperty advanced = groupProperty.FindPropertyRelative("enableAdvancedFlexible");
            EditorGUILayout.PropertyField(advanced, new GUIContent("啟用進階柔性設定", "Advanced Flexible Settings"));
            if (advanced.boolValue)
            {
                EditorGUI.indentLevel++;
                SerializedProperty points = groupProperty.FindPropertyRelative("endPoints");
                DrawEndPointList(points, group.endPoints, group.pivot != null ? group.pivot : ((FDX_SecondaryMotion)target).transform, 0);
                if (GUILayout.Button(new GUIContent("新增尾端控制點", "Add End Point"))) AddRootEndPoint(group);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawEndPointList(SerializedProperty listProperty,
            List<FDX_SecondaryMotion.FlexibleEndPoint> runtimeList, Transform parent, int depth)
        {
            for (int i = 0; i < listProperty.arraySize && i < runtimeList.Count; i++)
            {
                SerializedProperty pointProperty = listProperty.GetArrayElementAtIndex(i);
                FDX_SecondaryMotion.FlexibleEndPoint point = runtimeList[i];
                SerializedProperty expanded = pointProperty.FindPropertyRelative("expanded");
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                expanded.boolValue = EditorGUILayout.Foldout(expanded.boolValue,
                    string.IsNullOrWhiteSpace(point.displayName) ? $"控制點 {i + 1}" : point.displayName, true);
                if (GUILayout.Button("刪除", GUILayout.Width(45f)))
                {
                    RemoveEndPoint(runtimeList, i, point);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
                if (expanded.boolValue)
                {
                    EditorGUILayout.PropertyField(pointProperty.FindPropertyRelative("displayName"), new GUIContent("顯示名稱", "Display Name"));
                    EditorGUILayout.PropertyField(pointProperty.FindPropertyRelative("tip"), new GUIContent("控制點", "Tip"));
                    EditorGUILayout.Slider(pointProperty.FindPropertyRelative("detectionRadius"), 0.001f, 2f,
                        new GUIContent("偵測半徑", "Detection Radius"));
                    EditorGUILayout.Slider(pointProperty.FindPropertyRelative("motionMultiplier"), 0f, 2f,
                        new GUIContent("擺動倍率", "Motion Multiplier"));
                    EditorGUILayout.IntSlider(pointProperty.FindPropertyRelative("generatedSegments"), 1, 12,
                        new GUIContent("自動骨段數", "Generated Segments"));
                    EditorGUILayout.PropertyField(pointProperty.FindPropertyRelative("weightFalloff"), new GUIContent("權重衰減", "Weight Falloff"));
                    FDX_InspectorGUI.DrawAxisSettings(pointProperty.FindPropertyRelative("axisSettings"));

                    SerializedProperty liveMirror = pointProperty.FindPropertyRelative("liveMirror");
                    EditorGUILayout.PropertyField(liveMirror, new GUIContent("持續對稱編輯", "Live Mirror Editing"));
                    if (liveMirror.boolValue)
                    {
                        string[] axes = { "X 軸", "Y 軸", "Z 軸" };
                        SerializedProperty mirrorAxis = pointProperty.FindPropertyRelative("mirrorAxis");
                        mirrorAxis.enumValueIndex = EditorGUILayout.Popup(new GUIContent("對稱軸向", "Mirror Axis"), mirrorAxis.enumValueIndex, axes);
                    }

                    EditorGUILayout.BeginHorizontal();
                    string editLabel = editingTip == point.tip ? "結束編輯" : "編輯位置";
                    if (GUILayout.Button(editLabel)) editingTip = editingTip == point.tip ? null : point.tip;
                    if (GUILayout.Button(new GUIContent("複製鏡射", "Duplicate Mirrored")))
                    {
                        DuplicateMirrored(runtimeList, point, parent);
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                    if (GUILayout.Button(new GUIContent("新增子控制點", "Add Child End Point")))
                    {
                        AddChildEndPoint(point);
                        GUIUtility.ExitGUI();
                    }

                    SerializedProperty children = pointProperty.FindPropertyRelative("children");
                    if (children.arraySize > 0 && point.children != null)
                    {
                        EditorGUI.indentLevel++;
                        DrawEndPointList(children, point.children, point.tip != null ? point.tip : parent, depth + 1);
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawGizmos()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(new GUIContent("場景顯示", "Scene Gizmos"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showGizmos, new GUIContent("顯示輔助圖形", "Show Gizmos"));
            if (!showGizmos.boolValue) return;
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(showAllInfluenceRanges, new GUIContent("顯示全部影響範圍", "Show All Influence Ranges"));
            EditorGUILayout.PropertyField(pivotColor, new GUIContent("軸心顏色", "Pivot Color"));
            EditorGUILayout.PropertyField(endPointColor, new GUIContent("控制點顏色", "End Point Color"));
            EditorGUILayout.PropertyField(mirrorColor, new GUIContent("對稱預覽顏色", "Mirror Color"));
            EditorGUILayout.Slider(pivotGizmoRadius, 0.001f, 1f, new GUIContent("軸心標記大小", "Pivot Gizmo Radius"));
            EditorGUILayout.Slider(endPointGizmoRadius, 0.001f, 1f, new GUIContent("控制點標記大小", "End Point Gizmo Radius"));
            EditorGUI.indentLevel--;
        }

        private void DuringSceneGUI(SceneView sceneView)
        {
            if (editingTip == null || target == null) return;
            Handles.color = Color.yellow;
            EditorGUI.BeginChangeCheck();
            Vector3 next = Handles.PositionHandle(editingTip.position, editingTip.rotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(editingTip, "Move FDX End Point");
                editingTip.position = next;
                EditorUtility.SetDirty(editingTip);
                sceneView.Repaint();
            }
        }

        private void CreatePivot(int groupIndex)
        {
            var motion = (FDX_SecondaryMotion)target;
            if (groupIndex < 0 || groupIndex >= motion.PivotGroups.Count) return;
            var group = motion.PivotGroups[groupIndex];
            var go = new GameObject($"{motion.name}_FDX_Pivot_{groupIndex + 1}");
            Undo.RegisterCreatedObjectUndo(go, "Create FDX Pivot");
            Undo.SetTransformParent(go.transform, motion.transform, "Parent FDX Pivot");
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            Undo.RecordObject(motion, "Assign FDX Pivot");
            group.pivot = go.transform;
            if (groupIndex == 0) motion.RotationPivot = go.transform;
            EditorUtility.SetDirty(motion);
            serializedObject.Update();
        }

        private void AddRootEndPoint(FDX_SecondaryMotion.PivotGroup group)
        {
            var motion = (FDX_SecondaryMotion)target;
            Transform parent = group.pivot != null ? group.pivot : motion.transform;
            Undo.RecordObject(motion, "Add FDX End Point");
            group.endPoints.Add(CreateEndPoint(parent, $"FDX_EndPoint_{group.endPoints.Count + 1}"));
            EditorUtility.SetDirty(motion);
        }

        private void AddPivotGroup()
        {
            serializedObject.ApplyModifiedProperties();
            var motion = (FDX_SecondaryMotion)target;
            Undo.RecordObject(motion, "Add FDX Pivot Group");
            motion.PivotGroups.Add(new FDX_SecondaryMotion.PivotGroup
            {
                displayName = $"旋轉軸心 {motion.PivotGroups.Count + 1}"
            });
            EditorUtility.SetDirty(motion);
            serializedObject.Update();
        }

        private void RemovePivotGroup(int index, FDX_SecondaryMotion.PivotGroup group)
        {
            var motion = (FDX_SecondaryMotion)target;
            Undo.RecordObject(motion, "Remove FDX Pivot Group");
            if (editingTip == group.pivot) editingTip = null;
            motion.PivotGroups.RemoveAt(index);
            EditorUtility.SetDirty(motion);
            serializedObject.Update();
        }

        private void DuplicateMirroredPivot(int index)
        {
            serializedObject.ApplyModifiedProperties();
            var motion = (FDX_SecondaryMotion)target;
            if (index < 0 || index >= motion.PivotGroups.Count) return;
            var source = motion.PivotGroups[index];
            Transform center = source.mirrorCenter != null ? source.mirrorCenter : motion.transform;
            var clone = new FDX_SecondaryMotion.PivotGroup
            {
                displayName = SwapSideName(source.displayName),
                enabled = source.enabled,
                motionTarget = null,
                automaticSimulationAnchor = source.automaticSimulationAnchor,
                simulationAnchor = source.simulationAnchor,
                influenceRadius = source.influenceRadius,
                liveMirror = false,
                mirrorAxis = source.mirrorAxis,
                mirrorCenter = source.mirrorCenter,
                enableAdvancedFlexible = source.enableAdvancedFlexible
            };
            clone.axisSettings.CopyFrom(source.axisSettings);
            if (source.pivot != null)
            {
                var go = new GameObject(SwapSideName(source.pivot.name));
                Undo.RegisterCreatedObjectUndo(go, "Create Mirrored FDX Pivot");
                Undo.SetTransformParent(go.transform, source.pivot.parent, "Parent Mirrored FDX Pivot");
                Vector3 localToCenter = center.InverseTransformPoint(source.pivot.position);
                go.transform.position = center.TransformPoint(FDX_SecondaryMotion.MirrorLocalPoint(localToCenter, source.mirrorAxis));
                go.transform.rotation = center.rotation * MirrorRotation(center.rotation, source.pivot.rotation, source.mirrorAxis);
                go.transform.localScale = source.pivot.localScale;
                clone.pivot = go.transform;
            }
            Transform pointParent = clone.pivot != null ? clone.pivot : motion.transform;
            foreach (FDX_SecondaryMotion.FlexibleEndPoint point in source.endPoints)
                clone.endPoints.Add(CloneMirrored(point, pointParent, source.mirrorAxis));
            Undo.RecordObject(motion, "Duplicate Mirrored FDX Pivot Group");
            motion.PivotGroups.Add(clone);
            EditorUtility.SetDirty(motion);
            serializedObject.Update();
        }

        private static Quaternion MirrorRotation(Quaternion centerRotation, Quaternion sourceRotation,
            FDX_SecondaryMotion.MirrorAxis axis)
        {
            Quaternion local = Quaternion.Inverse(centerRotation) * sourceRotation;
            Vector3 euler = local.eulerAngles;
            if (axis == FDX_SecondaryMotion.MirrorAxis.X) { euler.y = -euler.y; euler.z = -euler.z; }
            else if (axis == FDX_SecondaryMotion.MirrorAxis.Y) { euler.x = -euler.x; euler.z = -euler.z; }
            else { euler.x = -euler.x; euler.y = -euler.y; }
            return Quaternion.Euler(euler);
        }

        private void AddChildEndPoint(FDX_SecondaryMotion.FlexibleEndPoint parentPoint)
        {
            var motion = (FDX_SecondaryMotion)target;
            Transform parent = parentPoint.tip != null ? parentPoint.tip : motion.transform;
            Undo.RecordObject(motion, "Add FDX Child End Point");
            if (parentPoint.children == null) parentPoint.children = new List<FDX_SecondaryMotion.FlexibleEndPoint>();
            parentPoint.children.Add(CreateEndPoint(parent, $"{parent.name}_Child_{parentPoint.children.Count + 1}"));
            EditorUtility.SetDirty(motion);
        }

        private static FDX_SecondaryMotion.FlexibleEndPoint CreateEndPoint(Transform parent, string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create FDX End Point");
            Undo.SetTransformParent(go.transform, parent, "Parent FDX End Point");
            go.transform.localPosition = Vector3.forward * 0.5f;
            go.transform.localRotation = Quaternion.identity;
            return new FDX_SecondaryMotion.FlexibleEndPoint { displayName = name, tip = go.transform };
        }

        private void DuplicateMirrored(List<FDX_SecondaryMotion.FlexibleEndPoint> siblings,
            FDX_SecondaryMotion.FlexibleEndPoint source, Transform parent)
        {
            var motion = (FDX_SecondaryMotion)target;
            Undo.RecordObject(motion, "Duplicate Mirrored FDX End Point");
            siblings.Add(CloneMirrored(source, parent, source.mirrorAxis));
            EditorUtility.SetDirty(motion);
        }

        private static FDX_SecondaryMotion.FlexibleEndPoint CloneMirrored(
            FDX_SecondaryMotion.FlexibleEndPoint source, Transform destinationParent, FDX_SecondaryMotion.MirrorAxis axis)
        {
            string name = SwapSideName(source.displayName);
            var clone = new FDX_SecondaryMotion.FlexibleEndPoint
            {
                displayName = name,
                detectionRadius = source.detectionRadius,
                motionMultiplier = source.motionMultiplier,
                generatedSegments = source.generatedSegments,
                weightFalloff = source.weightFalloff != null ? new AnimationCurve(source.weightFalloff.keys) : AnimationCurve.Linear(0f, 0f, 1f, 1f),
                liveMirror = false,
                mirrorAxis = axis
            };
            clone.axisSettings.CopyFrom(source.axisSettings);
            if (source.tip != null)
            {
                var go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Create Mirrored FDX End Point");
                Undo.SetTransformParent(go.transform, destinationParent, "Parent Mirrored FDX End Point");
                go.transform.localPosition = FDX_SecondaryMotion.MirrorLocalPoint(source.tip.localPosition, axis);
                Vector3 euler = source.tip.localEulerAngles;
                if (axis == FDX_SecondaryMotion.MirrorAxis.X) { euler.y = -euler.y; euler.z = -euler.z; }
                else if (axis == FDX_SecondaryMotion.MirrorAxis.Y) { euler.x = -euler.x; euler.z = -euler.z; }
                else { euler.x = -euler.x; euler.y = -euler.y; }
                go.transform.localEulerAngles = euler;
                go.transform.localScale = source.tip.localScale;
                clone.tip = go.transform;
            }
            Transform nextParent = clone.tip != null ? clone.tip : destinationParent;
            foreach (FDX_SecondaryMotion.FlexibleEndPoint child in source.children)
                clone.children.Add(CloneMirrored(child, nextParent, axis));
            return clone;
        }

        private void RemoveEndPoint(List<FDX_SecondaryMotion.FlexibleEndPoint> siblings, int index,
            FDX_SecondaryMotion.FlexibleEndPoint point)
        {
            var motion = (FDX_SecondaryMotion)target;
            Undo.RecordObject(motion, "Remove FDX End Point");
            if (editingTip == point.tip) editingTip = null;
            if (point.tip != null) Undo.DestroyObjectImmediate(point.tip.gameObject);
            siblings.RemoveAt(index);
            EditorUtility.SetDirty(motion);
        }

        private static string SwapSideName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Mirrored";
            string[,] pairs = { { ".L", ".R" }, { "_L", "_R" }, { "Left", "Right" }, { "left", "right" } };
            for (int i = 0; i < pairs.GetLength(0); i++)
            {
                string left = pairs[i, 0];
                string right = pairs[i, 1];
                if (name.Contains(left)) return name.Replace(left, right);
                if (name.Contains(right)) return name.Replace(right, left);
            }
            return name + "_Mirrored";
        }

        private void FindAndAddOppositeBone(int chainIndex)
        {
            var motion = (FDX_SecondaryMotion)target;
            if (chainIndex < 0 || chainIndex >= motion.BoneChains.Count) return;
            FDX_SecondaryMotion.BoneChain source = motion.BoneChains[chainIndex];
            if (source == null || source.root == null) return;
            string oppositeName = SwapSideName(source.root.name);
            if (oppositeName.EndsWith("_Mirrored", StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("FDX", "無法從名稱判斷左右側。", "確定");
                return;
            }

            Transform searchRoot = motion.SimulationAnchor != null ? motion.SimulationAnchor : motion.transform.root;
            Transform opposite = FindUniqueTransform(searchRoot, oppositeName);
            if (opposite == null)
            {
                EditorUtility.DisplayDialog("FDX", $"找不到唯一的對側骨架：{oppositeName}", "確定");
                return;
            }
            foreach (FDX_SecondaryMotion.BoneChain existing in motion.BoneChains)
                if (existing != null && existing.root == opposite) return;

            Undo.RecordObject(motion, "Add Opposite FDX Bone Chain");
            var clone = new FDX_SecondaryMotion.BoneChain
            {
                displayName = SwapSideName(source.displayName),
                enabled = source.enabled,
                root = opposite,
                includeChildBones = source.includeChildBones,
                includeAllBranches = source.includeAllBranches,
                includeEndBone = source.includeEndBone,
                influence = source.influence,
                displayRadius = source.displayRadius
            };
            clone.axisSettings.CopyFrom(source.axisSettings);
            motion.BoneChains.Add(clone);
            EditorUtility.SetDirty(motion);
        }

        private static Transform FindUniqueTransform(Transform root, string name)
        {
            if (root == null) return null;
            Transform found = null;
            var stack = new Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                Transform current = stack.Pop();
                if (string.Equals(current.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    if (found != null) return null;
                    found = current;
                }
                for (int i = 0; i < current.childCount; i++) stack.Push(current.GetChild(i));
            }
            return found;
        }

        private void DrawBoneCandidateScanner()
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField(new GUIContent("骨架候選", "Bone Candidates"), EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("掃描骨架候選", "Scan Bone Candidates"))) ScanBoneCandidates();
            if (boneCandidates.Count == 0)
            {
                EditorGUILayout.HelpBox("尚未掃描到可能的骨架鏈起點。掃描只會提案，不會自動修改設定。", MessageType.None);
                return;
            }

            foreach (Transform candidate in boneCandidates)
            {
                if (candidate == null) continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(candidate, typeof(Transform), true);
                if (GUILayout.Button(new GUIContent("加入", "Add"), GUILayout.Width(90f)))
                {
                    AddBoneChain(candidate);
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void ScanBoneCandidates()
        {
            boneCandidates.Clear();
            var motion = (FDX_SecondaryMotion)target;
            Transform root = motion.SimulationAnchor != null ? motion.SimulationAnchor : motion.transform;
            if (root == null) return;
            string[] keywords = { "skirt", "hair", "tail", "cloth", "accessory", "cape", "ribbon", "ear", "breast" };
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate == root || candidate.childCount == 0 || IsConfiguredRoot(motion, candidate)) continue;
                string lower = candidate.name.ToLowerInvariant();
                bool keywordMatch = false;
                foreach (string keyword in keywords)
                {
                    if (!lower.Contains(keyword)) continue;
                    keywordMatch = true;
                    break;
                }
                if (!keywordMatch) continue;

                // Keep only the highest matching node in a matching branch.
                Transform parent = candidate.parent;
                bool matchingParent = false;
                while (parent != null && parent != root.parent)
                {
                    string parentName = parent.name.ToLowerInvariant();
                    foreach (string keyword in keywords)
                    {
                        if (!parentName.Contains(keyword)) continue;
                        matchingParent = true;
                        break;
                    }
                    if (matchingParent || parent == root) break;
                    parent = parent.parent;
                }
                if (!matchingParent) boneCandidates.Add(candidate);
            }
        }

        private static bool IsConfiguredRoot(FDX_SecondaryMotion motion, Transform candidate)
        {
            foreach (FDX_SecondaryMotion.BoneChain chain in motion.BoneChains)
                if (chain != null && chain.root == candidate) return true;
            return false;
        }

        private void AddBoneChain(Transform root)
        {
            serializedObject.ApplyModifiedProperties();
            var motion = (FDX_SecondaryMotion)target;
            Undo.RecordObject(motion, "Add FDX Bone Chain");
            motion.BoneChains.Add(new FDX_SecondaryMotion.BoneChain
            {
                displayName = root != null ? root.name : $"Bone Chain {motion.BoneChains.Count + 1}",
                root = root
            });
            if (root != null) boneCandidates.Remove(root);
            motion.RebuildSimulation();
            EditorUtility.SetDirty(motion);
            serializedObject.Update();
        }

        private static void DrawObjectList(SerializedProperty list, string label, Type objectType)
        {
            list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, $"{label}  {list.arraySize}", true);
            if (!list.isExpanded) return;
            EditorGUI.indentLevel++;
            for (int i = 0; i < list.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), new GUIContent($"項目 {i + 1}"));
                if (GUILayout.Button("−", GUILayout.Width(24f))) { list.DeleteArrayElementAtIndex(i); break; }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("＋"))
            {
                int index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                list.GetArrayElementAtIndex(index).objectReferenceValue = null;
            }
            EditorGUI.indentLevel--;
        }

        private static void ShowValidation(FDX_SecondaryMotion motion)
        {
            List<string> issues = motion.ValidateSetup();
            EditorUtility.DisplayDialog("FDX 設定檢查",
                issues.Count == 0 ? "設定有效。" : string.Join("\n", issues), "確定");
        }

        private void OpenWeightEditor()
        {
            Selection.activeObject = target;
            EditorWindow.GetWindow<FDX_RigWeightEditor>("FDX Rig & Weight").Show();
        }
    }
}
