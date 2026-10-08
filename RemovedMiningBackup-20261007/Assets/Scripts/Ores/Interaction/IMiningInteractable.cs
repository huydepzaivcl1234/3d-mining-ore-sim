namespace MiningSimulator.Ores
{
    /// <summary>Shared contract for portal, NPC dialogue and future world interactions.</summary>
    public interface IMiningInteractable
    {
        string InteractionLabel { get; }
        bool CanInteract { get; }
        void Interact();
        void SetInteractionFocused(bool focused);
    }
}
