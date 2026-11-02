# Camera và daily loot — 29/10/2026

Project đang dùng: `D:/3d mining sim`. Mọi thao tác Editor dùng Unity CLI, không dùng chuột/bàn phím.

## Camera

Cài đặt thực tế trước sửa: Motion Blur tắt, Depth of Field tắt, AA None, Rigidbody Interpolate. Vì vậy không tự đổi chất lượng đồ họa hoặc bật/tắt hàng loạt hậu kỳ.

`TopDownSurvivor` trước đây xoay trực tiếp Transform của Rigidbody ở LateUpdate và bám ngay theo mọi dịch chuyển của player. Đã chuyển xoay thân sang `Rigidbody.MoveRotation` trong FixedUpdate; camera tiếp tục lấy vị trí render đã nội suy ở LateUpdate, bám bằng SmoothDamp ngắn 0,085 giây. Khi teleport xa camera snap lại; khi pause không chạy phép smoothing với deltaTime = 0. Giữ orbit, zoom, góc hiện tại và chống xuyên vật cản.

Kịch bản tự động chạy ngang/đảo hướng 4,5 giây:

| Chỉ số camera | Trước | Sau |
|---|---:|---:|
| Frame đo | 845 | 935 |
| Tốc độ camera trung bình (m/s) | 8,373 | 8,187 |
| Tốc độ lớn nhất (m/s) | 54,592 | 8,315 |
| RMS thay đổi tốc độ giữa frame (m/s) | 21,759 | 0,021 |

Đây là telemetry của kịch bản local, không phải mức tăng FPS hay cam kết giống hoàn toàn cảm giác trên mọi màn hình. Các file `CameraProbe-Before.json` và `CameraProbe-After.json` lưu dữ liệu.

## Rương mỗi sáng

- Model và 6 texture lấy từ ZIP bạn cung cấp. `AssetReceipt.json` ghi SHA-256; không chạy script trong archive. DAE import thành mesh 7.637 vertex.
- Dựng material URP Lit với albedo, normal, AO, emission; đóng gói metallic/smoothness từ metallic và roughness gốc. Prefab khoảng 0,84 × 1,10 × 0,84 m.
- Một rương mỗi buổi sáng. Khi vào game đang là ban ngày, rương chưa mở của ngày đó xuất hiện lại gần player; vào ban đêm chờ sáng tiếp theo.
- Chọn ngẫu nhiên trong khoảng **7–10 m**, kiểm tra cả khoảng cách 3D sau khi chiếu xuống đất. Ưu tiên NavMesh, kiểm tra mặt đất và chỗ trống. Nếu chưa có vị trí hợp lệ thì thử lại sau 2 giây.
- Đến gần trong 1,6 m để tự mở panel chọn 1 trong 3 ability. Không yêu cầu phím mới.
- Lưu trạng thái đã mở theo ngày; thoát/vào không nhận thêm rương trong cùng ngày. Rương chưa mở giữ qua đêm, sáng mới thay bằng rương mới. Reset game cũng reset trạng thái daily loot.
- Nếu tất cả ability đã max thì không sinh phần thưởng vô hiệu.

## Rương lấp lánh

- Tỷ lệ xuất hiện mặc định **10%**, chỉnh được.
- Emission mạnh hơn, ánh sáng vàng và hạt ánh sao có alpha mờ dần.
- Chỉ lần chọn ability từ rương đó được tăng **20% tương đối** cho các lượt thử rarity cao hơn Common. Ví dụ Rare 3% → 3,6%, Uncommon 17% → 20,4%; không cộng 20 điểm phần trăm.
- Không sửa asset rarity dùng chung, không thay weight từng ability, không biến thành buff vĩnh viễn, không bảo đảm chắc chắn ra Rare. Các bảng Epic/Legendary trống vẫn bị bỏ qua đúng cơ chế cũ.

Chỉnh tại `Assets/GameData/Survivor/LootBox/DailyLoot.asset`: khoảng cách, tỷ lệ rương lấp lánh, mức thưởng rarity, bán kính nhận. Prefab: `DailyLootBox.prefab` trong cùng thư mục.

## Kiểm tra

`LootValidation.json`: tất cả ca cuối đạt. Bao gồm đêm không sinh mới, dawn sinh một rương, không lặp trong cùng sáng, không nhận từ xa, nhận ability khi đến gần, chỉ nhận một lần, lưu/load cùng ngày không farm lại, sáng sau sinh mới, culling hiện có vẫn được gắn, camera không NaN/di chuyển khi pause và zoom còn hoạt động.

Smoke test bản gameplay sau khi đóng sân khấu preview: compile thành công, Console 0 lỗi / 0 cảnh báo. Đã trả về Edit Mode: TopDownSurvivor và PlayerController đều enabled, camera mask khôi phục về toàn bộ layer, DailyLoot.asset được chọn sẵn. Profile tạm đã được bỏ.

50/50 vị trí tìm được trong kịch bản đều trong khoảng 7–10 m. Chạy 100.000 lượt normal và 100.000 lượt boosted: Rare quan sát được **3,023% / 3,566%**, phù hợp gate 3% / 3,6%. Asset cấu hình upgrade không đổi sau roll.

Ảnh `Shiny-Loot-Preview.png` đã mở và xem: model/texture hiển thị đúng, ánh sao dùng alpha. Ảnh chụp trong sân khấu tạm để cây không che vật mẫu, tắt hậu kỳ trên camera preview; không phải ảnh chứng minh vị trí spawn trong map. Toàn bộ camera/đèn/sàn preview bị bỏ khi thoát Play Mode.

Culling gameplay không bị xóa hoặc sửa. Chỉ tạm bỏ culling trên vật mẫu trong sân khấu chụp xa; trạng thái đó không lưu vào prefab hay scene.

Tests dùng profile tạm; không reset tiến độ người chơi. Bản sao source/scene trước sửa ở `Before/`. Chưa build standalone hoặc đánh giá cảm giác camera trên màn hình khác; shader và hạt mới cần đánh giá thêm trên máy mục tiêu yếu.
