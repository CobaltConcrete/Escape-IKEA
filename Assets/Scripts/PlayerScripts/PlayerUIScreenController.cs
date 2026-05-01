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
    //[SerializeField] private GameObject dialogueBox;
    [SerializeField] private GameObject controlText;

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
        UIScreenState previousState = currentState;

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

        PlayInventoryTransitionSound(previousState, currentState);
    }

    private void HandleObjectiveToggle()
    {
        UIScreenState previousState = currentState;

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

        PlayInventoryTransitionSound(previousState, currentState);

        if (SoundManager.Instance != null)
        {
            if (previousState != UIScreenState.Objective && currentState == UIScreenState.Objective)
                SoundManager.Instance.PlaySound("ListOpen");
            else if (previousState == UIScreenState.Objective && currentState != UIScreenState.Objective)
                SoundManager.Instance.PlaySound("ListClose");
        }
    }

    private void PlayInventoryTransitionSound(UIScreenState previousState, UIScreenState newState)
    {
        if (SoundManager.Instance == null)
            return;

        bool inventoryWasOpen = previousState == UIScreenState.Inventory;
        bool inventoryIsOpen = newState == UIScreenState.Inventory;

        if (!inventoryWasOpen && inventoryIsOpen)
        {
            SoundManager.Instance.PlayInventoryOpen();
        }
        else if (inventoryWasOpen && !inventoryIsOpen)
        {
            SoundManager.Instance.PlayInventoryClose();
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

        //if (dialogueBox != null)
        //{
        //    dialogueBox.SetActive(!anyUIOpen);
        //}

        if (objectiveHint != null)
        {
            objectiveHint.SetActive(!anyUIOpen);
        }

        if (controlText != null)
        {
            controlText.SetActive(!anyUIOpen);
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