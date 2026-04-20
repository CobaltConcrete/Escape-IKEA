using System.Collections.Generic;
using UnityEngine;

public class RoomVisitTracker : MonoBehaviour
{
    public static RoomVisitTracker Instance { get; private set; }

    private readonly List<string> distinctRoomHistory = new List<string>();
    private string currentRoomKey;

    public IReadOnlyList<string> DistinctRoomHistory => distinctRoomHistory;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterRoomVisit(string roomKey)
    {
        if (string.IsNullOrWhiteSpace(roomKey))
            return;

        if (currentRoomKey == roomKey)
            return;

        currentRoomKey = roomKey;
        distinctRoomHistory.Add(roomKey);

        //Debug.Log($"[RoomVisitTracker] Visit: {roomKey}");

        string history = "";
        for (int i = 0; i < distinctRoomHistory.Count; i++)
        {
            history += distinctRoomHistory[i];
            if (i < distinctRoomHistory.Count - 1)
                history += " -> ";
        }

        Debug.Log($"[RoomVisitTracker] History: {history}");
    }

    public int CountDistinctRoomsAfter(string roomKey)
    {
        if (string.IsNullOrWhiteSpace(roomKey))
            return 0;

        int lastIndex = distinctRoomHistory.LastIndexOf(roomKey);
        if (lastIndex < 0)
            return 0;

        HashSet<string> distinctAfter = new HashSet<string>();

        for (int i = lastIndex + 1; i < distinctRoomHistory.Count; i++)
        {
            string laterRoomKey = distinctRoomHistory[i];
            if (!string.IsNullOrWhiteSpace(laterRoomKey) && laterRoomKey != roomKey)
            {
                distinctAfter.Add(laterRoomKey);
            }
        }

        return distinctAfter.Count;
    }
}