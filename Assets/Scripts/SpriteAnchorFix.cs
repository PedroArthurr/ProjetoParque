using UnityEngine;

public class SpriteAnchorFix : MonoBehaviour
{
    public enum Mode { RectPivot, Geometry }

    [SerializeField] SpriteRenderer sr;
    [SerializeField] Mode mode = Mode.Geometry;
    [SerializeField] Vector2 customAnchor01 = new Vector2(0.5f, 0f); // usado no RectPivot
    Vector3 basePos;
    Sprite lastSprite; bool lastFlip;

    void Awake()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        basePos = transform.localPosition;
        Apply();
    }

    void LateUpdate()
    {
        if (!sr) return;
        if (sr.sprite != lastSprite || sr.flipX != lastFlip) Apply();
    }

    void Apply()
    {
        lastSprite = sr.sprite; lastFlip = sr.flipX;
        if (!sr || !sr.sprite) { transform.localPosition = basePos; return; }

        Vector2 off;
        if (mode == Mode.RectPivot)
        {
            var s = sr.sprite; var r = s.rect; float ppu = s.pixelsPerUnit;
            Vector2 desiredPx = new Vector2(r.width * customAnchor01.x, r.height * customAnchor01.y);
            off = (desiredPx - s.pivot) / ppu;
        }
        else
        {
            var b = sr.sprite.bounds; // unidades do sprite (sem transparência extra)
            float cx = (b.min.x + b.max.x) * 0.5f;
            float by = b.min.y;
            off = new Vector2(-cx, -by); // bottom-center na origem
        }

        if (sr.flipX) off.x = -off.x;

        var ls = transform.localScale;
        Vector3 offScaled = new Vector3(off.x * ls.x, off.y * ls.y, 0f);
        transform.localPosition = basePos + offScaled;
    }
}
