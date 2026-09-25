using UnityEngine;

public static class UpgradeEffects
{
    // Techos absolutos, aunque se combinen varias mejoras: sin ellos el alcance llegaba
    // a cubrir la pantalla (todo moría al aparecer) y el reloj casi no drenaba.
    public const float MAX_ATTACK_RANGE = 6.5f;
    public const float MIN_ATTACK_RATE = 0.25f;
    public const float MIN_DRAIN_MODIFIER = 0.5f;

    public static void ApplyUpgrade(UpgradeData data)
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        PlayerMovement playerMovement = playerObj.GetComponent<PlayerMovement>();
        PlayerCombat playerCombat = playerObj.GetComponent<PlayerCombat>();

        switch (data.type)
        {
            case UpgradeType.IncreaseDamage:
                if (playerCombat != null) playerCombat.attackDamage += (int)data.value;
                break;
            case UpgradeType.IncreaseMoveSpeed:
                if (playerMovement != null) playerMovement.moveSpeed += data.value;
                break;
            case UpgradeType.IncreaseAttackSpeed:
                if (playerCombat != null) playerCombat.attackRate = Mathf.Max(MIN_ATTACK_RATE, playerCombat.attackRate - data.value);
                break;
            case UpgradeType.IncreaseAttackRange:
                if (playerCombat != null) playerCombat.attackRange = Mathf.Min(MAX_ATTACK_RANGE, playerCombat.attackRange + data.value);
                break;
            case UpgradeType.ReduceTimeDrain:
                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.PermanentDrainModifier = Mathf.Max(MIN_DRAIN_MODIFIER, TimeManager.Instance.PermanentDrainModifier - data.value);
                }
                break;
            case UpgradeType.RestoreTime:
                if (TimeManager.Instance != null) TimeManager.Instance.AddTime(data.value);
                break;
            case UpgradeType.DashCooldown:
                if (playerMovement != null) playerMovement.dashCooldown = Mathf.Max(0.3f, playerMovement.dashCooldown * (1f - data.value));
                break;
            case UpgradeType.AttackRangePercent:
                if (playerCombat != null) playerCombat.attackRange = Mathf.Min(MAX_ATTACK_RANGE, playerCombat.attackRange * (1f + data.value));
                break;
            case UpgradeType.DashWave:
                if (playerCombat != null) playerCombat.AddDashWaveLevel();
                break;
            case UpgradeType.DeadZone:
                if (playerCombat != null) playerCombat.AddDeadZoneLevel();
                break;
            // TimeChain, Magnetism, VoraciousClock y Fragmentation reaccionan a las bajas:
            // su estado vive en UpgradeManager (ApplyRunModifier).
        }
    }
}
