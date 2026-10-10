#if UNITY_INCLUDE_TESTS
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class PlayerGraphicsOptionsTests
{
    [Test]
    public void PartialAndCorruptPreferencesPreserveSafeDefaults()
    {
        var defaults=new PlayerGraphicsPreferences {shadowDistance=85,msaa=4};
        var partial=PlayerGraphicsPreferences.Read("{\"version\":1,\"renderScale\":0.75}",defaults);
        Assert.That(partial.renderScale,Is.EqualTo(.75f));
        Assert.That(partial.shadowDistance,Is.EqualTo(85));
        Assert.That(partial.msaa,Is.EqualTo(4));
        Assert.That(PlayerGraphicsPreferences.Read("not json",defaults).shadowDistance,Is.EqualTo(85));
        Assert.That(PlayerGraphicsPreferences.Read("{\"version\":9,\"shadowDistance\":0}",defaults).shadowDistance,Is.EqualTo(85));
        Assert.That(defaults.renderScale,Is.EqualTo(1));
    }

    [Test]
    public void SanitizationRejectsNonFiniteAndUnsupportedValues()
    {
        var values=new PlayerGraphicsPreferences {renderScale=float.NaN,shadowDistance=float.PositiveInfinity,
            aaMode=99,msaa=3,cascades=8,bloomMultiplier=-1,refreshDenominator=0};
        values.Sanitize();
        Assert.That(values.renderScale,Is.EqualTo(1));Assert.That(values.shadowDistance,Is.EqualTo(50));
        Assert.That(values.aaMode,Is.EqualTo(2));Assert.That(values.msaa,Is.EqualTo(4));
        Assert.That(values.cascades,Is.EqualTo(4));Assert.That(values.bloomMultiplier,Is.Zero);
        Assert.That(values.refreshDenominator,Is.EqualTo(1));
    }

    [Test]
    public void DisplayModeMatchIncludesRationalRefreshRate()
    {
        var modes=new[]{new Resolution {width=1920,height=1080,refreshRateRatio=new RefreshRate {numerator=60,denominator=1}},
            new Resolution {width=1920,height=1080,refreshRateRatio=new RefreshRate {numerator=144,denominator=1}}};
        Assert.That(PlayerGraphicsPreferences.MatchResolution(modes,1920,1080,120,2),Is.Zero);
        Assert.That(PlayerGraphicsPreferences.MatchResolution(modes,1920,1080,144,1),Is.EqualTo(1));
        Assert.That(PlayerGraphicsPreferences.MatchResolution(modes,2560,1440,60,1),Is.EqualTo(-1));
    }

    [Test]
    public void CinematicPreferencesDoNotAccumulateWhenLightingRecomputesBases()
    {
        var owner=new GameObject("Isolated graphics test");
        var bloom=ScriptableObject.CreateInstance<Bloom>();
        var color=ScriptableObject.CreateInstance<ColorAdjustments>();
        var vignette=ScriptableObject.CreateInstance<Vignette>();
        PlayerGraphicsOptions options=null;
        try
        {
            options=owner.AddComponent<PlayerGraphicsOptions>();options.Initialize();
            options.Values.bloom=false;options.Values.bloomMultiplier=2;options.Values.exposureOffset=.5f;
            for(int i=0;i<3;i++)
            {
                bloom.intensity.Override(.6f);color.postExposure.Override(.1f);
                options.ApplyCinematic(bloom,color,vignette);
                Assert.That(bloom.active,Is.False);Assert.That(bloom.intensity.value,Is.EqualTo(1.2f).Within(.001));
                Assert.That(color.postExposure.value,Is.EqualTo(.6f).Within(.001));
            }
        }
        finally
        {
            options?.Release();Object.DestroyImmediate(owner);Object.DestroyImmediate(bloom);
            Object.DestroyImmediate(color);Object.DestroyImmediate(vignette);
        }
    }
}
#endif
