# FDX Attachment Motion

Unity 角色配件掛載、自然慣性擺動與快速彎曲蒙皮工具。支援 Unity 能匯入的 Prefab、FBX、GLB 與一般場景物件，不依賴模型副檔名。

## 功能

- `FDX_AttachmentManager`：依 Humanoid 骨頭、直接 Transform 或名稱／相對路徑掛載 Prefab／場景物件。
- 管理器可摺疊「骨架掛物件設定」與偵測到的動態元件；同步設定直接位於元件清單標題列，停用同步時可逐一展開設定。
- 自動偵測 Animator 現有子階層內的 `FDX_SecondaryMotion`，即使掛載清單為空也能管理。
- 可在 Attachment Manager 勾選「編輯模式預覽」直接查看擺動效果。
- 無骨架物件可建立多個旋轉軸心，並以多軸鏡射或環狀複製預覽批次配置；需要獨立編輯時可一鍵複製成實體軸心。
- 每個軸心預設自動使用第一個不參與模擬的上層作為動作參考物件，也可手動指定。
- 現有骨架鏈只需指定第一根骨頭，可選擇全部骨頭晃動或限制晃動層數；Hips 等動作參考物件不會被修改。
- 可掃描裙擺、頭髮、尾巴等骨架候選，依相似名稱建立多層分類、套用共通設定、整組加入對側骨架，並將任一骨架鏈切為 Solo 單獨預覽。
- 尾端控制點可遞迴新增子控制點，並支援建立對稱軸心或一次性複製獨立的對稱軸心。
- 每個軸心、骨架鏈與控制點皆可啟用或停用 X／Y／Z，並選擇統一或各軸獨立的慣性、彈力、阻尼與最大角度。
- 慣性、彈力、阻尼、旋轉上限、動畫混合、重力、風力、碰撞、持續外力與瞬間衝量。
- Scene Gizmo 只顯示目前運作來源相關選項；骨架鏈起點使用獨立顏色，並可讓每一節標記依序縮小 0.8 倍。
- Inspector 可直接啟用控制點 Position Handle，不必切換 Hierarchy 選取。
- 支援動態設定複製／貼上、掛載管理器重新綁定與模擬重建。
- 距離模擬可選固定幀間隔或固定更新頻率，並提供低／中／高／自訂畫質；距離分界始終以公尺顯示。
- `FDX_RigWeightEditor` 提供自動蒙皮、頂點權重預覽、增加、減少、取代、平滑與正規化。
- 自動複製 Mesh，避免改寫原始 FBX／GLB 匯入資產。

## 安裝

### Unity Package Manager

在 Package Manager 選擇 **Add package from git URL**，輸入：

`https://github.com/Faidlix/unity-tool.git#release/fdx-attachment-motion-v1`

### `.unitypackage`

下載 Release 中的 `FDX_AttachmentMotion-1.4.0.unitypackage`，再以 Unity 匯入。

## 快速開始

1. 在角色 Animator 物件新增 `FDX_AttachmentManager`。
2. 在「骨架掛物件設定」選擇 Humanoid 骨頭、任意 Transform 或名稱／路徑，指定 Prefab／場景物件。
3. 在配件根物件新增 `FDX_SecondaryMotion`。
4. 純耳環等剛性物件使用「自動旋轉軸心」；左右對稱或環狀物件可用同一軸心建立複製配置。
5. 需要槍尖、羽毛或長飾品彎曲時，勾選進階彎曲設定並加入尾端控制點。
6. 由 `Tools > FDX > Attachment Motion > Rig & Weight Editor` 自動產生彎曲骨架與權重。
7. 只有自動權重不足時才開啟精細權重筆刷修正。

## 現有裙擺／頭髮骨架

1. 將 `FDX_SecondaryMotion` 掛在 `Hips` 或適合的控制物件。
2. 選擇「現有骨架鏈（Existing Bone Chains）」。
3. 「動作參考物件」會自動抓取各骨架鏈的共同上層（例如 `Hips`），也可手動改指定；參考物件本身不會參與擺動。
4. 每組裙擺只拖入第一根骨頭，例如 `Skirt_01.L`，子骨頭會自動遞迴加入。
5. 左右名稱符合 `.L/.R`、`_L/_R` 或 `Left/Right` 時，可使用「尋找並加入對側骨架」。

## 碰撞說明

碰撞採骨骼節點／骨骼鏈近似，用於減少頭髮、尾巴與配件穿過角色 Collider。它不是逐頂點布料碰撞；需要完整布料行為時應搭配 Unity Cloth 或專用軟體物理方案。

## 相容性

- 最低目標：Unity 2022.3 LTS
- 已驗證：Unity 6000.3.13f1
- 不需要 Animation Rigging 套件

## 授權

MIT License。詳見 `LICENSE.md`。
