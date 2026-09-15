# Mining localization migration

The base language is English. The Scene already has Lean Localization, English,
and Vietnamese languages; `MiningLanguageController` keeps English active while
its `Enable Language Switching` checkbox is off. Run **Mining Simulator >
Localization > Prepare English Base** once in `SampleScene` to attach the
controller to the existing Lean Localization object and remove old language
preferences. Save the Scene after reviewing the change.

For a static label, select the TMP label (or a small hierarchy of static labels)
and run **Bind Selected Static TMP Labels To Keys**. The tool creates an English
`UI_...` Phrase under `LeanLocalization/Phrases` and adds
`LeanLocalizedTextMeshProUGUI` with that key and an English fallback. Rename the
generated key to a stable semantic name such as `UI_Inventory` before other
systems use it. Do not select counters, status labels, or text written by a
script: bind those in code with `MiningPhrase.Format("UI_InventoryCount", count)`
and an English Phrase value such as `INVENTORY ({0})`.

Author every language's value on the same Phrase. Ensure the `LeanLocalization`
object is active before opening UI prefabs and that every prefab binding has a
nonempty `TranslationName`, an English fallback, and the expected TMP component.
Lean's TMP localized component listens to localization updates on enable, so
reopening or pooling a prefab refreshes its static labels. If a script also
writes to that TMP label, whichever updates last wins; use one owner per label.

Call `TrySetLanguage("Vietnamese")` on the scene controller from a settings
button after checking `Enable Language Switching`. In this vendored Lean version,
`SetCurrentLanguage(string)` is an **instance** method; the controller uses it
on the existing Lean object. It validates registered language names, saves one
choice, and falls back to English while switching is disabled.

**Audit Legacy Language Calls** reports the old bilingual call sites. They must
be converted to semantic keys and English Phrase values before removing
`MiningLocalization`; static binding alone cannot migrate runtime formatted
text, ScriptableObject fields, or messages. The legacy bridge remains so the
current game still renders English during this migration. Do not delete it until
the audit reaches zero and the UI has been checked in Play Mode with both
languages. Avoid duplicate Phrase names, blank translations, overriding TMP
labels from `Awake`/`OnEnable`, and relying on scene-only Phrase objects in a
prefab tested in isolation.
