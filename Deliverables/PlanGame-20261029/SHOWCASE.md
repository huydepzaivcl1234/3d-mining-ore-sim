# Plan Game — triển khai ngày 29/10/2026

Project thực tế: `D:/3d mining sim`. Triển khai và chạy kiểm tra qua Unity CLI, không điều khiển chuột/bàn phím.

## Đã triển khai

- Giữ camera Mega Bonk đã chốt ở các lượt trước, zoom/orbit và di chuyển hiện có. Không đổi lại camera 85 độ trong dòng plan cũ.
- Auto attack không còn băng đạn hoặc khoảng nghỉ để nạp. Tốc đánh điều khiển nhịp bắn. Gỡ liên kết tốc đánh/máu từ nâng cấp gun cũ khỏi Survivor; giữ asset/source cũ trong project để không phá tham chiếu.
- XP vẫn tăng level nhưng không tự tăng HP hoặc sát thương. Giữ ID của các nâng cấp và dữ liệu save cũ; mở rộng save lên version 7, đọc được version 6.
- 11 loại chỉ số đang dùng, có kích thước đạn +1%; bán kính sát thương tại điểm trúng tăng theo kích thước. Hai ID nạp đạn/băng đạn được giữ để đọc save nhưng không xuất hiện trong bảng roll.
- Thẻ lấp lánh: mặc định 5%, chỉnh `shinyPercent` trên `SurvivorUpgradeData`. Ngoài mức tăng thường, cộng thêm 20% chỉ số gốc: các chỉ số dạng phần trăm cộng 20 điểm phần trăm; giáp/kháng phép/hồi máu cộng 20% giá trị gốc trong SurvivorCombat. Giá trị gốc bằng 0 thì phần cộng thêm tương ứng bằng 0.
- 20 năng lực, cap 99: 6 Common, 7 Uncommon, 7 Rare. Thẻ max không roll lại. Giữ cơ chế roll rarity từ trên xuống và weight trong từng bảng.

### Common

Đạn phân nhánh (chỉ khi có quái thứ hai); Động năng (chỉ tính tốc chạy nhận thêm); Giáp phản công; Nguồn sống; Cường hóa (20% phần công nhận thêm, không nhân công gốc); Thiêu đốt phép trong 3 giây.

### Uncommon

Lá chắn; Hất tung sau 10 lần trúng trong cửa sổ 3 giây; Hào quang phép mỗi 0,5 giây; Nảy đạn đúng một lần; Bất tử định kỳ 1 giây; Nhịp chiến đấu; Sức mạnh cộng công phẳng.

Khiên = 15% HP tối đa + 10 × cấp. Nhận/nâng thẻ hoặc hồi sinh cấp đầy khiên. Vỡ khiên bắt đầu bộ đếm 15 giây; hết thời gian hồi đầy, các hit trong thời gian chờ không đặt lại bộ đếm. Khiên còn một phần không tự hồi. Vòng quanh nhân vật thể hiện khiên/bất tử; stats footer hiển thị lượng khiên.

Hất tung đi theo quỹ đạo cao tối đa 4 m trong khoảng 1,2 giây, tạm ngưng điều khiển AI của quái, khi chạm đất gây 1% HP tối đa của quái + 5 × cấp. CharacterController vẫn tôn trọng vật cản/trần.

### Rare

Song đạn (viên thứ hai sát thương phép); Giáp thích nghi theo số quái khác nhau đánh trúng; Suy yếu 3 giây, không cộng dồn hoặc làm mới; Cứu nguy dưới/ngang 30% HP; Phản đòn mỗi 5 hit; Sinh lực chinh phạt; Hai lựa chọn.

Sinh lực chinh phạt theo đính chính của bạn: **mỗi 5 boss nhận máu tối đa**, không nhận công. Mỗi mốc cộng `1 + cấp` HP (cấp 1 cộng 2 HP); đếm boss và HP thưởng được lưu. Nâng cấp sau này không nhân lại các phần thưởng đã nhận.

Giáp thích nghi: mỗi quái khác nhau cộng 1 giáp và 1 kháng phép; sau 2 giây không nhận thêm hit bắt đầu giảm 1 stack/giây. Không xóa stack ngay khi quái chết.

Hai lựa chọn mở liên tiếp hai lượt chọn thẻ chỉ số, mỗi lượt có thể có thẻ lấp lánh. Skip bỏ qua lượt đang mở, không bỏ mất lượt thứ hai.

## Quy ước ở những chỗ plan chưa chỉ rõ

- Có “không băng đạn/nạp đạn” và “tăng sức chứa băng đạn” trong cùng tài liệu: ưu tiên yêu cầu bỏ băng đạn, không roll nâng cấp đó.
- Với công thức có giá trị mở đầu và “mỗi cấp thêm”, giá trị mở đầu là ở cấp 1, các cấp sau mới cộng phần tăng. Công thức ghi `X/cấp` dùng trực tiếp cấp hiện tại.
- Hào quang có bán kính gốc 1 m, tăng 0,25 m từ mỗi cấp sau; loại sát thương phép. Sát thương khi rơi và phản đòn dùng vật lý.
- Song đạn và Giáp thích nghi chưa có hệ số tăng theo cấp trong plan nên giữ hiệu ứng đã mô tả, không tự thêm hệ số.
- Epic/Legendary chưa có năng lực trong plan nên không tự tạo. Các bảng vẫn có trong Inspector để bạn bổ sung.
- Không thay đổi culling hoặc cơ chế đóng băng actor ngoài khung hình đang có.

## Ảnh đã xem

- `Rare-Offers.png` — preview Suy yếu, Cứu nguy, Sinh lực chinh phạt ở 1920×1080.
- `Uncommon-Offers.png` — preview Lá chắn, Hào quang, Nảy đạn ở 1920×1080.

Hai ảnh là preview ép danh sách để kiểm tra bố cục/mô tả, không phải bằng chứng xác suất roll. Đã mở và kiểm tra cả hai ảnh: mô tả nằm trong thẻ, bố cục/font hiện có được giữ. Icon mới tạm dùng bộ icon dự phòng hiện có.

## Kiểm tra

Lần chạy cuối: **51/51 kiểm tra đạt**, 1.002 frame game đã chạy trong kịch bản. Unity compile thành công; Console hiện tại 0 lỗi và 0 cảnh báo. Sau cleanup: Edit Mode, SurvivorAbilities đã gắn vào actor, 0 PistolShooting đang hoạt động, cameraAngle = 18°, SurvivorUpgradeData được chọn sẵn.

`Validation.json` ghi kết quả các ca runtime thực thi trên profile tạm:

- XP không tăng HP/damage; bỏ card nạp/băng; default data có đủ định nghĩa mới; rarity table đúng.
- Khiên hấp thụ, vỡ đếm 15 giây, không hồi sớm, hồi đầy; nâng cấp và hồi sinh cấp lại khiên.
- Bất tử bắt đầu/kết thúc; tăng tốc đánh; cộng công phẳng; Cường hóa chỉ nhân phần bonus; Động năng chỉ tính tốc chạy thêm.
- Kích thước đạn; viên đạn phép; nảy một lần không nảy tiếp; đạn phân nhánh không phát sinh khi chỉ có một quái.
- Suy yếu giảm sát thương, không chồng; giáp theo quái riêng biệt và giảm dần; hồi 50% HP trong 3 giây và hồi chiêu.
- Phản đòn; hào quang; hồi máu mỗi 5 giây; Giáp phản công; burn được áp; hút máu theo damage thực tế.
- Đủ 5 boss mới thưởng HP; lưu/đọc lại HP thưởng; đọc save v6 không lệch ID; cap 99.
- 10 hit hất tung lên khoảng 4 m, trở về mặt đất và gây sát thương rơi.
- Bắn liên tục qua hơn 9 phát không có khoảng nghỉ nạp đạn; chọn thật thẻ Hai lựa chọn mở đủ hai panel chỉ số.

Test dùng profile riêng và đối tượng tạm ở ngoài khu chơi, không reset save của bạn. Khi kết thúc trả về Edit Mode, bỏ profile test, timeScale = 1 và audio unpause.

Chưa build standalone, chưa test một run dài đến cấp 99 trên map thực tế và chưa tinh chỉnh hiệu ứng/icon riêng cho từng năng lực. Hệ số cân bằng ngoài những gì plan ghi vẫn cần bạn chơi thử.

## Chỗ chỉnh

- `Assets/GameData/Survivor/SurvivorUpgradeData.asset`: định nghĩa, bảng rarity, weight, multiplier, tỷ lệ lấp lánh.
- `Assets/GameData/Survivor/SurvivorCombat.asset`: chỉ số gốc hiện tại của bạn.
- `Assets/GameData/Survivor/Scripts/SurvivorAbilities.cs`: quy tắc năng lực mới.
- `Assets/GameData/Survivor/Scripts/SurvivorEnemyStatus.cs`: suy yếu/hất tung trên quái.
- `Before/`: bản sao những file/asset trước khi sửa. Các script triển khai lưu để tra cứu, không chạy lại mù trên project đã cập nhật.
