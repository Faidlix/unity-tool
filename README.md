# FDX Attachment Motion

Unity 角色配件掛載、自然慣性擺動與快速柔性蒙皮工具。支援 Unity 能匯入的 Prefab、FBX、GLB 與一般場景物件，不依賴模型副檔名。

## 功能

- `FDX_AttachmentManager`：掛在角色 Animator，依 Humanoid 骨頭或任意 Transform 掛載 Prefab／場景物件。
- 統一或個別編輯掛載物件內的 `FDX_SecondaryMotion` 設定。
- 無骨架物件可自動建立單一旋轉軸心，適合耳環、吊墜等剛性配件。
- 一個旋轉軸心可連接多個柔性尾端，依圓柱偵測範圍自動產生骨頭與網格權重。
- 慣性、彈力、阻尼、旋轉上限、重力、風力、碰撞、持續外力與瞬間衝量。
- Scene Gizmo 顯示軸心、尾端與圓柱形影響區域。
- `FDX_RigWeightEditor` 提供自動蒙皮、頂點權重預覽、增加、減少、取代、平滑與正規化。
- 自動複製 Mesh，避免改寫原始 FBX／GLB 匯入資產。

## 安裝

### Unity Package Manager

在 Package Manager 選擇 **Add package from git URL**，輸入：

`https://github.com/Faidlix/unity-tool.git#release/fdx-attachment-motion-v1`

### `.unitypackage`

下載 Release 中的 `FDX_AttachmentMotion-1.0.0.unitypackage`，再以 Unity 匯入。

## 快速開始

1. 在角色 Animator 物件新增 `FDX_AttachmentManager`。
2. 在掛載清單選擇 Humanoid 骨頭或任意 Transform，指定 Prefab／場景物件。
3. 在配件根物件新增 `FDX_SecondaryMotion`。
4. 純耳環等剛性物件保持 `Automatic Pivot` 即可。
5. 需要槍尖、羽毛或長飾品彎曲時，勾選進階柔性設定並加入尾端控制點。
6. 由 `Tools > FDX > Attachment Motion > Rig & Weight Editor` 自動產生柔性骨架與權重。
7. 只有自動權重不足時才開啟精細權重筆刷修正。

## 碰撞說明

碰撞採骨骼節點／骨骼鏈近似，用於減少頭髮、尾巴與配件穿過角色 Collider。它不是逐頂點布料碰撞；需要完整布料行為時應搭配 Unity Cloth 或專用軟體物理方案。

## 相容性

- 最低目標：Unity 2022.3 LTS
- 已驗證：Unity 6000.3.13f1
- 不需要 Animation Rigging 套件

## 授權

MIT License。詳見 `LICENSE.md`。
