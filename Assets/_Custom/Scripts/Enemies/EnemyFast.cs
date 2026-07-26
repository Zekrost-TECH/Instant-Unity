using UnityEngine;

public class EnemyFast : EnemyBase
{
    [Header("Fast Behavior")]
    [Tooltip("Velocidad de persecución (más rápida que el Fodder).")]
    public float moveSpeed = 3.2f;
    [Tooltip("Frecuencia del zigzag.")]
    public float zigzagFrequency = 2f;
    [Tooltip("Amplitud del zigzag.")]
    public float zigzagAmplitude = 2f;

    private float spawnTime;

    protected override void OnEnable()
    {
        base.OnEnable();
        spawnTime = Time.time;
    }

    protected override void UpdateMovement(float deltaTime)
    {
        if (playerTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 directionToPlayer = ((Vector2)playerTransform.position - rb.position).normalized;
        Vector2 perpendicular = new Vector2(-directionToPlayer.y, directionToPlayer.x);

        float zigzagOffset = Mathf.Sin((Time.time - spawnTime) * zigzagFrequency) * zigzagAmplitude;

        rb.linearVelocity = (directionToPlayer * moveSpeed) + (perpendicular * zigzagOffset);
        FaceDirection(directionToPlayer);
    }
}
