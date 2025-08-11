using UnityEngine;

public class CannedSoda : StandardEnemy
{
    [SerializeField] float defaultMoveSpeed = 3f;
    [SerializeField] int defaultDirection = -1;
    [SerializeField] float defaultStun = 1.4f;
    [SerializeField] float defaultBounce = 14f;
    [SerializeField] float defaultStompThreshold = -1.5f;

    void Reset()
    {
        moveSpeed = defaultMoveSpeed;
        direction = defaultDirection;
        stunDuration = defaultStun;
        bounceForce = defaultBounce;
        stompYThreshold = defaultStompThreshold;
    }
}