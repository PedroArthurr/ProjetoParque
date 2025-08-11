using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [SerializeField] ItemType itemType;
    [SerializeField] int amount = 1;
    [SerializeField] float pickupRange = 3f;

    public ItemType Type => itemType;
    public int Amount => amount;
    public float Range => pickupRange;

    void OnMouseDown() { PickupManager.Instance?.TryPickup(this); }
}
public enum ItemType { Plastic, Paper, Glass, Organic, Metal }