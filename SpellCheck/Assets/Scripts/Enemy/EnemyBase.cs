using UnityEngine;

using static Elements;

public class EnemyBase : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float turnSpeed = 360f; // degrees per second

    [Header("Life")]
    public float life = 2;

    [Header("Element")]
    public Elements.elements element = elements.Null;


    protected Transform player;
    protected WaveSpawnerJson spawner;

    protected void TakeDamage(GameObject source)
    {
        HomingProjectileBase sourceProjectile = source.GetComponent<HomingProjectileBase>();
        // Placeholder damage logic; you can expand this with actual damage values, resistances, etc.
        if (Elements.WeakTo(sourceProjectile.element, element))
        {
            life -= sourceProjectile.damage * 2f; // Double damage if weak
            Debug.Log($"{name} is weak to {source.name}! Double damage taken.");
        }else
        {
            life -= sourceProjectile.damage;
            Debug.Log($"{name} is not weak to {source.name}. Normal damage taken.");
        }
        Debug.Log($"{name} took damage from {source.name}. Remaining life: {life}");

        if (life <= 0)
        {
            Debug.Log($"{name} was destroyed by {source.name}.");
            Destroy(gameObject);
        }
    }

    protected void Start()
    {
        AcquireClosestTarget();

        GameObject manager = GameObject.FindGameObjectWithTag("Manager");

        if (manager != null)
            spawner = manager.GetComponent<WaveSpawnerJson>();
    }

    protected void Update()
    {
        HomeAndMove();
    }

    // Finds the closest player by tag
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

    // Turns toward target (if any) and moves forward
    protected void HomeAndMove()
    {
        Vector3 forwardDir = transform.forward;

        if (player)
        {
            Vector3 toTarget = (player.position - transform.position);
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
    protected void OnTriggerEnter(Collider other)
    {
        HandleImpact(other.gameObject);
    }

    // Physics collision
    protected void OnCollisionEnter(Collision collision)
    {
        HandleImpact(collision.gameObject);
    }

    // Debug + destroy on any impact for now
    protected void HandleImpact(GameObject hitObject)
    {
        if (hitObject.CompareTag("Spell"))
        {
            TakeDamage(hitObject);
            spawner.NotifyEnemyDied(gameObject);
            Destroy(hitObject); // Destroy the spell on impact; you can change this if you want different behavior
        }
        else if (hitObject.CompareTag("Player"))
        {
            Debug.Log($"{name} hit the player!");
            // You can add logic here to damage the player, trigger effects, etc.
            Destroy(gameObject);
        }
         else
        {
            Debug.Log($"{name} hit {hitObject.name}.");
        }
    }
}