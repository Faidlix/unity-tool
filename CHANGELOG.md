# Changelog

## 1.1.0 - 2026-10-07

- Attachment Manager 現在會自動偵測 Animator 現有子階層內的 `FDX_SecondaryMotion`，不再要求先建立掛載清單項目。
- 掛點新增 Humanoid、直接 Transform、名稱／相對路徑三種模式，並處理重名、大小寫及 FBX 命名空間。
- 新增現有骨架鏈組、自動遞迴子骨頭、分支、指定終點、排除骨頭與左右對側搜尋。
- 新增遞迴子控制點、持續虛擬鏡像、獨立鏡射複製與鏡像自動權重骨架。
- 新增每節點 XYZ 鎖軸、統一／各軸動態設定、動畫混合、子步進及世界／基準本地力場。
- 新增控制點 Scene Position Handle、每節點球形範圍及父子圓柱 Gizmo。
- Inspector 全面改用中文（English）標示，Detection Radius 等範圍欄位改為 Slider。
- 新增姿勢重設、記錄目前預設姿勢與設定驗證。
- 新增骨架鏈 Solo、骨架候選掃描、動態設定複製／貼上與管理器重新綁定。
- 新增「編輯模式預覽（Edit Mode Preview）」，無需進入 Play Mode 即可預覽擺動並在關閉時還原姿勢。
- 修正子物件旋轉軸心只旋轉控制點、卻沒有帶動同物件 Mesh Renderer 的問題。
- 模擬會在 Animator 更新後保留最新基礎姿勢，再疊加 Secondary Motion。

## 1.0.0 - 2026-10-05

- 新增 Animator 骨頭／Transform 配件掛載管理器。
- 新增統一與個別 Secondary Motion 設定。
- 新增自動旋轉軸心、自然慣性、彈力、阻尼、重力、風力、碰撞與外力 API。
- 新增單軸心、多尾端、圓柱影響範圍、自動骨架與自動網格權重。
- 新增 Scene Gizmo 與精細權重編輯器。
- 新增 Unity 批次煙霧測試入口。
