using UnityEngine;

public class BuildSpot : MonoBehaviour
{
    [SerializeField] Transform placePoint;
    [SerializeField] bool occupied;

    public bool IsOccupied => occupied;
    public Vector3 Position => placePoint ? placePoint.position : transform.position;

    public GameObject Place(GameObject prefab)
    {
        if (!prefab || occupied) return null;
        var go = Instantiate(prefab, Position, Quaternion.identity);
        occupied = true;
        return go;
    }
}