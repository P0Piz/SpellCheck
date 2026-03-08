using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxLives = 4;
    public int currentLives;

    [Header("Invincibility")]
    public float invincibleDuration = 1f;
    public float invincibleAlpha = 0.5f;
    private bool invincible = false;

    [Header("Heart UI")]
    public Image[] hearts;
    public Sprite fullHeart;
    public Sprite emptyHeart;

    [Header("Death UI")]
    public GameObject restartScreen;

    [Header("Player Visuals")]
    public MeshRenderer[] playerRenderers;

    void Start()
    {
        currentLives = maxLives;

        if (playerRenderers == null || playerRenderers.Length == 0)
            playerRenderers = GetComponentsInChildren<MeshRenderer>();

        if (restartScreen != null)
            restartScreen.SetActive(false);

        RefreshHearts();
        SetOpacity(1f);
        Time.timeScale = 1f;
    }

    public void TakeDamage(int amount)
    {
        if (invincible)
            return;

        currentLives -= amount;

        if (currentLives < 0)
            currentLives = 0;

        Debug.Log("Player hit! Lives remaining: " + currentLives);

        RefreshHearts();

        if (currentLives <= 0)
        {
            PlayerDied();
            return;
        }

        StartCoroutine(InvincibilityFrames());
    }

    public void Heal(int amount)
    {
        currentLives += amount;

        if (currentLives > maxLives)
            currentLives = maxLives;

        Debug.Log("Player healed! Lives: " + currentLives);

        RefreshHearts();
    }

    IEnumerator InvincibilityFrames()
    {
        invincible = true;
        SetOpacity(invincibleAlpha);

        Debug.Log("Player is temporarily invincible");

        yield return new WaitForSeconds(invincibleDuration);

        invincible = false;
        SetOpacity(1f);

        Debug.Log("Player can take damage again");
    }

    void PlayerDied()
    {
        Debug.Log("Player has died");

        SetOpacity(1f);

        if (restartScreen != null)
            restartScreen.SetActive(true);

        Time.timeScale = 0f;
    }

    void RefreshHearts()
    {
        if (hearts == null || hearts.Length == 0)
            return;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null)
                continue;

            hearts[i].sprite = i < currentLives ? fullHeart : emptyHeart;
        }
    }

    void SetOpacity(float alpha)
    {
        if (playerRenderers == null)
            return;

        foreach (MeshRenderer renderer in playerRenderers)
        {
            if (renderer == null)
                continue;

            foreach (Material mat in renderer.materials)
            {
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;
            }
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }
}