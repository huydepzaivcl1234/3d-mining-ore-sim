#if UNITY_INCLUDE_TESTS
using System.Reflection;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MiningHudModalTests
{
    private GameObject canvas;
    private MiningUiPanelCoordinator coordinator;
    [SetUp] public void Setup()
    {
        canvas = new GameObject("Test HUD", typeof(RectTransform), typeof(Canvas));
        coordinator = canvas.AddComponent<MiningUiPanelCoordinator>();
        var shop = Make("NPC Shop");
        var settings = new SerializedObject(coordinator);
        settings.FindProperty("shopPanel").objectReferenceValue = shop;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
    private RectTransform Make(string name)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
        obj.transform.SetParent(canvas.transform, false);
        return (RectTransform)obj.transform;
    }
    private void SetVisible(bool visible) => typeof(MiningUiPanelCoordinator)
        .GetMethod("SetBasePanelsImmediately", BindingFlags.NonPublic | BindingFlags.Instance)
        .Invoke(coordinator, new object[] { visible });
    [TearDown] public void Cleanup() => Object.DestroyImmediate(canvas);
    [Test] public void StaminaAndNewHudHideAndRestoreAuthoredState()
    {
        var stamina = Make("Player Stamina Bar").GetComponent<CanvasGroup>();
        stamina.alpha = 0.65f; stamina.interactable = false; stamina.blocksRaycasts = false;
        var stats = Make("Player Stats Button").GetComponent<CanvasGroup>();
        SetVisible(false);
        Assert.That(stamina.alpha, Is.Zero);
        Assert.That(stats.alpha, Is.Zero);
        Assert.That(stats.blocksRaycasts, Is.False);
        SetVisible(true);
        Assert.That(stamina.alpha, Is.EqualTo(0.65f));
        Assert.That(stamina.interactable, Is.False);
        Assert.That(stamina.blocksRaycasts, Is.False);
        Assert.That(stats.alpha, Is.EqualTo(1));
    }
    [Test] public void ActiveModalAndInactiveHudAreNotHiddenOrActivated()
    {
        var modal = Make("Custom Panel");
        typeof(MiningUiPanelCoordinator).GetField("activeModal", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(coordinator, modal);
        var hidden = Make("Hidden Toast"); hidden.gameObject.SetActive(false);
        SetVisible(false);
        Assert.That(modal.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
        SetVisible(true);
        Assert.That(hidden.gameObject.activeSelf, Is.False);
    }
}
#endif
