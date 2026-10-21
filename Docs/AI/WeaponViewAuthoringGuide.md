# Rifle: chỉnh hiển thị và tự làm reload

## Tool đã có

Chọn PlayerArmature > Pistol Presentation > Mở Weapon View Authoring.
Hoặc menu Chest Defense > Weapons > Weapon View Authoring.

1. Chọn Player Presentation và RifleViews.asset.
2. Thoát Animation Preview cũ, rồi bật preview của tool.
3. Chọn cả hai tay hoặc súng trong tay. W kéo, E xoay trực tiếp trong Scene.
4. Toggle ADS để chỉnh vị trí ngắm riêng. Slider nhìn lên/xuống và trái/phải
   chỉ phục vụ kiểm tra authoring; thoát preview sẽ khôi phục camera.
5. Cập nhật ảnh từ camera player để xem bố cục FPS. Đây là ảnh camera,
   không có HUD overlay; ảnh không tự refresh mỗi frame.
6. Lưu offset vào preset đã chọn để giữ thay đổi. Muốn bộ riêng cho vũ khí
   hoặc góc nhìn khác, nhập tên rồi Lưu thành preset MỚI.
7. Nạp preset để gán vào presentation. Ctrl+S lưu cấu hình scene khi bạn muốn.

Preset lưu cả hip và ADS, offset súng, hướng cổ tay và hướng khuỷu.
Grip/xương ngón tay gốc được giữ nguyên, không chụp lại xương đã bị IK solve.
Muốn chỉnh riêng grip ngón tay, dùng tool Lưu pose tay cũ với asset riêng,
sau đó tạo view preset từ pose đó.

Đây là tool authoring góc nhìn, KHÔNG tạo controller third-person, không tự
đổi camera khi chơi, không tự blend preset theo góc ngẩng/cúi. Preset được
nạp bằng tool; camera gameplay vẫn do controller hiện tại điều khiển.

## Custom reload: pose không phải animation

Không bật Record rồi xoay trực tiếp xương Humanoid trong Unity: Transform
curve đó không tương đương muscle animation của Humanoid.

Nếu muốn tự tạo chuyển động tay đầy đủ miễn phí, làm clip ở Blender:

1. Nhập đúng FBX character/armature gốc đang dùng và model súng, không đổi
   skeleton player sang Generic. Làm trong file .blend riêng, không sửa FBX gốc.
2. Tạo Action Rifle_Reload. Chọn 30 FPS, khoảng frame 1–73 (2.4 giây).
3. Trong Pose Mode, chỉnh tay và Insert Keyframe (Rotation; Location chỉ cho
   IK control/root có chủ đích). Tay phải giữ grip, tay trái rời súng.
4. Frame 1: pose cầm. 10: nghiêng súng. 19: rút băng. 37: lấy băng mới.
   49: lắp băng. 61: kéo bolt nếu model có. 73: trở lại pose cầm.
5. Dùng IK trong Blender để kéo bàn tay, rồi Bake Action với Visual Keying
   trên bản sao export. Giữ motion root tại chỗ. Băng đạn/bolt là cơ chế súng,
   không phải human bones; cần xuất/bake track của chúng riêng.
6. Xuất FBX animation, đặt Rig = Humanoid trong Unity và kiểm tra Avatar
   hợp lệ. Dùng Copy From Other Avatar chỉ khi skeleton xuất khớp avatar đó;
   khác skeleton thì tạo/map avatar riêng. Tắt Loop Time cho reload.
7. Preview clip trên player trước. Clip chưa tự nối với nút R của rifle.
   Khi tích hợp, pose/IK cầm thường phải nhường quyền cho reload; không được
   để saved grip ghi đè các frame đang rút/lắp băng. Ammo, hủy reload và thời
   điểm hoàn tất vẫn do gameplay quản lý, không giao cho animation event.

Tool hiện tại chưa có timeline kéo IK target để author full reload trong
Unity. Nó chỉ tạo/chỉnh pose hiển thị; không tuyên bố biến preset thành clip.

## Reload target clip cũ

PistolReloadProfile.targetClip là clip Generic điều khiển WeaponOffset,
MagazineOffset và Control, không phải clip Humanoid của tay. Hệ cũ còn tính
đường lấy/lắp băng bằng profile; không phải mọi translation curve của
MagazineOffset đều được dùng. Đừng gán trực tiếp FBX Humanoid reload vào
targetClip và đừng gán profile pistol lên m4 khi đường magazine/bolt khác.

Nguồn hướng dẫn:
- https://docs.blender.org/manual/en/latest/animation/keyframes/editing.html
- https://docs.unity3d.com/Manual/ConfiguringtheAvatar.html
