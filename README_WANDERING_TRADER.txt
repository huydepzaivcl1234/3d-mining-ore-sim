WANDERING TRADER UI V2 - CACH CAI

1. Tat Play Mode trong Unity.
2. Giai nen ZIP vao thu muc goc project va chon Replace cac file trung ten.
3. Cho Unity compile xong, kiem tra Console khong co loi mau do.
4. Chon menu:
   Mining Simulator > Setup > Rebuild Wandering Trader Full Scene UI
5. Mo Hierarchy > Wandering Trader Panel de xem toan bo Offer Card va child.
6. Nhan Ctrl+S de luu SampleScene.

CHINH GIA TUNG ITEM

Chon asset trong Assets/GameData/Items, vi du Apple.asset hoac Banana.asset.
Trong muc Wandering Trader Values:

- Trader Buy Value: gia Coin cho 1 item tren trang mua.
- Trader Gem Buy Value: gia Gem cho 1 item tren trang mua.
- Trader Sell Value: so Gem nhan duoc khi ban 1 item.

Gia tren offer = gia cua 1 item x so luong offer.
Trang mua random item va random Coin/Gem sau moi lan restock.
Trang ban chi hien cac item nguoi choi dang co va luon tra Gem.

KY HIEU FONT

Mui ten, nut dong va tien te da dung ky tu ASCII (> / X / COIN / GEM),
nen khong con warning Unicode \u279C cua LiberationSans SDF.
