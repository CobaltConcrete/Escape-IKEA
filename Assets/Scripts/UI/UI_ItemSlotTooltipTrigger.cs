using UnityEngine;
using UnityEngine.EventSystems;

public class UI_ItemSlotTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Item item;

    public void SetItem(Item item)
    {
        this.item = item;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UI_ItemTooltip.Instance == null) return;
        UI_ItemTooltip.Instance.Show(item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (UI_ItemTooltip.Instance == null) return;
        UI_ItemTooltip.Instance.Hide();
    }

    private void OnDisable()
    {
        if (UI_ItemTooltip.Instance != null)
        {
            UI_ItemTooltip.Instance.Hide();
        }
    }

    private void OnDestroy()
    {
        if (UI_ItemTooltip.Instance != null)
        {
            UI_ItemTooltip.Instance.Hide();
        }
    }
}