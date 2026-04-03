using UnityEngine;

public class Dialogue : MonoBehaviour
{
    [SerializeField] private string beforePickupText;
    [SerializeField] private string afterPickupText  = "Oh cool, an item! I think this is on my shopping list";
    [SerializeField] private TMPro.TextMeshProUGUI dialogueText;
    private string controlText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controlText = "\n \nPress: \n [Arrow Keys] to move \n [J] to attack";
        dialogueText.text = beforePickupText + controlText;
    }

    public void ShowDialogue()
    {
        dialogueText.text = afterPickupText + controlText;
    }
}
