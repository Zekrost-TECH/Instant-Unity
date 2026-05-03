using UnityEngine;

public enum UpgradeType
{
    IncreaseDamage,
    IncreaseMoveSpeed,
    IncreaseAttackSpeed,
    IncreaseAttackRange,
    ReduceTimeDrain,
    RestoreTime
}

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Instant/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    public string id;
    public string title;
    [TextArea]
    public string description;
    public Sprite icon;
    public bool isRare;
    
    public UpgradeType type;
    public float value;
}
