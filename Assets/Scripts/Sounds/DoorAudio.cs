using UnityEngine;

public class DoorAudio : MonoBehaviour
{
    [Header("Sound Keys")]
    [SerializeField] private string openSoundKey = "SlideDoorOpen";
    [SerializeField] private string closeSoundKey = "SlideDoorClose";

    [Header("3D Audio Settings")]
    [SerializeField] private float minDistance = 1.2f;
    [SerializeField] private float maxDistance = 10f;

    [Header("Volume Multipliers")]
    [SerializeField] private float openVolumeMultiplier = 1f;
    [SerializeField] private float closeVolumeMultiplier = 1f;

    [Header("Anti Spam")]
    [SerializeField] private float soundCooldown = 0.08f;

    private float lastSoundTime = -999f;

    public void PlayOpenSound()
    {
        PlayDoorSound(openSoundKey, openVolumeMultiplier);
    }

    public void PlayCloseSound()
    {
        PlayDoorSound(closeSoundKey, closeVolumeMultiplier);
    }

    private void PlayDoorSound(string key, float volumeMultiplier)
    {

        if (Time.time < lastSoundTime + soundCooldown)
            return;

        lastSoundTime = Time.time;

        SoundManager.Instance.PlaySound(key, volumeMultiplier);
    }
}