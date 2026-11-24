namespace MadMantra.Interaction
{
    public interface IInteractable
    {
        string GetPrompt();
        void Interact(MadMantra.Player.PlayerInteractor interactor);
    }
}
