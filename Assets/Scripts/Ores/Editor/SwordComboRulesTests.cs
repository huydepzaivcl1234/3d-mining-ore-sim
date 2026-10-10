#if UNITY_INCLUDE_TESTS
using NUnit.Framework;

public sealed class SwordComboRulesTests
{
    [TestCase(.779f, false)]
    [TestCase(.78f, true)]
    [TestCase(.85f, true)]
    [TestCase(.969f, true)]
    [TestCase(.97f, false)]
    [TestCase(1.78f, false)]
    [TestCase(float.NaN, false)]
    public void LinkWindowIncludesStartButExcludesEnd(float phase, bool expected)
    {
        Assert.That(SwordComboRules.TryLink(SwordStrike.First, true, true, phase, .78f, .97f, out var next),
            Is.EqualTo(expected));
        if (expected) Assert.That(next, Is.EqualTo(SwordStrike.Second));
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(false, false)]
    public void LinkRequiresBothFreshPressAndContact(bool freshPress, bool contact)
    {
        Assert.That(SwordComboRules.TryLink(SwordStrike.First, freshPress, contact, .85f, .78f, .97f, out _), Is.False);
    }

    [Test]
    public void EarlySpamCannotBeRememberedByLaterFrames()
    {
        for (int i = 0; i < 78; i++)
            Assert.That(SwordComboRules.TryLink(SwordStrike.First, true, true, i / 100f, .78f, .97f, out _), Is.False);
        Assert.That(SwordComboRules.TryLink(SwordStrike.First, false, true, .85f, .78f, .97f, out _), Is.False);
        Assert.That(SwordComboRules.TryLink(SwordStrike.First, true, true, .85f, .78f, .97f, out var next), Is.True);
        Assert.That(next, Is.EqualTo(SwordStrike.Second));
    }

    [Test]
    public void ThirdStrikeCannotRestartEvenWithFreshPressInWindow()
    {
        Assert.That(SwordComboRules.TryLink(SwordStrike.Third, true, true, .85f, .78f, .97f, out _), Is.False);
    }

    [TestCase(.97f, .78f)]
    [TestCase(.8f, .8f)]
    [TestCase(-.1f, .97f)]
    [TestCase(.78f, 1.1f)]
    [TestCase(float.NaN, .97f)]
    public void InvalidDesignerWindowCannotAcceptInput(float start, float end)
    {
        Assert.That(SwordComboRules.TryLink(SwordStrike.First, true, true, .85f, start, end, out _), Is.False);
    }
}
#endif
