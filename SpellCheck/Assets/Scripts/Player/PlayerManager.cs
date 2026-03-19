using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class PlayerManager : MonoBehaviour
{
    [Header("Health")]
    public int maxLives = 4;
    public int currentLives;

    [Header("Invincibility")]
    public float invincibleDuration = 1f;
    public float invincibleAlpha = 0.5f;
    private bool invincible = false;

    [Header("Score")]
    public int currentScore = 0;
    public TMP_Text scoreDisplay;

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
        updateScoreUI();
        Time.timeScale = 1f;
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        Debug.Log("Score increased! Current Score: " + currentScore);
        updateScoreUI();
    }

    public void ResetScore()
    {
        currentScore = 0;
        Debug.Log("Score reset! Current Score: " + currentScore);
        updateScoreUI();
    }

    void updateScoreUI()
    {
        scoreDisplay.text = "Score: " + currentScore.ToString();
    }

    public void MinusScore(int amount)
    {
        if (HasEnoughScore(amount))
        {
            currentScore -= amount;
            Debug.Log("Score decreased! Current Score: " + currentScore);
        }
        else
        {
            Debug.Log("Not enough score to decrease! Current Score: " + currentScore);
        }
    }

    public bool HasEnoughScore(int amount)
    {
        return currentScore >= amount;
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