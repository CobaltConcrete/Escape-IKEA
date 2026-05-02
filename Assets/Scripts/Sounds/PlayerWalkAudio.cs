using UnityEngine;

public class PlayerWalkAudio : MonoBehaviour
{
    [SerializeField] private string walkSoundKey = "PlayerWalk";
    [SerializeField] private PlayerMovement playerMovement;

    private bool isWalking = false;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (playerMovement == null || SoundManager.Instance == null)
            return;

        bool shouldWalk = playerMovement.IsMoving;

        if (shouldWalk && !isWalking)
        {
            isWalking = true;
            SoundManager.Instance.PlayLoop(walkSoundKey);
        }
        else if (!shouldWalk && isWalking)
        {
            isWalking = false;
            SoundManager.Instance.StopLoop(walkSoundKey);
        }
    }

    private void OnDisable()
    {
        SoundManager.Instance?.StopLoop(walkSoundKey);
        isWalking = false;
    }
}