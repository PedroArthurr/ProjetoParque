using UnityEngine;

[CreateAssetMenu(menuName = "Game/Build Recipe")]
public class BuildRecipe : ScriptableObject
{
    public string id;
    public GameObject prefab;
    public ItemCost[] costs;
}

[System.Serializable]
public struct ItemCost
{
    public ItemType type;
    public int amount;
}