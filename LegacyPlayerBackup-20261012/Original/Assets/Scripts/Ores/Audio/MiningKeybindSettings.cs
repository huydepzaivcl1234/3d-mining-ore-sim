using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace MiningSimulator.Ores
{
    /// <summary>Rebinds the live PlayerInput instance, never the imported action asset.</summary>
    public sealed class MiningKeybindSettings : MonoBehaviour
    {
        [Serializable] public sealed class BindingRow
        {
            public string labelKey;
            public string actionName;
            public string compositePart;
            public bool cameraLock;
            public bool fpsWeapon;
            public Button button;
            public TextMeshProUGUI label;
            public TextMeshProUGUI value;
            [NonSerialized] public InputAction action;
            [NonSerialized] public int index = -1;
        }
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private ChestDefense.Fps.FpsWeaponController fpsWeapons;
        public bool IsRebinding => operation != null;
        [SerializeField] private MiningOrbitCamera cameraRig;
        [SerializeField] private GameObject controlsPanel;
        [SerializeField] private BindingRow[] rows;
        [SerializeField] private Button openButton, backButton, resetButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [Min(1f), SerializeField] private float rebindTimeoutSeconds = 10f;
        private const string PlayerKey = "MiningSimulator.PlayerBindings.v1";
        private const string CameraKey = "MiningSimulator.CameraLockBinding.v1";
        private InputActionRebindingExtensions.RebindingOperation operation;
        private BindingRow editing;
        private bool wasEnabled;
        private string previousOverride;
        private MiningAudioSettingsPanel settingsOwner;

        private void Start()
        {
            if (playerInput == null || playerInput.actions == null || cameraRig == null) return;
            EnsureBuildRow();
            try
            {
                if (PlayerPrefs.HasKey(PlayerKey)) playerInput.actions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PlayerKey));
                if (PlayerPrefs.HasKey(CameraKey)) cameraRig.ShiftLockAction.ApplyBindingOverride(0, PlayerPrefs.GetString(CameraKey));
            }
            catch (Exception e) { Debug.LogWarning("Unable to read saved controls: " + e.Message, this); }
            foreach (var row in rows)
            {
                row.action = row.fpsWeapon ? fpsWeapons?.GetAction(row.actionName) :
                    row.cameraLock ? cameraRig.ShiftLockAction : playerInput.actions.FindAction("Player/" + row.actionName);
                if (row.action != null)
                    for (int i = 0; i < row.action.bindings.Count; i++)
                    {
                        var binding = row.action.bindings[i];
                        if (!binding.isComposite && (binding.path.Contains("<Keyboard>") || row.cameraLock || row.fpsWeapon) &&
                            (string.IsNullOrEmpty(row.compositePart) || binding.name == row.compositePart))
                        { row.index = i; break; }
                    }
                var captured = row;
                row.button.onClick.AddListener(() => BeginRebind(captured));
            }
            openButton.onClick.AddListener(Open);
            backButton.onClick.AddListener(Close);
            resetButton.onClick.AddListener(ResetBindings);
            closeButton?.onClick.AddListener(Close);
            controlsPanel.SetActive(false);
            RefreshLabels();
        }
        public void EnsureBuildRow()
        {
            if(rows==null||rows.Length==0||controlsPanel==null)return;
            foreach(var row in rows)if(row.actionName=="Build"&&!row.cameraLock)return;
            var template=rows[rows.Length-1];
            if(template.button==null||template.label==null)return;
            var button=Instantiate(template.button,template.button.transform.parent,false);
            button.name="Build Binding";button.onClick=new Button.ButtonClickedEvent();
            ((RectTransform)button.transform).anchoredPosition-=new Vector2(0,48);
            var label=Instantiate(template.label,template.label.transform.parent,false);
            label.name="Build Action";label.rectTransform.anchoredPosition-=new Vector2(0,48);
            Array.Resize(ref rows,rows.Length+1);
            rows[rows.Length-1]=new BindingRow {labelKey="Build",actionName="Build",button=button,label=label,value=button.GetComponentInChildren<TextMeshProUGUI>(true)};
        }
        private void OnEnable()
        {
            MiningLocalization.LanguageChanged += RefreshLabels;
            settingsOwner ??= GetComponent<MiningAudioSettingsPanel>();
            if (settingsOwner != null) settingsOwner.PanelClosed += Close;
        }
        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= RefreshLabels;
            if (settingsOwner != null) settingsOwner.PanelClosed -= Close;
            Cancel();
        }
        private void Update() { if (operation != null && !controlsPanel.activeInHierarchy) Cancel(); }
        private void Open() { controlsPanel.SetActive(true); controlsPanel.transform.SetAsLastSibling(); RefreshLabels(); controlsPanel.GetComponent<MiningPanelMotion>()?.PlayOpen(); }
        private void Close() { Cancel(); if (controlsPanel == null) return; if (controlsPanel.activeInHierarchy && controlsPanel.TryGetComponent(out MiningPanelMotion motion)) motion.PlayClose(); else controlsPanel.SetActive(false); }
        public void BeginRebind(BindingRow row)
        {
            Cancel();
            if (row.action == null || row.index < 0) return;
            editing = row;
            wasEnabled = row.action.enabled;
            previousOverride = row.action.bindings[row.index].overridePath;
            row.action.Disable();
            statusLabel.text = MiningLocalization.Text("PRESS A KEY - ESC TO CANCEL");
            operation = row.action.PerformInteractiveRebinding(row.index)
                .WithExpectedControlType("Button")
                .WithControlsHavingToMatchPath("<Keyboard>/*")
                .WithControlsHavingToMatchPath("<Mouse>/*Button")
                .WithCancelingThrough("<Keyboard>/escape")
                .WithTimeout(rebindTimeoutSeconds)
                .OnCancel(_ => Finish(false))
                .OnComplete(_ => Finish(true));
            operation.Start();
        }
        private void Finish(bool completed)
        {
            var row = editing;
            var op = operation; operation = null; editing = null;
            bool duplicate = false;
            if (completed && row != null)
                foreach (var other in rows)
                    if (other != row && other.action != null && other.index >= 0 &&
                        string.Equals(other.action.bindings[other.index].effectivePath,
                            row.action.bindings[row.index].effectivePath, StringComparison.OrdinalIgnoreCase)) duplicate = true;
            if (row != null)
            {
                if (!completed || duplicate)
                {
                    if (previousOverride == null) row.action.RemoveBindingOverride(row.index);
                    else row.action.ApplyBindingOverride(row.index, previousOverride);
                }
                if (wasEnabled) row.action.Enable();
            }
            op?.Dispose();
            if (completed && !duplicate) Save();
            RefreshLabels();
            if (duplicate) statusLabel.text = MiningLocalization.Text("KEY ALREADY IN USE");
        }
        private void Cancel() { if (operation != null) operation.Cancel(); }
        public void ResetBindings()
        {
            Cancel();
            foreach (var row in rows) if (row.action != null && row.index >= 0) row.action.RemoveBindingOverride(row.index);
            Save(); RefreshLabels();
        }
        private void Save()
        {
            fpsWeapons?.SaveBindings();
            PlayerPrefs.SetString(PlayerKey, playerInput.actions.SaveBindingOverridesAsJson());
            var action = cameraRig.ShiftLockAction;
            if (action.bindings[0].overridePath == null) PlayerPrefs.DeleteKey(CameraKey);
            else PlayerPrefs.SetString(CameraKey, action.bindings[0].overridePath);
            PlayerPrefs.Save();
        }
        private void RefreshLabels()
        {
            if (rows == null) return;
            foreach (var row in rows)
            {
                if (row.label != null) row.label.text = row.actionName=="Build"?MiningLocalization.Text("Build","Xây dựng"):MiningLocalization.Text(row.labelKey);
                if (row.value != null) row.value.text = row.action != null && row.index >= 0 ? row.action.GetBindingDisplayString(row.index) : "—";
            }
            if (statusLabel != null && operation == null) statusLabel.text = MiningLocalization.Text("CLICK TO CHANGE KEY");
            SetText(openButton, "CONTROLS"); SetText(backButton, "BACK"); SetText(resetButton, "RESET KEYS");
        }
        private static void SetText(Button button, string key)
        {
            var text = button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (text != null) text.text = MiningLocalization.Text(key);
        }
    }
}
