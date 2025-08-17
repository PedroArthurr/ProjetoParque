using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxTileLoopX : MonoBehaviour
{
    [SerializeField] Transform cam;
    [SerializeField] int tilesCount = 3;
    [SerializeField] float tileWidthOverride = 0f;

    float tileW;

    void Awake()
    {
        if (!cam) cam = Camera.main ? Camera.main.transform : null;
        var sr = GetComponent<SpriteRenderer>();
        tileW = tileWidthOverride > 0f ? tileWidthOverride : (sr ? sr.bounds.size.x : 10f);
    }

    void LateUpdate()
    {
        if (!cam) return;
        float dist = cam.position.x - transform.position.x;
        float span = tileW * tilesCount;
        if (dist > tileW) transform.position += Vector3.right * span;
        else if (dist < -tileW) transform.position += Vector3.left * span;
    }
}
