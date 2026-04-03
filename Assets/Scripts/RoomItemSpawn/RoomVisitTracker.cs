using UnityEngine;

public class RoomVisitTracker : MonoBehaviour
{
    public static RoomVisitTracker Instance { get; private set; }

    public int CurrentVisitIndex { get; private set; } = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public int RegisterRoomVisit()
    {
        CurrentVisitIndex++;
        return CurrentVisitIndex;
    }
}