using System.Collections.Generic;
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
    public float turnSpeed = 360f; // degrees per second

    [Header("Lifetime")]
    public float maxLifetime = 6f;

    [Header("Element")]
    public Elements.elements element = elements.Null;

    [Header("Damage")]
    public float damage = 1f;

    [Header("Modifier - Unstable")]
    public bool unstable;
    [Tooltip("Damage multiplier when Unstable is active.")]
    public float unstableDamageMultiplier = 1.5f;

    [Header("Modifier - Greater")]
    public bool greater;
    [Tooltip("Scale multiplier when Greater is active.")]
    public float greaterScaleMultiplier = 1.75f;
    [Tooltip("Speed multiplier when Greater is active.")]
    public float greaterSpeedMultiplier = 0.65f;

    [Header("Modifier - Frozen")]
    public bool frozen;
    [Tooltip("How long the enemy is frozen for.")]
    public float frozenDuration = 2f;

    [Header("Modifier - Chained")]
    public bool chained;
    [Tooltip("How many extra enemies this spell can bounce to.")]
    public int chainCount = 2;
    [Tooltip("How far it can look for the next enemy.")]
    public float chainRange = 8f;

    [Header("Modifier - Rapid")]
    public bool rapid;
    [Tooltip("How many projectiles total to create.")]
    public int rapidProjectileCount = 3;
    [Tooltip("Spread angle across all rapid shots.")]
    public float rapidSpreadAngle = 20f;
    [Tooltip("Scale multiplier for rapid child projectiles.")]
    public float rapidChildScaleMultiplier = 0.7f;
    [Tooltip("Damage multiplier for rapid child projectiles.")]
    public float rapidDamageMultiplier = 0.7f;

    [Header("Modifier - Delayed")]
    public bool delayed;
    [Tooltip("How long the projectile waits before moving.")]
    public float delayedDuration = 0.4f;

    protected Transform target;

    float retargetTimer;
    float lifeTimer;
    float delayTimer;

    int remainingChains;
    bool initialized;
    bool isRapidChild;

    readonly HashSet<EnemyBase> hitEnemies = new HashSet<EnemyBase>();

    protected void Start()
    {
        InitializeModifiers();
        AcquireTarget();
    }

    protected void Update()
    {
        if (!initialized)
            return;

        // Lifetime
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            Debug.Log($"{name} expired (lifetime).");
            Destroy(gameObject);
            return;
        }

        // Delay before movement
        if (delayTimer > 0f)
        {
            delayTimer -= Time.deltaTime;
            return;
        }

        // Periodic retarget
        retargetTimer += Time.deltaTime;
        if (retargetTimer >= retargetInterval)
        {
            retargetTimer = 0f;
            AcquireTarget();
        }

        HomeAndMove();
    }

    void InitializeModifiers()
    {
        if (initialized)
            return;

        initialized = true;

        // Unstable
        if (unstable)
        {
            damage *= unstableDamageMultiplier;
        }

        // Greater
        if (greater)
        {
            transform.localScale *= greaterScaleMultiplier;
            moveSpeed *= greaterSpeedMultiplier;
        }

        // Chained
        remainingChains = chainCount;

        // Delayed
        if (delayed)
        {
            delayTimer = delayedDuration;
        }

        // Rapid
        if (rapid && !isRapidChild)
        {
            SpawnRapidProjectiles();
            Destroy(gameObject);
            return;
        }
    }

    void SpawnRapidProjectiles()
    {
        int count = Mathf.Max(2, rapidProjectileCount);

        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0f : (float)i / (count - 1);
            float angle = Mathf.Lerp(-rapidSpreadAngle * 0.5f, rapidSpreadAngle * 0.5f, t);

            Quaternion rotation = transform.rotation * Quaternion.Euler(0f, angle, 0f);
            GameObject clone = Instantiate(gameObject, transform.position, rotation);

            HomingProjectileBase proj = clone.GetComponent<HomingProjectileBase>();
            if (proj != null)
            {
                proj.isRapidChild = true;
                proj.rapid = false; // prevent recursive rapid spawning
                proj.damage *= rapidDamageMultiplier;
                proj.transform.localScale *= rapidChildScaleMultiplier;
            }
        }
    }

    protected void AcquireTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        if (enemies == null || enemies.Length == 0)
        {
            target = null;
            return;
        }

        // Unstable = random target
        if (unstable)
        {
            List<Transform> validTargets = new List<Transform>();

            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null) continue;

                EnemyBase eb = enemies[i].GetComponent<EnemyBase>();
                if (eb == null) continue;
                if (hitEnemies.Contains(eb)) continue;

                validTargets.Add(enemies[i].transform);
            }

            if (validTargets.Count > 0)
            {
                target = validTargets[Random.Range(0, validTargets.Count)];
                return;
            }
        }

        // Default = closest target
        Transform best = null;
        float bestDistSqr = float.PositiveInfinity;
        Vector3 pos = transform.position;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject e = enemies[i];
            if (!e) continue;

            EnemyBase eb = e.GetComponent<EnemyBase>();
            if (eb != null && hitEnemies.Contains(eb)) continue;

            float d = (e.transform.position - pos).sqrMagnitude;
            if (d < bestDistSqr)
            {
                bestDistSqr = d;
                best = e.transform;
            }
        }

        target = best;
    }

    protected void HomeAndMove()
    {
        Vector3 forwardDir = transform.forward;

        if (target)
        {
            Vector3 toTarget = (target.position - transform.position);
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
        // Let EnemyBase handle spell-vs-enemy impact so damage/freeze/chain only happens once
        if (hitObject.CompareTag(enemyTag))
            return;

        // Ignore other spells
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

        if (hitEnemies.Contains(enemy))
            return;

        hitEnemies.Add(enemy);

        enemy.TakeDamage(this);

        if (enemy == null)
            return;

        if (frozen)
        {
            enemy.ApplyFreeze(frozenDuration);
        }

        if (chained && remainingChains > 0)
        {
            EnemyBase nextEnemy = FindNextChainTarget(enemy);

            if (nextEnemy != null)
            {
                remainingChains--;

                target = nextEnemy.transform;

                Vector3 dir = (nextEnemy.transform.position - transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }

                // small push forward so it doesn't immediately re-collide on the same spot
                transform.position += transform.forward * 0.5f;
                return;
            }
        }

        Destroy(gameObject);
    }

    EnemyBase FindNextChainTarget(EnemyBase currentEnemy)
    {
        Collider[] hits = Physics.OverlapSphere(currentEnemy.transform.position, chainRange);

        EnemyBase best = null;
        float bestDistSqr = float.PositiveInfinity;
        Vector3 origin = currentEnemy.transform.position;

        for (int i = 0; i < hits.Length; i++)
        {
            EnemyBase candidate = hits[i].GetComponentInParent<EnemyBase>();
            if (candidate == null) continue;
            if (candidate == currentEnemy) continue;
            if (hitEnemies.Contains(candidate)) continue;

            float d = (candidate.transform.position - origin).sqrMagnitude;
            if (d < bestDistSqr)
            {
                bestDistSqr = d;
                best = candidate;
            }
        }

        return best;
    }
}