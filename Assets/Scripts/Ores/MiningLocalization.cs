using System;
using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum MiningLanguage
    {
        English = 0,
        Vietnamese = 1
    }

    /// <summary>Owns the saved language choice and shared player-facing translations.</summary>
    public static class MiningLocalization
    {
        private const string LanguageSaveKey = "Mining.Language";

        private readonly struct TranslationPair
        {
            public TranslationPair(string english, string vietnamese)
            {
                English = english;
                Vietnamese = vietnamese;
            }

            public string English { get; }
            public string Vietnamese { get; }
        }

        private static readonly TranslationPair[] StaticUiTranslations =
        {
            new("MINING AREA", "KHU ĐÀO QUẶNG"),
            new("UPGRADES", "NÂNG CẤP"),
            new("INVENTORY", "TÚI ĐỒ"),
            new("MINER PROGRESS", "TIẾN TRÌNH THỢ MỎ"),
            new("BACK", "QUAY LẠI"),
            new("CANCEL", "ĐỂ SAU"),
            new("BUY NPC", "Mua npc"),
            new("EMPTY", "TRỐNG"),
            new("Right mouse: rotate • WASD: move", "Chuột phải: xoay • WASD: di chuyển")
        };

        private static MiningLanguage currentLanguage;
        private static bool initialized;

        public static event Action LanguageChanged;

        public static MiningLanguage CurrentLanguage
        {
            get
            {
                EnsureInitialized();
                return currentLanguage;
            }
        }

        public static bool IsEnglish => CurrentLanguage == MiningLanguage.English;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            currentLanguage = MiningLanguage.English;
            initialized = false;
            LanguageChanged = null;
        }

        public static string Text(string english, string vietnamese)
        {
            return IsEnglish ? english : vietnamese;
        }

        public static void ToggleLanguage()
        {
            SetLanguage(IsEnglish ? MiningLanguage.Vietnamese : MiningLanguage.English);
        }

        public static void SetLanguage(MiningLanguage language)
        {
            EnsureInitialized();
            if (currentLanguage == language)
            {
                return;
            }

            currentLanguage = language;
            PlayerPrefs.SetInt(LanguageSaveKey, (int)currentLanguage);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
        }

        public static void ApplyToHierarchy(Transform root)
        {
            if (root == null)
            {
                return;
            }

            TextMeshProUGUI[] labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI label in labels)
            {
                if (label != null)
                {
                    label.text = TranslateStaticText(label.text);
                }
            }
        }

        public static string GetUpgradeName(MiningUpgradeType type, string fallback)
        {
            return type switch
            {
                MiningUpgradeType.MoneyReward => Text("Increase money earned", "Tăng tiền nhận được thêm"),
                MiningUpgradeType.RareOreSpawn => Text("Increase rare ore chance", "Tăng tỉ lệ quặng hiếm"),
                MiningUpgradeType.OreDamage => Text("Increase ore damage", "Tăng sát thương lên quặng"),
                MiningUpgradeType.OreSpawnSpeed => Text("Increase ore spawn speed", "Tăng tốc độ spawn quặng"),
                MiningUpgradeType.NpcMoveSpeed => Text("Increase NPC move speed", "Tăng tốc độ di chuyển NPC"),
                MiningUpgradeType.NpcCapacity => Text("Increase miner capacity", "Tăng giới hạn thợ mỏ"),
                MiningUpgradeType.LuckyBlockReward => Text("Increase Lucky Block money", "Tăng tiền Lucky Block"),
                MiningUpgradeType.LuckyBlockDropChance => Text("Increase Lucky Block chance", "Tăng tỉ lệ Lucky Block"),
                MiningUpgradeType.NpcExperience => Text("Increase NPC experience", "Tăng kinh nghiệm NPC"),
                MiningUpgradeType.ItemDropChance => Text("Increase item drop chance", "Tăng tỉ lệ rơi vật phẩm"),
                _ => fallback
            };
        }

        public static string GetItemName(string itemId, string fallback)
        {
            return itemId?.ToLowerInvariant() switch
            {
                "apple" => Text("Apple", "Táo"),
                "banana" => Text("Banana", "Chuối"),
                "grape" => Text("Grape", "Nho"),
                "rare_gift_box" => Text("Rare Gift Box", "Hộp Quà Hiếm"),
                _ => fallback
            };
        }

        public static string GetItemDescription(string itemId, string fallback)
        {
            return itemId?.ToLowerInvariant() switch
            {
                "apple" => Text("Increases NPC damage for a limited time.", fallback),
                "banana" => Text("Increases money earned from ores and Lucky Blocks for a limited time.", fallback),
                "grape" => Text("Increases NPC move speed for a limited time.", fallback),
                "rare_gift_box" => Text(
                    "Open it to spin for a weighted money or item reward.", fallback),
                _ => fallback
            };
        }

        public static string GetEffectName(MiningItemEffectType effectType, bool shortened)
        {
            if (shortened)
            {
                return effectType switch
                {
                    MiningItemEffectType.NpcDamage => Text("NPC DMG", "DMG NPC"),
                    MiningItemEffectType.MoneyReward => Text("MONEY", "VÀNG"),
                    MiningItemEffectType.NpcMoveSpeed => Text("NPC SPEED", "TỐC NPC"),
                    _ => Text("BUFF", "BUFF")
                };
            }

            return effectType switch
            {
                MiningItemEffectType.NpcDamage => Text("NPC damage", "Sát thương NPC"),
                MiningItemEffectType.MoneyReward => Text("Money earned", "Vàng nhận được"),
                MiningItemEffectType.NpcMoveSpeed => Text("NPC move speed", "Tốc chạy NPC"),
                _ => Text("Effect", "Hiệu ứng")
            };
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            int saved = PlayerPrefs.GetInt(LanguageSaveKey, (int)MiningLanguage.English);
            currentLanguage = saved == (int)MiningLanguage.Vietnamese
                ? MiningLanguage.Vietnamese
                : MiningLanguage.English;
            initialized = true;
        }

        private static string TranslateStaticText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (value == "SETTINGS" || value == "ÂM THANH" || value == "CÀI ĐẶT" ||
                value == "CÀI ĐẶT ÂM THANH")
            {
                return Text("SETTINGS", "CÀI ĐẶT");
            }

            foreach (TranslationPair translation in StaticUiTranslations)
            {
                if (string.Equals(value, translation.English, StringComparison.Ordinal) ||
                    string.Equals(value, translation.Vietnamese, StringComparison.Ordinal))
                {
                    return IsEnglish ? translation.English : translation.Vietnamese;
                }
            }

            return value;
        }
    }
}
