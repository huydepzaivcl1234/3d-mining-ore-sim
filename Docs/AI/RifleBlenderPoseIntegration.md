# Rifle Blender -> Unity (2026-10-17)

Supersedes the earlier Receiver-only calibration. All 10 rifle mesh parts now come from the exported FBX. Game materials and the original full-body character remain; no detached arms, input changes or progression changes.

## Lan sau tu lam

1. Mo Rifle_FPS_UnityReady.blend. Num0 de xem camera. Chi BEN TRONG khung camera la hinh vao game, khong phai ca viewport. Giu FOV doc 60 do, ti le 16:9 nhu Unity.
2. Chon Rifle_Player, Pose Mode, chinh Shoulder/Arm/ForeArm/Hand/ngon bang R. Khong doi rest pose, ten xuong, scale rig hay Apply Pose as Rest Pose. Dat frame co dang cam ban muon. Luu file rieng truoc khi thu.
3. Scripting > Open Export_Rifle_To_Unity.py > Run Script. Script tao ban sao action, backup va thu muc UnityExport, khong ghi de file Blender goc. HOLD_ARM_POSE=True lay dang tay o frame dang xem ap xuyen suot idle, giu animation than/tho. Sua 1 frame khong con giat ve dang cu o frame khac.
4. Copy CA HAI Rifle_Unity_Idle.fbx va Rifle_Unity_Idle.json vao Assets/GameData/Player/Weapons/Rifle/BlenderAuthoring/, ghi de file cung ten (GIU .meta Unity). Dung Play Mode truoc.
5. Chon JSON trong Project > Chest Defense > Player > Apply Blender Rifle Export. Tool cap nhat prefab sung, Muzzle/AimPoint, grip hai tay, ngon, camera-relative offset va clip rifle. Tool tu choi neu FOV khac; khong tu luu scene.
6. Play, dung bang debug F8 chon rifle; thu dung yen/chay/nhin len xuong/ngam/ban/reload. Dung Play, review roi Ctrl+S de luu scene neu dong y. Khoi dau van pistol, mo rifle qua God nhu truoc.

## Nguyen nhan khac Blender

Chinh camera/offset thoi chua du: Humanoid retarget thay doi tay trai/ngon tay; gun mesh parts cu khong dong bo; pose tay chinh rieng frame 47 lam idle giat ve pose cu. Pipeline moi xuat geometry, camera, grip, huong ngon tay va ap hold xuyen suot idle.

## Pham vi

- Chi rifle idle/hold duoc chuyen lan nay. Pistol giu nguyen. ADS, recoil, reload dung gameplay hien co.
- HOLD_ARM_POSE cho idle, KHONG dung xuat reload: no khoa dang tay/ngon. Blender reload can action rieng co chuyen dong tay/ngon/bang dan va duong import rieng; lan nay chua tich hop action reload moi.
- Pose asset luu 30 finger poses co dinh cho grip, khong phai animation mo/ngam ngon theo thoi gian. Mesh gang tay/material game khac mannequin xanh Blender, khong pixel-identical.
- Doi rig/rest pose/ten part/FOV can hieu chinh lai. Exporter cho Rifle_Player / Rifle_Grip_Idle hien tai, khong phai exporter moi vu khi.
- Runtime useAnimationPose/useAuthoredSupport/useAuthoredFingers chi bat cho rifle. Scene khong autosave. Backup o workspace AgentPreviews/BeforeFullBlenderRifle va UnityExport/Backups.

## Validation

18 rendered-frame checks passed after fixing the frame-47 spike: idle framing/loop, support grip, pitch/yaw, run/strafe grip, ADS, firing/recoil, reload, pistol fallback, frames advancing. Sample at URP endCameraRendering after LateUpdate, not intermediate Update-time bones. Source parity additionally compares projected mesh vertices, independent Blender wrist landmarks and finger directions. Reports/screenshots live outside Assets. These are bounded checks, not a complete game playtest or build audit.
