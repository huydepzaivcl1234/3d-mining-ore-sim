# Mining Simulator localization

The active game scene loads `Assets/GameData/Localization/English.txt` and
`Vietnamese.txt` through Lean `LeanLanguageCSV` sources (Legacy format). These
tables have matching keys. `MiningLocalization` owns the saved language choice;
menu and settings buttons call its `ToggleLanguage` or `SetLanguage` methods.
The existing `MiningLanguageSwitcher` component delegates to that same owner.
Lean components in `SampleScene` have device detection and their own save/load
disabled so they cannot overwrite the player's selection.

For script-owned UI, write the English text once:

```csharp
label.text = MiningLocalization.Text("Inventory is full.");
```

Add `Inventory is full. = Inventory is full.` to `English.txt` and the same
key with the Vietnamese text to `Vietnamese.txt`. Both tables must preserve
the same format placeholders (`{0}`, `{1:0.##}`, etc.). A missing translation
falls back to the English text. Lean Localization reads supplied translations;
it does not generate machine translations.

When one English phrase has different meanings, use a semantic key:

```csharp
label.text = MiningLocalization.TextKey("MENU_CANCEL_LABEL", "CANCEL");
```

For static authored TMP labels, use **Mining Simulator > Localization > Auto
Bind Scene TMP** in the Unity Editor after adding both language entries. Do not
bind counters or status labels that a script refreshes; those scripts should
call `MiningLocalization.Text` when they update their text. The menu uses
`MENU_*` keys because its labels and some gameplay labels share English text
but need different Vietnamese translations.
