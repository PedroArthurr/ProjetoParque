using UnityEngine;

public class UICollectTargetRegistry : MonoBehaviour
{
    public Canvas canvas;
    public Animator binsAnimator;

    public RectTransform plasticSlot, metalSlot, glassSlot, paperSlot, organicSlot;

    public UIBezierAuthor globalBezier;
    public UIBezierAuthor plasticBezier, metalBezier, glassBezier, paperBezier, organicBezier;

    public RectTransform GetTarget(ItemType t)
    {
        switch (t)
        {
            case ItemType.Plastic: return plasticSlot;
            case ItemType.Metal: return metalSlot;
            case ItemType.Glass: return glassSlot;
            case ItemType.Paper: return paperSlot;
            case ItemType.Organic: return organicSlot;
            default: return null;
        }
    }

    public UIBezierAuthor GetAuthor(ItemType t)
    {
        UIBezierAuthor a = null;
        switch (t)
        {
            case ItemType.Plastic: a = plasticBezier; break;
            case ItemType.Metal: a = metalBezier; break;
            case ItemType.Glass: a = glassBezier; break;
            case ItemType.Paper: a = paperBezier; break;
            case ItemType.Organic: a = organicBezier; break;
        }
        return a ? a : globalBezier;
    }
}
