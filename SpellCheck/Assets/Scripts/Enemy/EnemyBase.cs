using UnityEngine;
using static Elements;

public class EnemyBase : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float turnSpeed = 360f;

    [Header("Life")]
    public float life = 2f;

    [Header("Element")]
    public Elements.elements element = elements.Null;

    [Header("Spell Prompt")]
    public EnemySpellPrompt spellPrompt;
    public SpellDatabaseSO spellDatabase;

    [Header("Status")]
    public bool isFrozen;
    public float frozenTimer;

    protected Transform player;
    protected WaveSpawnerJson spawner;

    private SpellDefinition assignedSpell;
    private bool isActiveTarget;

    void Awake()
    {
        GameObject manager = GameObject.FindGameObjectWithTag("Manager");
        if (manager != null)
            spawner = manager.GetComponent<WaveSpawnerJson>();

        if (spellPrompt == null)
            spellPrompt = GetComponentInChildren<EnemySpellPrompt>(true);

        if (spellPrompt != null)
            spellPrompt.HideSpell();
    }

    void Start()
    {
        AcquireClosestTarget();
    }

    void Update()
    {
        UpdateFrozenState();

        if (!isFrozen)
            HomeAndMove();
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
            }
        }
    }

    public void ApplyFreeze(float duration)
    {
        isFrozen = true;
        frozenTimer = Mathf.Max(frozenTimer, duration);
    }

    public void TakeDamage(HomingProjectileBase sourceProjectile)
    {
        if (sourceProjectile == null)
            return;

        float finalDamage = sourceProjectile.damage;

        if (Elements.WeakTo(sourceProjectile.element, element))
            finalDamage *= 2f;

        life -= finalDamage;

        if (life <= 0f)
        {
            if (spawner != null)
                spawner.NotifyEnemyDied(gameObject);

            Destroy(gameObject);
        }
    }

    public void SetActiveTarget(bool active)
    {
        isActiveTarget = active;

        if (!active)
        {
            if (spellPrompt != null)
                spellPrompt.HideSpell();

            return;
        }

        RefreshAssignedSpell();
    }

    public void RefreshAssignedSpell()
    {
        if (spellDatabase == null)
        {
            Debug.LogWarning($"{name}: No SpellDatabase assigned.");
            assignedSpell = null;

            if (spellPrompt != null)
                spellPrompt.HideSpell();

            return;
        }

        Elements.elements counter = GetCounterElement();
        assignedSpell = spellDatabase.GetRandomSpellByElement(counter);

        if (assignedSpell == null)
        {
            Debug.LogWarning($"{name}: No spell found for counter element {counter}.");
            if (spellPrompt != null)
                spellPrompt.HideSpell();
            return;
        }

        if (spellPrompt != null)
            spellPrompt.ShowSpell(assignedSpell.spellName);
    }

    public SpellDefinition GetAssignedSpell()
    {
        return assignedSpell;
    }

    public bool IsActiveTarget()
    {
        return isActiveTarget;
    }

    public void SetTypedPreview(string typed)
    {
        if (spellPrompt != null)
            spellPrompt.SetTypedText(typed);
    }

    public void ClearTypedPreview()
    {
        if (spellPrompt != null)
            spellPrompt.ClearTypedText();
    }

    Elements.elements GetCounterElement()
    {
        switch (element)
        {
            case Elements.elements.Fire:
                return Elements.elements.Water;

            case Elements.elements.Water:
                return Elements.elements.Earth;

            case Elements.elements.Earth:
                return Elements.elements.Fire;

            default:
                return Elements.elements.Null;
        }
    }

    void AcquireClosestTarget()
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
            GameObject candidate = players[i];
            if (!candidate) continue;

            float d = (candidate.transform.position - pos).sqrMagnitude;
            if (d < bestDistSqr)
            {
                bestDistSqr = d;
                best = candidate.transform;
            }
        }

        player = best;
    }

    void HomeAndMove()
    {
        Vector3 forwardDir = transform.forward;

        if (player)
        {
            Vector3 toTarget = player.position - transform.position;
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

    void OnTriggerEnter(Collider other)
    {
        HandleImpact(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleImpact(collision.gameObject);
    }

    void HandleImpact(GameObject hitObject)
    {
        if (hitObject.CompareTag("Spell"))
        {
            HomingProjectileBase projectile = hitObject.GetComponent<HomingProjectileBase>();

            if (projectile != null)
                projectile.OnHitEnemy(this);
            else
                Destroy(hitObject);
        }
        else if (hitObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = hitObject.GetComponent<PlayerHealth>();

            if (playerHealth == null)
                playerHealth = hitObject.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
                playerHealth.TakeDamage(1);

            Destroy(gameObject);
        }
    }
}