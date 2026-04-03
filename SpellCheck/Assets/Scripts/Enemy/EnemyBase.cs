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

    [Header("Drops")]
    public int coinDrop = 1;
    public int scoreDrop = 100;

    [Header("Spell Prompt")]
    public EnemySpellPrompt spellPrompt;
    public SpellDatabaseSO spellDatabase;

    [Header("Death Effect")]
    public GameObject deathSpawnPrefab;
    public float deathObjectLifetime = 3f;

    [Header("Impact Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private Vector2 impactPitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField] private float impactVolume = 1f;

    protected Transform player;
    protected WaveSpawnerJson spawner;

    private SpellDefinition assignedSpell;
    private bool isActiveTarget;

    [Header("Stun")]
    [SerializeField] private bool isStunned = false;
    [SerializeField] private float stunTimer = 0f;

    void Awake()
    {
        GameObject manager = GameObject.FindGameObjectWithTag("Manager");

        if (manager == null)
        {
            Debug.LogError("No object with tag 'Manager' found in scene!");
            return;
        }

        audioSource = manager.GetComponent<AudioSource>();
        spawner = manager.GetComponent<WaveSpawnerJson>();

        if (spawner == null)
            Debug.LogError("Manager object does not have WaveSpawnerJson!");

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
        UpdateStun();

        if (isStunned)
            return;

        MoveForward();
    }

    void UpdateStun()
    {
        if (!isStunned)
            return;

        stunTimer -= Time.deltaTime;

        if (stunTimer <= 0f)
        {
            stunTimer = 0f;
            isStunned = false;
        }
    }

    public void ApplyStun(float duration)
    {
        if (duration <= 0f)
            return;

        isStunned = true;
        stunTimer = Mathf.Max(stunTimer, duration);
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

            PlayerManager playerManager = null;

            if (player != null)
            {
                playerManager = player.GetComponent<PlayerManager>();

                if (playerManager == null)
                    playerManager = player.GetComponentInParent<PlayerManager>();
            }

            if (playerManager != null)
                playerManager.AddScore(scoreDrop);

            SpawnDeathObject();

            Destroy(gameObject);
        }
    }

    void SpawnDeathObject()
    {
        if (deathSpawnPrefab == null)
            return;

        GameObject obj = Instantiate(
            deathSpawnPrefab,
            transform.position,
            Quaternion.identity
        );

        Destroy(obj, deathObjectLifetime);
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
            if (!candidate)
                continue;

            float d = (candidate.transform.position - pos).sqrMagnitude;
            if (d < bestDistSqr)
            {
                bestDistSqr = d;
                best = candidate.transform;
            }
        }

        player = best;
    }

    void MoveForward()
    {
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
        if (hitObject == null)
            return;

        if (hitObject.CompareTag("Spell"))
        {
            HomingProjectileBase projectile = hitObject.GetComponent<HomingProjectileBase>();

            if (projectile != null)
            {
                PlayImpactSound();
                projectile.OnHitEnemy(this);
            }
            else
            {
                Destroy(hitObject);
            }
        }
        else if (hitObject.CompareTag("Player"))
        {
            PlayerManager playerManager = hitObject.GetComponent<PlayerManager>();

            if (playerManager == null)
                playerManager = hitObject.GetComponentInParent<PlayerManager>();

            if (playerManager != null)
                playerManager.TakeDamage(1);

            Destroy(gameObject);
        }
    }

    void PlayImpactSound()
    {
        if (audioSource == null || impactSound == null)
            return;

        audioSource.pitch = Random.Range(impactPitchRange.x, impactPitchRange.y);
        audioSource.PlayOneShot(impactSound, impactVolume);
    }

    public void PlayPromptShake()
    {
        if (spellPrompt != null)
            spellPrompt.PlayWrongShake();
    }
}