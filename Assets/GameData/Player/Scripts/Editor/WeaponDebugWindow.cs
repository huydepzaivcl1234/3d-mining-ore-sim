using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameData.Player.Editor
{
    public sealed class WeaponDebugWindow : EditorWindow
    {
        private Script.PlayerWeaponProgression player;
        private VisualElement controls;
        private Label status;
        private HelpBox result;
        private Toggle clipOnly, pauseSpawns;

        [MenuItem("Tools/Debug/Weapon Test _F8")]
        public static void Open() => GetWindow<WeaponDebugWindow>("Weapon Debug");

        public void CreateGUI()
        {
            minSize = new Vector2(320, 360);
            var root = rootVisualElement;
            root.Clear();
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/GameData/Player/Scripts/Editor/WeaponDebugWindow.uss");
            if (sheet) root.styleSheets.Add(sheet);
            root.AddToClassList("weapon-debug");
            root.Add(new Label("ĐỔI SÚNG TEST / WEAPON DEBUG"));
            root.Add(new HelpBox("Chỉ đổi tạm trong Play Mode; không lưu mở khóa rifle.\nTemporary selection only; no unlock is saved.", HelpBoxMessageType.Info));
            status = new Label { name = "weaponStatus" }; root.Add(status);
            controls = new VisualElement(); root.Add(controls);
            controls.Add(new Button(() => SelectWeapon(false)) { text = "Pistol", name = "pistol" });
            controls.Add(new Button(() => SelectWeapon(true)) { text = "Rifle (test)", name = "rifle" });
            controls.Add(new Button(() => { if (player) player.RefillAmmo(); Refresh(); }) { text = "Đầy đạn / Refill ammo", name = "refill" });
            clipOnly = new Toggle("Chỉ animation clip / No pose overrides");
            clipOnly.SetValueWithoutNotify(true);
            clipOnly.SetEnabled(false); // Runtime pose overrides have been removed, not temporarily bypassed.
            controls.Add(clipOnly);
            pauseSpawns = new Toggle("Tạm dừng spawn quái / Pause spawning");
            pauseSpawns.RegisterValueChangedCallback(e => MiningSimulator.Ores.MonsterSpawnZone.DebugSpawningPaused = e.newValue);
            controls.Add(pauseSpawns);
            controls.Add(new HelpBox("Pause chặn quái mới và triệu hồi; quái đang có vẫn hoạt động. Trạng thái debug reset khi Play lại.", HelpBoxMessageType.Info));
            controls.Add(new Button(ReturnToGame) { text = "Về Game / Resume", name = "resume" });
            result = new HelpBox("F8 mở bảng. Chọn súng rồi bấm Về Game để test.", HelpBoxMessageType.Info);
            root.Add(result);
            root.schedule.Execute(Refresh).Every(250);
            Refresh();
        }
        private void Refresh()
        {
            if (!EditorApplication.isPlaying) player = null;
            else if (!player) player = Object.FindAnyObjectByType<Script.PlayerWeaponProgression>();
            controls.SetEnabled(EditorApplication.isPlaying && player && player.isActiveAndEnabled);
            if (!player) { status.text = "Bấm Play và vào game trước / Enter Play Mode first."; return; }
            clipOnly.SetValueWithoutNotify(true);
            pauseSpawns.SetValueWithoutNotify(MiningSimulator.Ores.MonsterSpawnZone.DebugSpawningPaused);
            var gun = player.GetComponent<Script.PistolShooting>();
            status.text = gun && gun.Data ? $"{gun.Data.displayName}   {gun.Ammo} / {gun.Reserve}" : "Đang khởi tạo / Initializing…";
            if (focusedWindow == this) player.GetComponent<Script.PistolShooting>().SetCursorReleased(true);
        }
        private void SelectWeapon(bool rifle)
        {
            if (!EditorApplication.isPlaying || !player) return;
            bool ok = player.DebugSelect(rifle);
            if (ok) player.GetComponent<Script.PistolShooting>().SetEquipped(true);
            result.text = ok ? "Đã đổi súng tạm / Temporary weapon selected." : "Loadout chưa sẵn sàng. Kiểm tra pose, muzzle và animation layer.";
            result.messageType = ok ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
            Refresh();
        }
        private void ReturnToGame()
        {
            if (!EditorApplication.isPlaying || !player) return;
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            player.GetComponent<Script.PistolShooting>().SetCursorReleased(false);
        }
    }
}


