# FDX Attachment Motion

Unity 角色配件掛載、自然慣性擺動與快速柔性蒙皮工具。支援 Unity 能匯入的 Prefab、FBX、GLB 與一般場景物件，不依賴模型副檔名。

## 功能

- `FDX_AttachmentManager`：依 Humanoid 骨頭、直接 Transform 或名稱／相對路徑掛載 Prefab／場景物件。
- 統一或個別編輯掛載物件內的 `FDX_SecondaryMotion` 設定。
- 自動偵測 Animator 現有子階層內的 `FDX_SecondaryMotion`，即使掛載清單為空也能管理。
- 可在 Attachment Manager 勾選「編輯模式預覽」直接查看擺動效果。
- 無骨架物件可建立多組旋轉軸心，左右耳環等對稱配件可各自獨立模擬。
- 每組軸心可自動使用上層作為模擬基準，或手動指定不同基準與擺動目標。
- 現有骨架鏈只需指定第一根骨頭，即可遞迴包含子骨頭、分支、指定終點與排除骨頭；Hips 等模擬基準不會被修改。
- 可掃描裙擺、頭髮、尾巴等骨架候選，並將任一骨架鏈切為 Solo 單獨預覽。
- 尾端控制點可遞迴新增子控制點，並支援持續虛擬鏡像或一次性獨立鏡射複製。
- 每個軸心、骨架鏈與控制點皆可啟用或停用 X／Y／Z，並選擇統一或各軸獨立的慣性、彈力、阻尼與最大角度。
- 慣性、彈力、阻尼、旋轉上限、動畫混合、重力、風力、碰撞、持續外力與瞬間衝量。
- Scene Gizmo 顯示每個軸心、控制點、鏡像點、球形範圍與父子間圓柱影響區域。
- Inspector 可直接啟用控制點 Position Handle，不必切換 Hierarchy 選取。
- 支援動態設定複製／貼上、掛載管理器重新綁定與模擬重建。
- `FDX_RigWeightEditor` 提供自動蒙皮、頂點權重預覽、增加、減少、取代、平滑與正規化。
- 自動複製 Mesh，避免改寫原始 FBX／GLB 匯入資產。

## 安裝

### Unity Package Manager

在 Package Manager 選擇 **Add package from git URL**，輸入：

`https://github.com/Faidlix/unity-tool.git#release/fdx-attachment-motion-v1`

### `.unitypackage`

下載 Release 中的 `FDX_AttachmentMotion-1.2.0.unitypackage`，再以 Unity 匯入。

## 快速開始

1. 在角色 Animator 物件新增 `FDX_AttachmentManager`。
2. 在掛載清單選擇 Humanoid 骨頭、任意 Transform 或名稱／路徑，指定 Prefab／場景物件。
3. 在配件根物件新增 `FDX_SecondaryMotion`。
4. 純耳環等剛性物件使用「自動旋轉軸心」；左右分件可建立兩組軸心並分別指定擺動目標。
5. 需要槍尖、羽毛或長飾品彎曲時，勾選進階柔性設定並加入尾端控制點。
6. 由 `Tools > FDX > Attachment Motion > Rig & Weight Editor` 自動產生柔性骨架與權重。
7. 只有自動權重不足時才開啟精細權重筆刷修正。

## 現有裙擺／頭髮骨架

1. 將 `FDX_SecondaryMotion` 掛在 `Hips` 或適合的控制物件。
2. 選擇「現有骨架鏈（Existing Bone Chains）」。
3. 將 `Hips` 指定為「模擬基準（Simulation Anchor）」；基準本身不會參與擺動。
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
