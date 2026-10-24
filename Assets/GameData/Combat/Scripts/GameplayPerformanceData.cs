using UnityEngine;

namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName="Mining Simulator/Game Data/Gameplay Performance")]
    public sealed class GameplayPerformanceData : ScriptableObject
    {
        [Tooltip("Khoảng vẽ camera thế giới, mét. 0 giữ thiết lập camera gốc. Không tắt AI, collider hoặc UI.")]
        [Min(0)] public float renderDistance=300f;
        [Header("Camera-frustum presentation / Ngoài khung camera")]
        public bool cullOffscreenActors = true;
        [Tooltip("Ngừng cập nhật pose xương ngoài khung nhìn; giữ state machine để đòn đánh/di chuyển không bị kẹt.")]
        public bool cullOffscreenAnimation = true;
        [Min(0)] public float actorBoundsPadding = .2f;
        [Header("Monster CPU / Tải AI quái")]
        [Tooltip("Giây giữa các lần tính đường/né vật cản. Di chuyển, đòn đánh và collider vẫn xử lý mỗi frame. 0 = tính mỗi frame như cũ.")]
        [Min(0)] public float monsterSteeringInterval=.05f;
        [Tooltip("Ngân sách tìm đường mỗi frame (ms). Công việc còn lại tiếp tục frame sau; 0 bỏ giới hạn. Một phép dò hoặc callback có thể vượt ngân sách.")]
        [Min(0)] public float navigationBudgetMilliseconds=2f;
        [Header("Spawn scheduling / Chia tải spawn")]
        public bool spreadSpawnsAcrossFrames=true;
        [Min(1)] public int maximumSpawnAttemptsPerFrame=2;
        [Tooltip("Ngừng bắt đầu spawn mới khi đã dùng hết ngân sách trong frame. Một lần Instantiate không thể bị ngắt giữa chừng.")]
        [Min(.1f)] public float spawnBudgetMilliseconds=2f;
        private static GameplayPerformanceData current;
        private static bool loaded;
        public static GameplayPerformanceData Current
        {
            get
            {
                if(!loaded){current=Resources.Load<GameplayPerformanceData>("GameplayPerformanceData");loaded=true;}
                return current;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache(){current=null;loaded=false;}
    }
}
