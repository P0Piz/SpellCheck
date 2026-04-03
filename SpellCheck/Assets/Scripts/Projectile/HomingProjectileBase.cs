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

    [Header("Augment Runtime")]
    public bool enableChain = false;
    public bool enablePierce = false;
    public bool enableSplit = false;

    [Tooltip("How many extra enemies a chain shot can jump to.")]
    public int maxChainJumps = 3;

    [Tooltip("How many extra enemies a piercing shot can pass through.")]
    public int maxPierceHits = 1;

    [Tooltip("How far split checks for nearby enemies around the hit target.")]
    public float splitRadius = 3f;

    protected Transform target;

    private EnemyBase forcedTarget;
    private float retargetTimer;
    private float lifeTimer;

    private int remainingChainJumps;
    private int remainingPierceHits;

    private bool straightLineMode = false;
    private Vector3 straightLineDirection;

    private HashSet<EnemyBase> hitEnemies = new HashSet<EnemyBase>();

    protected void Start()
    {
        remainingChainJumps = maxChainJumps;
        remainingPierceHits = maxPierceHits;
        AcquireTarget();
    }

    protected void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (!straightLineMode)
        {
            retargetTimer += Time.deltaTime;
            if (retargetTimer >= retargetInterval)
            {
                retargetTimer = 0f;
                AcquireTarget();
            }
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
        if (straightLineMode)
            return;

        if (forcedTarget != null)
        {
            if (forcedTarget.gameObject != null && !hitEnemies.Contains(forcedTarget))
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
            GameObject enemyObject = enemies[i];
            if (enemyObject == null)
                continue;

            EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
            if (enemy == null)
                continue;

            if (hitEnemies.Contains(enemy))
                continue;

            float d = (enemyObject.transform.position - pos).sqrMagnitude;
            if (d < bestDistSqr)
            {
                bestDistSqr = d;
                best = enemyObject.transform;
            }
        }

        target = best;
    }

    protected void HomeAndMove()
    {
        if (straightLineMode)
        {
            transform.position += straightLineDirection * moveSpeed * Time.deltaTime;
            return;
        }

        Vector3 forwardDir = transform.forward;

        if (target != null)
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
        if (hitObject == null)
            return;

        if (hitObject.CompareTag("Spell"))
            return;

        if (hitObject.CompareTag(enemyTag))
        {
            EnemyBase enemy = hitObject.GetComponent<EnemyBase>();
            if (enemy != null)
                OnHitEnemy(enemy);

            return;
        }

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

        if (enableSplit)
            DamageNearbyEnemies(enemy);

        bool chained = false;

        if (enableChain && remainingChainJumps > 0)
        {
            remainingChainJumps--;
            forcedTarget = null;
            target = FindClosestUntouchedEnemy(enemy.transform.position);

            if (target != null)
                chained = true;
        }

        if (chained)
            return;

        if (enablePierce && remainingPierceHits > 0)
        {
            remainingPierceHits--;

            straightLineMode = true;
            straightLineDirection = transform.forward.normalized;
            forcedTarget = null;
            target = null;
            return;
        }

        Destroy(gameObject);
    }

    void DamageNearbyEnemies(EnemyBase mainEnemy)
    {
        Collider[] hits = Physics.OverlapSphere(mainEnemy.transform.position, splitRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
                continue;

            GameObject go = hits[i].gameObject;

            if (!go.CompareTag(enemyTag))
                continue;

            EnemyBase enemy = go.GetComponent<EnemyBase>();
            if (enemy == null)
                continue;

            if (enemy == mainEnemy)
                continue;

            if (hitEnemies.Contains(enemy))
                continue;

            hitEnemies.Add(enemy);
            enemy.TakeDamage(this);
        }
    }

    Transform FindClosestUntouchedEnemy(Vector3 fromPosition)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        if (enemies == null || enemies.Length == 0)
            return null;

        Transform best = null;
        float bestDistSqr = float.PositiveInfinity;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemyObject = enemies[i];
            if (enemyObject == null)
                continue;

            EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
            if (enemy == null)
                continue;

            if (hitEnemies.Contains(enemy))
                continue;

            float d = (enemyObject.transform.position - fromPosition).sqrMagnitude;
            if (d < bestDistSqr)
            {
                bestDistSqr = d;
                best = enemyObject.transform;
            }
        }

        return best;
    }
}