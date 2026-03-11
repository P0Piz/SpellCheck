using UnityEngine;

using static Elements;

public class EnemyBase : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float turnSpeed = 360f; // degrees per second

    [Header("Life")]
    public float life = 2f;

    [Header("Element")]
    public Elements.elements element = elements.Null;

    [Header("Status")]
    public bool isFrozen;
    public float frozenTimer;

    protected Transform player;
    protected WaveSpawnerJson spawner;

    protected void Start()
    {
        AcquireClosestTarget();

        GameObject manager = GameObject.FindGameObjectWithTag("Manager");
        if (manager != null)
            spawner = manager.GetComponent<WaveSpawnerJson>();
    }

    protected void Update()
    {
        UpdateFrozenState();

        if (!isFrozen)
        {
            HomeAndMove();
        }
    }

    void UpdateFrozenState()
    {
        if (frozenTimer > 0f)
        {
            frozenTimer -= Time.deltaTime;

            if (frozenTimer <= 0f)
            {
                frozenTimer = 0f;
                isFrozen = false;
                Debug.Log($"{name} thawed out.");
            }
        }
    }

    public void ApplyFreeze(float duration)
    {
        isFrozen = true;
        frozenTimer = Mathf.Max(frozenTimer, duration);
        Debug.Log($"{name} was frozen for {duration} seconds.");
    }

    public void TakeDamage(HomingProjectileBase sourceProjectile)
    {
        if (sourceProjectile == null)
            return;

        float finalDamage = sourceProjectile.damage;

        if (Elements.WeakTo(sourceProjectile.element, element))
        {
            finalDamage *= 2f;
            Debug.Log($"{name} is weak to {sourceProjectile.name}! Double damage taken.");
        }
        else
        {
            Debug.Log($"{name} is not weak to {sourceProjectile.name}. Normal damage taken.");
        }

        life -= finalDamage;
        Debug.Log($"{name} took damage from {sourceProjectile.name}. Remaining life: {life}");

        if (life <= 0f)
        {
            Debug.Log($"{name} was destroyed by {sourceProjectile.name}.");

            if (spawner != null)
                spawner.NotifyEnemyDied(gameObject);

            Destroy(gameObject);
        }
    }

    protected void AcquireClosestTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players == null || players.Length == 0)
        {
            player = null;
            return;
        }

        Transform best = null;
        float bestDistSqr = float.PositiveInfinity;
        Vector3 pos = transform.position;

        for (int i = 0; i < players.Length; i++)
        {
            GameObject e = players[i];
            if (!e) continue;

            float d = (e.transform.position - pos).sqrMagnitude;
            if (d < bestDistSqr)
            {
                bestDistSqr = d;
                best = e.transform;
            }
        }

        player = best;
    }

    protected void HomeAndMove()
    {
        Vector3 forwardDir = transform.forward;

        if (player)
        {
            Vector3 toTarget = (player.position - transform.position);
            // toTarget.x = 0f;
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
        if (hitObject.CompareTag("Spell"))
        {
            HomingProjectileBase projectile = hitObject.GetComponent<HomingProjectileBase>();

            if (projectile != null)
            {
                projectile.OnHitEnemy(this);
            }
            else
            {
                Debug.LogWarning("Spell hit enemy but no HomingProjectileBase was found on: " + hitObject.name);
                Destroy(hitObject);
            }
        }
        else if (hitObject.CompareTag("Player"))
        {
            Debug.Log($"{name} hit the player!");

            PlayerHealth playerHealth = hitObject.GetComponent<PlayerHealth>();

            if (playerHealth == null)
                playerHealth = hitObject.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
                Debug.Log("Damage applied to player");
            }
            else
            {
                Debug.LogWarning("PlayerHealth not found on hit object or parent: " + hitObject.name);
            }

            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"{name} hit {hitObject.name}.");
        }
    }
}