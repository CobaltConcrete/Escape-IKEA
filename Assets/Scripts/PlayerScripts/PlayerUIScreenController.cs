using UnityEngine;

public class PlayerUIScreenController : MonoBehaviour
{
    public enum UIScreenState
    {
        None,
        Inventory,
        Objective
    }

    [Header("Main Panels")]
    [SerializeField] private GameObject inventoryRoot;
    [SerializeField] private GameObject objectiveRoot;

    [Header("Hide When Any UI Opens")]
    [SerializeField] private GameObject utilityQuickSlot;
    [SerializeField] private GameObject objectiveHint;
    [SerializeField] private GameObject dialogueBox;

    private UIScreenState currentState = UIScreenState.None;

    private void Start()
    {
        SetState(UIScreenState.None);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            HandleInventoryToggle();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            HandleObjectiveToggle();
        }
    }

    private void HandleInventoryToggle()
    {
        switch (currentState)
        {
            case UIScreenState.None:
                SetState(UIScreenState.Inventory);
                break;

            case UIScreenState.Inventory:
                SetState(UIScreenState.None);
                break;

            case UIScreenState.Objective:
                SetState(UIScreenState.Inventory);
                break;
        }
    }

    private void HandleObjectiveToggle()
    {
        switch (currentState)
        {
            case UIScreenState.None:
                SetState(UIScreenState.Objective);
                break;

            case UIScreenState.Inventory:
                SetState(UIScreenState.Objective);
                break;

            case UIScreenState.Objective:
                SetState(UIScreenState.None);
                break;
        }
    }

    private void SetState(UIScreenState newState)
    {
        currentState = newState;

        bool inventoryOpen = currentState == UIScreenState.Inventory;
        bool objectiveOpen = currentState == UIScreenState.Objective;
        bool anyUIOpen = currentState != UIScreenState.None;

        if (inventoryRoot != null)
        {
            inventoryRoot.SetActive(inventoryOpen);
        }

        if (objectiveRoot != null)
        {
            objectiveRoot.SetActive(objectiveOpen);
        }

        if (utilityQuickSlot != null)
        {
            utilityQuickSlot.SetActive(!anyUIOpen);
        }

        if (dialogueBox != null)
        {
            dialogueBox.SetActive(!anyUIOpen);
        }

        if (objectiveHint != null)
        {
            objectiveHint.SetActive(!anyUIOpen);
        }

        Cursor.visible = anyUIOpen;
        Cursor.lockState = anyUIOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public bool IsAnyUIOpen()
    {
        return currentState != UIScreenState.None;
    }

    public UIScreenState GetCurrentState()
    {
        return currentState;
    }
}