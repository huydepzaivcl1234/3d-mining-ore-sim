# Tự làm reload rifle trong Blender

## Hai file nguồn đang dùng

- Nhân vật và skeleton: `D:/3d mining sim/Assets/Starter Assets/Runtime/ThirdPersonController/Character/Models/Armature.fbx`.
- Súng: `D:/3d mining sim/Assets/GameData/Player/Weapons/Low Poly ShotGun Weapon Pack 2/Models/Weapons/ShotGun_G.fbx`.
- Trong scene súng có tên `m4`, nhưng file model thực tế là `ShotGun_G.fbx`.
- Băng đạn là object riêng `ShotGun_G_Mag`, không cần tách mesh lần nữa.

Làm trên file `.blend` mới; không ghi đè hai FBX nguồn. Project hiện chưa khai báo Unity FBX Exporter trong manifest; không có bước Export To FBX sẵn được xác nhận. Các offset, pose tay lưu bằng tool Unity và IK runtime KHÔNG tự nằm trong FBX gốc. Import FBX vào Blender không có nghĩa là đã lấy được đúng pose Game View hiện tại: phải căn pose tham chiếu hoặc chuẩn bị bản export pose trước.

## 1. Nhập và giữ rig gốc

Trong Blender: File > Import > FBX, nhập nhân vật rồi nhập súng. Kiểm tra tỷ lệ tương đối. Giữ tên bone, cấu trúc skeleton, rest pose và scale của rig; không Apply Pose as Rest Pose. Lưu thành `RifleReload_Work.blend` ngoài Assets. Đây là file để tự chỉnh lâu dài.

## 2. Bố trí đồ vật và điều khiển

- Tay phải giữ cò/tay cầm; súng theo bone bàn tay phải bằng bone parenting hoặc constraint.
- Tay trái đỡ nòng khi idle, được nhả ra khi reload.
- Giữ băng đạn là object độc lập. Có thể tạo control cho súng, băng đạn mới và tay trái; không gộp băng đạn vào thân súng.
- Có thể dùng một băng cũ để minh họa rơi trong Blender và một băng mới để animate trong tay. Phần rơi runtime không bắt buộc bake vào clip tay.
- Đặt một camera tham chiếu gần góc FPS để kiểm tra tay/băng đạn có ra khỏi khung không. Camera tham chiếu không thay camera gameplay.

## 3. Tạo động tác

Tạo Action `Rifle_Reload`. Pose Mode: chọn bone cần đổi, chỉnh pose rồi Insert Keyframe cho Location/Rotation khi cần. Keyframe súng/băng đạn bằng control/object tương ứng, không chỉ keyframe tay.

Ví dụ timeline 30 FPS, 75 frame (2,5 giây), để bắt đầu tập — không phải timing lấy từ demo:

| Frame | Động tác |
|---|---|
| 0 | Pose cầm súng bình thường; khóa key khởi đầu |
| 10 | Nghiêng súng, tay trái rời chỗ đỡ |
| 18 | Băng cũ thoát khỏi miệng lắp và bắt đầu rơi |
| 30 | Tay trái lấy băng mới từ bên dưới khung hình |
| 45 | Đưa băng mới vào đúng trục và lắp hết vào súng |
| 58 | Nhấn/chốt băng; kéo khóa nòng nếu model có phần chuyển động phù hợp |
| 75 | Tay trái trở về chỗ đỡ; kết thúc trùng pose idle |

Chỉnh tốc độ/spacing trong Dope Sheet và Graph Editor: rút và nhấn nhanh, khoảng lấy băng chậm hơn một chút. Không dùng cùng một đường chuyển động thẳng đều cho toàn bộ reload. Có thể làm hai Action riêng: tactical reload và empty reload.

## 4. Băng cũ rơi như thế nào?

Đến mốc nhả băng, hệ thống runtime có thể:

1. Lấy đúng world position/rotation của băng cũ tại thời điểm nhả.
2. Hiện một bản băng rơi độc lập; ẩn băng đang ở súng.
3. Băng rơi chạy theo trọng lực; nếu cần va chạm đất thì dùng Rigidbody và collider đơn giản. Không cho tay tiếp tục kéo nó theo sau khi nhả.
4. Hiện băng mới theo tay trái, lắp tới socket rồi trở lại thuộc súng.
5. Thu hồi/ẩn bản băng rơi sau một thời gian; hủy reload cũng phải khôi phục đúng trạng thái.

Trong project đã có bản băng rơi tái sử dụng cho pistol, tính quỹ đạo trọng lực bằng code, KHÔNG có va chạm sàn. Không cần animate từng frame rơi trong Blender nếu muốn tái sử dụng cơ chế này. Rifle chưa được nối reload riêng.

## 5. Xuất sang Unity

Export FBX mới, bật Bake Animation; nếu dùng IK/constraints phải bake kết quả chuyển động sang bone/object xuất. Chỉ xuất Action cần dùng, kiểm tra lại FBX bằng một lần import thử. Không xuất đè model production.

Nhập clip tay trên rig Humanoid phù hợp và kiểm tra Avatar. Chuyển động object súng/băng đạn cần đúng hierarchy/path hoặc một rig cơ khí riêng; retarget Humanoid không tự gắn mọi đường cong object vào súng hiện tại. Có thể đặt Animation Events ở mốc nhả/lắp băng để gọi VFX/âm thanh; số đạn thật vẫn do code gameplay quản lý.

## 6. Điểm cần tích hợp với code hiện tại

RifleWeaponData.reloadProfile hiện null: nhấn R chỉ chờ timer và cộng đạn. PistolPresentation vẫn áp pose/IK khi chơi. Vì vậy chỉ kéo một FBX reload vào Animator sẽ chưa đủ: phải nhả/blend phần pose/IK đang giữ tay trong reload rồi trả về sau đó.

PistolReloadVisual hiện lấy clip điều khiển `WeaponOffset`, `MagazineOffset`, `Control`, không phải phát trực tiếp một clip Humanoid tùy ý. Nó còn yêu cầu cả magazinePath và slidePath hợp lệ; không trỏ slidePath vào cả thân rifle để qua kiểm tra. Chọn một trong hai hướng khi nối clip:

- Clip Humanoid tay + clip cơ khí, với quyền điều khiển tay/IK được blend theo trạng thái reload.
- Chuyển dữ liệu clip sang format target rig hiện có và chỉnh cơ chế để slide/bolt là tùy chọn cho rifle.

Hướng đầu phù hợp nếu bạn muốn tự vẽ toàn bộ động tác tay trong Blender. Chưa thay controller, nhập FBX mới hay tạo reload rifle trong lần sửa tracer này.

Tài liệu: [Blender FBX](https://docs.blender.org/manual/en/5.0/addons/import_export/scene_fbx.html), [Unity nhập Humanoid](https://docs.unity3d.com/6000.0/Documentation/Manual/ConfiguringtheAvatar.html), [Unity Animation Events](https://docs.unity3d.com/6000.0/Documentation/Manual/AnimationEventsOnImportedClips.html).
