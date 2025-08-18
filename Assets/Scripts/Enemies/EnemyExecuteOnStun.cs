using UnityEngine;

public class EnemyExecuteOnStun : MonoBehaviour
{
    [SerializeField] StandardEnemy enemy;
    [SerializeField] Transform player;
    [SerializeField] string playerTag = "Player";
    [SerializeField] PlayerAnimationController playerAnim;
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField] float executeRange = 1.1f;
    [SerializeField] GameObject[] dropPrefabs;
    [SerializeField] Vector2 dropOffset = new Vector2(0f, 0.5f);
    [SerializeField] float destroyDelay = 0.5f;
    [SerializeField] bool debugLogs = true;

    void Awake()
    {
        if (!enemy) enemy = GetComponentInParent<StandardEnemy>();
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) player = p.transform;
        }
        if (!playerAnim && player) player.TryGetComponent(out playerAnim);
        if (!playerAnim && player) playerAnim = player.GetComponentInChildren<PlayerAnimationController>(true);
    }

    void Update()
    {
        if (!enemy || !player) return;
        if (!enemy.IsStunned) return;

        if (Input.GetKeyDown(interactKey))
        {
            float d = Vector2.Distance(player.position, enemy.transform.position);
            if (debugLogs) Debug.Log($"[Execute] Press E, dist={d:F2} need<={executeRange:F2}");
            if (d <= executeRange)
            {
                playerAnim?.PlayExecute();
                GameObject drop = GetRandomDrop();
                enemy.ExecuteKill(drop, dropOffset, destroyDelay);
            }
            else if (debugLogs) Debug.Log("[Execute] Too far");
        }
    }

    GameObject GetRandomDrop()
    {
        if (dropPrefabs == null || dropPrefabs.Length == 0) return null;
        int tries = 0;
        while (tries < 8)
        {
            var pick = dropPrefabs[Random.Range(0, dropPrefabs.Length)];
            if (pick != null) return pick;
            tries++;
        }
        for (int i = 0; i < dropPrefabs.Length; i++)
            if (dropPrefabs[i] != null) return dropPrefabs[i];
        return null;
    }

    void OnDrawGizmosSelected()
    {
        if (!enemy) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(enemy.transform.position, executeRange);
    }
}
