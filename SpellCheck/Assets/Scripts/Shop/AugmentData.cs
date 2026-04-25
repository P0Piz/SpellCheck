using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Augment")]
public class AugmentData : ScriptableObject
{
    public string augmentID;
    public string augmentName;
    [TextArea] public string description;

    public Sprite augmentImage;

    public int cost = 10;
}