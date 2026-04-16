using TMPro;
using UnityEngine;

public class UI_PlayerDebugStats : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerInventoryInteraction playerInventoryInteraction;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI armorText;

    private void Update()
    {
        if (playerHealth != null && hpText != null)
        {
            hpText.text = $"HP: {playerHealth.CurrentHealth:0}/{playerHealth.MaxHealth:0}";
        }

        if (armorText != null)
        {
            Item armor = null;

            if (playerInventoryInteraction != null)
            {
                armor = playerInventoryInteraction.GetEquippedArmorItem();
            }

            if (armor != null && armor.IsArmor())
            {
                armorText.text =
                    $"Armor: {armor.GetArmorCurrentDurability():0}/{armor.GetArmorMaxDurability():0}";
            }
            else
            {
                armorText.text = "Armor: None";
            }
        }
    }
}