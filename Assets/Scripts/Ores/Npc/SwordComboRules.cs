// Values are the routes authored in the existing CombatStrike Animator parameter.
public enum SwordStrike
{
    Lunge = -2,
    Turn = -1,
    First = 0,
    Second = 1,
    Third = 2
}

/// <summary>Stateless combo decisions; no input queue, Animator, movement or effects.</summary>
public static class SwordComboRules
{
    public static bool TryGetFollowUp(SwordStrike current, out SwordStrike next)
    {
        next = SwordStrike.First;
        switch (current)
        {
            case SwordStrike.Lunge: next = SwordStrike.Turn; return true;
            case SwordStrike.Turn: return true;
            case SwordStrike.First: next = SwordStrike.Second; return true;
            case SwordStrike.Second: next = SwordStrike.Third; return true;
            default: return false;
        }
    }

    public static bool TryLink(SwordStrike current, bool pressedThisFrame, bool contactConsumed,
        float phase, float startPhase, float endPhase, out SwordStrike next)
    {
        next = SwordStrike.First;
        // Explicit positive comparisons also reject NaN and invalid/reversed windows.
        return pressedThisFrame && contactConsumed && startPhase >= 0f && endPhase <= 1f &&
            startPhase < endPhase && phase >= startPhase && phase < endPhase &&
            TryGetFollowUp(current, out next);
    }
}
