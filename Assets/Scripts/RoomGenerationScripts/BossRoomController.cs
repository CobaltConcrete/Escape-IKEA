using UnityEngine;

public class BossRoomController : MonoBehaviour
{
    [SerializeField] private Transform enemyContainer;
    [SerializeField] private string bossNameHint = "CafeteriaEmployee";
    [SerializeField] private string clearMessage = "Boss defeated! Leave the store.";
    [SerializeField] private float winDoorDistance = 1.1f;
    [Header("Exit Signs")]
    [SerializeField] private Sprite exitSignSprite;
    [SerializeField] private Vector2 exitSignScale = new Vector2(0.85f, 0.85f);
    [SerializeField] private float exitSignDoorOffset = 0.9f;
    [SerializeField] private float exitSignVerticalLift = 0.25f;
    [SerializeField] private int exitSignSortingOrder = 220;

    private EnemyCombat bossCombat;
    private Collider2D roomTrigger;
    private bool encounterStarted;
    private bool bossDefeated;
    private Transform player;
    private Door[] bossRoomDoors;
    private bool winTriggered;
    private bool exitSignsShown;
    private static int activeBossRoomPlayerCount;

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
            EnsureBossRoomDoorsCached();
            ShowExitSigns();
            TryTriggerWinAtDoor();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
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

    private void EnsureBossRoomDoorsCached()
    {
        if (bossRoomDoors == null || bossRoomDoors.Length == 0)
            CacheBossRoomDoors();
    }

    private void ShowExitSigns()
    {
        if (exitSignsShown || exitSignSprite == null || bossRoomDoors == null || bossRoomDoors.Length == 0)
            return;

        Transform parent = transform.Find("Decorations");
        if (parent == null)
        {
            GameObject root = new GameObject("BossExitSigns");
            root.transform.SetParent(transform, false);
            parent = root.transform;
        }

        Vector3 roomCenter = roomTrigger != null ? roomTrigger.bounds.center : transform.position;
        for (int i = 0; i < bossRoomDoors.Length; i++)
        {
            Door door = bossRoomDoors[i];
            if (door == null)
                continue;

            Vector3 outward = GetDoorOutwardDirection(door.transform.position, roomCenter);
            if (outward.y < 0.5f)
                continue;

            GameObject sign = new GameObject("BossExitSign");
            sign.transform.SetParent(parent, true);
            sign.transform.position = door.transform.position + outward * exitSignDoorOffset + Vector3.up * exitSignVerticalLift;
            sign.transform.localScale = new Vector3(exitSignScale.x, exitSignScale.y, 1f);
            sign.transform.rotation = Quaternion.identity;

            SpriteRenderer renderer = sign.AddComponent<SpriteRenderer>();
            renderer.sprite = exitSignSprite;
            renderer.color = Color.white;
            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
                renderer.sharedMaterial = new Material(spriteShader);
            renderer.sortingLayerName = "Item";
            renderer.sortingOrder = exitSignSortingOrder;
        }

        exitSignsShown = true;
        Room room = GetComponent<Room>();
        room?.RefreshRendererRegistry();
    }

    private static Vector3 GetDoorOutwardDirection(Vector3 doorPosition, Vector3 roomCenter)
    {
        Vector3 delta = doorPosition - roomCenter;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            return delta.x >= 0f ? Vector3.right : Vector3.left;

        return delta.y >= 0f ? Vector3.up : Vector3.down;
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
