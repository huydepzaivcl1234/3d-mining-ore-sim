using System;
using Lean.Localization;
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
        private const string LanguageNameSaveKey = "Mining.LanguageName";
        public const string EnglishLanguageName = "English";
        public const string VietnameseLanguageName = "Vietnamese";

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
            new("EMPTY", "TRỐNG")
        };

        private static MiningLanguage currentLanguage;
        private static string currentLanguageName;
        private static bool initialized;
        private static bool applyingLeanLanguage;

        public static event Action LanguageChanged;

        public static MiningLanguage CurrentLanguage
        {
            get
            {
                EnsureInitialized();
                return currentLanguage;
            }
        }

        public static bool IsEnglish => string.Equals(CurrentLanguageName, EnglishLanguageName,
            StringComparison.OrdinalIgnoreCase);

        /// <summary>The Lean Localization language name. New languages can use this without changing the legacy enum.</summary>
        public static string CurrentLanguageName
        {
            get
            {
                EnsureInitialized();
                return GetEffectiveLanguageName();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            currentLanguage = MiningLanguage.English;
            currentLanguageName = EnglishLanguageName;
            initialized = false;
            applyingLeanLanguage = false;
            LanguageChanged = null;
            LeanLocalization.OnLocalizationChanged -= HandleLeanLocalizationChanged;
            LeanLocalization.OnLocalizationChanged += HandleLeanLocalizationChanged;
        }

        /// <summary>Looks up an English phrase in the Lean CSV tables.</summary>
        public static string Text(string english)
        {
            EnsureInitialized();
            return LeanLocalization.GetTranslationText(GetPhraseName(english), english,
                replaceTokens: false) ?? english;
        }

        /// <summary>Uses a semantic key when identical English text has different meanings.</summary>
        public static string TextKey(string key, string englishFallback)
        {
            EnsureInitialized();
            return LeanLocalization.GetTranslationText(key, englishFallback,
                replaceTokens: false) ?? englishFallback;
        }

        // Compatibility for serialized pairs until their English keys are in the CSV.
        public static string Text(string english, string vietnamese)
        {
            EnsureInitialized();
            string fallback = string.Equals(GetEffectiveLanguageName(), VietnameseLanguageName,
                StringComparison.OrdinalIgnoreCase)
                ? vietnamese
                : english;
            return LeanLocalization.GetTranslationText(GetPhraseName(english), fallback,
                replaceTokens: false) ?? fallback;
        }

        public static void ToggleLanguage()
        {
            SetLanguage(IsEnglish ? MiningLanguage.Vietnamese : MiningLanguage.English);
        }

        public static void SetLanguage(MiningLanguage language)
        {
            SetLanguage(language == MiningLanguage.Vietnamese
                ? VietnameseLanguageName
                : EnglishLanguageName);
        }

        /// <summary>Changes language by Lean language name so additional languages can be added later.</summary>
        public static void SetLanguage(string languageName)
        {
            EnsureInitialized();
            languageName = string.IsNullOrWhiteSpace(languageName)
                ? EnglishLanguageName
                : languageName.Trim();
            if (string.Equals(currentLanguageName, languageName,
                StringComparison.OrdinalIgnoreCase))
            {
                // Lean may have initialized after this class. Always re-apply the
                // requested language so every LeanLocalization instance agrees.
                ApplyLeanLanguage();
                LanguageChanged?.Invoke();
                return;
            }

            currentLanguageName = languageName;
            currentLanguage = ToLegacyLanguage(languageName);
            PlayerPrefs.SetString(LanguageNameSaveKey, currentLanguageName);
            PlayerPrefs.SetInt(LanguageSaveKey, (int)currentLanguage);
            PlayerPrefs.Save();
            ApplyLeanLanguage();
            LanguageChanged?.Invoke();
        }

        /// <summary>Applies the saved choice after LeanLocalization has registered its sources.</summary>
        public static void InitializeRuntime()
        {
            EnsureInitialized();
            ApplyLeanLanguage();
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
                MiningUpgradeType.LuckyBlockReward => Text("Increase Lucky Block money", "Tăng tiền Lucky Block"),
                MiningUpgradeType.LuckyBlockDropChance => Text("Increase Lucky Block chance", "Tăng tỉ lệ Lucky Block"),
                MiningUpgradeType.ItemDropChance => Text("Increase item drop chance", "Tăng tỉ lệ rơi vật phẩm"),
                MiningUpgradeType.RegenIntervalReduction => Text("Reduce regeneration interval", "Giảm thời gian hồi máu"),
                MiningUpgradeType.HealingEffectiveness => Text("Increase healing effectiveness", "Tăng hiệu quả hồi máu"),
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
                "yellow_potion" => Text("Yellow Potion", "Lọ vàng"),
                "green_potion" => Text("Green Potion", "Lọ xanh"),
                "red_potion" => Text("Red Potion", "Lọ đỏ"),
                "necklace_red" => Text("Red Necklace", "Dây chuyền đỏ"),
                "necklace_green" => Text("Green Necklace", "Dây chuyền xanh"),
                "necklace_orange" => Text("Orange Necklace", "Dây chuyền cam"),
                _ => fallback
            };
        }

        public static string GetItemDescription(string itemId, string fallback)
        {
            return itemId?.ToLowerInvariant() switch
            {
                "apple" => Text("Restores 10 HP over 5 seconds.", "Hồi 10 HP trong 5 giây."),
                "yellow_potion" => Text("Temporarily increases player attack speed.", "Tạm thời tăng tốc độ đánh của người chơi."),
                "green_potion" => Text("Temporarily increases player experience received.", "Tạm thời tăng XP người chơi nhận được."),
                "red_potion" => Text("Temporarily increases player damage.", "Tạm thời tăng sát thương của người chơi."),
                "banana" => Text("Adds 5 movement speed for 3 minutes.", "Cộng 5 tốc chạy trong 3 phút."),
                "grape" => Text("Adds 5% maximum health for 3 minutes.", "Tăng 5% máu tối đa trong 3 phút."),
                "carrot" => Text("Adds 1% attack speed for 3 minutes.", "Tăng 1% tốc đánh trong 3 phút."),
                "ice_cream" => Text("Reduces incoming damage by 1% for 3 minutes.", "Giảm 1% sát thương nhận trong 3 phút."),
                "pea" => Text("Hits add 0.5% of monster maximum HP as true damage for 150 seconds.", "Đòn đánh thêm 0,5% HP tối đa của quái thành sát thương chuẩn trong 150 giây."),
                "rare_gift_box" => Text(
                    "Open it to spin for a weighted money or item reward.", fallback),
                _ => fallback
            };
        }

        public static string GetEffectName(MiningItemEffectType effectType, bool shortened)
        {
            switch (effectType)
            {
                case MiningItemEffectType.PlayerHealing: return Text("HEAL HP", "HỒI HP");
                case MiningItemEffectType.PlayerMoveSpeed: return Text("MOVE SPEED", "TỐC CHẠY");
                case MiningItemEffectType.PlayerMaxHealth: return Text("MAX HP", "HP TỐI ĐA");
                case MiningItemEffectType.PlayerDamageReduction: return Text("DAMAGE REDUCTION", "MIỄN THƯƠNG");
                case MiningItemEffectType.MonsterMaxHealthTrueDamage: return Text("MAX HP TRUE DAMAGE", "SÁT THƯƠNG CHUẨN % HP");
            }
            if (shortened)
            {
                return effectType switch
                {
                    MiningItemEffectType.NpcDamage => Text("NPC DMG", "DMG NPC"),
                    MiningItemEffectType.PlayerAttackSpeed => Text("ATTACK SPEED", "TỐC ĐÁNH"),
                    MiningItemEffectType.PlayerExperience => Text("PLAYER XP", "XP PLAYER"),
                    MiningItemEffectType.PlayerDamage => Text("PLAYER DMG", "DMG PLAYER"),
                    MiningItemEffectType.MoneyReward => Text("MONEY", "VÀNG"),
                MiningItemEffectType.NpcMoveSpeed => Text("NPC SPEED", "TỐC NPC"),
                MiningItemEffectType.MiningSpeed => Text("MINING SPEED", "TỐC ĐÀO"),
                MiningItemEffectType.OreLuckyCritical => Text("CRITICAL", "CHÍ MẠNG"),
                MiningItemEffectType.EventChance => Text("EVENT CHANCE", "TỈ LỆ SỰ KIỆN"),
                    _ => Text("BUFF", "BUFF")
                };
            }

            return effectType switch
            {
                MiningItemEffectType.NpcDamage => Text("NPC damage", "Sát thương NPC"),
                MiningItemEffectType.PlayerAttackSpeed => Text("Player attack speed", "Tốc độ đánh người chơi"),
                MiningItemEffectType.PlayerExperience => Text("Player XP received", "XP người chơi nhận được"),
                MiningItemEffectType.PlayerDamage => Text("Player damage", "Sát thương người chơi"),
                MiningItemEffectType.MoneyReward => Text("Money earned", "Vàng nhận được"),
                MiningItemEffectType.NpcMoveSpeed => Text("NPC move speed", "Tốc chạy NPC"),
                MiningItemEffectType.MiningSpeed => Text("Mining speed", "Tốc độ đào"),
                MiningItemEffectType.OreLuckyCritical => Text("Ore/Lucky critical damage", "Chí mạng quặng/Lucky Block"),
                MiningItemEffectType.EventChance => Text("Good and bad event chance", "Tỉ lệ sự kiện tốt và xấu"),
                _ => Text("Effect", "Hiệu ứng")
            };
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            string savedName = PlayerPrefs.GetString(LanguageNameSaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(savedName))
            {
                int legacyValue = PlayerPrefs.GetInt(LanguageSaveKey,
                    (int)MiningLanguage.English);
                savedName = legacyValue == (int)MiningLanguage.Vietnamese
                    ? VietnameseLanguageName
                    : EnglishLanguageName;
                PlayerPrefs.SetString(LanguageNameSaveKey, savedName);
                PlayerPrefs.Save();
            }

            currentLanguageName = savedName.Trim();
            currentLanguage = ToLegacyLanguage(currentLanguageName);
            initialized = true;
        }

        private static void ApplyLeanLanguage()
        {
            if (applyingLeanLanguage)
            {
                return;
            }

            applyingLeanLanguage = true;
            try
            {
                LeanLocalization.SetCurrentLanguageAll(currentLanguageName);
            }
            finally
            {
                applyingLeanLanguage = false;
            }
        }

        private static string GetEffectiveLanguageName()
        {
            string leanLanguage = LeanLocalization.GetFirstCurrentLanguage();
            return string.IsNullOrWhiteSpace(leanLanguage)
                ? currentLanguageName
                : leanLanguage;
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
                    return Text(translation.English, translation.Vietnamese);
                }
            }

            return value;
        }

        private static string GetPhraseName(string english)
        {
            return string.IsNullOrEmpty(english)
                ? english
                : english.Replace("\r\n", "\n").Replace("\n", "\\n");
        }

        private static MiningLanguage ToLegacyLanguage(string languageName)
        {
            return string.Equals(languageName, VietnameseLanguageName,
                StringComparison.OrdinalIgnoreCase)
                ? MiningLanguage.Vietnamese
                : MiningLanguage.English;
        }

        private static void HandleLeanLocalizationChanged()
        {
            if (!initialized || applyingLeanLanguage)
            {
                return;
            }

            string leanLanguage = LeanLocalization.GetFirstCurrentLanguage();
            if (!string.IsNullOrWhiteSpace(leanLanguage) &&
                !string.Equals(currentLanguageName, leanLanguage,
                    StringComparison.OrdinalIgnoreCase))
            {
                // A Lean language changed outside the mining menu is still a valid
                // change. Mirror it into the one persisted mining state.
                currentLanguageName = leanLanguage;
                currentLanguage = ToLegacyLanguage(leanLanguage);
                PlayerPrefs.SetString(LanguageNameSaveKey, currentLanguageName);
                PlayerPrefs.SetInt(LanguageSaveKey, (int)currentLanguage);
                PlayerPrefs.Save();
            }
            // Lean sources may register after the menu; refresh script-owned labels then.
            LanguageChanged?.Invoke();
        }
    }
}
