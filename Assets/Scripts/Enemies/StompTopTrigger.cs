using UnityEngine;

public class StompTopTrigger : MonoBehaviour
{
    [SerializeField] private StandardEnemy enemy;

    private void Awake()
    {
        if (!enemy) enemy = GetComponentInParent<StandardEnemy>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!enemy) return;
        if (!other.CompareTag("Player")) return;

        var prb = other.attachedRigidbody;
        if (!prb) return;

        float vy = prb.linearVelocity.y;
        if (vy <= enemy.stompYThreshold) enemy.ApplyStun(prb);
    }
}