using UnityEngine;

public class TetrapaksteinAnimEventsRelay : MonoBehaviour
{
    [SerializeField] Tetrapakstein owner;

    void Awake()
    {
        if (!owner) owner = GetComponentInParent<Tetrapakstein>();
    }

    public void AttackHitOn() { if (owner) owner.Anim_AttackHitOn(); }
    public void AttackHitOff() { if (owner) owner.Anim_AttackHitOff(); }
    public void AttackEnd() { if (owner) owner.Anim_AttackEnd(); }
}
