# FDX Attachment Motion

Unity 角色配件掛載、自然慣性擺動與快速彎曲蒙皮工具。支援 Unity 能匯入的 Prefab、FBX、GLB 與一般場景物件，不依賴模型副檔名。

## 功能

- `FDX_AttachmentManager`：只負責依 Humanoid 骨頭、直接 Transform 或名稱／相對路徑掛載 Prefab／場景物件。
- `FDX_SecondaryMotionManager`：只列出階層內掛有 `FDX_SecondaryMotion` 的物件，提供全域模擬開關及快速跳轉。
- 管理器的「裝備設定」可依已設定／未設定與動態／靜態切換，並以獨立摺疊項目管理多個來源物件。
- 自動管理同物件或上層 Animator；只有無法自動偵測時才顯示手動角色清單。
- Prefab 與現有場景物件使用各自保留的來源清單，位置設定可在整組共用與逐一設定之間切換。
- 可在 Secondary Motion Manager 使用「全域模擬擺動」；選 Manager 預覽全部，選單一動態元件時只預覽該元件。
- 無骨架物件可建立多個旋轉軸心；多軸鏡射或環狀配置會產生參與模擬與蒙皮的複製軸心，亦可複製為獨立設定。
- 動作參考物件預設自動取掛載骨頭或有效父物件；進階「自訂動作參考物件」才顯示手動指定欄。
- 耳環等配件會依參考物件的位置、角速度與旋轉加速度產生慣性；Scene 軸心箭頭可對照初始方向與目前旋轉。
- Secondary Motion 可單獨使用；沒有 Manager 時開啟動態模擬與預覽自動擺動即可預覽，有 Manager 時遵循即時模擬擺動開關。
- 現有場景裝備在編輯模式即成為掛點子物件，來源列可選取物件、切換全域設定及轉為動態／靜態。
- 可偵測骨架下的眼鏡、粒子及可見裝備根物件；純 Collider、純程式物件和角色 Body／Face 不自動列入，仍可手動指定來源。
- 現有骨架鏈只需指定第一根骨頭，可選擇全部骨頭晃動或限制晃動層數；Hips 等動作參考物件不會被修改。
- 可掃描裙擺、頭髮、尾巴等骨架候選，依相似名稱建立多層分類、套用共通設定、整組加入對側骨架，並將任一骨架鏈切為 Solo 單獨預覽。
- 尾端控制點可遞迴新增子控制點，並支援建立對稱軸心或一次性複製獨立的對稱軸心。
- 每個軸心、骨架鏈與控制點皆可啟用或停用 X／Y／Z，並選擇統一或各軸獨立的慣性、彈力、阻尼與最大角度。
- 慣性、彈力、阻尼、旋轉上限、擺動疊加程度、重力、風力、碰撞、持續外力與瞬間衝量。
- 全域與各骨架鏈動態設定可各自指定碰撞器；舊版共用清單會自動移轉至全域動態設定。
- Scene Gizmo 只顯示目前運作來源相關選項；骨架鏈起點使用獨立顏色，並可讓每一節標記依序縮小 0.8 倍。
- Inspector 可直接啟用控制點 Position Handle，不必切換 Hierarchy 選取。
- 支援動態設定複製／貼上，掛載變更會自動重綁與重建；「修復掛載」可隨時檢查舊版資料、多軸心 Renderer 與失效掛點。
- 距離模擬可選固定幀間隔或固定更新頻率，並提供低／中／高／自訂畫質；距離分界始終以公尺顯示。
- `FDX_RigWeightEditor` 提供自動蒙皮、頂點權重預覽、增加、減少、取代、平滑與正規化。
- 自動複製 Mesh，避免改寫原始 FBX／GLB 匯入資產。

## 安裝

### Unity Package Manager

在 Package Manager 選擇 **Add package from git URL**，輸入：

`https://github.com/Faidlix/unity-tool.git#release/fdx-attachment-motion-v1`

### `.unitypackage`

下載 Release 中的 `FDX_AttachmentMotion-1.8.1.unitypackage`，再以 Unity 匯入。

## 快速開始

1. 在角色 Animator、其子物件或獨立管理物件新增 `FDX_AttachmentManager`；它會同時建立獨立的 `FDX_SecondaryMotionManager`。
2. 在「裝備設定」選擇 Humanoid 骨頭、任意 Transform 或名稱／路徑，再加入 Prefab 或現有場景物件。
3. 在配件根物件新增 `FDX_SecondaryMotion`。
4. 純耳環等剛性物件使用「自動旋轉軸心」；左右對稱或環狀物件可用同一軸心建立複製配置。
5. 需要槍尖、羽毛或長飾品彎曲時，勾選進階彎曲設定並加入尾端控制點。
6. 由 `Tools > FDX > Attachment Motion > Rig & Weight Editor` 自動產生彎曲骨架與權重。
7. 只有自動權重不足時才開啟精細權重筆刷修正。

## 現有裙擺／頭髮骨架

1. 將 `FDX_SecondaryMotion` 掛在 `Hips` 或適合的控制物件。
2. 選擇「現有骨架鏈（Existing Bone Chains）」。
3. 動作參考物件自動使用掛載骨頭或骨架鏈上層（例如 `Hips`）；需要覆寫時啟用進階自訂選項。
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
