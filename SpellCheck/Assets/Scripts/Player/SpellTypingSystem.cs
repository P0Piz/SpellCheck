using UnityEngine;
using TMPro;
using System.Collections;

public class SpellTypingSystem : MonoBehaviour
{
    [Header("Hidden Input")]
    public TMP_InputField inputField;

    [Header("Where spells spawn from")]
    public Transform spawnPoint;

    [Header("Wave / Targeting")]
    public WaveSpawnerJson waveSpawner;

    [Header("Timing")]
    [SerializeField] private float wrongClearDelay = 0.15f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typeSound;
    [SerializeField] private AudioClip wrongSound;
    [SerializeField] private Vector2 typePitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField] private Vector2 wrongPitchRange = new Vector2(0.9f, 1.0f);
    [SerializeField] private float typeVolume = 1f;
    [SerializeField] private float wrongVolume = 1f;

    [Header("Animation")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string windupBoolName = "IsWindingUp";
    [SerializeField] private string castTriggerName = "Cast";
    [SerializeField] private string failTriggerName = "Fail";

    private string lastValidText = "";
    private bool isCasting;
    private bool isClearingWrongInput;
    private Coroutine wrongInputRoutine;
    private EnemyBase lastPreviewEnemy;

    void Awake()
    {
        if (inputField != null)
        {
            inputField.contentType = TMP_InputField.ContentType.Custom;
            inputField.characterValidation = TMP_InputField.CharacterValidation.None;
            inputField.onValidateInput += ValidateInput;
            inputField.richText = false;
            inputField.onValueChanged.AddListener(OnInputChanged);
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        ForceFocus();
        SetWindup(false);
    }

    void OnDestroy()
    {
        if (inputField != null)
            inputField.onValueChanged.RemoveListener(OnInputChanged);
    }

    private char ValidateInput(string text, int charIndex, char addedChar)
    {
        if (char.IsLetter(addedChar))
            return char.ToLower(addedChar);

        if (addedChar == ' ')
        {
            if (text.Length == 0)
                return '\0';

            if (text[text.Length - 1] == ' ')
                return '\0';

            return addedChar;
        }

        return '\0';
    }

    void Update()
    {
        if (inputField != null && !inputField.isFocused)
            ForceFocus();
    }

    void LateUpdate()
    {
        if (inputField == null)
            return;

        if (inputField.text.Length > lastValidText.Length + 1)
        {
            inputField.text = lastValidText;
            inputField.caretPosition = inputField.text.Length;
        }

        lastValidText = inputField.text;
    }

    void OnInputChanged(string rawText)
    {
        if (isCasting || isClearingWrongInput)
            return;

        if (waveSpawner == null)
        {
            ClearLastPreview();
            SetWindup(false);
            return;
        }

        EnemyBase activeEnemy = waveSpawner.GetActiveEnemy();
        if (activeEnemy == null)
        {
            ClearLastPreview();
            SetWindup(false);
            return;
        }

        SpellDefinition assignedSpell = activeEnemy.GetAssignedSpell();
        if (assignedSpell == null)
        {
            activeEnemy.ClearTypedPreview();
            lastPreviewEnemy = activeEnemy;
            SetWindup(false);
            return;
        }

        if (lastPreviewEnemy != null && lastPreviewEnemy != activeEnemy)
            lastPreviewEnemy.ClearTypedPreview();

        lastPreviewEnemy = activeEnemy;

        string typed = string.IsNullOrWhiteSpace(rawText) ? "" : rawText.ToLower();
        string targetWord = assignedSpell.spellName.ToLower().Trim();

        activeEnemy.SetTypedPreview(typed);

        if (string.IsNullOrEmpty(typed))
        {
            SetWindup(false);
            return;
        }

        bool wrong = false;
        int checkLength = Mathf.Min(typed.Length, targetWord.Length);

        for (int i = 0; i < checkLength; i++)
        {
            if (typed[i] != targetWord[i])
            {
                wrong = true;
                break;
            }
        }

        if (typed.Length > targetWord.Length)
            wrong = true;

        if (wrong)
        {
            SetWindup(false);
            TriggerFailAnimation();
            PlayWrongSound();

            if (wrongInputRoutine != null)
                StopCoroutine(wrongInputRoutine);

            wrongInputRoutine = StartCoroutine(ClearWrongInputAfterDelay(activeEnemy));
            return;
        }

        PlayTypingSoundIfNewCharacter(rawText);

        // If partially correct, start / keep windup animation going
        if (typed.Length < targetWord.Length)
        {
            SetWindup(true);
            return;
        }

        // If exact match, cast
        if (typed == targetWord)
        {
            SetWindup(false);
            TriggerCastAnimation();
            CastAssignedSpell(activeEnemy, assignedSpell);
        }
    }

    IEnumerator ClearWrongInputAfterDelay(EnemyBase activeEnemy)
    {
        isClearingWrongInput = true;

        yield return new WaitForSeconds(wrongClearDelay);

        ClearAndRefocus(activeEnemy);

        isClearingWrongInput = false;
        wrongInputRoutine = null;
    }

    void CastAssignedSpell(EnemyBase activeEnemy, SpellDefinition assignedSpell)
    {
        if (isCasting)
            return;

        isCasting = true;

        if (wrongInputRoutine != null)
        {
            StopCoroutine(wrongInputRoutine);
            wrongInputRoutine = null;
        }

        if (activeEnemy != null)
            activeEnemy.ClearTypedPreview();

        if (assignedSpell.healAmount > 0)
        {
            PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.Heal(assignedSpell.healAmount);
        }

        if (assignedSpell.spawnPrefab != null && spawnPoint != null)
        {
            Vector3 pos = spawnPoint.TransformPoint(assignedSpell.spawnOffset);
            GameObject spawned = Instantiate(assignedSpell.spawnPrefab, pos, spawnPoint.rotation);

            if (assignedSpell.parentToSpawnPoint)
                spawned.transform.SetParent(spawnPoint, true);

            HomingProjectileBase projectile = spawned.GetComponent<HomingProjectileBase>();
            if (projectile != null)
            {
                projectile.SetForcedTarget(activeEnemy);
            }
        }

        if (waveSpawner != null)
            waveSpawner.AdvanceToNextEnemyImmediately(activeEnemy);

        ClearAndRefocus();
        isCasting = false;
    }

    void ClearAndRefocus(EnemyBase activeEnemy = null)
    {
        if (inputField != null)
            inputField.text = "";

        lastValidText = "";

        if (activeEnemy != null)
            activeEnemy.ClearTypedPreview();

        SetWindup(false);
        ForceFocus();
    }

    void ClearLastPreview()
    {
        if (lastPreviewEnemy != null)
            lastPreviewEnemy.ClearTypedPreview();

        lastPreviewEnemy = null;
    }

    void ForceFocus()
    {
        if (inputField == null)
            return;

        inputField.Select();
        inputField.ActivateInputField();
        inputField.caretPosition = inputField.text.Length;
    }

    void PlayTypingSoundIfNewCharacter(string currentText)
    {
        if (string.IsNullOrEmpty(currentText))
            return;

        if (currentText.Length <= lastValidText.Length)
            return;

        PlaySound(typeSound, typePitchRange, typeVolume);
    }

    void PlayWrongSound()
    {
        PlaySound(wrongSound, wrongPitchRange, wrongVolume);
    }

    void PlaySound(AudioClip clip, Vector2 pitchRange, float volume)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, volume);
    }

    void SetWindup(bool value)
    {
        if (playerAnimator == null || string.IsNullOrEmpty(windupBoolName))
            return;

        playerAnimator.SetBool(windupBoolName, value);
    }

    void TriggerCastAnimation()
    {
        if (playerAnimator == null || string.IsNullOrEmpty(castTriggerName))
            return;

        playerAnimator.ResetTrigger(failTriggerName);
        playerAnimator.SetTrigger(castTriggerName);
    }

    void TriggerFailAnimation()
    {
        if (playerAnimator == null || string.IsNullOrEmpty(failTriggerName))
            return;

        playerAnimator.ResetTrigger(castTriggerName);
        playerAnimator.SetTrigger(failTriggerName);
    }
}