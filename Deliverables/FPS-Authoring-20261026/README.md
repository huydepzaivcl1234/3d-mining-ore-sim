# Ban Blender de ban tu chinh tay

File hien tai: FullBody_Editable.blend. Scene: FPS_AUTHORING.
Khong sua Unity, khong ghi de animation goc.

1. Trong vung 3D, bam N, chon tab FPS Author.
2. Mouse look: di chuot de nhin; chuot phai doi hip/ADS; Esc dung.
   Hoac keo Yaw/Pitch. Pitch duong la cui, am la ngua. ADS 0 thuong / 1 ngam.
3. Chon Both hands / weapon -> G de dich, R de xoay ca cum tay-sung.
4. Chon RightHand / LeftHand -> G/R chinh tung ban tay.
   RightElbow / LeftElbow -> G chinh huong khuyu.
   Nut chon van hoat dong khi cac hinh control bi an de khong che tam nhin.
5. Bam giu chuot giua de orbit model ngoai; Numpad 0 hoac FPS camera de ve FPS.
   Bat Show head neu muon xem dau o goc ngoai.
6. Spine/Chest/Upper chest/Neck va Follow down la cac thong so ban tu thu.
7. Ctrl+S luu ban .blend nay. Save adjustment JSON luu offset, KHONG dua vao Unity.
8. Neu mo lai khong co tab: Text Editor -> FPS_Controls.py -> Alt+P (Run Script).
   Khong can bat Auto Run toan cuc cho file khong tin cay.

Control offset ap dung cho moi goc nhin. Muon rieng tung goc, luu ban rieng hoac keyframe
control va cac gia tri Pitch/ADS tren timeline. Khong tu dong convert thanh code Unity.

Gioi han: day la preview IK hinh hoc tu pose idle/ADS da capture, khong phai Unity Animator,
va khong mo phong collision/sway/recoil/draw/reload dong trong scene preview.
Reload goc van o scene 04_Reload_Return_Right. Cac scene goc duoc giu nguyen.
Ngon tay khong bi tool chinh. Constrain reach giu hai tay trong tam voi.
Live OFF cho phep pose bones tu do; bat Live lai se reset pose bones. Hay dung CTRL_* de giu offset.

Kiem tra: 5 goc hip/ADS va yaw/pitch; hand target error < 0.001 mm; finger local delta < 0.000001.
VALIDATION: fail 0, warn 3 (khong phai simulation Unity; hint khoi tao tu snapshot;
can Run Script sau reopen neu auto-run tat). Da xem 4 render FPS va render world.
run_blender.py / generic mesh validator: NOT RUN, dung live Blender MCP, khong dung lai mesh.
Mouse math da test synthetic; chua thu phien mouse-look bang chuot vat ly.
