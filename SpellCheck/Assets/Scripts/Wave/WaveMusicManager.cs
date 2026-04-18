using System.Collections;
using UnityEngine;

public class WaveMusicManager : MonoBehaviour
{
    public static WaveMusicManager Instance;

    [Header("Audio Sources")]
    public AudioSource chillSource;
    public AudioSource combatSource;

    [Header("Music Clips")]
    public AudioClip chillMusic;
    public AudioClip earlyWaveMusic;
    public AudioClip intenseWaveMusic;

    [Header("Settings")]
    public float fadeDuration = 1.25f;
    public float maxVolume = 1f;
    public int intenseWaveStart = 4;

    private Coroutine fadeRoutine;
    private AudioSource currentMainCombatSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetupSource(chillSource, chillMusic, maxVolume);
        SetupSource(combatSource, earlyWaveMusic, 0f);

        currentMainCombatSource = combatSource;
    }

    public void PlayChill()
    {
        FadeTo(chillVol: maxVolume, combatVol: 0f);
    }

    public void PlayForWave(int waveNumber)
    {
        AudioClip wantedCombatClip = waveNumber >= intenseWaveStart ? intenseWaveMusic : earlyWaveMusic;

        if (wantedCombatClip != null && combatSource.clip != wantedCombatClip)
        {
            float previousTime = 0f;

            if (combatSource.clip != null && combatSource.clip.length > 0f)
                previousTime = combatSource.time % combatSource.clip.length;

            combatSource.clip = wantedCombatClip;
            combatSource.loop = true;
            combatSource.Play();

            if (combatSource.clip.length > 0f)
                combatSource.time = Mathf.Min(previousTime, combatSource.clip.length - 0.01f);
        }

        FadeTo(chillVol: 0f, combatVol: maxVolume);
    }

    private void FadeTo(float chillVol, float combatVol)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(chillVol, combatVol));
    }

    private IEnumerator FadeRoutine(float targetChillVol, float targetCombatVol)
    {
        float startChill = chillSource != null ? chillSource.volume : 0f;
        float startCombat = combatSource != null ? combatSource.volume : 0f;

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = fadeDuration <= 0f ? 1f : timer / fadeDuration;

            if (chillSource != null)
                chillSource.volume = Mathf.Lerp(startChill, targetChillVol, t);

            if (combatSource != null)
                combatSource.volume = Mathf.Lerp(startCombat, targetCombatVol, t);

            yield return null;
        }

        if (chillSource != null)
            chillSource.volume = targetChillVol;

        if (combatSource != null)
            combatSource.volume = targetCombatVol;

        fadeRoutine = null;
    }

    private void SetupSource(AudioSource source, AudioClip clip, float startVolume)
    {
        if (source == null || clip == null)
            return;

        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = startVolume;

        if (!source.isPlaying)
            source.Play();
    }
}