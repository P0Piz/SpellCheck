using UnityEngine;

public abstract class HomingProjectileBase : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("Enemies should use this tag.")]
    public string enemyTag = "Enemy";

    [Tooltip("How often to re-scan for the closest enemy (seconds).")]
    public float retargetInterval = 0.25f;

    [Header("Movement")]
    public float moveSpeed = 14f;
    public float turnSpeed = 360f; // degrees per second

    [Header("Lifetime")]
    public float maxLifetime = 6f;

    protected Transform target;

    float retargetTimer;
    float lifeTimer;

    protected virtual void Start()
    {
        AcquireClosestTarget();
    }

    protected virtual void Update()
    {
        // Self-destruct if it lives too long
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            Debug.Log($"{name} expired (lifetime).");
            Destroy(gameObject);
            return;
        }

        // Periodically retarget (so it can swap if a closer enemy appears)
        retargetTimer += Time.deltaTime;
        if (retargetTimer >= retargetInterval)
        {
            retargetTimer = 0f;
            AcquireClosestTarget();
        }

        HomeAndMove();
    }

    // Finds the closest enemy by tag
    protected virtual void AcquireClosestTarget()
    {
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
            GameObject e = enemies[i];
            if (!e) continue;

            float d = (e.transform.position - pos).sqrMagnitude;
            if (d < bestDistSqr)
            {
                bestDistSqr = d;
                best = e.transform;
            }
        }

        target = best;
    }

    // Turns toward target (if any) and moves forward
    protected virtual void HomeAndMove()
    {
        Vector3 forwardDir = transform.forward;

        if (target)
        {
            Vector3 toTarget = (target.position - transform.position);
            toTarget.y = 0f; //keeps the projectiles on flat plane; remove if you want full 3D homing

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

    // Trigger-based collision
    protected virtual void OnTriggerEnter(Collider other)
    {
        HandleImpact(other.gameObject);
    }

    // Physics collision
    protected virtual void OnCollisionEnter(Collision collision)
    {
        HandleImpact(collision.gameObject);
    }

    // Debug + destroy on any impact for now
    protected virtual void HandleImpact(GameObject hitObject)
    {
        Debug.Log($"{name} hit {hitObject.name} and was destroyed.");
        Destroy(gameObject);
    }
}