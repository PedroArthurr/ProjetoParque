using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class UIFlyToSlotMB : MonoBehaviour
{
    public static UIFlyToSlotMB Instance { get; private set; }
    void Awake() { Instance = this; }

    public void Fly(Sprite icon, Vector3 worldStart, Transform playerHand, RectTransform uiTarget, Canvas canvas,
                    float toPlayerTime, float holdAtPlayer, float toSlotTime, float startScale, float endScale,
                    Action onArrived = null, UIBezierAuthor bezier = null, float spinDeg = 240f, bool strictBezier = true)
    {
        if (!icon || !uiTarget || !canvas) return;

        var go = new GameObject("PickupFly", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = canvas.gameObject.layer;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        var img = go.GetComponent<Image>();
        img.sprite = icon; img.preserveAspect = true; img.SetNativeSize(); img.raycastTarget = false;

        StartCoroutine(FlyRoutine());

        IEnumerator FlyRoutine()
        {
            var canvasRT = canvas.transform as RectTransform;
            Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            Vector2 ToCanvasLocalFromWorld(Vector3 w)
            {
                var sp = RectTransformUtility.WorldToScreenPoint(Camera.main, w);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, sp, uiCam, out var lp);
                return lp;
            }
            Vector2 ToCanvasLocalFromUI(RectTransform u)
            {
                var sp = RectTransformUtility.WorldToScreenPoint(uiCam, u.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, sp, uiCam, out var lp);
                return lp;
            }

            Vector2 pStart = ToCanvasLocalFromWorld(worldStart);
            Vector2 pHand = playerHand ? ToCanvasLocalFromWorld(playerHand.position) : pStart;
            Vector2 pEnd = ToCanvasLocalFromUI(uiTarget);

            rt.anchoredPosition = pStart;
            rt.localScale = Vector3.one * startScale;
            rt.localEulerAngles = Vector3.zero;

            float spinA = spinDeg * 0.35f;
            float spinB = spinDeg - spinA;

            yield return Tween(rt, pStart, pHand, toPlayerTime, startScale, Mathf.Max(startScale * 0.85f, endScale), spinA);
            yield return new WaitForSeconds(holdAtPlayer);

            Vector2 c1, c2;
            if (bezier && bezier.control1 && bezier.control2)
            {
                c1 = ToCanvasLocalFromUI(bezier.control1);
                c2 = ToCanvasLocalFromUI(bezier.control2);
            }
            else
            {
                var dir = (pEnd - pHand);
                var n = dir.sqrMagnitude > 0.0001f ? new Vector2(-dir.y, dir.x).normalized : Vector2.up;
                float arc = Mathf.Max(120f, dir.magnitude * 0.35f);
                c1 = pHand + n * arc;
                c2 = (pHand + pEnd) * 0.5f + n * arc * 0.6f;
            }

            if (!strictBezier && !(bezier && bezier.control1 && bezier.control2))
            {
                var boost = 1.4f;
                var m = (pHand + pEnd) * 0.5f;
                c1 = m + (c1 - m) * boost;
                c2 = m + (c2 - m) * boost;
            }

            yield return Bezier(rt, pHand, c1, c2, pEnd, toSlotTime, Mathf.Max(startScale, 0.9f), endScale, spinB);

            onArrived?.Invoke();
            Destroy(go);
        }
    }

    IEnumerator Tween(RectTransform t, Vector2 a, Vector2 b, float dur, float sA, float sB, float spin)
    {
        float e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float k = Mathf.Clamp01(e / dur);
            float ease = 1f - Mathf.Pow(1f - k, 3f);
            t.anchoredPosition = Vector2.LerpUnclamped(a, b, ease);
            t.localScale = Vector3.one * Mathf.LerpUnclamped(sA, sB, ease);
            t.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpUnclamped(0f, spin, ease));
            yield return null;
        }
        t.anchoredPosition = b;
        t.localScale = Vector3.one * sB;
        t.localEulerAngles = new Vector3(0f, 0f, spin);
    }

    IEnumerator Bezier(RectTransform t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float dur, float sA, float sB, float spin)
    {
        float e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float k = Mathf.Clamp01(e / dur);
            float ease = 1f - Mathf.Pow(1f - k, 3f);
            Vector2 pt = Cubic(p0, p1, p2, p3, ease);
            t.anchoredPosition = pt;
            t.localScale = Vector3.one * Mathf.LerpUnclamped(sA, sB, ease);
            t.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpUnclamped(0f, spin, ease));
            yield return null;
        }
        t.anchoredPosition = p3;
        t.localScale = Vector3.one * sB;
        t.localEulerAngles = new Vector3(0f, 0f, spin);
    }

    static Vector2 Cubic(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        float u = 1f - t;
        return u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
    }
}
