using UnityEngine;

public class BossRoomController : MonoBehaviour
{
    [SerializeField] private Transform enemyContainer;
    [SerializeField] private string bossNameHint = "CafeteriaEmployee";
    [SerializeField] private string clearMessage = "Boss defeated! Leave the store.";
    [SerializeField] private float winDoorDistance = 1.1f;

    [Header("Exit signs (after boss defeat)")]
    [Tooltip("Assign e.g. Assets/Sprites/Cafeteria/Exit.")]
    [SerializeField] private Sprite exitSignSprite;
    [Tooltip("How far past the doorway (toward the next room) along the wall normal.")]
    [SerializeField] private float exitSignOutsideDistance = 0.52f;
    [SerializeField] private float exitSignUniformScale = 1.38f;
    [Tooltip("Added after outside push + rotation (fine-tune in editor if needed).")]
    [SerializeField] private Vector3 exitSignExtraOffset = Vector3.zero;
    [Tooltip("Extra degrees on top of auto rotation (left/right vs top/bottom doors).")]
    [SerializeField] private float exitSignRotationOffsetZ = 0f;
    [SerializeField] private float exitSignSortingOrder = 22f;
    [SerializeField] private string exitSignSortingLayerName = "Player";

    private EnemyCombat bossCombat;
    private Collider2D roomTrigger;
    private bool encounterStarted;
    private bool bossDefeated;
    private Transform player;
    private Door[] bossRoomDoors;
    private bool winTriggered;
    private static int activeBossRoomPlayerCount;
    private Transform exitSignsRoot;
    private bool exitSignsSpawned;
    private static bool warnedMissingExitSprite;

    private void Awake()
    {
        roomTrigger = GetComponent<Collider2D>();
        if (enemyContainer == null)
        {
            Transform t = transform.Find("SpawnedObjects");
            if (t != null) enemyContainer = t;
        }
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        CacheBossRoomDoors();
        PushBoundsToBoss();
    }

    private void Update()
    {
        if (!encounterStarted) return;

        if (!bossDefeated)
        {
            if (bossCombat == null)
                bossCombat = FindBossCombat();

            if (bossCombat == null)
                bossDefeated = true;
        }

        if (bossDefeated)
        {
            EnsureExitSignsVisible();
            TryTriggerWinAtDoor();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        CacheBossRoomDoors();
        activeBossRoomPlayerCount = 1;
        if (encounterStarted) return;

        encounterStarted = true;
        bossCombat = FindBossCombat();
        ActivateBossMode();
        PushBoundsToBoss();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        activeBossRoomPlayerCount = 0;
        if (!bossDefeated) return;
        TriggerWin();
    }

    private void OnDisable()
    {
        activeBossRoomPlayerCount = 0;
    }

    public static bool IsPlayerInsideBossRoom()
    {
        return activeBossRoomPlayerCount > 0;
    }

    private EnemyCombat FindBossCombat()
    {
        if (enemyContainer == null) return null;
        EnemyCombat[] combats = enemyContainer.GetComponentsInChildren<EnemyCombat>(true);
        for (int i = 0; i < combats.Length; i++)
        {
            EnemyCombat c = combats[i];
            if (c == null) continue;
            EnemyAimerShooter shooter = c.GetComponent<EnemyAimerShooter>() ?? c.GetComponentInParent<EnemyAimerShooter>();
            if (shooter != null)
                return c;
            if (c.gameObject.name.Contains(bossNameHint))
                return c;
        }
        return null;
    }

    private void PushBoundsToBoss()
    {
        if (enemyContainer == null) return;
        if (roomTrigger == null || !roomTrigger.isTrigger) return;
        Bounds b = roomTrigger.bounds;
        CafeteriaBossPattern[] patterns = enemyContainer.GetComponentsInChildren<CafeteriaBossPattern>(true);
        for (int i = 0; i < patterns.Length; i++)
        {
            if (patterns[i] != null) patterns[i].SetRoomBounds(b);
        }
    }

    private void CacheBossRoomDoors()
    {
        Door[] allDoors = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (allDoors == null || allDoors.Length == 0)
        {
            bossRoomDoors = System.Array.Empty<Door>();
            return;
        }

        System.Collections.Generic.List<Door> connected = new System.Collections.Generic.List<Door>();
        for (int i = 0; i < allDoors.Length; i++)
        {
            Door d = allDoors[i];
            if (d != null && d.IsConnectedToRoom(gameObject))
                connected.Add(d);
        }
        bossRoomDoors = connected.ToArray();
    }

    private void EnsureExitSignsVisible()
    {
        if (exitSignsSpawned) return;
        if (exitSignSprite == null)
        {
            if (!warnedMissingExitSprite)
            {
                warnedMissingExitSprite = true;
                Debug.LogWarning("BossRoomController: assign Exit Sign Sprite (e.g. Sprites/Cafeteria/Exit) to show exit markers at doors.");
            }
            exitSignsSpawned = true;
            return;
        }

        CacheBossRoomDoors();
        if (bossRoomDoors == null || bossRoomDoors.Length == 0)
        {
            exitSignsSpawned = true;
            return;
        }

        if (exitSignsRoot == null)
        {
            GameObject holder = new GameObject("BossRoomExitSigns");
            holder.transform.SetParent(transform, false);
            exitSignsRoot = holder.transform;
        }

        Vector3 roomCenter = roomTrigger != null ? roomTrigger.bounds.center : transform.position;

        for (int i = 0; i < bossRoomDoors.Length; i++)
        {
            Door d = bossRoomDoors[i];
            if (d == null) continue;

            Vector3 doorWorld = d.GetDoorwayWorldPosition();
            Vector2 delta = (Vector2)(doorWorld - roomCenter);

            Vector2 outward;
            if (d.isHorizontal)
            {
                float sy = Mathf.Sign(delta.y);
                if (sy == 0f) sy = 1f;
                outward = new Vector2(0f, sy);
            }
            else
            {
                float sx = Mathf.Sign(delta.x);
                if (sx == 0f) sx = 1f;
                outward = new Vector2(sx, 0f);
            }

            float zRot;
            if (d.isHorizontal)
                zRot = delta.y >= 0f ? 0f : 180f;
            else
                zRot = delta.x >= 0f ? -90f : 90f;
            zRot += exitSignRotationOffsetZ;

            GameObject sign = new GameObject("ExitSign");
            sign.transform.SetParent(exitSignsRoot, false);
            sign.transform.SetPositionAndRotation(
                doorWorld + (Vector3)(outward * exitSignOutsideDistance) + exitSignExtraOffset,
                Quaternion.Euler(0f, 0f, zRot));
            sign.transform.localScale = Vector3.one * exitSignUniformScale;

            SpriteRenderer sr = sign.AddComponent<SpriteRenderer>();
            sr.sprite = exitSignSprite;
            sr.sortingLayerName = exitSignSortingLayerName;
            sr.sortingOrder = Mathf.RoundToInt(exitSignSortingOrder);
        }

        exitSignsSpawned = true;
    }

    private void TryTriggerWinAtDoor()
    {
        if (winTriggered || player == null || bossRoomDoors == null) return;

        for (int i = 0; i < bossRoomDoors.Length; i++)
        {
            Door d = bossRoomDoors[i];
            if (d == null || !d.IsOpen()) continue;
            float dist = Vector2.Distance(player.position, d.transform.position);
            if (dist <= winDoorDistance)
            {
                TriggerWin();
                return;
            }
        }
    }

    private void TriggerWin()
    {
        if (winTriggered) return;
        winTriggered = true;
        if (PageManager.Instance != null)
            PageManager.Instance.WinGame();
    }

    private void ActivateBossMode()
    {
        if (bossCombat == null) return;

        EnemyWander wander = bossCombat.GetComponent<EnemyWander>() ?? bossCombat.GetComponentInParent<EnemyWander>();
        if (wander != null) wander.enabled = false;

        EnemyAimerShooter shooter = bossCombat.GetComponent<EnemyAimerShooter>() ?? bossCombat.GetComponentInParent<EnemyAimerShooter>();
        if (shooter != null) shooter.enabled = false;

        CafeteriaBossPattern pattern = bossCombat.GetComponent<CafeteriaBossPattern>() ?? bossCombat.GetComponentInParent<CafeteriaBossPattern>();
        if (pattern == null)
            pattern = bossCombat.gameObject.AddComponent<CafeteriaBossPattern>();
        pattern.enabled = true;

        bossCombat.ConfigureBossMode(120f, true, -0.1f);
    }

    private void OnGUI()
    {
        if (!bossDefeated) return;
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 18;
        style.alignment = TextAnchor.MiddleCenter;
        Rect r = new Rect((Screen.width - 460) * 0.5f, 16f, 460f, 36f);
        GUI.Box(r, clearMessage, style);
    }
}
