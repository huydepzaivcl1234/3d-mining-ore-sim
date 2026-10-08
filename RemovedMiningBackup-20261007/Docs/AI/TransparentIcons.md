# Tao icon trong suot cho quai va item

Cong cu: Assets/Scripts/Ores/Editor/TransparentPrefabIcon.cs.

1. Tao/lua chon prefab cua quai trong Project (khong chon object trong Hierarchy).
2. Chuot phai -> Mining Simulator -> Export Transparent Icon.
3. Neu model toi nhu Forest Golem, chon Export Transparent Icon (Bright).
4. Luu PNG vao Assets/Resources/MiningMonsterIcons/TenPrefab.png.
5. Mo Assets/Resources/MonsterSpawnRoster.asset, tim entry cua quai va keo PNG vao Icon.

Cong cu tu import Sprite (2D and UI), Single, bat alphaIsTransparency va tat mipmap.
No chi render mesh/material, lay pose Idle neu co; khong render health bar, UI,
collider, VFX hoac chay gameplay trong SampleScene. Camera tu can giua model.
Khi ghi de PNG, cong cu hoi xac nhan va giu meta/GUID cu.

ForestGolem.png da duoc tao va gan vao roster. Bay icon nen xam cua 3 potion,
3 necklace va Mushranon da duoc render lai tu prefab goc voi alpha trong suot.
Goc chup/anh sang duoc render lai, khong phai cat nen pixel-identical. Sprite
tham chieu cu duoc giu. Cac frame UI, sprite nen panel, normal/base-color texture
va asset vendor khong bi sua. Scan game Sprite trong GameData/Resources/Prefabs
khong con icon co corner nen xam. Tam icon deu co alpha=0 o bon canh va co mesh
foreground; Forest entry co sprite. Console khong co loi bien dich moi.

Render bang Unity Editor goc; khong dung dich vu AI image hay tai asset ngoai.
Khong thay doi SampleScene, prefab gameplay, combat hay save data.
