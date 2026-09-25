using UnityEngine;

public enum UpgradeType
{
    IncreaseDamage,
    IncreaseMoveSpeed,
    IncreaseAttackSpeed,
    IncreaseAttackRange,
    ReduceTimeDrain,
    RestoreTime,
    // Sinergias del GDD. Se añaden al final: los .asset guardan el enum como entero.
    DashCooldown,
    AttackRangePercent,
    TimeChain,
    Magnetism,
    DashWave,
    VoraciousClock,
    Fragmentation,
    DeadZone
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
    [Tooltip("Veces que puede elegirse por partida. 0 = sin límite. Al llegar al tope deja de ofrecerse.")]
    public int maxStacks = 0;
}
