using UnityEngine;
using UnityEngine.EventSystems;
using static EquipmentEnum;

public class EquipmentSlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private EquipTag equipTag;
    [SerializeField] private PlayerInventoryInteraction playerInventoryInteraction;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (playerInventoryInteraction != null)
            {
                playerInventoryInteraction.UnequipItem(equipTag);
            }
        }
    }
}