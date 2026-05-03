using UnityEngine;

public static class UpgradeEffects
{
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
                if (playerCombat != null) playerCombat.attackRate = Mathf.Max(0.1f, playerCombat.attackRate - data.value);
                break;
            case UpgradeType.IncreaseAttackRange:
                if (playerCombat != null) playerCombat.attackRange += data.value;
                break;
            case UpgradeType.ReduceTimeDrain:
                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.PermanentDrainModifier = Mathf.Max(0.1f, TimeManager.Instance.PermanentDrainModifier - data.value);
                }
                break;
            case UpgradeType.RestoreTime:
                if (TimeManager.Instance != null) TimeManager.Instance.AddTime(data.value);
                break;
        }
    }
}
