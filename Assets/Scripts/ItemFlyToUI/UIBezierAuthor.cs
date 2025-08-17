using UnityEngine;

[ExecuteAlways]
public class UIBezierAuthor : MonoBehaviour
{
    public Canvas canvas;
    public RectTransform control1;
    public RectTransform control2;
    public RectTransform target;        // optional (preview)
    public RectTransform previewStart;  // optional (preview)
    public Color curveColor = new Color(1f, 0.7f, 0.1f, 0.9f);
    public int steps = 32;

    void OnDrawGizmos()
    {
        if (!canvas || !control1 || !control2) return;

        var wC1 = control1.position;
        var wC2 = control2.position;

        DrawCross(wC1, 0.08f, Color.white);
        DrawCross(wC2, 0.08f, Color.white);

        if (previewStart && target)
        {
            var wA = previewStart.position;
            var wB = target.position;

            Gizmos.color = curveColor;
            Vector3 prev = wA;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 pt = Cubic(wA, wC1, wC2, wB, t);
                Gizmos.DrawLine(prev, pt);
                prev = pt;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(wA, wC1);
            Gizmos.DrawLine(wB, wC2);
        }
    }

    static Vector3 Cubic(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float u = 1f - t;
        return u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
    }

    static void DrawCross(Vector3 p, float size, Color col)
    {
        Gizmos.color = col;
        Gizmos.DrawLine(p + Vector3.left * size, p + Vector3.right * size);
        Gizmos.DrawLine(p + Vector3.up * size, p + Vector3.down * size);
    }
}
