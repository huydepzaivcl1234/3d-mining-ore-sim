# Survivor UI / Monster Discovery — 2026-10-29

Live project: `D:/3d mining sim`. Scene: `Assets/Scenes/SampleScene.unity`.

## Đã sửa

- Modal nâng cấp dùng layout 1460 × 870, mỗi thẻ 1004 × 178 và gap 18 px, Anton cho heading, Roboto cho nội dung; icon lấy trực tiếp từ Figma. Bỏ viền dày và stripe cũ; bổ sung số trước → sau theo dữ liệu thật. Các nút chọn/bỏ qua và pause giữ nguyên luồng đang dùng.
- Bảng `Daily Monster Forecast` không còn được tạo. Loại quái mới spawn lần đầu trong phiên chạy được xếp hàng, hiện ở góc trên phải, xoay 360° trong khoảng 1 giây rồi ẩn. Boss có thông báo riêng. Một loại không lặp lại mỗi lần spawn.
- Preview dùng mesh được bake sau khi Animator có một frame để cập nhật. Bounds tính từ hình học, bỏ TMP/health label, dùng camera/RenderTexture và đèn point riêng ở xa bản đồ. Không clone gameplay scripts, collider hay AI vào preview.
- Culling giữ nguyên thuật toán. Chỉ thêm getter đọc trạng thái renderer trước khi bị culling, để có thể tạo preview mà không bật renderer hoặc tiếp tục mô phỏng của actor ngoài màn hình.
- `Assets/GameData/Survivor/SurvivorCombat.asset` là nguồn chỉ số active duy nhất. PlayerStats và SurvivorAutoCombat cùng trỏ asset này; combat không còn bị Player Core ghi đè. Đã chuyển 25 giá trị HP/regen/phòng thủ/XP/stamina/di chuyển/jump từ dữ liệu cũ bằng Unity SerializedObject. Giữ jump 300, regen 1, HP 50, health/level 1.
- Damage 20; attack interval 0.65 giây (~1.54 lần/s); range 13 m; projectile speed 22 m/s. Thẻ và buff được cộng sau nên số trong panel có thể khác chỉ số gốc.
- Asset PlayerCoreStatsData cũ và meta nằm ở `Retired/`, ngoài Assets. Type legacy giữ lại cho bản lưu lịch sử, bỏ khỏi menu tạo asset mới. Không xóa các hệ thống legacy khác chưa được yêu cầu trong lượt này.

## Bằng chứng đã kiểm tra

- `Unity-Upgrade-Final.png`: ảnh Game view thực tế, đã mở và xem trước bàn giao. Ba offer trong ảnh được cố định trong profile kiểm tra để đối chiếu layout common/uncommon/rare; lượt chơi thật vẫn quay ngẫu nhiên như cũ.
- `Figma-Reference.png`: node 2:3, https://www.figma.com/design/9GC36WP5jGDsYDEeM1fjzF?node-id=2-3
- `Contact-Sheet.png`: Figma và crop modal Unity cùng tỷ lệ, kèm năm model preview.
- `Monster-Discovery.png`, `Monster-Discovery-Rotated.png`: hai thời điểm popup đang xoay trong Game view.
- `Preview-*.png`, `SpeciesValidation.json`: cả Mushroom, Golem, Bat, Mushranon, Forest Golem đều có pixel model và không xếp hàng trùng.
- `Validation.json`: ba nút chọn tăng đúng cấp; bỏ qua trả timeScale 1; font đúng; một asset stat; popup xoay, ẩn sau một giây; không có bảng quái; các frame đã thực sự tiến trong Play Mode.
- `OffscreenValidation.json`: quái đang hidden + SimulationPaused vẫn có preview; renderer thế giới tiếp tục tắt, AI tiếp tục đứng yên.
- `ConfigurationValidation.json`: một asset SurvivorCombat, không có missing script; giá trị gốc đúng sau migration.
- `LifecycleValidation.json`: đã sửa và kiểm tra thoát Play Mode khi panel nâng cấp đang mở; không còn gọi coroutine trên UI Systems đã inactive.
- Console lượt chạy cuối: 0 Error, 0 Warning. Dự án có cảnh báo API obsolete từ code legacy khi compile; không mở rộng sửa toàn bộ cảnh báo ngoài phạm vi này.

## Giới hạn kiểm tra

- Đã kiểm tra Editor/Play Mode và ảnh 2560 × 1440, layout tham chiếu 1920 × 1080; chưa build standalone hoặc thử mọi tỉ lệ màn hình.
- Hook God Boss và minion đã nối và compile; chưa chạy trọn một trận God Boss.
- Preview giữ pose mesh để xoay, không chạy AI/animation chiến đấu trong ô thông báo.
- Số liệu, thẻ sở hữu và mô tả là nội dung động; khác các con số mẫu trong Figma. Mép bo/anti-aliasing uGUI khác rất nhẹ renderer của Figma.

Unity được trả về Edit Mode, timeScale 1, âm thanh bật; đã bỏ testStore và chọn SurvivorCombat trong Inspector. Test dùng profile riêng, không reset tiến độ người chơi.
