using UnityEngine;

public class EnemyExecuteOnStun : MonoBehaviour
{
    [SerializeField] StandardEnemy enemy;
    [SerializeField] Transform player;
    [SerializeField] string playerTag = "Player";
    [SerializeField] PlayerAnimationController playerAnim;
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField] float executeRange = 1.1f;
    [SerializeField] GameObject dropPrefab;
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
                enemy.ExecuteKill(dropPrefab, dropOffset, destroyDelay);
            }
            else if (debugLogs) Debug.Log("[Execute] Too far");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!enemy) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(enemy.transform.position, executeRange);
    }
}
