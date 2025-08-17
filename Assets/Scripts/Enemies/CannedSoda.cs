using UnityEngine;

public class CannedSoda : StandardEnemy
{
    [SerializeField] private float defaultMoveSpeed = 3f;
    [SerializeField] private int defaultDirection = -1;
    [SerializeField] private float defaultStun = 1.4f;
    [SerializeField] private float defaultBounce = 10f;
    [SerializeField] private float defaultStompThreshold = -1.5f;

    private void Reset()
    {
        moveSpeed = defaultMoveSpeed;
        direction = defaultDirection;
        stunDuration = defaultStun;
        bounceForce = defaultBounce;
        stompYThreshold = defaultStompThreshold;
    }
}