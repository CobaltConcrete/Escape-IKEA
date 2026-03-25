using UnityEngine;

public class ToggleInventory : MonoBehaviour
{
    [SerializeField] private GameObject uiRoot;
    [SerializeField] private GameObject utilityQuickSlot;

    private bool isOpen = false;

    private void Start()
    {
        SetInventoryState(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            SetInventoryState(!isOpen);
        }
    }

    private void SetInventoryState(bool open)
    {
        isOpen = open;

        if (uiRoot != null)
        {
            uiRoot.SetActive(isOpen);
        }

        if (utilityQuickSlot != null)
        {
            utilityQuickSlot.SetActive(!isOpen);
        }

        if (isOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}