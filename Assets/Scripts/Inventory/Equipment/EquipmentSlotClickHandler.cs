using UnityEngine;
using UnityEngine.EventSystems;
using static EquipmentEnum;

public class EquipmentSlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private EquipTag equipTag;
    [SerializeField] private PlayerInventoryInteraction playerInventoryInteraction;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (playerInventoryInteraction == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            playerInventoryInteraction.UnequipItem(equipTag);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            playerInventoryInteraction.DropEquippedItem(equipTag);
        }
    }
}