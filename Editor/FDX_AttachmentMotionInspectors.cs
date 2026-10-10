using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Faidlix.UnityTools.Editor
{
    internal static class FDX_InspectorGUI
    {
        private static int activeDistanceHandle = -1;
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
            EditorGUILayout.PropertyField(property.FindPropertyRelative("useSceneViewCameraInEditMode"),
                C("編輯模式使用場景視角", "Use Scene view camera while previewing outside Play Mode"));
            SerializedProperty distanceReference = property.FindPropertyRelative("distanceReference");
            if (distanceReference.objectReferenceValue == null && Camera.main != null)
                distanceReference.objectReferenceValue = Camera.main.transform;
            EditorGUILayout.PropertyField(distanceReference,
                C("距離參考物件", "Distance Reference; automatically uses Main Camera"));

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
            DrawDistanceBar(mode.enumValueIndex, frameValues, frequencyValues, first, second, third);

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
            SerializedProperty first, SerializedProperty second, SerializedProperty third)
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
            float maximum = Mathf.Max(50f, Mathf.Ceil((third.floatValue + 1f) / 10f) * 10f);
            float[] values = { 0f, first.floatValue, second.floatValue, third.floatValue, maximum };
            string[] distances = { $"0～{first.floatValue:0.#} 公尺", $"{first.floatValue:0.#}～{second.floatValue:0.#} 公尺",
                $"{second.floatValue:0.#}～{third.floatValue:0.#} 公尺", $"{third.floatValue:0.#} 公尺以上" };
            for (int i = 0; i < 4; i++)
            {
                float xMin = rect.x + rect.width * values[i] / maximum;
                float xMax = i == 3 ? rect.xMax : rect.x + rect.width * values[i + 1] / maximum;
                Rect cell = new Rect(xMin, rect.y, Mathf.Max(1f, xMax - xMin - 1f), rect.height);
                EditorGUI.DrawRect(cell, colors[i]);
                GUI.Label(new Rect(cell.x + 6f, cell.y + 5f, cell.width - 10f, 20f), labels[i], EditorStyles.whiteLabel);
                GUI.Label(new Rect(cell.x + 6f, cell.y + 29f, cell.width - 10f, 20f), distances[i], EditorStyles.whiteMiniLabel);
            }

            int controlId = GUIUtility.GetControlID("FDXDistanceBar".GetHashCode(), FocusType.Passive, rect);
            Event current = Event.current;
            SerializedProperty[] handles = { first, second, third };
            for (int i = 0; i < handles.Length; i++)
            {
                float x = rect.x + rect.width * handles[i].floatValue / maximum;
                Rect handleRect = new Rect(x - 5f, rect.y, 10f, rect.height);
                EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);
                EditorGUI.DrawRect(new Rect(x - 1f, rect.y, 2f, rect.height), new Color(0.35f, 1f, 0.35f, 0.9f));
                if (current.type == EventType.MouseDown && current.button == 0 && handleRect.Contains(current.mousePosition))
                {
                    GUIUtility.hotControl = controlId;
                    activeDistanceHandle = i;
                    current.Use();
                }
            }
            if (GUIUtility.hotControl == controlId && current.type == EventType.MouseDrag && activeDistanceHandle >= 0)
            {
                float next = Mathf.Clamp01((current.mousePosition.x - rect.x) / rect.width) * maximum;
                float minimum = activeDistanceHandle == 0 ? 0f : handles[activeDistanceHandle - 1].floatValue;
                float upper = activeDistanceHandle == 2 ? maximum : handles[activeDistanceHandle + 1].floatValue;
                handles[activeDistanceHandle].floatValue = Mathf.Clamp(next, minimum, upper);
                GUI.changed = true;
                current.Use();
            }
            if (GUIUtility.hotControl == controlId && current.type == EventType.MouseUp)
            {
                GUIUtility.hotControl = 0;
                activeDistanceHandle = -1;
                current.Use();
            }
        }

        public static void DrawAxisSettings(SerializedProperty property, bool showIndividualDynamics = true)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(C("啟用軸向", "Enabled Axes"));
            DrawEnabledAxis(property.FindPropertyRelative("lockX"), "X");
            DrawEnabledAxis(property.FindPropertyRelative("lockY"), "Y");
            DrawEnabledAxis(property.FindPropertyRelative("lockZ"), "Z");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (!showIndividualDynamics) return;

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

        public static void DrawSoftMaxSlider(SerializedProperty property, float minimum, float sliderMaximum,
            GUIContent label)
        {
            Rect row = EditorGUILayout.GetControlRect();
            Rect content = EditorGUI.PrefixLabel(row, label);
            const float fieldWidth = 78f;
            Rect sliderRect = new Rect(content.x, content.y, Mathf.Max(20f, content.width - fieldWidth - 4f), content.height);
            Rect fieldRect = new Rect(sliderRect.xMax + 4f, content.y, fieldWidth, content.height);
            float original = property.floatValue;
            EditorGUI.BeginChangeCheck();
            float sliderValue = GUI.HorizontalSlider(sliderRect, Mathf.Clamp(original, minimum, sliderMaximum), minimum, sliderMaximum);
            if (EditorGUI.EndChangeCheck()) property.floatValue = sliderValue;
            EditorGUI.BeginChangeCheck();
            float typed = EditorGUI.FloatField(fieldRect, property.floatValue);
            if (EditorGUI.EndChangeCheck()) property.floatValue = Mathf.Max(minimum, typed);
        }

        public static bool DrawContainedFoldout(bool expanded, GUIContent label, float inset = 18f)
        {
            Rect row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            row.x += inset;
            row.width = Mathf.Max(0f, row.width - inset);
            return EditorGUI.Foldout(row, expanded, label, true);
        }
    }

    [CustomEditor(typeof(FDX_AttachmentManager))]
    internal sealed class FDX_AttachmentManagerBilingualInspector : UnityEditor.Editor
    {
        private readonly Dictionary<int, UnityEditor.Editor> motionEditors = new Dictionary<int, UnityEditor.Editor>();
        private SerializedProperty animator;
        private SerializedProperty additionalAnimators;
        private SerializedProperty attachments;
        private SerializedProperty settingsMode;
        private SerializedProperty sharedMotionSettings;
        private SerializedProperty applySharedSettingsOnAttach;
        private SerializedProperty sharedSimulate;
        private SerializedProperty sharedForceSpace;
        private SerializedProperty previewInEditMode;
        private SerializedProperty attachmentsExpanded;
        private SerializedProperty detectedMotionsExpanded;
        private SerializedProperty sharedSettingsExpanded;
        private SerializedProperty equipmentStatusTab;
        private SerializedProperty equipmentMotionTab;
        private SerializedProperty defaultUnequippedParent;
        private int editingAttachmentIndex = -1;
        private bool attachmentToolsHidden;
        private bool previousAttachmentToolsHidden;
        private bool equipmentCacheDirty = true;
        private List<EquipmentCandidateGroup> cachedUnconfigured = new List<EquipmentCandidateGroup>();
        private List<NonEquippableMotionGroup> cachedNonEquippable = new List<NonEquippableMotionGroup>();
        private List<FDX_SecondaryMotion> cachedAllMotions = new List<FDX_SecondaryMotion>();
        private readonly Dictionary<FDX_AttachmentManager.AttachmentSlot, bool> cachedSlotMotion =
            new Dictionary<FDX_AttachmentManager.AttachmentSlot, bool>();
        private readonly Dictionary<GameObject, FDX_SecondaryMotion[]> cachedSourceMotions =
            new Dictionary<GameObject, FDX_SecondaryMotion[]>();
        private readonly Dictionary<FDX_SecondaryMotion, string> cachedMotionGroupNames =
            new Dictionary<FDX_SecondaryMotion, string>();

        private void OnEnable()
        {
            animator = serializedObject.FindProperty("animator");
            additionalAnimators = serializedObject.FindProperty("additionalAnimators");
            attachments = serializedObject.FindProperty("attachments");
            settingsMode = serializedObject.FindProperty("settingsMode");
            sharedMotionSettings = serializedObject.FindProperty("sharedMotionSettings");
            applySharedSettingsOnAttach = serializedObject.FindProperty("applySharedSettingsOnAttach");
            sharedSimulate = serializedObject.FindProperty("sharedSimulate");
            sharedForceSpace = serializedObject.FindProperty("sharedForceSpace");
            previewInEditMode = serializedObject.FindProperty("previewInEditMode");
            attachmentsExpanded = serializedObject.FindProperty("attachmentsExpanded");
            detectedMotionsExpanded = serializedObject.FindProperty("detectedMotionsExpanded");
            sharedSettingsExpanded = serializedObject.FindProperty("sharedSettingsExpanded");
            equipmentStatusTab = serializedObject.FindProperty("equipmentStatusTab");
            equipmentMotionTab = serializedObject.FindProperty("equipmentMotionTab");
            defaultUnequippedParent = serializedObject.FindProperty("defaultUnequippedParent");
            SceneView.duringSceneGui += DuringAttachmentSceneGUI;
            EditorApplication.hierarchyChanged += MarkEquipmentCacheDirty;
            Undo.undoRedoPerformed += MarkEquipmentCacheDirty;
            equipmentCacheDirty = true;
        }

        private void MarkEquipmentCacheDirty()
        {
            equipmentCacheDirty = true;
            FDX_EditModePreviewDriver.InvalidateDiscovery();
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var manager = (FDX_AttachmentManager)target;
            manager.RefreshAutomaticAnimator();
            serializedObject.UpdateIfRequiredOrScript();
            settingsMode.enumValueIndex = applySharedSettingsOnAttach.boolValue
                ? (int)FDX_AttachmentManager.MotionSettingsMode.Unified
                : (int)FDX_AttachmentManager.MotionSettingsMode.PerAttachment;
            if (GUILayout.Button(new GUIContent("修復掛載", "Repair attachments, legacy motion data, and renderer conversion state")))
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(manager, "Repair FDX Attachments");
                FDX_SecondaryMotionBilingualInspector.RepairManagedMotionSetups(manager);
                manager.RebindAndRebuild();
                EditorUtility.SetDirty(manager);
                MarkEquipmentCacheDirty();
                GUIUtility.ExitGUI();
            }
            DrawManagedAnimators(manager);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(previewInEditMode, new GUIContent("即時模擬擺動", "Live Motion Preview"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (!manager.PreviewInEditMode)
                {
                    manager.StopPreview();
                    FDX_EditModePreviewDriver.Deactivate(manager);
                }
                EditorUtility.SetDirty(manager);
                FDX_EditModePreviewDriver.InvalidateDiscovery();
                serializedObject.Update();
            }
            EditorGUI.BeginChangeCheck();
            DrawAttachments();
            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                manager.RebindAndRebuild();
                manager.ApplySharedSettingsToAll();
                EditorUtility.SetDirty(manager);
                MarkEquipmentCacheDirty();
            }
        }

        private void DrawManagedAnimators(FDX_AttachmentManager manager)
        {
            if (animator.objectReferenceValue != null) return;
            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(additionalAnimators, new GUIContent("手動指定角色", "Animator list used when automatic detection finds nothing"), true);
            if (additionalAnimators.arraySize == 0)
                EditorGUILayout.HelpBox("找不到 Animator，請將 Manager 放在角色或其子物件，或手動指定角色 Animator。", MessageType.Warning);
        }

        private void DrawAttachments()
        {
            EditorGUILayout.Space(8f);
            attachmentsExpanded.boolValue = EditorGUILayout.Foldout(attachmentsExpanded.boolValue,
                new GUIContent($"裝備設定（{attachments.arraySize}）", "Equipment Settings"), true);
            if (!attachmentsExpanded.boolValue) return;

            var manager = (FDX_AttachmentManager)target;
            RefreshEquipmentCache(manager);
            List<EquipmentCandidateGroup> unconfigured = cachedUnconfigured;
            List<NonEquippableMotionGroup> nonEquippable = cachedNonEquippable;
            int configuredDynamic = 0;
            int configuredStatic = 0;
            for (int i = 0; i < manager.Attachments.Count; i++)
            {
                if (cachedSlotMotion.TryGetValue(manager.Attachments[i], out bool hasMotion) && hasMotion)
                    configuredDynamic++;
                else configuredStatic++;
            }
            int unconfiguredDynamic = unconfigured.FindAll(item => item.dynamic).Count;
            int unconfiguredStatic = unconfigured.Count - unconfiguredDynamic;
            int nonEquippableDynamic = nonEquippable.Count;
            int nonEquippableStatic = 0;

            EditorGUILayout.BeginHorizontal();
            DrawTabButton(equipmentMotionTab, 0,
                $"動態（{configuredDynamic + unconfiguredDynamic + nonEquippableDynamic}）");
            DrawTabButton(equipmentMotionTab, 1,
                $"靜態（{configuredStatic + unconfiguredStatic + nonEquippableStatic}）");
            EditorGUILayout.EndHorizontal();
            bool showDynamic = equipmentMotionTab.intValue == 0;
            EditorGUILayout.BeginHorizontal();
            DrawTabButton(equipmentStatusTab, 0, $"已裝備（{(showDynamic ? configuredDynamic : configuredStatic)}）");
            DrawTabButton(equipmentStatusTab, 1, $"未裝備（{(showDynamic ? unconfiguredDynamic : unconfiguredStatic)}）");
            DrawTabButton(equipmentStatusTab, 2, $"不可裝備（{(showDynamic ? nonEquippableDynamic : nonEquippableStatic)}）");
            EditorGUILayout.EndHorizontal();

            if (equipmentStatusTab.intValue == 0 && GUILayout.Button(new GUIContent("新增裝備設定", "Add Equipment Setting")))
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(manager, "Add FDX Equipment");
                manager.AddAttachment();
                EditorUtility.SetDirty(manager);
                serializedObject.Update();
                equipmentMotionTab.intValue = 1;
                MarkEquipmentCacheDirty();
                GUIUtility.ExitGUI();
            }

            if (equipmentStatusTab.intValue == 0)
            {
                for (int i = 0; i < attachments.arraySize && i < manager.Attachments.Count; i++)
                    if (cachedSlotMotion.TryGetValue(manager.Attachments[i], out bool hasMotion) && hasMotion == showDynamic)
                        DrawAttachmentSlot(i, showDynamic);
            }
            else if (equipmentStatusTab.intValue == 1)
            {
                DrawUnequippedParent(manager);
                foreach (EquipmentCandidateGroup candidate in unconfigured)
                    if (candidate.dynamic == showDynamic) DrawUnconfiguredEquipment(candidate);
                if ((showDynamic ? unconfiguredDynamic : unconfiguredStatic) == 0)
                    EditorGUILayout.HelpBox("目前沒有符合此分類的未裝備物件。", MessageType.None);
            }
            else
            {
                foreach (NonEquippableMotionGroup group in nonEquippable)
                    if (showDynamic) DrawNonEquippableMotion(group);
                if (!showDynamic || nonEquippable.Count == 0)
                    EditorGUILayout.HelpBox("目前沒有符合此分類的不可裝備物件。", MessageType.None);
            }

            if (showDynamic) DrawSharedSettings(manager, cachedAllMotions);
        }

        private void RefreshEquipmentCache(FDX_AttachmentManager manager)
        {
            if (!equipmentCacheDirty || manager == null) return;
            equipmentCacheDirty = false;
            if (manager.ConsolidateBoundEquipmentSlots())
            {
                EditorUtility.SetDirty(manager);
                serializedObject.Update();
            }
            if (AutoRegisterMountedEquipment(manager))
            {
                serializedObject.Update();
                manager.ConsolidateBoundEquipmentSlots();
                serializedObject.Update();
            }

            cachedUnconfigured = FindUnconfiguredEquipmentGroups(manager);
            cachedNonEquippable = FindNonEquippableMotionGroups(manager);
            cachedAllMotions = manager.FindAllMotionComponents();
            cachedSlotMotion.Clear();
            cachedSourceMotions.Clear();
            cachedMotionGroupNames.Clear();
            foreach (FDX_AttachmentManager.AttachmentSlot slot in manager.Attachments)
            {
                if (slot == null) continue;
                bool dynamic = false;
                foreach (FDX_AttachmentManager.AttachmentSource source in manager.GetActiveSources(slot))
                {
                    if (source?.source == null) continue;
                    FDX_SecondaryMotion[] motions = source.source.GetComponentsInChildren<FDX_SecondaryMotion>(true);
                    cachedSourceMotions[source.source] = motions;
                    if (motions.Length > 0) dynamic = true;
                    foreach (FDX_SecondaryMotion motion in motions)
                        if (motion != null && !cachedMotionGroupNames.ContainsKey(motion))
                            cachedMotionGroupNames.Add(motion,
                                string.IsNullOrWhiteSpace(slot.displayName) ? motion.name : slot.displayName);
                }
                cachedSlotMotion[slot] = dynamic;
            }
            FDX_EditModePreviewDriver.InvalidateDiscovery();
        }

        private static void DrawTabButton(SerializedProperty tab, int value, string label)
        {
            bool selected = tab.intValue == value;
            bool next = GUILayout.Toggle(selected, label, EditorStyles.miniButton);
            if (next && !selected) tab.intValue = value;
        }

        private void DrawUnequippedParent(FDX_AttachmentManager manager)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("預設放置位置", GUILayout.Width(86f));
            Transform current = defaultUnequippedParent.objectReferenceValue as Transform;
            if (current == null) current = manager.transform;
            EditorGUI.BeginChangeCheck();
            Transform next = EditorGUILayout.ObjectField(current, typeof(Transform), true) as Transform;
            if (EditorGUI.EndChangeCheck())
            {
                if (next != null && manager.IsValidUnequippedParent(next)) defaultUnequippedParent.objectReferenceValue = next;
                else EditorUtility.DisplayDialog("放置位置不符", "未裝備物件的放置位置不能位於角色骨架內。", "確定");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAttachmentSlot(int index, bool dynamic)
        {
            if (index < 0 || index >= attachments.arraySize) return;
            var manager = (FDX_AttachmentManager)target;
            SerializedProperty slot = attachments.GetArrayElementAtIndex(index);
            SerializedProperty name = slot.FindPropertyRelative("displayName");
            SerializedProperty expanded = slot.FindPropertyRelative("expanded");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            SerializedProperty enabled = slot.FindPropertyRelative("enabled");
            enabled.boolValue = GUILayout.Toggle(enabled.boolValue, GUIContent.none, GUILayout.Width(18f));
            GUILayout.Space(10f);
            Rect foldoutRect = GUILayoutUtility.GetRect(14f, EditorGUIUtility.singleLineHeight, GUILayout.Width(14f));
            expanded.boolValue = EditorGUI.Foldout(foldoutRect, expanded.boolValue, GUIContent.none, true);
            GUILayout.Label("群組名稱", GUILayout.Width(62f));
            name.stringValue = EditorGUILayout.TextField(name.stringValue, GUILayout.MinWidth(40f));
            if (GUILayout.Button(new GUIContent("解除裝備", "Remove equipment settings without deleting objects"), GUILayout.Width(78f)))
                RemoveAttachmentSlot(index, manager);
            EditorGUILayout.EndHorizontal();
            if (!expanded.boolValue) { EditorGUILayout.EndVertical(); return; }

            DrawAnchorRow(slot, index);
            SerializedProperty sourceMode = slot.FindPropertyRelative("sourceMode");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("來源類型", GUILayout.Width(70f));
            DrawEnumButton(sourceMode, (int)FDX_AttachmentManager.AttachmentSourceMode.RuntimePrefab, "執行時建立");
            DrawEnumButton(sourceMode, (int)FDX_AttachmentManager.AttachmentSourceMode.ExistingSceneObject, "使用現有場景物件");
            EditorGUILayout.EndHorizontal();
            SerializedProperty activeSources = sourceMode.enumValueIndex ==
                (int)FDX_AttachmentManager.AttachmentSourceMode.RuntimePrefab
                ? slot.FindPropertyRelative("prefabSources") : slot.FindPropertyRelative("sceneSources");
            DrawSourceList(activeSources, sourceMode.enumValueIndex ==
                (int)FDX_AttachmentManager.AttachmentSourceMode.RuntimePrefab, !dynamic);
            DrawPositionSettings(slot, activeSources, index);

            EditorGUILayout.EndVertical();
        }

        private void RemoveAttachmentSlot(int index, FDX_AttachmentManager manager)
        {
            serializedObject.ApplyModifiedProperties();
            Undo.RegisterCompleteObjectUndo(manager, "Remove FDX Equipment");
            if (editingAttachmentIndex == index) SetEditingAttachment(-1);
            if (index < manager.Attachments.Count) manager.RemoveAttachment(manager.Attachments[index]);
            EditorUtility.SetDirty(manager);
            serializedObject.Update();
            MarkEquipmentCacheDirty();
            GUIUtility.ExitGUI();
        }

        private void DrawAnchorRow(SerializedProperty slot, int slotIndex)
        {
            SerializedProperty anchorMode = slot.FindPropertyRelative("anchorMode");
            string[] anchorNames = { "人形骨架", "直接指定", "名稱／路徑" };
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("掛載模式", "Anchor Mode"), GUILayout.Width(70f));
            anchorMode.enumValueIndex = EditorGUILayout.Popup(anchorMode.enumValueIndex, anchorNames, GUILayout.MinWidth(90f));
            if (anchorMode.enumValueIndex == (int)FDX_AttachmentManager.AnchorMode.HumanoidBone)
                DrawHumanoidAnchor(slot, slotIndex);
            else if (anchorMode.enumValueIndex == (int)FDX_AttachmentManager.AnchorMode.DirectTransform)
                EditorGUILayout.PropertyField(slot.FindPropertyRelative("customAnchor"), GUIContent.none);
            else
                EditorGUILayout.PropertyField(slot.FindPropertyRelative("anchorNameOrPath"), GUIContent.none);
            EditorGUILayout.EndHorizontal();
            if (anchorMode.enumValueIndex == (int)FDX_AttachmentManager.AnchorMode.NameOrPath)
            {
                EditorGUILayout.PropertyField(slot.FindPropertyRelative("searchRoot"), new GUIContent("搜尋根節點", "Search Root"));
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(slot.FindPropertyRelative("ignoreCase"), new GUIContent("忽略大小寫", "Ignore Case"));
                EditorGUILayout.PropertyField(slot.FindPropertyRelative("ignoreNamespace"), new GUIContent("忽略命名空間", "Ignore Namespace"));
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawHumanoidAnchor(SerializedProperty slot, int slotIndex)
        {
            SerializedProperty bone = slot.FindPropertyRelative("humanoidBone");
            bone.enumValueIndex = EditorGUILayout.Popup(bone.enumValueIndex, bone.enumDisplayNames, GUILayout.MinWidth(90f));
            var manager = (FDX_AttachmentManager)target;
            Transform resolved = slotIndex >= 0 && slotIndex < manager.Attachments.Count
                ? manager.ResolveAnchor(manager.Attachments[slotIndex]) : null;
            EditorGUI.BeginChangeCheck();
            Transform selected = EditorGUILayout.ObjectField(resolved, typeof(Transform), true) as Transform;
            if (!EditorGUI.EndChangeCheck() || selected == resolved) return;
            if (TryMapHumanoidBone(manager, selected, out HumanBodyBones mapped)) bone.enumValueIndex = (int)mapped;
            else EditorUtility.DisplayDialog("只能選擇人形骨架", "掛載目標必須是目前角色的人形骨架內的標準骨骼。", "確定");
        }

        private static bool TryMapHumanoidBone(FDX_AttachmentManager manager, Transform selected, out HumanBodyBones mapped)
        {
            mapped = HumanBodyBones.LastBone;
            if (selected == null) return false;
            foreach (Animator animator in manager.FindManagedAnimators())
                for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
                {
                    HumanBodyBones bone = (HumanBodyBones)i;
                    Transform candidate = animator.isHuman ? animator.GetBoneTransform(bone) :
                        Array.Find(animator.GetComponentsInChildren<Transform>(true), item =>
                            string.Equals(item.name, bone.ToString(), StringComparison.OrdinalIgnoreCase));
                    if (candidate == selected) { mapped = bone; return true; }
                }
            return false;
        }

        private static void DrawEnumButton(SerializedProperty property, int value, string label)
        {
            bool selected = property.enumValueIndex == value;
            bool next = GUILayout.Toggle(selected, label, EditorStyles.miniButton);
            if (next && !selected) property.enumValueIndex = value;
        }

        private void DrawSourceList(SerializedProperty list, bool prefabOnly, bool allowConvert)
        {
            EditorGUILayout.LabelField("來源物件", EditorStyles.boldLabel);
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                SerializedProperty source = entry.FindPropertyRelative("source");
                GameObject sourceObject = source.objectReferenceValue as GameObject;
                EditorGUILayout.BeginHorizontal();
                if (sourceObject != null)
                {
                    if (GUILayout.Button("選取物件", GUILayout.MinWidth(64f), GUILayout.MaxWidth(90f)))
                    {
                        Selection.activeGameObject = sourceObject;
                        EditorGUIUtility.PingObject(sourceObject);
                    }
                }
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(source, GUIContent.none);
                if (EditorGUI.EndChangeCheck() && source.objectReferenceValue is GameObject selected)
                {
                    bool valid = prefabOnly ? EditorUtility.IsPersistent(selected) && PrefabUtility.IsPartOfPrefabAsset(selected) : selected.scene.IsValid();
                    if (!valid)
                    {
                        source.objectReferenceValue = null;
                        EditorUtility.DisplayDialog("來源類型不符",
                            prefabOnly ? "此清單只能加入 Prefab 資產。" : "此清單只能加入目前場景中的物件。", "確定");
                    }
                }
                if (sourceObject != null) DrawSourceActions(list, sourceObject, prefabOnly);
                if (GUILayout.Button("－", GUILayout.Width(28f)))
                {
                    list.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("＋ 新增來源物件"))
            {
                int index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                SerializedProperty entry = list.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("source").objectReferenceValue = null;
                entry.FindPropertyRelative("useSourceMotionSettings").boolValue = false;
                entry.FindPropertyRelative("localPosition").vector3Value = Vector3.zero;
                entry.FindPropertyRelative("localEulerAngles").vector3Value = Vector3.zero;
                entry.FindPropertyRelative("localScale").vector3Value = Vector3.one;
            }
        }

        private void DrawSourceActions(SerializedProperty list, GameObject sourceObject, bool prefabOnly)
        {
            var manager = (FDX_AttachmentManager)target;
            FDX_SecondaryMotion[] motions = GetCachedSourceMotions(sourceObject);
            if (motions.Length > 0)
            {
                bool individual = manager.UsesIndividualSettings(motions[0]);
                bool next = GUILayout.Toggle(individual, "使用各別動態設定", EditorStyles.miniButton);
                if (next != individual)
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(manager, "Change FDX Individual Settings");
                    foreach (FDX_SecondaryMotion motion in motions) manager.SetIndividualSettings(motion, next);
                    if (!next) manager.ApplySharedSettingsToAll();
                    EditorUtility.SetDirty(manager);
                    GUIUtility.ExitGUI();
                }
            }
            if (GUILayout.Button(motions.Length > 0 ? "轉為靜態物件" : "轉為動態物件"))
            {
                serializedObject.ApplyModifiedProperties();
                ConvertSource(sourceObject, prefabOnly, motions.Length == 0);
                manager.RebindAndRebuild();
                bool anyDynamic = false;
                for (int sourceIndex = 0; sourceIndex < list.arraySize; sourceIndex++)
                    anyDynamic |= HasMotion(list.GetArrayElementAtIndex(sourceIndex).FindPropertyRelative("source").objectReferenceValue as GameObject);
                serializedObject.Update();
                equipmentMotionTab.intValue = anyDynamic ? 0 : 1;
                serializedObject.ApplyModifiedProperties();
                MarkEquipmentCacheDirty();
                GUIUtility.ExitGUI();
            }
        }

        private FDX_SecondaryMotion[] GetCachedSourceMotions(GameObject sourceObject)
        {
            if (sourceObject == null) return Array.Empty<FDX_SecondaryMotion>();
            if (cachedSourceMotions.TryGetValue(sourceObject, out FDX_SecondaryMotion[] motions)) return motions;
            motions = sourceObject.GetComponentsInChildren<FDX_SecondaryMotion>(true);
            cachedSourceMotions[sourceObject] = motions;
            return motions;
        }

        private void DrawSourceMotionSettings(FDX_AttachmentManager manager, FDX_SecondaryMotion[] motions)
        {
            var individual = new List<FDX_SecondaryMotion>();
            foreach (FDX_SecondaryMotion motion in motions)
                if (motion != null && manager.UsesIndividualSettings(motion)) individual.Add(motion);
            if (individual.Count == 0) return;
            bool expanded = manager.IsMotionExpanded(individual[0]);
            bool next = FDX_InspectorGUI.DrawContainedFoldout(expanded,
                new GUIContent($"動態設定（{individual.Count}）", "Individual Motion Settings"));
            if (next != expanded)
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(manager, "Toggle FDX Motion Settings");
                foreach (FDX_SecondaryMotion motion in individual) manager.SetMotionExpanded(motion, next);
                EditorUtility.SetDirty(manager);
                GUIUtility.ExitGUI();
            }
            if (!expanded) return;
            foreach (FDX_SecondaryMotion motion in individual) DrawMotionOnlySettings(motion);
        }

        private static void ConvertSource(GameObject source, bool prefab, bool dynamic)
        {
            string path = prefab ? AssetDatabase.GetAssetPath(source) : null;
            GameObject root = prefab ? PrefabUtility.LoadPrefabContents(path) : source;
            try
            {
                if (dynamic)
                {
                    if (root.GetComponent<FDX_SecondaryMotion>() == null)
                    {
                        if (prefab) root.AddComponent<FDX_SecondaryMotion>();
                        else Undo.AddComponent<FDX_SecondaryMotion>(root);
                    }
                }
                else
                    foreach (FDX_SecondaryMotion motion in root.GetComponentsInChildren<FDX_SecondaryMotion>(true))
                        FDX_SecondaryMotionBilingualInspector.ConvertToStatic(motion);
                if (prefab) PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { if (prefab) PrefabUtility.UnloadPrefabContents(root); }
        }

        private void ConvertSourcesToDynamic(SerializedProperty list, bool prefabOnly)
        {
            serializedObject.ApplyModifiedProperties();
            int converted = 0;
            for (int i = 0; i < list.arraySize; i++)
            {
                GameObject source = list.GetArrayElementAtIndex(i).FindPropertyRelative("source").objectReferenceValue as GameObject;
                if (source == null || source.GetComponent<FDX_SecondaryMotion>() != null) continue;
                if (!prefabOnly)
                {
                    Undo.AddComponent<FDX_SecondaryMotion>(source);
                    converted++;
                    continue;
                }
                string path = AssetDatabase.GetAssetPath(source);
                if (string.IsNullOrEmpty(path)) continue;
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (root.GetComponent<FDX_SecondaryMotion>() == null)
                    {
                        root.AddComponent<FDX_SecondaryMotion>();
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        converted++;
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            if (converted == 0)
                EditorUtility.DisplayDialog("轉為動態物件", "清單中沒有需要轉換的來源物件。", "確定");
            serializedObject.Update();
        }

        private void DrawPositionSettings(SerializedProperty slot, SerializedProperty activeSources, int slotIndex)
        {
            SerializedProperty expanded = slot.FindPropertyRelative("positionExpanded");
            SerializedProperty individual = slot.FindPropertyRelative("useIndividualTransforms");
            string editLabel = editingAttachmentIndex == slotIndex ? "結束編輯掛載位置" : "編輯掛載位置";
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            expanded.boolValue = EditorGUILayout.Foldout(expanded.boolValue, "調整位置", true);
            individual.boolValue = GUILayout.Toggle(individual.boolValue, "各別位置設定", EditorStyles.miniButton);
            if (GUILayout.Button(editLabel, EditorStyles.miniButton))
                SetEditingAttachment(editingAttachmentIndex == slotIndex ? -1 : slotIndex);
            EditorGUILayout.EndHorizontal();
            if (!expanded.boolValue) return;
            if (!individual.boolValue)
            {
                EditorGUILayout.PropertyField(slot.FindPropertyRelative("localPosition"), new GUIContent("位置", "Local Position"));
                EditorGUILayout.PropertyField(slot.FindPropertyRelative("localEulerAngles"), new GUIContent("旋轉", "Local Rotation"));
                EditorGUILayout.PropertyField(slot.FindPropertyRelative("localScale"), new GUIContent("縮放", "Local Scale"));
                return;
            }
            for (int i = 0; i < activeSources.arraySize; i++)
            {
                SerializedProperty entry = activeSources.GetArrayElementAtIndex(i);
                GameObject source = entry.FindPropertyRelative("source").objectReferenceValue as GameObject;
                EditorGUILayout.LabelField(source != null ? source.name : $"來源 {i + 1}", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("localPosition"), new GUIContent("位置", "Local Position"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("localEulerAngles"), new GUIContent("旋轉", "Local Rotation"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("localScale"), new GUIContent("縮放", "Local Scale"));
            }
        }

        private sealed class EquipmentCandidateGroup
        {
            public string displayName;
            public readonly List<GameObject> members = new List<GameObject>();
            public bool dynamic;
        }

        private sealed class NonEquippableMotionGroup
        {
            public FDX_SecondaryMotion motion;
            public readonly List<Transform> controlledRoots = new List<Transform>();
        }

        private void DrawUnconfiguredEquipment(EquipmentCandidateGroup candidate)
        {
            if (candidate == null || candidate.members.Count == 0) return;
            var manager = (FDX_AttachmentManager)target;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            string key = "FDX.EquipmentCandidate." + candidate.members[0].GetInstanceID();
            bool expanded = SessionState.GetBool(key, candidate.members.Count > 1);
            expanded = EditorGUILayout.Foldout(expanded,
                $"{candidate.displayName}（群組 {candidate.members.Count}）", true);
            SessionState.SetBool(key, expanded);
            if (GUILayout.Button("加入裝備設定"))
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(manager, "Add Detected FDX Equipment");
                FDX_AttachmentManager.AttachmentSlot slot = manager.AddAttachment();
                slot.displayName = candidate.displayName;
                slot.enabled = true;
                slot.sourceMode = FDX_AttachmentManager.AttachmentSourceMode.ExistingSceneObject;
                foreach (GameObject member in candidate.members)
                    if (member != null)
                        slot.sceneSources.Add(new FDX_AttachmentManager.AttachmentSource { source = member });
                EditorUtility.SetDirty(manager);
                serializedObject.Update();
                equipmentStatusTab.intValue = 0;
                MarkEquipmentCacheDirty();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
            if (expanded)
                foreach (GameObject member in candidate.members)
                    if (member != null) DrawCompactObjectButton(member);
            EditorGUILayout.EndVertical();
        }

        private static void DrawNonEquippableMotion(NonEquippableMotionGroup group)
        {
            if (group == null || group.motion == null) return;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            DrawCompactObjectButton(group.motion.gameObject);
            GUILayout.Label("角色骨架動態", EditorStyles.miniLabel, GUILayout.Width(88f));
            EditorGUILayout.EndHorizontal();
            foreach (Transform root in group.controlledRoots)
                if (root != null)
                    EditorGUILayout.ObjectField(root, typeof(Transform), true);
            EditorGUILayout.EndVertical();
        }

        private static void DrawCompactObjectButton(GameObject gameObject)
        {
            Rect row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(row, GUIContent.none, EditorStyles.objectField))
            {
                Selection.activeGameObject = gameObject;
                EditorGUIUtility.PingObject(gameObject);
            }
            Texture icon = EditorGUIUtility.ObjectContent(gameObject, typeof(GameObject)).image;
            Rect iconRect = new Rect(row.x + 3f, row.y + 1f, 16f, 16f);
            if (icon != null) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(iconRect.xMax + 4f, row.y, row.width - 24f, row.height), gameObject.name);
        }

        private void DrawMotionCard(FDX_AttachmentManager manager, FDX_SecondaryMotion motion, bool prefab)
        {
            if (motion == null) return;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("選取物件"))
            {
                Selection.activeGameObject = motion.gameObject;
                EditorGUIUtility.PingObject(motion.gameObject);
            }
            GUILayout.Label(motion.name, GUILayout.MinWidth(70f));
            bool individual = manager.UsesIndividualSettings(motion);
            bool next = GUILayout.Toggle(individual, "使用各別動態設定", EditorStyles.miniButton);
            if (next != individual)
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(manager, "Change FDX Motion Settings Source");
                manager.SetIndividualSettings(motion, next);
                if (!next) manager.ApplySharedSettingsToAll();
                EditorUtility.SetDirty(manager);
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("轉為靜態物件"))
            {
                serializedObject.ApplyModifiedProperties();
                ConvertSource(motion.gameObject, prefab, false);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
            DrawSourceMotionSettings(manager, new[] { motion });
            EditorGUILayout.EndVertical();
        }

        private static bool HasMotion(GameObject root) => root != null &&
            root.GetComponentInChildren<FDX_SecondaryMotion>(true) != null;

        private static bool SlotHasMotion(FDX_AttachmentManager.AttachmentSlot slot)
        {
            if (slot == null) return false;
            List<FDX_AttachmentManager.AttachmentSource> sources = slot.sourceMode ==
                FDX_AttachmentManager.AttachmentSourceMode.RuntimePrefab ? slot.prefabSources : slot.sceneSources;
            return sources != null && sources.Exists(item => item != null && HasMotion(item.source));
        }

        private static List<FDX_SecondaryMotion> GetActiveSlotMotions(FDX_AttachmentManager.AttachmentSlot slot)
        {
            var results = new List<FDX_SecondaryMotion>();
            var found = new HashSet<FDX_SecondaryMotion>();
            if (slot == null) return results;
            List<FDX_AttachmentManager.AttachmentSource> sources = slot.sourceMode ==
                FDX_AttachmentManager.AttachmentSourceMode.RuntimePrefab ? slot.prefabSources : slot.sceneSources;
            if (sources == null) return results;
            foreach (FDX_AttachmentManager.AttachmentSource source in sources)
            {
                if (source == null || source.source == null) continue;
                foreach (FDX_SecondaryMotion motion in source.source.GetComponentsInChildren<FDX_SecondaryMotion>(true))
                    if (motion != null && found.Add(motion)) results.Add(motion);
            }
            return results;
        }

        internal static List<GameObject> FindUnconfiguredEquipment(FDX_AttachmentManager manager)
        {
            var configured = new HashSet<GameObject>();
            HashSet<Transform> characterBones = CollectCharacterBones(manager);
            foreach (FDX_AttachmentManager.AttachmentSlot slot in manager.Attachments)
            {
                if (slot == null) continue;
                if (slot.prefabSources != null)
                    foreach (FDX_AttachmentManager.AttachmentSource source in slot.prefabSources)
                        if (source != null && source.source != null) configured.Add(source.source);
                if (slot.sceneSources != null)
                    foreach (FDX_AttachmentManager.AttachmentSource source in slot.sceneSources)
                        if (source != null && source.source != null) configured.Add(source.source);
            }
            foreach (GameObject source in new List<GameObject>(configured))
                foreach (Animator animator in manager.FindManagedAnimators())
                    foreach (Transform child in animator.transform)
                        if (manager.AreBoundEquipmentRelated(source, child.gameObject)) configured.Add(child.gameObject);
            var result = new List<GameObject>();
            for (int i = 0; i < manager.transform.childCount; i++)
                AddEquipmentCandidate(manager.transform.GetChild(i).gameObject, configured, characterBones, result);
            for (int i = result.Count - 1; i >= 0; i--)
                for (int j = 0; j < result.Count; j++)
                    if (i != j && manager.AreBoundEquipmentRelated(result[i], result[j]) &&
                        result[j].GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
                    { result.RemoveAt(i); break; }
            return result;
        }

        private static List<EquipmentCandidateGroup> FindUnconfiguredEquipmentGroups(FDX_AttachmentManager manager)
        {
            List<GameObject> candidates = FindUnconfiguredEquipment(manager);
            var groups = new List<EquipmentCandidateGroup>();
            var assigned = new HashSet<GameObject>();
            foreach (GameObject candidate in candidates)
            {
                if (candidate == null || !assigned.Add(candidate)) continue;
                var group = new EquipmentCandidateGroup();
                group.members.Add(candidate);
                foreach (Animator animator in manager.FindManagedAnimators())
                {
                    if (animator == null) continue;
                    for (int i = 0; i < animator.transform.childCount; i++)
                    {
                        GameObject sibling = animator.transform.GetChild(i).gameObject;
                        if (sibling == candidate || assigned.Contains(sibling) ||
                            !manager.AreBoundEquipmentRelated(candidate, sibling)) continue;
                        group.members.Add(sibling);
                        assigned.Add(sibling);
                    }
                }
                GameObject visible = group.members.Find(item => item != null &&
                    item.GetComponentInChildren<Renderer>(true) != null);
                group.displayName = visible != null ? visible.name : candidate.name;
                group.dynamic = group.members.Exists(HasMotion);
                groups.Add(group);
            }
            return groups;
        }

        internal static bool AutoRegisterMountedEquipment(FDX_AttachmentManager manager)
        {
            HashSet<Transform> characterBones = CollectCharacterBones(manager);
            List<EquipmentCandidateGroup> groups = FindUnconfiguredEquipmentGroups(manager);
            bool changed = false;
            foreach (EquipmentCandidateGroup group in groups)
            {
                Transform anchor = null;
                foreach (GameObject member in group.members)
                {
                    anchor = FindMountedAnchor(member != null ? member.transform : null, characterBones);
                    if (anchor != null) break;
                }
                if (anchor == null) continue;
                if (!changed) Undo.RecordObject(manager, "Detect Mounted FDX Equipment");
                FDX_AttachmentManager.AttachmentSlot slot = manager.AddAttachment();
                slot.displayName = group.displayName;
                slot.enabled = true;
                slot.sourceMode = FDX_AttachmentManager.AttachmentSourceMode.ExistingSceneObject;
                if (TryMapHumanoidBone(manager, anchor, out HumanBodyBones humanoidBone))
                {
                    slot.anchorMode = FDX_AttachmentManager.AnchorMode.HumanoidBone;
                    slot.humanoidBone = humanoidBone;
                }
                else
                {
                    slot.anchorMode = FDX_AttachmentManager.AnchorMode.DirectTransform;
                    slot.customAnchor = anchor;
                }
                slot.useIndividualTransforms = true;
                foreach (GameObject member in group.members)
                {
                    if (member == null) continue;
                    Transform item = member.transform;
                    var source = new FDX_AttachmentManager.AttachmentSource
                    {
                        source = member,
                        localPosition = anchor.InverseTransformPoint(item.position),
                        localEulerAngles = (Quaternion.Inverse(anchor.rotation) * item.rotation).eulerAngles,
                        localScale = new Vector3(
                            anchor.lossyScale.x != 0f ? item.lossyScale.x / anchor.lossyScale.x : item.localScale.x,
                            anchor.lossyScale.y != 0f ? item.lossyScale.y / anchor.lossyScale.y : item.localScale.y,
                            anchor.lossyScale.z != 0f ? item.lossyScale.z / anchor.lossyScale.z : item.localScale.z)
                    };
                    slot.sceneSources.Add(source);
                }
                changed = true;
            }
            if (changed) EditorUtility.SetDirty(manager);
            return changed;
        }

        private static Transform FindMountedAnchor(Transform item, HashSet<Transform> characterBones)
        {
            if (item == null) return null;
            Transform parent = item.parent;
            while (parent != null)
            {
                if (characterBones.Contains(parent)) return parent;
                parent = parent.parent;
            }
            return null;
        }

        private static List<NonEquippableMotionGroup> FindNonEquippableMotionGroups(FDX_AttachmentManager manager)
        {
            var results = new List<NonEquippableMotionGroup>();
            HashSet<Transform> characterBones = CollectCharacterBones(manager);
            var equipmentMotions = new HashSet<FDX_SecondaryMotion>();
            foreach (FDX_AttachmentManager.AttachmentSlot slot in manager.Attachments)
                foreach (FDX_SecondaryMotion motion in manager.FindMotionComponents(slot))
                    if (motion != null) equipmentMotions.Add(motion);
            foreach (FDX_SecondaryMotion motion in manager.FindHierarchyMotionComponents())
            {
                if (motion == null || equipmentMotions.Contains(motion) ||
                    motion.Source != FDX_SecondaryMotion.MotionSource.ExistingBones) continue;
                var group = new NonEquippableMotionGroup { motion = motion };
                foreach (FDX_SecondaryMotion.BoneChain chain in motion.BoneChains)
                    if (chain != null && chain.root != null && characterBones.Contains(chain.root) &&
                        !group.controlledRoots.Contains(chain.root)) group.controlledRoots.Add(chain.root);
                if (group.controlledRoots.Count == 0 && characterBones.Contains(motion.transform))
                    group.controlledRoots.Add(motion.transform);
                if (group.controlledRoots.Count > 0) results.Add(group);
            }
            return results;
        }

        private static HashSet<Transform> CollectCharacterBones(FDX_AttachmentManager manager)
        {
            var bones = new HashSet<Transform>();
            foreach (Animator animator in manager.FindManagedAnimators())
            {
                if (animator == null) continue;
                bones.Add(animator.transform);
                if (animator.isHuman)
                    for (int boneIndex = 0; boneIndex < (int)HumanBodyBones.LastBone; boneIndex++)
                    {
                        Transform bone = animator.GetBoneTransform((HumanBodyBones)boneIndex);
                        if (bone != null) bones.Add(bone);
                    }
                foreach (SkinnedMeshRenderer renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (!IsCharacterBodyName(renderer.name)) continue;
                    foreach (Transform bone in renderer.bones) if (bone != null) bones.Add(bone);
                    if (renderer.rootBone != null) bones.Add(renderer.rootBone);
                }
                // Generic imported rigs also expose their main hierarchy through Hips.
                foreach (Transform item in animator.GetComponentsInChildren<Transform>(true))
                    if (item.name.Equals("Hips", StringComparison.OrdinalIgnoreCase))
                        foreach (Transform bone in item.GetComponentsInChildren<Transform>(true))
                        {
                            bool equipmentRig = false;
                            for (Transform ancestor = bone; ancestor != null && ancestor != item; ancestor = ancestor.parent)
                                if (HasIndependentEquipmentRig(ancestor)) { equipmentRig = true; break; }
                            if (!equipmentRig && bone.GetComponent<Renderer>() == null)
                                bones.Add(bone);
                        }
            }
            return bones;
        }

        private static bool IsCharacterRigObject(GameObject candidate, HashSet<Transform> characterBones)
        {
            if (candidate == null) return false;
            if (IsCharacterBodyName(candidate.name)) return true;
            Transform candidateTransform = candidate.transform;
            foreach (Transform bone in characterBones)
                if (bone != null && (bone == candidateTransform || bone.IsChildOf(candidateTransform))) return true;

            return false;
        }

        private static bool IsCharacterBodyName(string name) =>
            string.Equals(name, "Body", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "Face", StringComparison.OrdinalIgnoreCase);

        private static bool HasIndependentEquipmentRig(Transform root)
        {
            if (root == null || IsCharacterBodyName(root.name)) return false;
            if (Enum.TryParse(root.name, true, out HumanBodyBones _)) return false;
            foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.bones.Length == 0) continue;
                bool ownsBones = true;
                foreach (Transform bone in renderer.bones)
                    if (bone == null || (bone != root && !bone.IsChildOf(root))) { ownsBones = false; break; }
                if (ownsBones) return true;
            }
            return false;
        }

        private static void AddEquipmentCandidate(GameObject candidate, HashSet<GameObject> configured,
            HashSet<Transform> characterBones, List<GameObject> result)
        {
            if (candidate == null || configured.Contains(candidate)) return;
            bool generated = candidate.name.IndexOf("_FDX_", StringComparison.OrdinalIgnoreCase) >= 0 ||
                candidate.name.StartsWith("FDX_", StringComparison.OrdinalIgnoreCase);
            bool rig = IsCharacterRigObject(candidate, characterBones);
            if (!generated && !rig && candidate.GetComponentInChildren<Renderer>(true) != null)
            {
                if (!result.Contains(candidate)) result.Add(candidate);
                return;
            }
            if (IsCharacterBodyName(candidate.name)) return;
            for (int i = 0; i < candidate.transform.childCount; i++)
                AddEquipmentCandidate(candidate.transform.GetChild(i).gameObject, configured, characterBones, result);
        }

        private void DrawSharedSettings(FDX_AttachmentManager manager, List<FDX_SecondaryMotion> motions)
        {
            EditorGUILayout.Space(6f);
            detectedMotionsExpanded.boolValue = FDX_InspectorGUI.DrawContainedFoldout(
                detectedMotionsExpanded.boolValue, new GUIContent("動態設定", "Motion Settings"));
            if (!detectedMotionsExpanded.boolValue) return;
            applySharedSettingsOnAttach.boolValue = true;
            settingsMode.enumValueIndex = (int)FDX_AttachmentManager.MotionSettingsMode.Unified;
            sharedSettingsExpanded.boolValue = FDX_InspectorGUI.DrawContainedFoldout(
                sharedSettingsExpanded.boolValue, new GUIContent("全域動態設定", "Global Motion Settings"));
            if (sharedSettingsExpanded.boolValue)
            {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            sharedSimulate.boolValue = GUILayout.Toggle(sharedSimulate.boolValue, "動態模擬", EditorStyles.miniButton);
            SerializedProperty sharedPreview = sharedMotionSettings.FindPropertyRelative("previewAutoSway");
            if (sharedSimulate.boolValue)
                sharedPreview.boolValue = GUILayout.Toggle(sharedPreview.boolValue, "預覽自動擺動", EditorStyles.miniButton);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (motions.Count > 0) Undo.RecordObjects(motions.ToArray(), "Toggle All FDX Simulation");
                manager.SetAllSimulation(manager.SharedSimulate);
                serializedObject.Update();
            }
            DrawCompactExclusiveOptions(sharedForceSpace, "力場座標", "世界座標", "動作參考物件座標");
            FDX_InspectorGUI.DrawMotionSettings(sharedMotionSettings);
            if (GUILayout.Button(new GUIContent("將統一設定套用到全部物件", "Apply Shared Settings To All")))
            {
                if (motions.Count > 0) Undo.RecordObjects(motions.ToArray(), "Apply FDX Motion Settings");
                serializedObject.ApplyModifiedProperties();
                manager.ApplySharedSettingsToAll();
                serializedObject.Update();
            }
            EditorGUILayout.EndVertical();
            }

            foreach (FDX_SecondaryMotion motion in motions)
            {
                if (motion == null || !manager.UsesIndividualSettings(motion)) continue;
                bool expanded = manager.IsMotionExpanded(motion);
                string groupName = GetMotionGroupName(motion);
                bool next = FDX_InspectorGUI.DrawContainedFoldout(expanded,
                    new GUIContent(groupName + " 動態設定", "Individual Motion Settings"));
                if (next != expanded)
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(manager, "Toggle FDX Motion Settings");
                    manager.SetMotionExpanded(motion, next);
                    EditorUtility.SetDirty(manager);
                    serializedObject.Update();
                }
                if (next)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    DrawMotionOnlySettings(motion);
                    EditorGUILayout.EndVertical();
                }
            }
        }

        private string GetMotionGroupName(FDX_SecondaryMotion motion)
        {
            if (motion != null && cachedMotionGroupNames.TryGetValue(motion, out string groupName))
                return groupName;
            return motion.name;
        }

        private static void DrawCompactExclusiveOptions(SerializedProperty property, string label,
            string first, string second)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(70f));
            bool firstSelected = property.enumValueIndex == 0;
            if (GUILayout.Toggle(firstSelected, first, EditorStyles.miniButton) && !firstSelected)
                property.enumValueIndex = 0;
            bool secondSelected = property.enumValueIndex == 1;
            if (GUILayout.Toggle(secondSelected, second, EditorStyles.miniButton) && !secondSelected)
                property.enumValueIndex = 1;
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawMotionOnlySettings(FDX_SecondaryMotion motion)
        {
            var motionObject = new SerializedObject(motion);
            motionObject.Update();
            EditorGUILayout.BeginHorizontal();
            SerializedProperty motionSimulate = motionObject.FindProperty("simulate");
            motionSimulate.boolValue = GUILayout.Toggle(motionSimulate.boolValue, "動態模擬", EditorStyles.miniButton);
            SerializedProperty motionPreview = motionObject.FindProperty("settings").FindPropertyRelative("previewAutoSway");
            if (motionSimulate.boolValue)
                motionPreview.boolValue = GUILayout.Toggle(motionPreview.boolValue, "預覽自動擺動", EditorStyles.miniButton);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            DrawCompactExclusiveOptions(motionObject.FindProperty("forceSpace"), "力場座標", "世界座標", "動作參考物件座標");
            FDX_InspectorGUI.DrawMotionSettings(motionObject.FindProperty("settings"));
            motionObject.ApplyModifiedProperties();
        }

        private void SetEditingAttachment(int index)
        {
            if (editingAttachmentIndex < 0 && index >= 0)
            {
                previousAttachmentToolsHidden = Tools.hidden;
                Tools.hidden = true;
                attachmentToolsHidden = true;
            }
            else if (editingAttachmentIndex >= 0 && index < 0 && attachmentToolsHidden)
            {
                Tools.hidden = previousAttachmentToolsHidden;
                attachmentToolsHidden = false;
            }
            editingAttachmentIndex = index;
            SceneView.RepaintAll();
        }

        private void DuringAttachmentSceneGUI(SceneView sceneView)
        {
            if (editingAttachmentIndex < 0 || target == null) return;
            var manager = (FDX_AttachmentManager)target;
            if (editingAttachmentIndex >= manager.Attachments.Count) { SetEditingAttachment(-1); return; }
            FDX_AttachmentManager.AttachmentSlot slot = manager.Attachments[editingAttachmentIndex];
            List<FDX_AttachmentManager.AttachmentSource> sources = manager.GetActiveSources(slot);
            var transforms = new List<Transform>();
            foreach (FDX_AttachmentManager.AttachmentSource source in sources)
                if (source?.source != null && source.source.scene.IsValid() && !transforms.Contains(source.source.transform))
                    transforms.Add(source.source.transform);
            if (transforms.Count == 0) return;

            Transform primary = transforms[0];
            Vector3 oldPosition = primary.position;
            Quaternion oldRotation = primary.rotation;
            Vector3 oldScale = primary.localScale;
            Vector3 nextPosition = oldPosition;
            Quaternion nextRotation = oldRotation;
            Vector3 nextScale = oldScale;
            Handles.color = Color.cyan;
            EditorGUI.BeginChangeCheck();
            if (Tools.current == Tool.Rotate) nextRotation = Handles.RotationHandle(oldRotation, oldPosition);
            else if (Tools.current == Tool.Scale) nextScale = Handles.ScaleHandle(oldScale, oldPosition, oldRotation,
                HandleUtility.GetHandleSize(oldPosition));
            else nextPosition = Handles.PositionHandle(oldPosition, oldRotation);
            if (!EditorGUI.EndChangeCheck()) return;

            Undo.RecordObjects(transforms.ToArray(), "Edit FDX Attachment Position");
            Undo.RecordObject(manager, "Edit FDX Attachment Position");
            Quaternion rotationDelta = nextRotation * Quaternion.Inverse(oldRotation);
            Vector3 scaleFactor = new Vector3(
                Mathf.Approximately(oldScale.x, 0f) ? 1f : nextScale.x / oldScale.x,
                Mathf.Approximately(oldScale.y, 0f) ? 1f : nextScale.y / oldScale.y,
                Mathf.Approximately(oldScale.z, 0f) ? 1f : nextScale.z / oldScale.z);
            foreach (Transform item in transforms)
            {
                if (Tools.current == Tool.Rotate)
                {
                    item.position = oldPosition + rotationDelta * (item.position - oldPosition);
                    item.rotation = rotationDelta * item.rotation;
                }
                else if (Tools.current == Tool.Scale)
                {
                    Vector3 offset = item.position - oldPosition;
                    item.position = oldPosition + Vector3.Scale(offset, scaleFactor);
                    item.localScale = Vector3.Scale(item.localScale, scaleFactor);
                }
                else item.position += nextPosition - oldPosition;
            }
            slot.useIndividualTransforms = true;
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i]?.source == null) continue;
                Transform item = sources[i].source.transform;
                sources[i].localPosition = item.localPosition;
                sources[i].localEulerAngles = item.localEulerAngles;
                sources[i].localScale = item.localScale;
            }
            EditorUtility.SetDirty(manager);
            serializedObject.UpdateIfRequiredOrScript();
            sceneView.Repaint();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringAttachmentSceneGUI;
            EditorApplication.hierarchyChanged -= MarkEquipmentCacheDirty;
            Undo.undoRedoPerformed -= MarkEquipmentCacheDirty;
            SetEditingAttachment(-1);
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
        private SerializedProperty motionSettingsExpanded;
        private SerializedProperty globalMotionSettingsExpanded;
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
        private SerializedProperty showCollisionRadiusGizmos;
        private SerializedProperty pivotColor;
        private SerializedProperty endPointColor;
        private SerializedProperty mirrorColor;
        private SerializedProperty boneRootColor;
        private SerializedProperty collisionRadiusColor;
        private SerializedProperty pivotGizmoRadius;
        private SerializedProperty endPointGizmoRadius;
        private Transform editingTip;
        private bool previousToolsHidden;
        private bool toolsHiddenByInspector;
        private FDX_AttachmentManager cachedRelatedManager;
        private double nextRelatedManagerRefreshTime;
        private bool relatedManagerCacheDirty = true;

        private void OnEnable()
        {
            motionSource = serializedObject.FindProperty("motionSource");
            simulate = serializedObject.FindProperty("simulate");
            forceSpace = serializedObject.FindProperty("forceSpace");
            settings = serializedObject.FindProperty("settings");
            motionSettingsExpanded = serializedObject.FindProperty("motionSettingsExpanded");
            globalMotionSettingsExpanded = serializedObject.FindProperty("globalMotionSettingsExpanded");
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
            showCollisionRadiusGizmos = serializedObject.FindProperty("showCollisionRadiusGizmos");
            pivotColor = serializedObject.FindProperty("pivotColor");
            endPointColor = serializedObject.FindProperty("endPointColor");
            mirrorColor = serializedObject.FindProperty("mirrorColor");
            boneRootColor = serializedObject.FindProperty("boneRootColor");
            collisionRadiusColor = serializedObject.FindProperty("collisionRadiusColor");
            pivotGizmoRadius = serializedObject.FindProperty("pivotGizmoRadius");
            endPointGizmoRadius = serializedObject.FindProperty("endPointGizmoRadius");
            SceneView.duringSceneGui += DuringSceneGUI;
            EditorApplication.hierarchyChanged += MarkRelatedManagerCacheDirty;
            Undo.undoRedoPerformed += MarkRelatedManagerCacheDirty;
            relatedManagerCacheDirty = true;
            RepairStaleSinglePivotRenderer((FDX_SecondaryMotion)target);
            EnsureDefaultPivot();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
            EditorApplication.hierarchyChanged -= MarkRelatedManagerCacheDirty;
            Undo.undoRedoPerformed -= MarkRelatedManagerCacheDirty;
            if (toolsHiddenByInspector) Tools.hidden = previousToolsHidden;
            toolsHiddenByInspector = false;
            editingTip = null;
        }

        private void MarkRelatedManagerCacheDirty()
        {
            relatedManagerCacheDirty = true;
            Repaint();
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
            if (group != null)
            {
                if (group.mirrorCenter == null) group.mirrorCenter = motion.transform;
                if (group.automaticSimulationAnchor && group.simulationAnchor == null)
                    group.simulationAnchor = motion.transform.parent != null ? motion.transform.parent : motion.transform;
            }
            if (group == null) return;
            if (group.pivot != null)
            {
                if (!motion.AutomaticMultiPivotSkinning && CountEffectivePivots(motion) <= 1)
                    EnsureSinglePivotWrapper(motion);
                return;
            }

            var pivotObject = new GameObject($"{motion.name}_FDX_Pivot_1");
            Undo.RegisterCreatedObjectUndo(pivotObject, "Create Default FDX Pivot");
            Undo.SetTransformParent(pivotObject.transform, motion.transform, "Parent Default FDX Pivot");
            pivotObject.transform.localPosition = Vector3.zero;
            pivotObject.transform.localRotation = Quaternion.identity;
            pivotObject.transform.localScale = Vector3.one;
            Undo.RecordObject(motion, "Assign Default FDX Pivot");
            group.pivot = pivotObject.transform;
            motion.RotationPivot = pivotObject.transform;
            EnsureSinglePivotWrapper(motion);
            EditorUtility.SetDirty(motion);
            serializedObject.UpdateIfRequiredOrScript();
        }

        private static void RepairStaleSinglePivotRenderer(FDX_SecondaryMotion motion)
        {
            if (Application.isPlaying || motion == null || RequiresAutomaticSkinning(motion)) return;
            MeshFilter filter = motion.GetComponentInChildren<MeshFilter>(true);
            if (filter == null) return;
            SkinnedMeshRenderer skinned = filter.GetComponent<SkinnedMeshRenderer>();
            if (skinned == null || (!motion.AutomaticMultiPivotSkinning && HasValidSkin(skinned))) return;
            RestoreOriginalMeshRenderer(motion);
        }

        private static void EnsureSinglePivotWrapper(FDX_SecondaryMotion motion)
        {
            if (motion == null || motion.PivotGroups == null) return;
            FDX_SecondaryMotion.PivotGroup group = motion.PivotGroups.Find(item =>
                item != null && item.enabled && item.pivot != null);
            if (group == null || group.pivot == motion.transform.parent || !group.pivot.IsChildOf(motion.transform)) return;
            Transform pivot = group.pivot;
            Transform originalParent = motion.transform.parent;
            Undo.SetTransformParent(pivot, originalParent, "Move FDX Pivot Outside Mesh");
            Undo.SetTransformParent(motion.transform, pivot, "Parent Mesh To FDX Pivot");
            group.simulationAnchor = originalParent != null ? originalParent : pivot;
            EditorUtility.SetDirty(motion);
        }

        private static void PreparePivotsForSkinning(FDX_SecondaryMotion motion)
        {
            if (motion == null) return;
            foreach (FDX_SecondaryMotion.PivotGroup group in motion.PivotGroups)
            {
                Transform pivot = group != null ? group.pivot : null;
                if (pivot == null || !motion.transform.IsChildOf(pivot)) continue;
                Transform pivotParent = pivot.parent;
                Undo.SetTransformParent(motion.transform, pivotParent, "Detach Mesh From FDX Pivot");
                Undo.SetTransformParent(pivot, motion.transform, "Move FDX Pivot Into Mesh");
            }
        }

        private static void UnwrapPivotBeforeDelete(FDX_SecondaryMotion motion, Transform pivot)
        {
            if (motion == null || pivot == null || !motion.transform.IsChildOf(pivot)) return;
            Undo.SetTransformParent(motion.transform, pivot.parent, "Detach Mesh From Removed FDX Pivot");
        }

        private static void SetPivotLocalPositionPreservingMesh(FDX_SecondaryMotion motion, Transform pivot,
            Vector3 localPosition)
        {
            if (pivot == null) return;
            bool wrapsMotion = motion != null && motion.transform.IsChildOf(pivot);
            Vector3 motionPosition = wrapsMotion ? motion.transform.position : Vector3.zero;
            Quaternion motionRotation = wrapsMotion ? motion.transform.rotation : Quaternion.identity;
            pivot.localPosition = localPosition;
            if (!wrapsMotion) return;
            motion.transform.position = motionPosition;
            motion.transform.rotation = motionRotation;
        }

        private static void SetPivotWorldPositionPreservingMesh(FDX_SecondaryMotion motion, Transform pivot,
            Vector3 worldPosition)
        {
            if (pivot == null) return;
            bool wrapsMotion = motion != null && motion.transform.IsChildOf(pivot);
            Vector3 motionPosition = wrapsMotion ? motion.transform.position : Vector3.zero;
            Quaternion motionRotation = wrapsMotion ? motion.transform.rotation : Quaternion.identity;
            pivot.position = worldPosition;
            if (!wrapsMotion) return;
            motion.transform.position = motionPosition;
            motion.transform.rotation = motionRotation;
        }

        private void ResetEverything()
        {
            if (!EditorUtility.DisplayDialog("全部重設", "要將此動態元件恢復成剛掛上腳本的狀態嗎？\n工具建立的軸心與控制點會移除，使用者骨架和模型不會被刪除。", "重設", "取消"))
                return;

            var motion = (FDX_SecondaryMotion)target;
            if (motion.AutomaticMultiPivotSkinning) RestoreOriginalMeshRenderer(motion);
            var generatedObjects = new HashSet<GameObject>();
            foreach (FDX_SecondaryMotion.PivotGroup group in motion.PivotGroups)
            {
                if (group == null) continue;
                if (group.pivot != null && motion.transform.IsChildOf(group.pivot))
                {
                    Transform wrapper = group.pivot;
                    UnwrapPivotBeforeDelete(motion, wrapper);
                    if (wrapper.name.IndexOf("_FDX_Pivot", StringComparison.OrdinalIgnoreCase) >= 0)
                        generatedObjects.Add(wrapper.gameObject);
                }
                AddToolOwnedObject(motion, group.pivot, generatedObjects);
                if (group.replicatedPivots != null)
                    foreach (Transform replica in group.replicatedPivots) AddToolOwnedObject(motion, replica, generatedObjects);
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

        internal static void ConvertToStatic(FDX_SecondaryMotion motion)
        {
            if (motion == null) return;
            motion.StopPreview();
            if (motion.AutomaticMultiPivotSkinning) RestoreOriginalMeshRenderer(motion);
            var generated = new HashSet<GameObject>();
            foreach (FDX_SecondaryMotion.PivotGroup group in motion.PivotGroups)
            {
                if (group == null) continue;
                if (group.pivot != null && motion.transform.IsChildOf(group.pivot))
                {
                    Transform wrapper = group.pivot;
                    UnwrapPivotBeforeDelete(motion, wrapper);
                    if (wrapper.name.IndexOf("_FDX_Pivot", StringComparison.OrdinalIgnoreCase) >= 0) generated.Add(wrapper.gameObject);
                }
                AddToolOwnedObject(motion, group.pivot, generated);
                if (group.replicatedPivots != null)
                    foreach (Transform replica in group.replicatedPivots) AddToolOwnedObject(motion, replica, generated);
                foreach (FDX_SecondaryMotion.FlexibleEndPoint point in group.endPoints) CollectToolOwnedEndPoints(motion, point, generated);
            }
            foreach (GameObject item in RemoveNestedObjects(generated))
                if (item != null) Undo.DestroyObjectImmediate(item);
            Undo.DestroyObjectImmediate(motion);
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
            var motion = (FDX_SecondaryMotion)target;
            if (PruneEmptyGroups(motion)) serializedObject.Update();
            FDX_AttachmentManager relatedManager = GetRelatedManager(motion);
            if (GUILayout.Button(new GUIContent("顯示腳本檔案", "Locate this component script in the Project window")))
            {
                MonoScript script = MonoScript.FromMonoBehaviour(motion);
                Selection.activeObject = script;
                EditorGUIUtility.PingObject(script);
            }
            EditorGUILayout.BeginHorizontal();
            if (relatedManager != null && GUILayout.Button(new GUIContent("回到 Manager", "Select the related Attachment Manager")))
            {
                Selection.activeGameObject = relatedManager.gameObject;
                EditorGUIUtility.PingObject(relatedManager.gameObject);
                EditorGUILayout.EndHorizontal();
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button(new GUIContent("全部重設", "Reset this component to its initial state"))) ResetEverything();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            simulate.boolValue = GUILayout.Toggle(simulate.boolValue, "動態模擬", EditorStyles.miniButton);
            SerializedProperty previewAutoSway = settings.FindPropertyRelative("previewAutoSway");
            if (simulate.boolValue)
                previewAutoSway.boolValue = GUILayout.Toggle(previewAutoSway.boolValue, "預覽自動擺動", EditorStyles.miniButton);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("重設姿勢", "Reset Pose"))) motion.ResetSimulation();
            EditorGUILayout.EndHorizontal();
            DrawExclusiveOptions(forceSpace, "力場座標",
                new[] { "世界座標", "動作參考物件座標" },
                new[]
                {
                    "World Space：角色怎麼轉，重力仍然永遠向下，風也維持原本方向。",
                    "Reference Local Space：角色轉身或傾斜時，重力和風也會跟著角色一起轉。"
                });
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("運作來源", GUILayout.Width(70f));
            DrawSourceModeButton(FDX_SecondaryMotion.MotionSource.ExistingBones, "現有骨架鏈");
            DrawSourceModeButton(FDX_SecondaryMotion.MotionSource.AutomaticPivot, "自動旋轉軸心");
            bool customReference = motion.SimulationAnchor != null && !motion.SimulationAnchorAutomaticallyAssigned;
            bool customNext = GUILayout.Toggle(customReference, "自訂動作參考物件（進階）", EditorStyles.miniButton);
            if (customNext != customReference)
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(motion, "Change FDX Action Reference");
                motion.SimulationAnchorAutomaticallyAssigned = !customNext;
                if (customNext && motion.SimulationAnchor == null) motion.SimulationAnchor = motion.transform.parent;
                else if (!customNext) motion.RefreshAutomaticSimulationAnchor(true);
                serializedObject.Update();
            }
            if (customNext) EditorGUILayout.PropertyField(simulationAnchor, GUIContent.none, GUILayout.MinWidth(50f));
            EditorGUILayout.EndHorizontal();

            if ((FDX_SecondaryMotion.MotionSource)motionSource.enumValueIndex == FDX_SecondaryMotion.MotionSource.ExistingBones)
                DrawBoneChains();
            else
                DrawAutomaticPivot();

            DrawCentralMotionSettings(motion);

            EditorGUILayout.Space(6f);
            EditorGUILayout.PropertyField(explicitColliders, new GUIContent("指定碰撞器", "Explicit Colliders"), true);
            DrawGizmos();
            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                FDX_EditModePreviewDriver.InvalidateDiscovery();
                if (!motion.Simulate) motion.StopPreview();
                else motion.RebuildSimulation();
            }
            List<string> issues = motion.ValidateSetup();
            if (issues.Count > 0) EditorGUILayout.HelpBox(string.Join("\n", issues), MessageType.Warning);
            if (GUI.changed && motion.AutomaticMultiPivotSkinning) UpdateAutomaticMultiPivotSkinning(motion);
        }

        private void DrawCentralMotionSettings(FDX_SecondaryMotion motion)
        {
            EditorGUILayout.Space(4f);
            motionSettingsExpanded.boolValue = FDX_InspectorGUI.DrawContainedFoldout(
                motionSettingsExpanded.boolValue, new GUIContent("動態設定", "Motion Settings"));
            if (!motionSettingsExpanded.boolValue) return;

            globalMotionSettingsExpanded.boolValue = FDX_InspectorGUI.DrawContainedFoldout(
                globalMotionSettingsExpanded.boolValue, new GUIContent("全域動態設定", "Global Motion Settings"));
            if (globalMotionSettingsExpanded.boolValue)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                FDX_InspectorGUI.DrawMotionSettings(settings);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("複製設定", "Copy Settings")))
                {
                    serializedObject.ApplyModifiedProperties();
                    copiedSettings = new FDX_MotionSettings();
                    copiedSettings.CopyFrom(motion.Settings);
                    serializedObject.Update();
                }
                using (new EditorGUI.DisabledScope(copiedSettings == null))
                {
                    if (GUILayout.Button(new GUIContent("貼上設定", "Paste Settings")))
                    {
                        serializedObject.ApplyModifiedProperties();
                        Undo.RecordObject(motion, "Paste FDX Motion Settings");
                        motion.Settings.CopyFrom(copiedSettings);
                        motion.RebuildSimulation();
                        EditorUtility.SetDirty(motion);
                        serializedObject.Update();
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            if ((FDX_SecondaryMotion.MotionSource)motionSource.enumValueIndex !=
                FDX_SecondaryMotion.MotionSource.ExistingBones) return;
            for (int i = 0; i < boneChains.arraySize; i++)
            {
                SerializedProperty chain = boneChains.GetArrayElementAtIndex(i);
                if (!chain.FindPropertyRelative("useIndividualMotionSettings").boolValue) continue;
                SerializedProperty expanded = chain.FindPropertyRelative("motionSettingsExpanded");
                string name = chain.FindPropertyRelative("displayName").stringValue;
                expanded.boolValue = FDX_InspectorGUI.DrawContainedFoldout(expanded.boolValue,
                    new GUIContent((string.IsNullOrWhiteSpace(name) ? $"骨架鏈 {i + 1}" : name) + " 動態設定",
                        "Individual Motion Settings"));
                if (!expanded.boolValue) continue;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                FDX_InspectorGUI.DrawMotionSettings(chain.FindPropertyRelative("motionSettings"));
                EditorGUILayout.EndVertical();
            }
        }

        private static FDX_AttachmentManager FindRelatedManager(FDX_SecondaryMotion motion)
        {
            if (motion == null) return null;
            FDX_AttachmentManager manager = motion.GetComponentInParent<FDX_AttachmentManager>();
            if (manager != null && manager.FindAllMotionComponents().Contains(motion)) return manager;
            foreach (FDX_AttachmentManager candidate in Resources.FindObjectsOfTypeAll<FDX_AttachmentManager>())
                if (candidate != null && candidate.gameObject.scene.IsValid() && candidate.FindAllMotionComponents().Contains(motion))
                    return candidate;
            return null;
        }

        private FDX_AttachmentManager GetRelatedManager(FDX_SecondaryMotion motion)
        {
            double now = EditorApplication.timeSinceStartup;
            if (!relatedManagerCacheDirty && cachedRelatedManager != null) return cachedRelatedManager;
            if (!relatedManagerCacheDirty && now < nextRelatedManagerRefreshTime) return null;
            relatedManagerCacheDirty = false;
            nextRelatedManagerRefreshTime = now + 1d;
            cachedRelatedManager = FindRelatedManager(motion);
            return cachedRelatedManager;
        }

        private static void DrawExclusiveOptions(SerializedProperty property, string label,
            string[] labels, string[] tooltips)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(70f));
            for (int i = 0; i < labels.Length; i++)
            {
                bool selected = property.enumValueIndex == i;
                bool next = GUILayout.Toggle(selected, new GUIContent(labels[i], tooltips[i]), EditorStyles.miniButton);
                if (next && !selected) property.enumValueIndex = i;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSourceModeButton(FDX_SecondaryMotion.MotionSource value, string label)
        {
            bool selected = motionSource.enumValueIndex == (int)value;
            if (GUILayout.Toggle(selected, label, EditorStyles.miniButton) && !selected)
            {
                motionSource.enumValueIndex = (int)value;
                serializedObject.ApplyModifiedProperties();
                if (value == FDX_SecondaryMotion.MotionSource.AutomaticPivot) EnsureDefaultPivot();
                ((FDX_SecondaryMotion)target).RebuildSimulation();
                GUIUtility.ExitGUI();
            }
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
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            bool candidatesExpanded = EditorGUILayout.Foldout(motion.BoneCandidatesExpanded,
                new GUIContent($"骨架候選（{boneCandidates.Count}）", "Bone Candidates"), true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(motion, "Toggle Bone Candidates");
                motion.BoneCandidatesExpanded = candidatesExpanded;
            }
            if (GUILayout.Button(new GUIContent("掃描骨架候選", "Scan Bone Candidates"))) ScanBoneCandidates();
            using (new EditorGUI.DisabledScope(boneCandidates.Count == 0))
                if (GUILayout.Button(new GUIContent("全部加入", "Add All Candidates")))
                {
                    foreach (Transform candidate in boneCandidates.ToArray()) AddBoneChain(candidate);
                    GUIUtility.ExitGUI();
                }
            EditorGUILayout.EndHorizontal();
            if (candidatesExpanded) DrawBoneCandidateScannerContent(false);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            bool chainsExpanded = EditorGUILayout.Foldout(motion.BoneChainsExpanded,
                new GUIContent($"骨架鏈（{motion.BoneChains.Count}）", "Bone Chains"), true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(motion, "Toggle Bone Chains");
                motion.BoneChainsExpanded = chainsExpanded;
            }
            if (GUILayout.Button(new GUIContent("新增骨架鏈組", "Add Bone Chain"))) { AddBoneChain(null); GUIUtility.ExitGUI(); }
            using (new EditorGUI.DisabledScope(!CanClassify(null)))
                if (GUILayout.Button("自動分類")) { ClassifyChains(null); GUIUtility.ExitGUI(); }
            using (new EditorGUI.DisabledScope(!HasDirectGroups(null)))
                if (GUILayout.Button("取消此層分組")) { ResetGroupLevel(null); GUIUtility.ExitGUI(); }
            using (new EditorGUI.DisabledScope(motion.BoneChainGroups.Count == 0))
                if (GUILayout.Button("取消全部分組")) { CancelAllGroups(); GUIUtility.ExitGUI(); }
            EditorGUILayout.EndHorizontal();

            if (!chainsExpanded) return;
            DrawGroupChildren(null, 0);
            DrawUngroupedChains(null);

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
            SerializedProperty chainEnabled = chain.FindPropertyRelative("enabled");
            chainEnabled.boolValue = GUILayout.Toggle(chainEnabled.boolValue, GUIContent.none, GUILayout.Width(18f));
            GUILayout.Space(10f);
            SerializedProperty expanded = chain.FindPropertyRelative("expanded");
            Rect chainFoldoutRect = GUILayoutUtility.GetRect(14f, EditorGUIUtility.singleLineHeight, GUILayout.Width(14f));
            expanded.boolValue = EditorGUI.Foldout(chainFoldoutRect, expanded.boolValue, GUIContent.none, true);
            GUILayout.Label("群組名稱", GUILayout.Width(62f));
            EditorGUILayout.PropertyField(chain.FindPropertyRelative("displayName"), GUIContent.none, GUILayout.MinWidth(50f));
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(chain.FindPropertyRelative("root"), GUIContent.none, GUILayout.MinWidth(90f));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                ((FDX_SecondaryMotion)target).RefreshAutomaticSimulationAnchor(false);
                serializedObject.Update();
            }
            if (GUILayout.Button(new GUIContent("刪除", "Remove"), GUILayout.Width(50f)))
            {
                string id = chain.FindPropertyRelative("editorId").stringValue;
                serializedObject.ApplyModifiedProperties();
                var motion = (FDX_SecondaryMotion)target;
                Undo.RecordObject(motion, "Remove FDX Bone Chain");
                RemoveChainFromGroups(id);
                motion.BoneChains.RemoveAt(index);
                PruneEmptyGroups(motion);
                motion.RebuildSimulation();
                EditorUtility.SetDirty(motion);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
            if (!expanded.boolValue) { EditorGUILayout.EndVertical(); return; }
            EditorGUI.indentLevel += 2;
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
                SerializedProperty allBones = chain.FindPropertyRelative("allBonesSway");
                EditorGUILayout.PropertyField(allBones, new GUIContent("全部骨頭晃動", "Animate All Bones"));
                if (!allBones.boolValue)
                {
                    int maximum = FDX_SecondaryMotion.GetBoneChainDepth(((FDX_SecondaryMotion)target).BoneChains[index]);
                    SerializedProperty levels = chain.FindPropertyRelative("boneMotionLevels");
                    levels.intValue = EditorGUILayout.IntSlider(new GUIContent("骨頭晃動數量", "Animated Bone Levels"),
                        Mathf.Clamp(levels.intValue, 1, maximum), 1, maximum);
                }
            }
            DrawObjectList(chain.FindPropertyRelative("excludedBones"), "排除骨頭", typeof(Transform));
            EditorGUILayout.Slider(chain.FindPropertyRelative("influence"), 0f, 2f, new GUIContent("影響倍率", "Influence"));
            EditorGUILayout.Slider(chain.FindPropertyRelative("displayRadius"), 0.001f, 1f,
                new GUIContent("骨架鏈起點大小", "Bone Chain Root Size"));
            EditorGUILayout.PropertyField(chain.FindPropertyRelative("scaleGizmoByDepth"),
                new GUIContent("逐節縮小（每節 0.8 倍）", "Scale Each Bone By 0.8"));
            FDX_InspectorGUI.DrawAxisSettings(chain.FindPropertyRelative("axisSettings"), false);
            SerializedProperty individualMotion = chain.FindPropertyRelative("useIndividualMotionSettings");
            individualMotion.boolValue = GUILayout.Toggle(individualMotion.boolValue,
                "使用各別動態設定", EditorStyles.miniButton);
            EditorGUI.indentLevel -= 2;
            EditorGUILayout.EndVertical();
        }

        private void DrawGroupChildren(string parentId, int depth)
        {
            var motion = (FDX_SecondaryMotion)target;
            foreach (FDX_SecondaryMotion.BoneChainGroup group in motion.BoneChainGroups.ToArray())
            {
                if (group == null || !ParentIdsEqual(group.parentId, parentId)) continue;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                bool groupEnabled = GUILayout.Toggle(group.enabled, GUIContent.none, GUILayout.Width(18f));
                GUILayout.Space(10f);
                bool expanded = EditorGUILayout.Foldout(group.expanded,
                    $"{group.displayName}（{CountChainsRecursive(group.id)}）", true);
                if (EditorGUI.EndChangeCheck())
                {
                    ChangeMotionData("Toggle Bone Chain Group", _ =>
                    {
                        group.enabled = groupEnabled;
                        group.expanded = expanded;
                    }, true);
                }
                using (new EditorGUI.DisabledScope(!CanClassify(group.id)))
                    if (GUILayout.Button("自動分類")) { ClassifyChains(group.id); GUIUtility.ExitGUI(); }
                if (GUILayout.Button("取消此分組")) { ResetGroup(group.id); GUIUtility.ExitGUI(); }
                int oppositeState = GetOppositeGroupState(group.id);
                string oppositeLabel = oppositeState == 2 ? "補上另一側骨架" :
                    oppositeState == 1 ? "另一側骨架已加入" : "找不到另一側骨架";
                using (new EditorGUI.DisabledScope(oppositeState != 2))
                    if (GUILayout.Button(oppositeLabel)) { AddOppositeForGroup(group.id); GUIUtility.ExitGUI(); }
                if (GUILayout.Button("刪除群組骨架")) { DeleteGroupChains(group.id); GUIUtility.ExitGUI(); }
                EditorGUILayout.EndHorizontal();
                if (expanded)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20f + depth * 12f);
                    EditorGUI.BeginChangeCheck();
                    bool commonExpanded = EditorGUILayout.Foldout(group.commonSettingsExpanded, "共通設定", true);
                    bool soloPreview = GUILayout.Toggle(group.soloPreview, "單獨群組預覽", GUILayout.Width(112f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        ChangeMotionData("Change Bone Chain Group Preview", current =>
                        {
                            group.commonSettingsExpanded = commonExpanded;
                            if (soloPreview && !group.soloPreview)
                                foreach (FDX_SecondaryMotion.BoneChainGroup other in current.BoneChainGroups)
                                    if (other != null) other.soloPreview = false;
                            group.soloPreview = soloPreview;
                        }, true);
                    }
                    EditorGUILayout.EndHorizontal();
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
        }

        private void ChangeMotionData(string undoName, Action<FDX_SecondaryMotion> change, bool rebuild)
        {
            serializedObject.ApplyModifiedProperties();
            var motion = (FDX_SecondaryMotion)target;
            Undo.RecordObject(motion, undoName);
            change(motion);
            if (rebuild) motion.RebuildSimulation();
            EditorUtility.SetDirty(motion);
            serializedObject.Update();
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
            DrawCommonBool("全部骨頭晃動", indices, chain => chain.allBonesSway,
                (chain, value) => chain.allBonesSway = value);
            DrawCommonFloat("影響倍率", indices, chain => chain.influence,
                (chain, value) => chain.influence = value, 0f, 2f);
            DrawCommonFloat("骨架鏈起點大小", indices, chain => chain.displayRadius,
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
            List<string> ids = string.IsNullOrEmpty(parentId) ? GetUngroupedChainIds() : new List<string>(GetGroup(parentId).chainIds);
            Dictionary<string, List<string>> groups = BuildClassification(ids);
            if (groups.Count < 2) return;
            Undo.RecordObject(motion, "Classify FDX Bone Chains");
            if (!string.IsNullOrEmpty(parentId)) GetGroup(parentId).chainIds.Clear();
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
            serializedObject.ApplyModifiedProperties();
            var group = GetGroup(groupId);
            if (group == null) return;
            var motion = (FDX_SecondaryMotion)target;
            Undo.RecordObject(motion, "Reset FDX Bone Chain Group");
            FDX_SecondaryMotion.BoneChainGroup parent = GetGroup(group.parentId);
            if (parent != null) parent.chainIds.AddRange(group.chainIds);
            foreach (FDX_SecondaryMotion.BoneChainGroup child in motion.BoneChainGroups)
                if (child != null && child.parentId == group.id) child.parentId = group.parentId;
            motion.BoneChainGroups.Remove(group);
            PruneEmptyGroups(motion);
            EditorUtility.SetDirty(motion);
            serializedObject.Update();
        }

        private void ResetGroupLevel(string parentId)
        {
            serializedObject.ApplyModifiedProperties();
            var motion = (FDX_SecondaryMotion)target;
            List<FDX_SecondaryMotion.BoneChainGroup> direct = motion.BoneChainGroups.FindAll(
                group => group != null && ParentIdsEqual(group.parentId, parentId));
            Undo.RecordObject(motion, "Reset FDX Bone Chain Group Level");
            foreach (FDX_SecondaryMotion.BoneChainGroup group in direct) ResetGroupWithoutUndo(group);
            PruneEmptyGroups(motion);
            EditorUtility.SetDirty(motion);
            serializedObject.Update();
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
            serializedObject.ApplyModifiedProperties();
            var motion = (FDX_SecondaryMotion)target;
            Undo.RecordObject(motion, "Cancel All FDX Bone Chain Groups");
            motion.BoneChainGroups.Clear();
            EditorUtility.SetDirty(motion);
            serializedObject.Update();
        }

        internal static bool ParentIdsEqual(string first, string second) =>
            string.IsNullOrEmpty(first) ? string.IsNullOrEmpty(second) : first == second;

        private bool HasDirectGroups(string parentId) => ((FDX_SecondaryMotion)target).BoneChainGroups.Exists(
            group => group != null && ParentIdsEqual(group.parentId, parentId));

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

        internal static bool PruneEmptyGroups(FDX_SecondaryMotion motion)
        {
            var valid = new HashSet<string>();
            foreach (FDX_SecondaryMotion.BoneChain chain in motion.BoneChains)
                if (chain != null && chain.root != null) valid.Add(chain.editorId);
            bool changed = false;
            bool removed;
            do
            {
                removed = false;
                for (int i = motion.BoneChainGroups.Count - 1; i >= 0; i--)
                {
                    var group = motion.BoneChainGroups[i];
                    if (group == null) { motion.BoneChainGroups.RemoveAt(i); removed = changed = true; continue; }
                    if (group.chainIds == null) group.chainIds = new List<string>();
                    if (!group.chainIds.Exists(valid.Contains) &&
                        !motion.BoneChainGroups.Exists(child => child != null && child.parentId == group.id))
                    {
                        motion.BoneChainGroups.RemoveAt(i);
                        removed = changed = true;
                    }
                }
            } while (removed);
            if (changed) EditorUtility.SetDirty(motion);
            return changed;
        }

        private void DeleteGroupChains(string groupId)
        {
            if (!EditorUtility.DisplayDialog("刪除群組骨架", "移除此群組及子群組中的骨架鏈設定？場景中的骨頭會保留。", "刪除", "取消")) return;
            serializedObject.ApplyModifiedProperties();
            var motion = (FDX_SecondaryMotion)target;
            Undo.RecordObject(motion, "Delete FDX Group Bone Chains");
            List<int> indices = GetChainIndicesRecursive(groupId);
            indices.Sort();
            for (int i = indices.Count - 1; i >= 0; i--)
            {
                RemoveChainFromGroups(motion.BoneChains[indices[i]].editorId);
                motion.BoneChains.RemoveAt(indices[i]);
            }
            PruneEmptyGroups(motion);
            motion.RebuildSimulation();
            EditorUtility.SetDirty(motion);
        }

        private void AddOppositeForGroup(string groupId)
        {
            serializedObject.ApplyModifiedProperties();
            FDX_SecondaryMotion.BoneChainGroup group = GetGroup(groupId);
            if (group == null) return;
            var motion = (FDX_SecondaryMotion)target;
            List<int> indices = GetChainIndicesRecursive(groupId);
            int addedCount = 0;
            int skippedCount = 0;
            int missingCount = 0;
            for (int i = indices.Count - 1; i >= 0; i--)
            {
                int previousCount = motion.BoneChains.Count;
                int result = FindAndAddOppositeBone(indices[i], false);
                if (result > 0) addedCount++;
                else if (result == 0) skippedCount++;
                else missingCount++;
                if (motion.BoneChains.Count > previousCount)
                {
                    FDX_SecondaryMotion.BoneChain added = motion.BoneChains[motion.BoneChains.Count - 1];
                    if (!group.chainIds.Contains(added.editorId)) group.chainIds.Add(added.editorId);
                }
            }
            EditorUtility.SetDirty(motion);
            serializedObject.Update();
            EditorUtility.DisplayDialog("補上另一側骨架",
                $"新增：{addedCount}\n略過：{skippedCount}\n找不到：{missingCount}", "確定");
        }

        // 0: no counterpart can be found, 1: all found counterparts already exist, 2: at least one can be added.
        private int GetOppositeGroupState(string groupId)
        {
            var motion = (FDX_SecondaryMotion)target;
            bool foundCounterpart = false;
            foreach (int index in GetChainIndicesRecursive(groupId))
            {
                if (index < 0 || index >= motion.BoneChains.Count) continue;
                FDX_SecondaryMotion.BoneChain chain = motion.BoneChains[index];
                if (chain == null || chain.root == null) continue;
                string oppositeName = SwapSideName(chain.root.name);
                if (oppositeName.EndsWith("_Mirrored", StringComparison.Ordinal)) continue;
                Transform searchRoot = motion.SimulationAnchor != null ? motion.SimulationAnchor : motion.transform.root;
                Transform opposite = FindUniqueTransform(searchRoot, oppositeName);
                if (opposite == null) continue;
                foundCounterpart = true;
                bool alreadyAdded = motion.BoneChains.Exists(existing => existing != null && existing.root == opposite);
                if (!alreadyAdded) return 2;
            }
            return foundCounterpart ? 1 : 0;
        }

        private void DrawAutomaticPivot()
        {
            EditorGUILayout.Space(6f);
            var motion = (FDX_SecondaryMotion)target;
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            bool expanded = EditorGUILayout.Foldout(motion.PivotGroupsExpanded,
                new GUIContent($"旋轉軸心（{motion.PivotGroups.Count}）", "Rotation Pivots"), true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(motion, "Toggle FDX Pivot Groups");
                motion.PivotGroupsExpanded = expanded;
                EditorUtility.SetDirty(motion);
            }
            if (GUILayout.Button(new GUIContent("新增旋轉軸心", "Add Rotation Pivot")))
            {
                if (!ConfirmMultiPivotSkinning(motion)) { EditorGUILayout.EndHorizontal(); return; }
                AddPivotGroup();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
            if (!expanded) return;
            for (int i = 0; i < pivotGroups.arraySize && i < motion.PivotGroups.Count; i++)
                DrawPivotGroup(i, pivotGroups.GetArrayElementAtIndex(i), motion.PivotGroups[i]);

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
            var motion = (FDX_SecondaryMotion)target;
            SerializedProperty expanded = groupProperty.FindPropertyRelative("expanded");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            SerializedProperty pivotEnabled = groupProperty.FindPropertyRelative("enabled");
            pivotEnabled.boolValue = GUILayout.Toggle(pivotEnabled.boolValue, GUIContent.none, GUILayout.Width(18f));
            GUILayout.Space(10f);
            Rect pivotFoldoutRect = GUILayoutUtility.GetRect(14f, EditorGUIUtility.singleLineHeight, GUILayout.Width(14f));
            expanded.boolValue = EditorGUI.Foldout(pivotFoldoutRect, expanded.boolValue, GUIContent.none, true);
            GUILayout.Label("群組名稱", GUILayout.Width(62f));
            EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("displayName"), GUIContent.none, GUILayout.MinWidth(80f));
            SerializedProperty replicationMode = groupProperty.FindPropertyRelative("replicationMode");
            bool mirror = replicationMode.enumValueIndex == (int)FDX_SecondaryMotion.PivotReplicationMode.Mirror;
            bool mirrorNext = EditorGUILayout.ToggleLeft(new GUIContent("啟用鏡射", "Enable Mirror"), mirror);
            if (mirrorNext != mirror)
            {
                ChangeReplication(index, mirrorNext ? FDX_SecondaryMotion.PivotReplicationMode.Mirror :
                    FDX_SecondaryMotion.PivotReplicationMode.None);
                GUIUtility.ExitGUI();
            }
            bool radial = replicationMode.enumValueIndex == (int)FDX_SecondaryMotion.PivotReplicationMode.Radial;
            bool radialNext = EditorGUILayout.ToggleLeft(new GUIContent("啟用環狀複製", "Enable Radial Copies"), radial);
            if (radialNext != radial)
            {
                ChangeReplication(index, radialNext ? FDX_SecondaryMotion.PivotReplicationMode.Radial :
                    FDX_SecondaryMotion.PivotReplicationMode.None);
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("刪除", GUILayout.Width(45f)))
            {
                RemovePivotGroup(index, group);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
            if (!expanded.boolValue) { EditorGUILayout.EndVertical(); return; }

            if (replicationMode.enumValueIndex == (int)FDX_SecondaryMotion.PivotReplicationMode.Mirror)
            {
                SerializedProperty mirrorCenter = groupProperty.FindPropertyRelative("mirrorCenter");
                if (mirrorCenter.objectReferenceValue == null) mirrorCenter.objectReferenceValue = motion.transform;
                EditorGUILayout.PropertyField(mirrorCenter,
                    new GUIContent("對稱中心", "Mirror Center"));
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("對稱軸向（可複選）", GUILayout.Width(145f));
                DrawAxisToggleButton(groupProperty.FindPropertyRelative("mirrorX"), "X");
                DrawAxisToggleButton(groupProperty.FindPropertyRelative("mirrorY"), "Y");
                DrawAxisToggleButton(groupProperty.FindPropertyRelative("mirrorZ"), "Z");
                EditorGUILayout.EndHorizontal();
            }
            else if (replicationMode.enumValueIndex == (int)FDX_SecondaryMotion.PivotReplicationMode.Radial)
            {
                SerializedProperty radialCenter = groupProperty.FindPropertyRelative("mirrorCenter");
                if (radialCenter.objectReferenceValue == null) radialCenter.objectReferenceValue = motion.transform;
                EditorGUILayout.PropertyField(radialCenter,
                    new GUIContent("環狀中心", "Radial Center"));
                SerializedProperty radialAxis = groupProperty.FindPropertyRelative("radialAxis");
                radialAxis.enumValueIndex = EditorGUILayout.Popup(new GUIContent("環狀旋轉軸", "Radial Axis"),
                    radialAxis.enumValueIndex, new[] { "X 軸", "Y 軸", "Z 軸" });
                SerializedProperty copies = groupProperty.FindPropertyRelative("radialCopies");
                EditorGUILayout.IntSlider(copies, 1, 32, new GUIContent("環狀複製數量", "Additional Radial Copies"));
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.FloatField(new GUIContent("間隔角度", "Automatic Angle Spacing"), 360f / (copies.intValue + 1f));
            }

            EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("solo"), new GUIContent("單獨預覽", "Solo"));
            EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("pivot"), new GUIContent("旋轉軸心", "Rotation Pivot"));
            SerializedProperty automaticAnchor = groupProperty.FindPropertyRelative("automaticSimulationAnchor");
            EditorGUILayout.BeginHorizontal();
            bool customAnchor = GUILayout.Toggle(!automaticAnchor.boolValue, "自訂動作參考物件（進階）", EditorStyles.miniButton,
                GUILayout.MinWidth(150f), GUILayout.MaxWidth(210f));
            automaticAnchor.boolValue = !customAnchor;
            if (customAnchor) EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("simulationAnchor"), GUIContent.none);
            EditorGUILayout.EndHorizontal();
            FDX_InspectorGUI.DrawSoftMaxSlider(groupProperty.FindPropertyRelative("influenceRadius"), 0.001f, 0.5f,
                new GUIContent("軸心影響範圍", "Slider up to 0.5; type a larger value when needed"));
            FDX_InspectorGUI.DrawAxisSettings(groupProperty.FindPropertyRelative("axisSettings"));

            if (group.pivot == null && GUILayout.Button(new GUIContent("建立旋轉軸心", "Create Rotation Pivot"))) CreatePivot(index);
            if (group.pivot != null)
            {
                EditorGUILayout.BeginHorizontal();
                string editLabel = editingTip == group.pivot ? "結束編輯" : "編輯軸心位置";
                if (GUILayout.Button(new GUIContent(editLabel, "Edit Pivot Position"), GUILayout.MinWidth(100f)))
                    SetEditingTip(editingTip == group.pivot ? null : group.pivot);
                EditorGUI.BeginChangeCheck();
                Vector3 localPosition = group.pivot.localPosition;
                float previousLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 16f;
                localPosition.x = EditorGUILayout.FloatField(new GUIContent("X", "Drag to adjust X"), localPosition.x);
                localPosition.y = EditorGUILayout.FloatField(new GUIContent("Y", "Drag to adjust Y"), localPosition.y);
                localPosition.z = EditorGUILayout.FloatField(new GUIContent("Z", "Drag to adjust Z"), localPosition.z);
                EditorGUIUtility.labelWidth = previousLabelWidth;
                GUILayout.FlexibleSpace();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(group.pivot, "Edit FDX Pivot Position");
                    if (motion.transform.IsChildOf(group.pivot)) Undo.RecordObject(motion.transform, "Preserve FDX Mesh Position");
                    SetPivotLocalPositionPreservingMesh(motion, group.pivot, localPosition);
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
            if (group.pivot != null && group.replicationMode != FDX_SecondaryMotion.PivotReplicationMode.None &&
                GUILayout.Button(new GUIContent("複製軸心", "Create Independent Copies From The Current Pattern")))
            {
                DuplicateReplicatedPivots(index);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndVertical();
        }

        private static void DrawAxisToggleButton(SerializedProperty property, string label)
        {
            bool next = GUILayout.Toggle(property.boolValue, label, EditorStyles.miniButton, GUILayout.Width(42f));
            if (next != property.boolValue) property.boolValue = next;
        }

        private void ChangeReplication(int index, FDX_SecondaryMotion.PivotReplicationMode mode)
        {
            serializedObject.ApplyModifiedProperties();
            var motion = (FDX_SecondaryMotion)target;
            var group = motion.PivotGroups[index];
            var previous = group.replicationMode;
            Undo.RecordObject(motion, "Change FDX Pivot Replication");
            group.replicationMode = mode;
            if (mode != FDX_SecondaryMotion.PivotReplicationMode.None && !ConfirmMultiPivotSkinning(motion))
                group.replicationMode = previous;
            UpdateAutomaticMultiPivotSkinning(motion);
            if (!motion.AutomaticMultiPivotSkinning && CountEffectivePivots(motion) <= 1) EnsureSinglePivotWrapper(motion);
            motion.RebuildSimulation();
            EditorUtility.SetDirty(motion);
        }

        private bool ConfirmMultiPivotSkinning(FDX_SecondaryMotion motion)
        {
            if (motion == null || motion.AutomaticMultiPivotSkinning) return true;
            MeshFilter filter = motion.GetComponentInChildren<MeshFilter>(true);
            SkinnedMeshRenderer existingSkin = filter != null ? filter.GetComponent<SkinnedMeshRenderer>() : null;
            if (existingSkin != null && HasValidSkin(existingSkin)) return true;
            if (existingSkin != null) RestoreOriginalMeshRenderer(motion);
            MeshRenderer renderer = filter != null ? filter.GetComponent<MeshRenderer>() : null;
            if (filter == null || filter.sharedMesh == null || renderer == null)
            {
                EditorUtility.DisplayDialog("無法建立複數軸心", "找不到可轉換的 MeshFilter 與 MeshRenderer。", "確定");
                return false;
            }
            if (!EditorUtility.DisplayDialog("轉換為 SkinnedMeshRenderer",
                    "此物件需要轉換為 SkinnedMeshRenderer，才能讓多個軸心控制不同區域。是否轉換並繼續？",
                    "轉換並繼續", "維持單軸心")) return false;
            serializedObject.ApplyModifiedProperties();
            PreparePivotsForSkinning(motion);
            bool converted = ConvertToAutomaticMultiPivotSkinning(motion, filter, renderer);
            return converted;
        }

        private static bool ConvertToAutomaticMultiPivotSkinning(FDX_SecondaryMotion motion,
            MeshFilter filter, MeshRenderer renderer)
        {
            Mesh generated = null;
            SkinnedMeshRenderer skinned = null;
            string assetPath = null;
            Material[] originalMaterials = renderer != null ? renderer.sharedMaterials : Array.Empty<Material>();
            bool originalEnabled = renderer != null && renderer.enabled;
            try
            {
                generated = Instantiate(filter.sharedMesh);
                generated.name = filter.sharedMesh.name + "_FDX_MultiPivot";
                _ = generated.vertices;
                EnsureGeneratedAssetFolder();
                string safeName = string.Concat(motion.name.Split(Path.GetInvalidFileNameChars()));
                assetPath = AssetDatabase.GenerateUniqueAssetPath($"Assets/FDX_Generated/{safeName}_MultiPivot.asset");
                AssetDatabase.CreateAsset(generated, assetPath);
                generated = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);

                Undo.RecordObject(motion, "Convert FDX Multi Pivot Skinning");
                Undo.RecordObject(renderer, "Replace Original FDX Renderer");
                motion.OriginalMeshMaterials = originalMaterials;
                motion.OriginalMeshRendererEnabled = originalEnabled;
                motion.OriginalMeshFilter = filter;
                skinned = Undo.AddComponent<SkinnedMeshRenderer>(filter.gameObject);
                if (skinned == null) throw new InvalidOperationException("Unity could not create SkinnedMeshRenderer.");
                if (renderer != null) Undo.DestroyObjectImmediate(renderer);
                Undo.RecordObject(skinned, "Configure FDX Multi Pivot Renderer");
                skinned.sharedMesh = generated;
                skinned.sharedMaterials = originalMaterials;
                skinned.enabled = originalEnabled;
                skinned.updateWhenOffscreen = true;
                motion.OriginalMeshRenderer = null;
                motion.AutomaticMultiPivotRenderer = skinned;
                motion.DeformingRenderer = skinned;
                motion.AutomaticMultiPivotSkinning = true;
                UpdateAutomaticMultiPivotSkinning(motion, false);
                if (!HasValidSkin(skinned)) throw new InvalidOperationException("Generated skinning data is incomplete.");
                EditorUtility.SetDirty(motion);
                AssetDatabase.SaveAssets();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (skinned != null) Undo.DestroyObjectImmediate(skinned);
                MeshRenderer restored = filter != null ? filter.GetComponent<MeshRenderer>() : null;
                if (restored == null && filter != null) restored = Undo.AddComponent<MeshRenderer>(filter.gameObject);
                if (restored != null)
                {
                    restored.sharedMaterials = originalMaterials;
                    restored.enabled = originalEnabled;
                }
                motion.AutomaticMultiPivotRenderer = null;
                motion.AutomaticMultiPivotSkinning = false;
                motion.DeformingRenderer = null;
                motion.OriginalMeshFilter = null;
                motion.OriginalMeshRenderer = null;
                motion.OriginalMeshMaterials = Array.Empty<Material>();
                if (!string.IsNullOrEmpty(assetPath) && AssetDatabase.LoadAssetAtPath<Mesh>(assetPath) != null)
                    AssetDatabase.DeleteAsset(assetPath);
                else if (generated != null && !AssetDatabase.Contains(generated)) DestroyImmediate(generated);
                EditorUtility.SetDirty(motion);
                EditorUtility.DisplayDialog("無法轉換", "轉換未完成，已恢復一般 MeshRenderer。請確認模型 Read/Write 已開啟。", "確定");
                return false;
            }
        }

        private static bool RequiresAutomaticSkinning(FDX_SecondaryMotion motion)
        {
            return motion != null && (CountEffectivePivots(motion) > 1 || motion.EnablePreciseWeights ||
                motion.PivotGroups.Exists(group => group != null && group.enableAdvancedFlexible));
        }

        private static bool HasValidSkin(SkinnedMeshRenderer renderer)
        {
            if (renderer == null || renderer.sharedMesh == null) return false;
            Mesh mesh = renderer.sharedMesh;
            return mesh.vertexCount > 0 && mesh.boneWeights != null && mesh.boneWeights.Length == mesh.vertexCount &&
                   mesh.bindposes != null && mesh.bindposes.Length > 0 && renderer.bones != null && renderer.bones.Length > 0;
        }

        internal static void RepairManagedMotionSetups(FDX_AttachmentManager manager)
        {
            if (manager == null || Application.isPlaying) return;
            List<FDX_SecondaryMotion> motions = manager.FindAllMotionComponents();
            var conversions = new List<FDX_SecondaryMotion>();
            foreach (FDX_SecondaryMotion motion in motions)
            {
                if (motion == null || !RequiresAutomaticSkinning(motion)) continue;
                MeshFilter filter = motion.GetComponentInChildren<MeshFilter>(true);
                SkinnedMeshRenderer skin = filter != null ? filter.GetComponent<SkinnedMeshRenderer>() : null;
                if (!motion.AutomaticMultiPivotSkinning && (skin == null || !HasValidSkin(skin)) ||
                    motion.AutomaticMultiPivotSkinning && !HasValidSkin(motion.AutomaticMultiPivotRenderer))
                    conversions.Add(motion);
            }
            bool convert = conversions.Count == 0 || EditorUtility.DisplayDialog("修復多軸心物件",
                $"偵測到 {conversions.Count} 個多軸心物件尚未完成 SkinnedMeshRenderer 轉換。是否轉換並修復？",
                "轉換並修復", "略過 Mesh 轉換");

            foreach (FDX_SecondaryMotion motion in motions)
            {
                if (motion == null) continue;
                if (!RequiresAutomaticSkinning(motion))
                {
                    RepairStaleSinglePivotRenderer(motion);
                    continue;
                }
                if (motion.AutomaticMultiPivotSkinning && HasValidSkin(motion.AutomaticMultiPivotRenderer))
                {
                    UpdateAutomaticMultiPivotSkinning(motion, false);
                    continue;
                }
                MeshFilter filter = motion.GetComponentInChildren<MeshFilter>(true);
                if (filter == null || filter.sharedMesh == null || !convert) continue;
                SkinnedMeshRenderer skin = filter.GetComponent<SkinnedMeshRenderer>();
                if (skin != null && !HasValidSkin(skin)) RestoreOriginalMeshRenderer(motion);
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null) continue;
                PreparePivotsForSkinning(motion);
                ConvertToAutomaticMultiPivotSkinning(motion, filter, renderer);
            }
        }

        private static void EnsureGeneratedAssetFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/FDX_Generated"))
                AssetDatabase.CreateFolder("Assets", "FDX_Generated");
        }

        private static int CountEffectivePivots(FDX_SecondaryMotion motion)
        {
            int total = 0;
            foreach (FDX_SecondaryMotion.PivotGroup group in motion.PivotGroups)
            {
                if (group == null || !group.enabled || group.pivot == null) continue;
                if (group.replicationMode == FDX_SecondaryMotion.PivotReplicationMode.Mirror)
                {
                    int axes = (group.mirrorX ? 1 : 0) + (group.mirrorY ? 1 : 0) + (group.mirrorZ ? 1 : 0);
                    total += 1 << axes;
                }
                else if (group.replicationMode == FDX_SecondaryMotion.PivotReplicationMode.Radial)
                    total += 1 + Mathf.Max(1, group.radialCopies);
                else total++;
            }
            return total;
        }

        internal static void UpdateAutomaticMultiPivotSkinning(FDX_SecondaryMotion motion, bool allowRestore = true)
        {
            if (motion == null || !motion.AutomaticMultiPivotSkinning) return;
            motion.StopPreview();
            SynchronizeReplicatedPivots(motion);
            bool requiresSkinning = CountEffectivePivots(motion) > 1 || motion.EnablePreciseWeights ||
                motion.PivotGroups.Exists(group => group != null && group.enableAdvancedFlexible);
            if (!requiresSkinning)
            {
                if (allowRestore)
                {
                    RestoreOriginalMeshRenderer(motion);
                    return;
                }
            }

            MeshFilter filter = motion.OriginalMeshFilter;
            SkinnedMeshRenderer skinned = motion.AutomaticMultiPivotRenderer;
            if (filter == null || filter.sharedMesh == null || skinned == null || skinned.sharedMesh == null) return;
            var bones = new List<Transform> { motion.transform };
            var radii = new List<float> { float.PositiveInfinity };
            foreach (FDX_SecondaryMotion.PivotGroup group in motion.PivotGroups)
            {
                if (group == null || !group.enabled || group.pivot == null || bones.Contains(group.pivot)) continue;
                bones.Add(group.pivot);
                radii.Add(Mathf.Max(0.001f, group.influenceRadius));
                foreach (Transform replica in group.replicatedPivots)
                    if (replica != null && !bones.Contains(replica))
                    {
                        bones.Add(replica);
                        radii.Add(Mathf.Max(0.001f, group.influenceRadius));
                    }
            }
            Mesh mesh = skinned.sharedMesh;
            Matrix4x4[] bindPoses = new Matrix4x4[bones.Count];
            for (int i = 0; i < bones.Count; i++) bindPoses[i] = bones[i].worldToLocalMatrix * filter.transform.localToWorldMatrix;
            var weights = new BoneWeight[mesh.vertexCount];
            Vector3[] vertices = mesh.vertices;
            for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
            {
                Vector3 world = filter.transform.TransformPoint(vertices[vertexIndex]);
                int nearest = 0;
                float nearestNormalized = float.PositiveInfinity;
                for (int boneIndex = 1; boneIndex < bones.Count; boneIndex++)
                {
                    float normalized = Vector3.Distance(world, bones[boneIndex].position) / radii[boneIndex];
                    if (normalized >= nearestNormalized || normalized > 1f) continue;
                    nearestNormalized = normalized;
                    nearest = boneIndex;
                }
                weights[vertexIndex].boneIndex0 = nearest;
                weights[vertexIndex].weight0 = 1f;
            }
            Undo.RecordObject(mesh, "Update FDX Multi Pivot Weights");
            Undo.RecordObject(skinned, "Update FDX Multi Pivot Renderer");
            mesh.bindposes = bindPoses;
            mesh.boneWeights = weights;
            mesh.RecalculateBounds();
            skinned.rootBone = motion.transform;
            skinned.bones = bones.ToArray();
            EditorUtility.SetDirty(mesh);
            EditorUtility.SetDirty(skinned);
        }

        private static void SynchronizeReplicatedPivots(FDX_SecondaryMotion motion)
        {
            foreach (FDX_SecondaryMotion.PivotGroup group in motion.PivotGroups)
            {
                if (group == null) continue;
                if (group.replicatedPivots == null) group.replicatedPivots = new List<Transform>();
                var positions = new List<Vector3>();
                if (group.enabled) motion.GetReplicatedPivotPositions(group, positions);
                while (group.replicatedPivots.Count > positions.Count)
                {
                    int last = group.replicatedPivots.Count - 1;
                    Transform replica = group.replicatedPivots[last];
                    group.replicatedPivots.RemoveAt(last);
                    if (replica != null) Undo.DestroyObjectImmediate(replica.gameObject);
                }
                for (int i = 0; i < positions.Count; i++)
                {
                    Transform replica = i < group.replicatedPivots.Count ? group.replicatedPivots[i] : null;
                    if (replica == null)
                    {
                        var go = new GameObject($"{motion.name}_FDX_Replica_{i + 1}");
                        Undo.RegisterCreatedObjectUndo(go, "Create FDX Replicated Pivot");
                        Undo.SetTransformParent(go.transform, motion.transform, "Parent FDX Replicated Pivot");
                        replica = go.transform;
                        if (i < group.replicatedPivots.Count) group.replicatedPivots[i] = replica;
                        else group.replicatedPivots.Add(replica);
                    }
                    Undo.RecordObject(replica, "Update FDX Replicated Pivot");
                    replica.position = positions[i];
                    replica.rotation = group.pivot.rotation;
                    replica.localScale = Vector3.one;
                }
            }
        }

        private static void RestoreOriginalMeshRenderer(FDX_SecondaryMotion motion)
        {
            if (motion == null) return;
            MeshFilter filter = motion.OriginalMeshFilter != null ? motion.OriginalMeshFilter :
                motion.GetComponentInChildren<MeshFilter>(true);
            SkinnedMeshRenderer skinned = motion.AutomaticMultiPivotRenderer != null ?
                motion.AutomaticMultiPivotRenderer : filter != null ? filter.GetComponent<SkinnedMeshRenderer>() : null;
            if (filter == null || skinned == null) return;
            string assetPath = skinned != null && skinned.sharedMesh != null ? AssetDatabase.GetAssetPath(skinned.sharedMesh) : null;
            Material[] materials = motion.OriginalMeshMaterials != null && motion.OriginalMeshMaterials.Length > 0
                ? motion.OriginalMeshMaterials : skinned.sharedMaterials;
            bool rendererEnabled = motion.AutomaticMultiPivotSkinning ? motion.OriginalMeshRendererEnabled : skinned.enabled;
            Undo.RecordObject(motion, "Restore Original FDX Mesh");
            if (motion.DeformingRenderer == skinned) motion.DeformingRenderer = null;
            motion.AutomaticMultiPivotRenderer = null;
            motion.AutomaticMultiPivotSkinning = false;
            motion.OriginalMeshFilter = null;
            motion.OriginalMeshRenderer = null;
            motion.OriginalMeshMaterials = Array.Empty<Material>();
            Undo.DestroyObjectImmediate(skinned);
            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = Undo.AddComponent<MeshRenderer>(filter.gameObject);
            if (renderer != null)
            {
                Undo.RecordObject(renderer, "Restore Original FDX Renderer");
                renderer.sharedMaterials = materials;
                renderer.enabled = rendererEnabled;
            }
            if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith("Assets/FDX_Generated/", StringComparison.Ordinal))
                AssetDatabase.DeleteAsset(assetPath);
            if (!RequiresAutomaticSkinning(motion)) EnsureSinglePivotWrapper(motion);
            EditorUtility.SetDirty(motion);
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
                GUILayout.Space(20f);
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
            var motion = (FDX_SecondaryMotion)target;
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(new GUIContent("場景顯示", "Scene Gizmos"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showGizmos, new GUIContent("顯示輔助圖形", "Show Gizmos"));
            if (!showGizmos.boolValue) return;
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(showAllInfluenceRanges, new GUIContent("顯示全部影響範圍", "Show All Influence Ranges"));
            EditorGUILayout.PropertyField(showCollisionRadiusGizmos, new GUIContent("顯示碰撞半徑", "Show Collision Radius"));
            if (showCollisionRadiusGizmos.boolValue)
                EditorGUILayout.PropertyField(collisionRadiusColor, new GUIContent("碰撞半徑顏色", "Collision Radius Color"));
            if (motion.Source == FDX_SecondaryMotion.MotionSource.ExistingBones)
            {
                EditorGUILayout.PropertyField(boneRootColor, new GUIContent("骨架鏈起點顏色", "Bone Chain Root Color"));
                EditorGUILayout.PropertyField(endPointColor, new GUIContent("骨架顏色", "Bone Color"));
            }
            else
            {
                bool hasAdvancedPoints = motion.PivotGroups.Exists(group => group != null &&
                    group.enableAdvancedFlexible && group.endPoints != null && group.endPoints.Count > 0);
                bool hasReplication = motion.PivotGroups.Exists(group => group != null &&
                    group.replicationMode != FDX_SecondaryMotion.PivotReplicationMode.None);
                EditorGUILayout.PropertyField(pivotColor, new GUIContent("軸心顏色", "Pivot Color"));
                if (hasAdvancedPoints)
                    EditorGUILayout.PropertyField(endPointColor, new GUIContent("控制點顏色", "End Point Color"));
                if (hasReplication)
                    EditorGUILayout.PropertyField(mirrorColor, new GUIContent("複製預覽顏色", "Replication Preview Color"));
                FDX_InspectorGUI.DrawSoftMaxSlider(pivotGizmoRadius, 0.001f, 0.5f,
                    new GUIContent("軸心標記大小", "Slider up to 0.5; type a larger value when needed"));
                if (hasAdvancedPoints)
                    EditorGUILayout.Slider(endPointGizmoRadius, 0.001f, 1f, new GUIContent("控制點標記大小", "End Point Gizmo Radius"));
            }
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
                var motion = (FDX_SecondaryMotion)target;
                bool editingPivot = motion.PivotGroups.Exists(group => group != null && group.pivot == editingTip);
                if (editingPivot && motion.transform.IsChildOf(editingTip))
                    Undo.RecordObject(motion.transform, "Preserve FDX Mesh Position");
                if (editingPivot) SetPivotWorldPositionPreservingMesh(motion, editingTip, next);
                else editingTip.position = next;
                if (motion.AutomaticMultiPivotSkinning) UpdateAutomaticMultiPivotSkinning(motion);
                motion.RebuildSimulation();
                EditorUtility.SetDirty(editingTip);
                sceneView.Repaint();
            }
        }

        private void SetEditingTip(Transform next)
        {
            if (editingTip == null && next != null)
            {
                previousToolsHidden = Tools.hidden;
                Tools.hidden = true;
                toolsHiddenByInspector = true;
            }
            else if (editingTip != null && next == null)
            {
                Tools.hidden = previousToolsHidden;
                toolsHiddenByInspector = false;
            }
            editingTip = next;
            SceneView.RepaintAll();
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
            UpdateAutomaticMultiPivotSkinning(motion);
            if (!motion.AutomaticMultiPivotSkinning && CountEffectivePivots(motion) <= 1)
                EnsureSinglePivotWrapper(motion);
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
            int groupIndex = motion.PivotGroups.Count;
            var group = new FDX_SecondaryMotion.PivotGroup
            {
                displayName = $"旋轉軸心 {groupIndex + 1}",
                simulationAnchor = motion.transform.parent != null ? motion.transform.parent : motion.transform
            };
            var go = new GameObject($"{motion.name}_FDX_Pivot_{groupIndex + 1}");
            Undo.RegisterCreatedObjectUndo(go, "Create FDX Pivot");
            Undo.SetTransformParent(go.transform, motion.transform, "Parent FDX Pivot");
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            group.pivot = go.transform;
            motion.PivotGroups.Add(group);
            UpdateAutomaticMultiPivotSkinning(motion);
            EditorUtility.SetDirty(motion);
            serializedObject.Update();
        }

        private void RemovePivotGroup(int index, FDX_SecondaryMotion.PivotGroup group)
        {
            var motion = (FDX_SecondaryMotion)target;
            Undo.RecordObject(motion, "Remove FDX Pivot Group");
            if (editingTip == group.pivot) SetEditingTip(null);
            UnwrapPivotBeforeDelete(motion, group.pivot);
            motion.PivotGroups.RemoveAt(index);
            if (group.replicatedPivots != null)
                foreach (Transform replica in group.replicatedPivots)
                    if (replica != null) Undo.DestroyObjectImmediate(replica.gameObject);
            if (group.pivot != null && group.pivot != motion.transform &&
                group.pivot.name.IndexOf("_FDX_Pivot", StringComparison.OrdinalIgnoreCase) >= 0)
                Undo.DestroyObjectImmediate(group.pivot.gameObject);
            UpdateAutomaticMultiPivotSkinning(motion);
            if (!motion.AutomaticMultiPivotSkinning && CountEffectivePivots(motion) <= 1)
                EnsureSinglePivotWrapper(motion);
            EditorUtility.SetDirty(motion);
            serializedObject.Update();
        }

        private void DuplicateReplicatedPivots(int index)
        {
            serializedObject.ApplyModifiedProperties();
            var motion = (FDX_SecondaryMotion)target;
            if (index < 0 || index >= motion.PivotGroups.Count) return;
            var source = motion.PivotGroups[index];
            if (source == null || source.pivot == null) return;
            Transform center = source.mirrorCenter != null ? source.mirrorCenter : motion.transform;
            var positions = new List<Vector3>();
            motion.GetReplicatedPivotPositions(source, positions);
            if (positions.Count == 0) return;

            Undo.RecordObject(motion, "Duplicate FDX Pivot Pattern");
            Vector3 sourceOffset = source.pivot.position - center.position;
            for (int copyIndex = 0; copyIndex < positions.Count; copyIndex++)
            {
                string suffix = source.replicationMode == FDX_SecondaryMotion.PivotReplicationMode.Mirror
                    ? $"_Mirror_{copyIndex + 1}"
                    : $"_Radial_{copyIndex + 1}";
                var clone = new FDX_SecondaryMotion.PivotGroup
                {
                    dataVersion = 1,
                    displayName = source.displayName + suffix,
                    enabled = source.enabled,
                    expanded = source.expanded,
                    solo = false,
                    motionTarget = null,
                    automaticSimulationAnchor = source.automaticSimulationAnchor,
                    simulationAnchor = source.simulationAnchor,
                    influenceRadius = source.influenceRadius,
                    replicationMode = FDX_SecondaryMotion.PivotReplicationMode.None,
                    mirrorCenter = source.mirrorCenter,
                    enableAdvancedFlexible = source.enableAdvancedFlexible
                };
                clone.axisSettings.CopyFrom(source.axisSettings);

                var go = new GameObject(source.pivot.name + suffix);
                Undo.RegisterCreatedObjectUndo(go, "Create Replicated FDX Pivot");
                Undo.SetTransformParent(go.transform, source.pivot.parent, "Parent Replicated FDX Pivot");
                go.transform.position = positions[copyIndex];
                Vector3 destinationOffset = positions[copyIndex] - center.position;
                Quaternion offsetRotation = sourceOffset.sqrMagnitude > 0.000001f && destinationOffset.sqrMagnitude > 0.000001f
                    ? Quaternion.FromToRotation(sourceOffset, destinationOffset)
                    : Quaternion.identity;
                go.transform.rotation = offsetRotation * source.pivot.rotation;
                go.transform.localScale = source.pivot.localScale;
                clone.pivot = go.transform;

                foreach (FDX_SecondaryMotion.FlexibleEndPoint point in source.endPoints)
                    clone.endPoints.Add(CloneEndPointRelative(point, go.transform));
                motion.PivotGroups.Add(clone);
            }
            EditorUtility.SetDirty(motion);
            UpdateAutomaticMultiPivotSkinning(motion);
            serializedObject.Update();
        }

        private static FDX_SecondaryMotion.FlexibleEndPoint CloneEndPointRelative(
            FDX_SecondaryMotion.FlexibleEndPoint source, Transform destinationParent)
        {
            var clone = new FDX_SecondaryMotion.FlexibleEndPoint
            {
                displayName = source.displayName,
                detectionRadius = source.detectionRadius,
                motionMultiplier = source.motionMultiplier,
                generatedSegments = source.generatedSegments,
                weightFalloff = source.weightFalloff != null
                    ? new AnimationCurve(source.weightFalloff.keys)
                    : AnimationCurve.Linear(0f, 0f, 1f, 1f),
                liveMirror = false,
                mirrorAxis = source.mirrorAxis
            };
            clone.axisSettings.CopyFrom(source.axisSettings);
            if (source.tip != null)
            {
                var go = new GameObject(source.tip.name);
                Undo.RegisterCreatedObjectUndo(go, "Create Replicated FDX End Point");
                Undo.SetTransformParent(go.transform, destinationParent, "Parent Replicated FDX End Point");
                go.transform.localPosition = source.tip.localPosition;
                go.transform.localRotation = source.tip.localRotation;
                go.transform.localScale = source.tip.localScale;
                clone.tip = go.transform;
            }
            Transform nextParent = clone.tip != null ? clone.tip : destinationParent;
            foreach (FDX_SecondaryMotion.FlexibleEndPoint child in source.children)
                clone.children.Add(CloneEndPointRelative(child, nextParent));
            return clone;
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

        private int FindAndAddOppositeBone(int chainIndex, bool showFailure = true)
        {
            var motion = (FDX_SecondaryMotion)target;
            if (chainIndex < 0 || chainIndex >= motion.BoneChains.Count) return -1;
            FDX_SecondaryMotion.BoneChain source = motion.BoneChains[chainIndex];
            if (source == null || source.root == null) return -1;
            string oppositeName = SwapSideName(source.root.name);
            if (oppositeName.EndsWith("_Mirrored", StringComparison.Ordinal))
            {
                if (showFailure) EditorUtility.DisplayDialog("FDX", "無法從名稱判斷左右側。", "確定");
                return -1;
            }

            Transform searchRoot = motion.SimulationAnchor != null ? motion.SimulationAnchor : motion.transform.root;
            Transform opposite = FindUniqueTransform(searchRoot, oppositeName);
            if (opposite == null)
            {
                if (showFailure) EditorUtility.DisplayDialog("FDX", $"找不到唯一的對側骨架：{oppositeName}", "確定");
                return -1;
            }
            foreach (FDX_SecondaryMotion.BoneChain existing in motion.BoneChains)
                if (existing != null && existing.root == opposite) return 0;

            Undo.RecordObject(motion, "Add Opposite FDX Bone Chain");
            var clone = new FDX_SecondaryMotion.BoneChain
            {
                editorId = Guid.NewGuid().ToString("N"),
                displayName = SwapSideName(source.displayName),
                enabled = source.enabled,
                root = opposite,
                includeChildBones = source.includeChildBones,
                includeAllBranches = source.includeAllBranches,
                allBonesSway = source.allBonesSway,
                boneMotionLevels = source.boneMotionLevels,
                influence = source.influence,
                displayRadius = source.displayRadius,
                scaleGizmoByDepth = source.scaleGizmoByDepth,
                useIndividualMotionSettings = source.useIndividualMotionSettings,
                motionSettingsExpanded = source.motionSettingsExpanded,
                expanded = source.expanded,
                dataVersion = 1
            };
            clone.axisSettings.CopyFrom(source.axisSettings);
            clone.motionSettings.CopyFrom(source.motionSettings);
            motion.BoneChains.Add(clone);
            EditorUtility.SetDirty(motion);
            return 1;
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

        private void DrawBoneCandidateScannerContent(bool includeActions = true)
        {
            EditorGUILayout.Space(5f);
            if (includeActions)
            {
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
            }
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
            ScanBoneCandidatesUnder(motion, motion.transform);
            if (boneCandidates.Count == 0 && motion.SimulationAnchor != null &&
                motion.SimulationAnchor != motion.transform)
                ScanBoneCandidatesUnder(motion, motion.SimulationAnchor);
        }

        private void ScanBoneCandidatesUnder(FDX_SecondaryMotion motion, Transform root)
        {
            if (root == null) return;
            string[] keywords = { "skirt", "hair", "tail", "cloth", "accessory", "cape", "ribbon", "ear", "breast" };
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            var possible = new List<Transform>();
            var keywordMatches = new HashSet<Transform>();
            foreach (Transform candidate in transforms)
            {
                if (candidate == root || candidate.childCount == 0 || IsConfiguredRoot(motion, candidate)) continue;
                string lower = candidate.name.ToLowerInvariant();
                foreach (string keyword in keywords)
                {
                    if (!lower.Contains(keyword)) continue;
                    keywordMatches.Add(candidate);
                    break;
                }
                if (keywordMatches.Contains(candidate) || candidate.parent == root ||
                    (candidate.parent != null && candidate.parent.childCount > 1)) possible.Add(candidate);
            }

            foreach (Transform candidate in possible)
            {
                bool keep = true;
                foreach (Transform other in possible)
                {
                    if (other == candidate) continue;
                    if (other.IsChildOf(candidate) && !keywordMatches.Contains(candidate)) { keep = false; break; }
                    if (candidate.IsChildOf(other) && keywordMatches.Contains(other)) { keep = false; break; }
                }
                if (keep && !boneCandidates.Contains(candidate)) boneCandidates.Add(candidate);
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
            list.isExpanded = FDX_InspectorGUI.DrawContainedFoldout(list.isExpanded,
                new GUIContent($"{label}  {list.arraySize}", label));
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
