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
            new("EMPTY", "TRỐNG"),
            new("Right mouse: rotate • WASD: move", "Chuột phải: xoay • WASD: di chuyển")
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
