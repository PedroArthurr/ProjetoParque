using UnityEngine;
using System;

public class PlayerStun : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer sprite;

    [Header("VFX")]
    [SerializeField] private GameObject stunVFXRoot;
    [SerializeField] private ParticleSystem[] playOnStun;

    [Header("Params")]
    [SerializeField] private float defaultStunTime = 0.6f;
    [SerializeField] private float invulnAfterStun = 0.5f;
    [SerializeField] private Vector2 defaultKnockback = new Vector2(8f, 6f);
    [SerializeField] private Color hurtFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private float hurtFlashTime = 0.15f;
    [SerializeField] private bool debugLogs = true;

    public bool IsStunned { get; private set; }
    public bool CanBeStunned => reStunTimer <= 0f;
    public float RestunCooldown => Mathf.Max(0f, reStunTimer);

    public event Action OnStunned;

    private float stunTimer, flashTimer, reStunTimer;
    private float stunStartTime, plannedStun;
    private Color origColor;
    private bool vfxActive;

    private void Awake()
    {
        if (!movement) movement = GetComponent<PlayerMovement>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite) origColor = sprite.color;
        if (!stunVFXRoot)
        {
            var ps = GetComponentInChildren<ParticleSystem>(true);
            if (ps) stunVFXRoot = ps.gameObject;
        }
        SetVFX(false);
    }

    private void OnEnable()
    {
        SetVFX(IsStunned);
    }

    private void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && sprite) sprite.color = origColor;
        }

        if (reStunTimer > 0f) reStunTimer -= Time.deltaTime;

        if (!IsStunned) return;

        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0f) Unstun();
    }

    public void ApplyStun(float duration, Vector2 knockback)
    {
        if (!CanBeStunned)
        {
            if (debugLogs) Debug.Log($"[PlayerStun] Ignored (invuln {RestunCooldown:F2}s)");
            return;
        }

        IsStunned = true;
        plannedStun = duration > 0f ? duration : defaultStunTime;
        stunTimer = plannedStun;
        reStunTimer = stunTimer + invulnAfterStun;
        stunStartTime = Time.time;

        movement?.BlockMovement();

        Vector2 kb = new Vector2(knockback.x != 0 ? knockback.x : defaultKnockback.x,
                                 knockback.y != 0 ? knockback.y : defaultKnockback.y);
        if (rb) rb.linearVelocity = kb;

        if (sprite) { sprite.color = hurtFlashColor; flashTimer = hurtFlashTime; }

        SetVFX(true);
        PlayEnterParticles();

        if (debugLogs) Debug.Log($"[PlayerStun] HURT dur={stunTimer:F2} kb={kb} invulnNext={reStunTimer:F2}");
        OnStunned?.Invoke();
    }

    public void ApplyStun(float duration, int dirSign)
    {
        Vector2 kb = new Vector2(Mathf.Sign(dirSign) * defaultKnockback.x, defaultKnockback.y);
        ApplyStun(duration, kb);
    }

    private void Unstun()
    {
        IsStunned = false;
        movement?.UnblockMovement();
        SetVFX(false);
        if (debugLogs)
        {
            float elapsed = Time.time - stunStartTime;
            Debug.Log($"[PlayerStun] Recovered after {elapsed:F3}s (planned {plannedStun:F3}s)");
        }
    }

    private void SetVFX(bool on)
    {
        if (stunVFXRoot && vfxActive != on)
        {
            stunVFXRoot.SetActive(on);
            vfxActive = on;
        }
    }

    private void PlayEnterParticles()
    {
        if (playOnStun == null) return;
        for (int i = 0; i < playOnStun.Length; i++)
        {
            var ps = playOnStun[i];
            if (!ps) continue;
            ps.gameObject.SetActive(true);
            ps.Clear();
            ps.Play();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var pos = transform.position + Vector3.up * 1.6f;
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(pos, $"stun:{(IsStunned ? stunTimer : 0f):F2}s  invuln:{Mathf.Max(0f, reStunTimer):F2}s");
    }
#endif
}
