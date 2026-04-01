using UnityEngine;

public interface IInteractable
{
    void Interact(PlayerInventoryInteraction player);
    string GetInteractionText();
    Vector3 GetInteractionPosition();
}