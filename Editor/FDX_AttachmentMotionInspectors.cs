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

            DrawDistanceSettings(property);
        }

        private static void DrawDistanceSettings(SerializedProperty property)
        {
            EditorGUILayout.Space(5f);
            SerializedProperty enabled = property.FindPropertyRelative("enableDistanceSimulation");
            EditorGUILayout.PropertyField(enabled, C("距離模擬設定", "Distance Simulation Settings"));
            if (!enabled.boolValue) return;
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(property.FindPropertyRelative("distanceReference"),
                C("距離參考物件", "Distance Reference; empty uses Main Camera"));

            SerializedProperty mode = property.FindPropertyRelative("distanceUpdateMode");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(C("更新方式", "Update Mode"));
            DrawExclusiveEnum(mode, (int)FDX_DistanceUpdateMode.FixedFrameInterval, "固定幀間隔", "Fixed Frame Interval");
            DrawExclusiveEnum(mode, (int)FDX_DistanceUpdateMode.FixedUpdateFrequency, "固定更新頻率", "Fixed Update Frequency");
            EditorGUILayout.EndHorizontal();

            SerializedProperty quality = property.FindPropertyRelative("distanceQuality");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(C("畫質設定", "Quality Preset"));
            DrawExclusiveEnum(quality, (int)FDX_DistanceQuality.Low, "低", "Low");
            DrawExclusiveEnum(quality, (int)FDX_DistanceQuality.Medium, "中", "Medium");
            DrawExclusiveEnum(quality, (int)FDX_DistanceQuality.High, "高", "High");
            DrawExclusiveEnum(quality, (int)FDX_DistanceQuality.Custom, "自訂", "Custom" );
            EditorGUILayout.EndHorizontal();

            SerializedProperty first = property.FindPropertyRelative("fullSimulationDistance");
            SerializedProperty second = property.FindPropertyRelative("reducedSimulationDistance");
            SerializedProperty third = property.FindPropertyRelative("minimalSimulationDistance");
            first.floatValue = Mathf.Max(0f, first.floatValue);
            second.floatValue = Mathf.Max(first.floatValue, second.floatValue);
            third.floatValue = Mathf.Max(second.floatValue, third.floatValue);

            Vector3Int frameValues = ResolveFrameValues(quality.enumValueIndex,
                property.FindPropertyRelative("customFrameIntervals").vector3IntValue);
            Vector3 frequencyValues = ResolveFrequencyValues(quality.enumValueIndex,
                property.FindPropertyRelative("customUpdateFrequencies").vector3Value);
            DrawDistanceBar(mode.enumValueIndex, frameValues, frequencyValues,
                first.floatValue, second.floatValue, third.floatValue);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(C("距離分界（公尺）", "Distance Thresholds In Meters"));
            first.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(first.floatValue));
            second.floatValue = Mathf.Max(first.floatValue, EditorGUILayout.FloatField(second.floatValue));
            third.floatValue = Mathf.Max(second.floatValue, EditorGUILayout.FloatField(third.floatValue));
            EditorGUILayout.EndHorizontal();

            if (quality.enumValueIndex == (int)FDX_DistanceQuality.Custom)
            {
                if (mode.enumValueIndex == (int)FDX_DistanceUpdateMode.FixedFrameInterval)
                {
                    SerializedProperty custom = property.FindPropertyRelative("customFrameIntervals");
                    Vector3Int value = custom.vector3IntValue;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(C("自訂幀間隔", "Custom Frame Intervals"));
                    value.x = Mathf.Max(1, EditorGUILayout.IntField(value.x));
                    value.y = Mathf.Max(1, EditorGUILayout.IntField(value.y));
                    value.z = Mathf.Max(1, EditorGUILayout.IntField(value.z));
                    EditorGUILayout.EndHorizontal();
                    custom.vector3IntValue = value;
                }
                else
                {
                    SerializedProperty custom = property.FindPropertyRelative("customUpdateFrequencies");
                    Vector3 value = custom.vector3Value;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(C("每秒更新次數", "Updates Per Second"));
                    value.x = Mathf.Max(1f, EditorGUILayout.FloatField(value.x));
                    value.y = Mathf.Max(1f, EditorGUILayout.FloatField(value.y));
                    value.z = Mathf.Max(1f, EditorGUILayout.FloatField(value.z));
                    EditorGUILayout.EndHorizontal();
                    custom.vector3Value = value;
                }
                if (GUILayout.Button(C("儲存自訂預設", "Save Custom Preset"))) GUI.FocusControl(null);
            }
            EditorGUI.indentLevel--;
        }

        private static void DrawExclusiveEnum(SerializedProperty property, int value, string label, string tooltip)
        {
            bool selected = property.enumValueIndex == value;
            bool next = EditorGUILayout.ToggleLeft(new GUIContent(label, tooltip), selected, GUILayout.MinWidth(45f));
            if (next && !selected) property.enumValueIndex = value;
        }

        private static Vector3Int ResolveFrameValues(int quality, Vector3Int custom)
        {
            if (quality == (int)FDX_DistanceQuality.Low) return new Vector3Int(2, 4, 8);
            if (quality == (int)FDX_DistanceQuality.Medium) return new Vector3Int(1, 3, 6);
            if (quality == (int)FDX_DistanceQuality.High) return new Vector3Int(1, 2, 4);
            return new Vector3Int(Mathf.Max(1, custom.x), Mathf.Max(1, custom.y), Mathf.Max(1, custom.z));
        }

        private static Vector3 ResolveFrequencyValues(int quality, Vector3 custom)
        {
            if (quality == (int)FDX_DistanceQuality.Low) return new Vector3(20f, 10f, 5f);
            if (quality == (int)FDX_DistanceQuality.Medium) return new Vector3(30f, 20f, 10f);
            if (quality == (int)FDX_DistanceQuality.High) return new Vector3(60f, 30f, 15f);
            return new Vector3(Mathf.Max(1f, custom.x), Mathf.Max(1f, custom.y), Mathf.Max(1f, custom.z));
        }

        private static void DrawDistanceBar(int mode, Vector3Int frames, Vector3 frequencies,
            float first, float second, float third)
        {
            Rect rect = GUILayoutUtility.GetRect(10f, 58f, GUILayout.ExpandWidth(true));
            Color[] colors =
            {
                new Color(0.24f, 0.32f, 0.10f), new Color(0.16f, 0.21f, 0.28f),
                new Color(0.12f, 0.25f, 0.29f), new Color(0.38f, 0.02f, 0.02f)
            };
            string[] labels = mode == (int)FDX_DistanceUpdateMode.FixedFrameInterval
                ? new[] { $"每 {frames.x} 幀模擬", $"每 {frames.y} 幀模擬", $"每 {frames.z} 幀模擬", "停止模擬" }
                : new[] { $"每秒 {frequencies.x:0} 次", $"每秒 {frequencies.y:0} 次", $"每秒 {frequencies.z:0} 次", "停止模擬" };
            string[] distances = { $"0～{first:0.#} 公尺", $"{first:0.#}～{second:0.#} 公尺",
                $"{second:0.#}～{third:0.#} 公尺", $"{third:0.#} 公尺以上" };
            for (int i = 0; i < 4; i++)
            {
                Rect cell = new Rect(rect.x + rect.width * i / 4f, rect.y, rect.width / 4f - 1f, rect.height);
                EditorGUI.DrawRect(cell, colors[i]);
                GUI.Label(new Rect(cell.x + 6f, cell.y + 5f, cell.width - 10f, 20f), labels[i], EditorStyles.whiteLabel);
                GUI.Label(new Rect(cell.x + 6f, cell.y + 29f, cell.width - 10f, 20f), distances[i], EditorStyles.whiteMiniLabel);
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
        private SerializedProperty attachmentsExpanded;
        private SerializedProperty detectedMotionsExpanded;

        private void OnEnable()
        {
            animator = serializedObject.FindProperty("animator");
            attachments = serializedObject.FindProperty("attachments");
            settingsMode = serializedObject.FindProperty("settingsMode");
            sharedMotionSettings = serializedObject.FindProperty("sharedMotionSettings");
            applySharedSettingsOnAttach = serializedObject.FindProperty("applySharedSettingsOnAttach");
            previewInEditMode = serializedObject.FindProperty("previewInEditMode");
            attachmentsExpanded = serializedObject.FindProperty("attachmentsExpanded");
            detectedMotionsExpanded = serializedObject.FindProperty("detectedMotionsExpanded");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            settingsMode.enumValueIndex = applySharedSettingsOnAttach.boolValue
                ? (int)FDX_AttachmentManager.MotionSettingsMode.Unified
                : (int)FDX_AttachmentManager.MotionSettingsMode.PerAttachment;
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

            serializedObject.ApplyModifiedProperties();
            DrawDetectedMotions();
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("動態設定同步", "Motion Settings Sync"), EditorStyles.boldLabel);
            bool syncEnabled = applySharedSettingsOnAttach.boolValue;
            EditorGUI.BeginChangeCheck();
            syncEnabled = EditorGUILayout.Toggle(syncEnabled, GUILayout.Width(20f));
            if (EditorGUI.EndChangeCheck())
            {
                applySharedSettingsOnAttach.boolValue = syncEnabled;
                settingsMode.enumValueIndex = syncEnabled
                    ? (int)FDX_AttachmentManager.MotionSettingsMode.Unified
                    : (int)FDX_AttachmentManager.MotionSettingsMode.PerAttachment;
            }
            EditorGUILayout.EndHorizontal();
            if (syncEnabled)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(new GUIContent("統一動態設定", "Shared Motion Settings"), EditorStyles.boldLabel);
                FDX_InspectorGUI.DrawMotionSettings(sharedMotionSettings);
                EditorGUILayout.EndVertical();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAttachments()
        {
            EditorGUILayout.Space(8f);
            attachmentsExpanded.boolValue = EditorGUILayout.Foldout(attachmentsExpanded.boolValue,
                new GUIContent($"骨架掛物件設定（{attachments.arraySize}）", "Bone Attachment Settings"), true);
            if (!attachmentsExpanded.boolValue) return;
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
            List<FDX_SecondaryMotion> motions = manager.FindAllMotionComponents();
            detectedMotionsExpanded.boolValue = EditorGUILayout.Foldout(detectedMotionsExpanded.boolValue,
                new GUIContent($"偵測到的動態元件（{motions.Count}）", "Detected Motion Components"), true);
            if (!detectedMotionsExpanded.boolValue) return;
            if (manager.SettingsMode == FDX_AttachmentManager.MotionSettingsMode.Unified &&
                GUILayout.Button(new GUIContent("將統一設定套用到全部物件", "Apply Shared Settings To All")))
            {
                List<FDX_SecondaryMotion> all = manager.FindAllMotionComponents();
                if (all.Count > 0) Undo.RecordObjects(all.ToArray(), "Apply FDX Motion Settings");
                manager.ApplySharedSettingsToAll();
            }

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
                bool syncEnabled = manager.SharedSettingsEnabled;
                bool individual = manager.UsesIndividualSettings(motion);
                if (syncEnabled)
                {
                    EditorGUI.BeginChangeCheck();
                    individual = EditorGUILayout.Toggle(new GUIContent("使用個別設定", "Individual Settings"), individual);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(manager, "Change FDX Individual Settings");
                        manager.SetIndividualSettings(motion, individual);
                        EditorUtility.SetDirty(manager);
                    }
                }
                if (syncEnabled && !individual)
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
            EnsureDefaultPivot();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
            editingTip = null;
        }

        private void EnsureDefaultPivot()
        {
            if (Application.isPlaying) return;
            var motion = (FDX_SecondaryMotion)target;
            if (motion == null || motion.Source != FDX_SecondaryMotion.MotionSource.AutomaticPivot) return;
            if (motion.PivotGroups.Count == 0)
            {
                Undo.RecordObject(motion, "Create Default FDX Pivot Group");
                motion.PivotGroups.Add(new FDX_SecondaryMotion.PivotGroup
                {
                    displayName = motion.name + " 軸心"
                });
            }
            FDX_SecondaryMotion.PivotGroup group = motion.PivotGroups[0];
            if (group == null || group.pivot != null) return;

            var pivotObject = new GameObject($"{motion.name}_FDX_Pivot_1");
            Undo.RegisterCreatedObjectUndo(pivotObject, "Create Default FDX Pivot");
            Undo.SetTransformParent(pivotObject.transform, motion.transform, "Parent Default FDX Pivot");
            pivotObject.transform.localPosition = Vector3.zero;
            pivotObject.transform.localRotation = Quaternion.identity;
            pivotObject.transform.localScale = Vector3.one;
            Undo.RecordObject(motion, "Assign Default FDX Pivot");
            group.pivot = pivotObject.transform;
            motion.RotationPivot = pivotObject.transform;
            EditorUtility.SetDirty(motion);
            serializedObject.UpdateIfRequiredOrScript();
        }

        private void ResetEverything()
        {
            if (!EditorUtility.DisplayDialog("全部重設", "要將此動態元件恢復成剛掛上腳本的狀態嗎？\n工具建立的軸心與控制點會移除，使用者骨架和模型不會被刪除。", "重設", "取消"))
                return;

            var motion = (FDX_SecondaryMotion)target;
            var generatedObjects = new HashSet<GameObject>();
            foreach (FDX_SecondaryMotion.PivotGroup group in motion.PivotGroups)
            {
                if (group == null) continue;
                AddToolOwnedObject(motion, group.pivot, generatedObjects);
                foreach (FDX_SecondaryMotion.FlexibleEndPoint point in group.endPoints)
                    CollectToolOwnedEndPoints(motion, point, generatedObjects);
            }

            Undo.RecordObject(motion, "Reset FDX Secondary Motion");
            motion.ResetToDefaults();
            foreach (GameObject go in RemoveNestedObjects(generatedObjects))
                if (go != null) Undo.DestroyObjectImmediate(go);
            EditorUtility.SetDirty(motion);
            serializedObject.Update();
            EnsureDefaultPivot();
            GUIUtility.ExitGUI();
        }

        private static void CollectToolOwnedEndPoints(FDX_SecondaryMotion motion,
            FDX_SecondaryMotion.FlexibleEndPoint point, HashSet<GameObject> result)
        {
            if (point == null) return;
            AddToolOwnedObject(motion, point.tip, result);
            if (point.generatedBones != null)
                foreach (Transform bone in point.generatedBones) AddToolOwnedObject(motion, bone, result);
            if (point.children != null)
                foreach (FDX_SecondaryMotion.FlexibleEndPoint child in point.children)
                    CollectToolOwnedEndPoints(motion, child, result);
        }

        private static void AddToolOwnedObject(FDX_SecondaryMotion motion, Transform item, HashSet<GameObject> result)
        {
            if (item == null || item == motion.transform || !item.IsChildOf(motion.transform)) return;
            string objectName = item.name ?? string.Empty;
            if (objectName.StartsWith("FDX_", StringComparison.OrdinalIgnoreCase) ||
                objectName.IndexOf("_FDX_Pivot", StringComparison.OrdinalIgnoreCase) >= 0)
                result.Add(item.gameObject);
        }

        private static List<GameObject> RemoveNestedObjects(HashSet<GameObject> objects)
        {
            var roots = new List<GameObject>();
            foreach (GameObject candidate in objects)
            {
                if (candidate == null) continue;
                bool nested = false;
                Transform parent = candidate.transform.parent;
                while (parent != null)
                {
                    if (objects.Contains(parent.gameObject)) { nested = true; break; }
                    parent = parent.parent;
                }
                if (!nested) roots.Add(candidate);
            }
            return roots;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (GUILayout.Button(new GUIContent("全部重設", "Reset this component to its initial state"))) ResetEverything();
            EditorGUILayout.PropertyField(simulate, new GUIContent("動態模擬", "Dynamic Simulation"));
            DrawExclusiveOptions(forceSpace, "力場座標",
                new[] { "世界座標", "動作參考物件座標" },
                new[]
                {
                    "World Space：角色怎麼轉，重力仍然永遠向下，風也維持原本方向。",
                    "Reference Local Space：角色轉身或傾斜時，重力和風也會跟著角色一起轉。"
                });
            DrawExclusiveOptions(motionSource, "運作來源",
                new[] { "自動旋轉軸心", "現有骨架鏈" },
                new[]
                {
                    "Automatic Pivot：適合沒有可用骨架的耳環、飾品或需要自動建立彎曲骨架的網格。",
                    "Existing Bone Chains：適合模型已經具有裙擺、頭髮或尾巴骨架的情況。"
                });

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

        private static void DrawExclusiveOptions(SerializedProperty property, string label,
            string[] labels, string[] tooltips)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            for (int i = 0; i < labels.Length; i++)
            {
                bool selected = property.enumValueIndex == i;
                bool next = EditorGUILayout.ToggleLeft(new GUIContent(labels[i], tooltips[i]), selected);
                if (next && !selected) property.enumValueIndex = i;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBoneChains()
        {
            EditorGUILayout.Space(6f);
            var motion = (FDX_SecondaryMotion)target;
            if (motion.SimulationAnchor == null && motion.BoneChains.Exists(chain => chain != null && chain.root != null))
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(motion, "Assign FDX Action Reference");
                motion.RefreshAutomaticSimulationAnchor(false);
                EditorUtility.SetDirty(motion);
                serializedObject.Update();
            }
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(simulationAnchor, new GUIContent("動作參考物件", "Simulation Anchor"));
            if (EditorGUI.EndChangeCheck()) motion.SimulationAnchorAutomaticallyAssigned = false;

            EditorGUI.BeginChangeCheck();
            bool candidatesExpanded = EditorGUILayout.Foldout(motion.BoneCandidatesExpanded,
                new GUIContent($"骨架候選（{boneCandidates.Count}）", "Bone Candidates"), true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(motion, "Toggle Bone Candidates");
                motion.BoneCandidatesExpanded = candidatesExpanded;
            }
            if (candidatesExpanded) DrawBoneCandidateScannerContent();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            bool chainsExpanded = EditorGUILayout.Foldout(motion.BoneChainsExpanded,
                new GUIContent($"骨架鏈（{motion.BoneChains.Count}）", "Bone Chains"), true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(motion, "Toggle Bone Chains");
                motion.BoneChainsExpanded = chainsExpanded;
            }
            using (new EditorGUI.DisabledScope(!CanClassify(null)))
                if (GUILayout.Button("自動分類", GUILayout.Width(72f))) { ClassifyChains(null); GUIUtility.ExitGUI(); }
            using (new EditorGUI.DisabledScope(!HasDirectGroups(null)))
                if (GUILayout.Button("重設", GUILayout.Width(48f))) { ResetGroupLevel(null); GUIUtility.ExitGUI(); }
            using (new EditorGUI.DisabledScope(motion.BoneChainGroups.Count == 0))
                if (GUILayout.Button("取消全部分組", GUILayout.Width(96f))) { CancelAllGroups(); GUIUtility.ExitGUI(); }
            EditorGUILayout.EndHorizontal();

            if (!chainsExpanded) return;
            DrawGroupChildren(null, 0);
            DrawUngroupedChains(null);
            if (GUILayout.Button(new GUIContent("新增骨架鏈組", "Add Bone Chain"))) { AddBoneChain(null); GUIUtility.ExitGUI(); }

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

        private void DrawChain(int index)
        {
            if (index < 0 || index >= boneChains.arraySize) return;
            SerializedProperty chain = boneChains.GetArrayElementAtIndex(index);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(chain.FindPropertyRelative("enabled"), GUIContent.none, GUILayout.Width(18f));
            EditorGUILayout.PropertyField(chain.FindPropertyRelative("displayName"), GUIContent.none);
            if (GUILayout.Button(new GUIContent("刪除", "Remove"), GUILayout.Width(50f)))
            {
                string id = chain.FindPropertyRelative("editorId").stringValue;
                RemoveChainFromGroups(id);
                boneChains.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(chain.FindPropertyRelative("root"), new GUIContent("骨架鏈起點", "Chain Root"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                var motion = (FDX_SecondaryMotion)target;
                motion.RefreshAutomaticSimulationAnchor(false);
                serializedObject.Update();
            }
            EditorGUILayout.PropertyField(chain.FindPropertyRelative("solo"), new GUIContent("單獨預覽", "Solo"));
            if (chain.FindPropertyRelative("root").objectReferenceValue != null &&
                GUILayout.Button(new GUIContent("尋找並加入對側骨架", "Find Opposite Bone")))
            {
                FindAndAddOppositeBone(index);
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

        private void DrawGroupChildren(string parentId, int depth)
        {
            var motion = (FDX_SecondaryMotion)target;
            foreach (FDX_SecondaryMotion.BoneChainGroup group in motion.BoneChainGroups.ToArray())
            {
                if (group == null || group.parentId != parentId) continue;
                EditorGUI.indentLevel = depth;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                bool expanded = EditorGUILayout.Foldout(group.expanded,
                    $"{group.displayName}（{CountChainsRecursive(group.id)}）", true);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(motion, "Toggle Bone Chain Group");
                    group.expanded = expanded;
                    EditorUtility.SetDirty(motion);
                }
                using (new EditorGUI.DisabledScope(!CanClassify(group.id)))
                    if (GUILayout.Button("自動分類", GUILayout.Width(72f))) { ClassifyChains(group.id); GUIUtility.ExitGUI(); }
                if (GUILayout.Button("重設", GUILayout.Width(48f))) { ResetGroup(group.id); GUIUtility.ExitGUI(); }
                if (GUILayout.Button("整組加入對側", GUILayout.Width(92f))) { AddOppositeForGroup(group.id); GUIUtility.ExitGUI(); }
                EditorGUILayout.EndHorizontal();
                if (expanded)
                {
                    group.commonSettingsExpanded = EditorGUILayout.Foldout(group.commonSettingsExpanded, "共通設定", true);
                    if (group.commonSettingsExpanded) DrawGroupCommonSettings(GetChainIndicesRecursive(group.id));
                    DrawGroupChildren(group.id, depth + 1);
                    foreach (string chainId in group.chainIds.ToArray())
                    {
                        int index = FindChainIndex(chainId);
                        if (index >= 0) DrawChain(index);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUI.indentLevel = 0;
        }

        private void DrawUngroupedChains(string parentId)
        {
            var motion = (FDX_SecondaryMotion)target;
            var assigned = new HashSet<string>();
            foreach (FDX_SecondaryMotion.BoneChainGroup group in motion.BoneChainGroups)
                if (group != null && group.chainIds != null)
                    foreach (string id in group.chainIds) assigned.Add(id);
            for (int i = 0; i < motion.BoneChains.Count; i++)
            {
                FDX_SecondaryMotion.BoneChain chain = motion.BoneChains[i];
                if (chain != null && !assigned.Contains(chain.editorId)) DrawChain(i);
            }
        }

        private void DrawGroupCommonSettings(List<int> indices)
        {
            if (indices.Count == 0) return;
            var motion = (FDX_SecondaryMotion)target;
            FDX_SecondaryMotion.BoneChain first = motion.BoneChains[indices[0]];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawCommonBool("自動包含子骨頭", indices, chain => chain.includeChildBones,
                (chain, value) => chain.includeChildBones = value);
            DrawCommonBool("包含全部分支", indices, chain => chain.includeAllBranches,
                (chain, value) => chain.includeAllBranches = value);
            DrawCommonBool("模擬結束骨頭", indices, chain => chain.includeEndBone,
                (chain, value) => chain.includeEndBone = value);
            DrawCommonFloat("影響倍率", indices, chain => chain.influence,
                (chain, value) => chain.influence = value, 0f, 2f);
            DrawCommonFloat("顯示範圍", indices, chain => chain.displayRadius,
                (chain, value) => chain.displayRadius = value, 0.001f, 1f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("啟用軸向");
            DrawCommonAxis(indices, 0, "X");
            DrawCommonAxis(indices, 1, "Y");
            DrawCommonAxis(indices, 2, "Z");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawCommonBool(string label, List<int> indices,
            Func<FDX_SecondaryMotion.BoneChain, bool> getter,
            Action<FDX_SecondaryMotion.BoneChain, bool> setter)
        {
            var motion = (FDX_SecondaryMotion)target;
            bool value = getter(motion.BoneChains[indices[0]]);
            bool mixed = indices.Exists(i => getter(motion.BoneChains[i]) != value);
            EditorGUI.showMixedValue = mixed;
            EditorGUI.BeginChangeCheck();
            bool next = EditorGUILayout.Toggle(label, value);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(motion, "Edit Common Bone Chain Settings");
                foreach (int index in indices) setter(motion.BoneChains[index], next);
                EditorUtility.SetDirty(motion);
            }
            EditorGUI.showMixedValue = false;
        }

        private void DrawCommonFloat(string label, List<int> indices,
            Func<FDX_SecondaryMotion.BoneChain, float> getter,
            Action<FDX_SecondaryMotion.BoneChain, float> setter, float min, float max)
        {
            var motion = (FDX_SecondaryMotion)target;
            float value = getter(motion.BoneChains[indices[0]]);
            bool mixed = indices.Exists(i => !Mathf.Approximately(getter(motion.BoneChains[i]), value));
            EditorGUI.showMixedValue = mixed;
            EditorGUI.BeginChangeCheck();
            float next = EditorGUILayout.Slider(label, value, min, max);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(motion, "Edit Common Bone Chain Settings");
                foreach (int index in indices) setter(motion.BoneChains[index], next);
                EditorUtility.SetDirty(motion);
            }
            EditorGUI.showMixedValue = false;
        }

        private void DrawCommonAxis(List<int> indices, int axis, string label)
        {
            var motion = (FDX_SecondaryMotion)target;
            Func<FDX_SecondaryMotion.BoneChain, bool> locked = chain => axis == 0
                ? chain.axisSettings.lockX : axis == 1 ? chain.axisSettings.lockY : chain.axisSettings.lockZ;
            bool enabled = !locked(motion.BoneChains[indices[0]]);
            bool mixed = indices.Exists(i => !locked(motion.BoneChains[i]) != enabled);
            EditorGUI.showMixedValue = mixed;
            EditorGUI.BeginChangeCheck();
            bool next = EditorGUILayout.ToggleLeft(label, enabled, GUILayout.Width(36f));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(motion, "Edit Common Bone Chain Axis");
                foreach (int index in indices)
                {
                    FDX_AxisControlSettings settings = motion.BoneChains[index].axisSettings;
                    if (axis == 0) settings.lockX = !next;
                    else if (axis == 1) settings.lockY = !next;
                    else settings.lockZ = !next;
                }
                EditorUtility.SetDirty(motion);
            }
            EditorGUI.showMixedValue = false;
        }

        private bool CanClassify(string groupId)
        {
            List<string> ids = groupId == null ? GetUngroupedChainIds() : GetGroup(groupId)?.chainIds;
            if (ids == null || ids.Count < 2) return false;
            return BuildClassification(ids).Count > 1;
        }

        private Dictionary<string, List<string>> BuildClassification(List<string> ids)
        {
            var tokens = new List<string[]>();
            foreach (string id in ids)
            {
                int index = FindChainIndex(id);
                string name = index >= 0 ? GetChainName(((FDX_SecondaryMotion)target).BoneChains[index]) : string.Empty;
                tokens.Add(TokenizeBoneName(name));
            }
            int common = 0;
            bool keepChecking = true;
            while (keepChecking)
            {
                string token = tokens.Count > 0 && tokens[0].Length > common ? tokens[0][common] : null;
                if (string.IsNullOrEmpty(token)) break;
                foreach (string[] parts in tokens)
                    if (parts.Length <= common || !string.Equals(parts[common], token, StringComparison.OrdinalIgnoreCase))
                    { keepChecking = false; break; }
                if (keepChecking) common++;
            }
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < ids.Count; i++)
            {
                string key = tokens[i].Length > common ? string.Join("_", tokens[i], 0, common + 1)
                    : tokens[i].Length > 0 ? string.Join("_", tokens[i]) : "其他";
                if (!result.TryGetValue(key, out List<string> list)) result.Add(key, list = new List<string>());
                list.Add(ids[i]);
            }
            return result;
        }

        private static string[] TokenizeBoneName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return Array.Empty<string>();
            int colon = name.LastIndexOf(':');
            if (colon >= 0) name = name.Substring(colon + 1);
            string[] raw = name.Replace('.', '_').Replace('-', '_').Replace(' ', '_')
                .Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>();
            foreach (string token in raw)
            {
                if (int.TryParse(token, out _)) continue;
                if (token.Equals("L", StringComparison.OrdinalIgnoreCase) || token.Equals("R", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("Left", StringComparison.OrdinalIgnoreCase) || token.Equals("Right", StringComparison.OrdinalIgnoreCase)) continue;
                result.Add(token);
            }
            return result.ToArray();
        }

        private static string GetChainName(FDX_SecondaryMotion.BoneChain chain) => chain.root != null
            ? chain.root.name : chain.displayName;

        private void ClassifyChains(string parentId)
        {
            serializedObject.ApplyModifiedProperties();
            var motion = (FDX_SecondaryMotion)target;
            List<string> ids = parentId == null ? GetUngroupedChainIds() : new List<string>(GetGroup(parentId).chainIds);
            Dictionary<string, List<string>> groups = BuildClassification(ids);
            if (groups.Count < 2) return;
            Undo.RecordObject(motion, "Classify FDX Bone Chains");
            if (parentId != null) GetGroup(parentId).chainIds.Clear();
            foreach (KeyValuePair<string, List<string>> pair in groups)
                motion.BoneChainGroups.Add(new FDX_SecondaryMotion.BoneChainGroup
                {
                    id = Guid.NewGuid().ToString("N"), parentId = parentId, displayName = pair.Key,
                    chainIds = new List<string>(pair.Value)
                });
            EditorUtility.SetDirty(motion);
            serializedObject.Update();
        }

        private void ResetGroup(string groupId)
        {
            var group = GetGroup(groupId);
            if (group == null) return;
            var motion = (FDX_SecondaryMotion)target;
            Undo.RecordObject(motion, "Reset FDX Bone Chain Group");
            FDX_SecondaryMotion.BoneChainGroup parent = GetGroup(group.parentId);
            if (parent != null) parent.chainIds.AddRange(group.chainIds);
            foreach (FDX_SecondaryMotion.BoneChainGroup child in motion.BoneChainGroups)
                if (child != null && child.parentId == group.id) child.parentId = group.parentId;
            motion.BoneChainGroups.Remove(group);
            EditorUtility.SetDirty(motion);
        }

        private void ResetGroupLevel(string parentId)
        {
            var motion = (FDX_SecondaryMotion)target;
            List<FDX_SecondaryMotion.BoneChainGroup> direct = motion.BoneChainGroups.FindAll(g => g != null && g.parentId == parentId);
            Undo.RecordObject(motion, "Reset FDX Bone Chain Group Level");
            foreach (FDX_SecondaryMotion.BoneChainGroup group in direct) ResetGroupWithoutUndo(group);
            EditorUtility.SetDirty(motion);
        }

        private void ResetGroupWithoutUndo(FDX_SecondaryMotion.BoneChainGroup group)
        {
            var motion = (FDX_SecondaryMotion)target;
            FDX_SecondaryMotion.BoneChainGroup parent = GetGroup(group.parentId);
            if (parent != null) parent.chainIds.AddRange(group.chainIds);
            foreach (FDX_SecondaryMotion.BoneChainGroup child in motion.BoneChainGroups)
                if (child != null && child.parentId == group.id) child.parentId = group.parentId;
            motion.BoneChainGroups.Remove(group);
        }

        private void CancelAllGroups()
        {
            var motion = (FDX_SecondaryMotion)target;
            Undo.RecordObject(motion, "Cancel All FDX Bone Chain Groups");
            motion.BoneChainGroups.Clear();
            EditorUtility.SetDirty(motion);
        }

        private bool HasDirectGroups(string parentId) => ((FDX_SecondaryMotion)target).BoneChainGroups.Exists(
            group => group != null && group.parentId == parentId);

        private FDX_SecondaryMotion.BoneChainGroup GetGroup(string id) => string.IsNullOrEmpty(id) ? null :
            ((FDX_SecondaryMotion)target).BoneChainGroups.Find(group => group != null && group.id == id);

        private List<string> GetUngroupedChainIds()
        {
            var motion = (FDX_SecondaryMotion)target;
            var assigned = new HashSet<string>();
            foreach (FDX_SecondaryMotion.BoneChainGroup group in motion.BoneChainGroups)
                if (group != null && group.chainIds != null)
                    foreach (string id in group.chainIds) assigned.Add(id);
            var result = new List<string>();
            foreach (FDX_SecondaryMotion.BoneChain chain in motion.BoneChains)
                if (chain != null && !assigned.Contains(chain.editorId)) result.Add(chain.editorId);
            return result;
        }

        private int FindChainIndex(string id) => ((FDX_SecondaryMotion)target).BoneChains.FindIndex(
            chain => chain != null && chain.editorId == id);

        private int CountChainsRecursive(string groupId) => GetChainIndicesRecursive(groupId).Count;

        private List<int> GetChainIndicesRecursive(string groupId)
        {
            var result = new List<int>();
            FDX_SecondaryMotion.BoneChainGroup group = GetGroup(groupId);
            if (group == null) return result;
            foreach (string id in group.chainIds)
            {
                int index = FindChainIndex(id);
                if (index >= 0 && !result.Contains(index)) result.Add(index);
            }
            foreach (FDX_SecondaryMotion.BoneChainGroup child in ((FDX_SecondaryMotion)target).BoneChainGroups)
                if (child != null && child.parentId == groupId)
                    foreach (int index in GetChainIndicesRecursive(child.id)) if (!result.Contains(index)) result.Add(index);
            return result;
        }

        private void RemoveChainFromGroups(string id)
        {
            foreach (FDX_SecondaryMotion.BoneChainGroup group in ((FDX_SecondaryMotion)target).BoneChainGroups)
                if (group != null && group.chainIds != null) group.chainIds.Remove(id);
        }

        private void AddOppositeForGroup(string groupId)
        {
            FDX_SecondaryMotion.BoneChainGroup group = GetGroup(groupId);
            if (group == null) return;
            var motion = (FDX_SecondaryMotion)target;
            List<int> indices = GetChainIndicesRecursive(groupId);
            for (int i = indices.Count - 1; i >= 0; i--)
            {
                int previousCount = motion.BoneChains.Count;
                FindAndAddOppositeBone(indices[i]);
                if (motion.BoneChains.Count > previousCount)
                {
                    FDX_SecondaryMotion.BoneChain added = motion.BoneChains[motion.BoneChains.Count - 1];
                    if (!group.chainIds.Contains(added.editorId)) group.chainIds.Add(added.editorId);
                }
            }
            EditorUtility.SetDirty(motion);
        }

        private void DrawAutomaticPivot()
        {
            EditorGUILayout.Space(6f);
            var motion = (FDX_SecondaryMotion)target;
            EditorGUI.BeginChangeCheck();
            bool expanded = EditorGUILayout.Foldout(motion.PivotGroupsExpanded,
                new GUIContent($"旋轉軸心組（{motion.PivotGroups.Count}）", "Rotation Pivot Groups"), true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(motion, "Toggle FDX Pivot Groups");
                motion.PivotGroupsExpanded = expanded;
                EditorUtility.SetDirty(motion);
            }
            if (!expanded) return;
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
            EditorGUILayout.PropertyField(automaticAnchor, new GUIContent("自動取得動作參考物件", "Automatic Action Reference"));
            if (automaticAnchor.boolValue)
            {
                Transform resolved = group.pivot != null && group.pivot.parent != null ? group.pivot.parent : group.pivot;
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField(new GUIContent("目前動作參考物件", "Resolved Action Reference"), resolved, typeof(Transform), true);
            }
            else EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("simulationAnchor"), new GUIContent("動作參考物件", "Action Reference"));
            EditorGUILayout.Slider(groupProperty.FindPropertyRelative("influenceRadius"), 0.001f, 2f,
                new GUIContent("軸心影響範圍", "Pivot Influence Radius"));
            FDX_InspectorGUI.DrawAxisSettings(groupProperty.FindPropertyRelative("axisSettings"));

            SerializedProperty liveMirror = groupProperty.FindPropertyRelative("liveMirror");
            EditorGUILayout.PropertyField(liveMirror, new GUIContent("建立對稱軸心", "Create Symmetric Pivot"));
            if (liveMirror.boolValue)
            {
                string[] axes = { "X 軸", "Y 軸", "Z 軸" };
                SerializedProperty mirrorAxis = groupProperty.FindPropertyRelative("mirrorAxis");
                mirrorAxis.enumValueIndex = EditorGUILayout.Popup(new GUIContent("對稱軸向", "Mirror Axis"), mirrorAxis.enumValueIndex, axes);
                EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("mirrorCenter"),
                    new GUIContent("對稱中心", "Mirror Center; empty uses this component Transform"));
                if (group.pivot != null && GUILayout.Button(new GUIContent("複製對稱軸心", "Duplicate Symmetric Pivot")))
                {
                    DuplicateMirroredPivot(index);
                    GUIUtility.ExitGUI();
                }
            }
            if (group.pivot == null && GUILayout.Button(new GUIContent("建立旋轉軸心", "Create Rotation Pivot"))) CreatePivot(index);
            if (group.pivot != null)
            {
                EditorGUILayout.BeginHorizontal();
                string editLabel = editingTip == group.pivot ? "結束編輯" : "編輯軸心位置";
                if (GUILayout.Button(new GUIContent(editLabel, "Edit Pivot Position"), GUILayout.Width(120f)))
                    editingTip = editingTip == group.pivot ? null : group.pivot;
                EditorGUI.BeginChangeCheck();
                Vector3 localPosition = group.pivot.localPosition;
                GUILayout.Label("X", GUILayout.Width(14f));
                localPosition.x = EditorGUILayout.FloatField(localPosition.x);
                GUILayout.Label("Y", GUILayout.Width(14f));
                localPosition.y = EditorGUILayout.FloatField(localPosition.y);
                GUILayout.Label("Z", GUILayout.Width(14f));
                localPosition.z = EditorGUILayout.FloatField(localPosition.z);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(group.pivot, "Edit FDX Pivot Position");
                    group.pivot.localPosition = localPosition;
                    EditorUtility.SetDirty(group.pivot);
                }
                EditorGUILayout.EndHorizontal();
            }

            SerializedProperty advanced = groupProperty.FindPropertyRelative("enableAdvancedFlexible");
            EditorGUILayout.PropertyField(advanced, new GUIContent("啟用進階彎曲設定", "Advanced Bending Settings"));
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
                editorId = Guid.NewGuid().ToString("N"),
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

        private void DrawBoneCandidateScannerContent()
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("掃描骨架候選", "Scan Bone Candidates"))) ScanBoneCandidates();
            using (new EditorGUI.DisabledScope(boneCandidates.Count == 0))
            {
                if (GUILayout.Button(new GUIContent("全部加入", "Add All Candidates")))
                {
                    foreach (Transform candidate in boneCandidates.ToArray()) AddBoneChain(candidate);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
            if (boneCandidates.Count == 0)
                return;

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
                root = root,
                editorId = Guid.NewGuid().ToString("N")
            });
            if (root != null) boneCandidates.Remove(root);
            motion.RefreshAutomaticSimulationAnchor(false);
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
