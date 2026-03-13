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
    public float turnSpeed = 360f;

    [Header("Lifetime")]
    public float maxLifetime = 6f;

    [Header("Element")]
    public Elements.elements element = elements.Null;

    [Header("Damage")]
    public float damage = 1f;

    [Header("Modifier - Unstable")]
    public bool unstable;
    public float unstableDamageMultiplier = 1.5f;

    [Header("Modifier - Greater")]
    public bool greater;
    public float greaterScaleMultiplier = 1.75f;
    public float greaterSpeedMultiplier = 0.65f;

    [Header("Modifier - Frozen")]
    public bool frozen;
    public float frozenDuration = 2f;

    [Header("Modifier - Chained")]
    public bool chained;
    public int chainCount = 2;
    public float chainRange = 8f;

    [Header("Modifier - Rapid")]
    public bool rapid;
    public int rapidProjectileCount = 3;
    public float rapidSpreadAngle = 20f;
    public float rapidChildScaleMultiplier = 0.7f;
    public float rapidDamageMultiplier = 0.7f;

    [Header("Modifier - Delayed")]
    public bool delayed;
    public float delayedDuration = 0.4f;

    protected Transform target;

    private EnemyBase forcedTarget;
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

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            Debug.Log($"{name} expired (lifetime).");
            Destroy(gameObject);
            return;
        }

        if (delayTimer > 0f)
        {
            delayTimer -= Time.deltaTime;
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

    void InitializeModifiers()
    {
        if (initialized)
            return;

        initialized = true;

        if (unstable)
            damage *= unstableDamageMultiplier;

        if (greater)
        {
            transform.localScale *= greaterScaleMultiplier;
            moveSpeed *= greaterSpeedMultiplier;
        }

        remainingChains = chainCount;

        if (delayed)
            delayTimer = delayedDuration;

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
                proj.rapid = false;
                proj.damage *= rapidDamageMultiplier;
                proj.transform.localScale *= rapidChildScaleMultiplier;

                if (forcedTarget != null)
                    proj.SetForcedTarget(forcedTarget);
            }
        }
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

        Transform best = null;
        float bestDistSqr = float.PositiveInfinity;
        Vector3 pos = transform.position;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemy = enemies[i];
            if (!enemy) continue;

            EnemyBase eb = enemy.GetComponent<EnemyBase>();
            if (eb != null && hitEnemies.Contains(eb)) continue;

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

        if (hitEnemies.Contains(enemy))
            return;

        hitEnemies.Add(enemy);

        enemy.TakeDamage(this);

        if (frozen)
            enemy.ApplyFreeze(frozenDuration);

        if (chained && remainingChains > 0)
        {
            EnemyBase nextEnemy = FindNextChainTarget(enemy);

            if (nextEnemy != null)
            {
                remainingChains--;
                forcedTarget = null;
                target = nextEnemy.transform;

                Vector3 dir = nextEnemy.transform.position - transform.position;
                dir.y = 0f;

                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

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