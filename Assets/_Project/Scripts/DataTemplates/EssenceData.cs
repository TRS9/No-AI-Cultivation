using UnityEngine;

[CreateAssetMenu(fileName = "NewEssence", menuName = "Cultivation/Essence Data")]
public class EssenceData : ScriptableObject
{
    public string essenceName;
    public string description;
    public int qiValue;
    public Color essenceColor = Color.white;
    public Sprite icon;

    // You could even add a prefab here later for special visual effects!
    public GameObject collectionEffect;
}