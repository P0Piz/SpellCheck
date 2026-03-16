using UnityEngine;
using static Elements;

public class HomingProjectileBase : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("Enemies should use this tag.")]
    public string enemyTag = "Enemy";

    [Tooltip("How often to re-scan for the target (seconds).")]
    public float retargetInterval = 0.25f;

    [Header("Movement")]
    public float moveSpeed = 14f;
    public float turnSpeed = 360f;

    [Header("Lifetime")]
    public float maxLifetime = 6f;

    [Header("Element")]
    public Elements.elements element = elements.Null;

    [Header("Damage")]
    public float damage = 1f;

    protected Transform target;

    private EnemyBase forcedTarget;
    float retargetTimer;
    float lifeTimer;

    protected void Start()
    {
        AcquireTarget();
    }

    protected void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            Debug.Log($"{name} expired (lifetime).");
            Destroy(gameObject);
            return;
        }

        retargetTimer += Time.deltaTime;
        if (retargetTimer >= retargetInterval)
        {
            retargetTimer = 0f;
            AcquireTarget();
        }

        HomeAndMove();
    }

    public void SetForcedTarget(EnemyBase enemy)
    {
        forcedTarget = enemy;

        if (forcedTarget != null)
            target = forcedTarget.transform;
    }

    protected void AcquireTarget()
    {
        if (forcedTarget != null)
        {
            if (forcedTarget.gameObject != null)
            {
                target = forcedTarget.transform;
                return;
            }
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        if (enemies == null || enemies.Length == 0)
        {
            target = null;
            return;
        }

        Transform best = null;
        float bestDistSqr = float.PositiveInfinity;
        Vector3 pos = transform.position;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemy = enemies[i];
            if (!enemy) continue;

            float d = (enemy.transform.position - pos).sqrMagnitude;
            if (d < bestDistSqr)
            {
                bestDistSqr = d;
                best = enemy.transform;
            }
        }

        target = best;
    }

    protected void HomeAndMove()
    {
        Vector3 forwardDir = transform.forward;

        if (target)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Vector3 desiredDir = toTarget.normalized;
                float maxRadians = turnSpeed * Mathf.Deg2Rad * Time.deltaTime;

                Vector3 newDir = Vector3.RotateTowards(forwardDir, desiredDir, maxRadians, 0f);
                transform.rotation = Quaternion.LookRotation(newDir, Vector3.up);
            }
        }

        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    protected void OnTriggerEnter(Collider other)
    {
        HandleImpact(other.gameObject);
    }

    protected void OnCollisionEnter(Collision collision)
    {
        HandleImpact(collision.gameObject);
    }

    protected void HandleImpact(GameObject hitObject)
    {
        if (hitObject.CompareTag(enemyTag))
            return;

        if (hitObject.CompareTag("Spell"))
            return;

        Debug.Log($"{name} hit {hitObject.name} and was destroyed.");
        Destroy(gameObject);
    }

    public void OnHitEnemy(EnemyBase enemy)
    {
        if (enemy == null)
        {
            Destroy(gameObject);
            return;
        }

        enemy.TakeDamage(this);
        Destroy(gameObject);
    }
}