using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxLives = 4;
    public int currentLives;

    [Header("Invincibility")]
    public float invincibleDuration = 1f;
    private bool invincible = false;

    [Header("Heart UI")]
    public Image[] hearts;
    public Sprite fullHeart;
    public Sprite emptyHeart;

    void Start()
    {
        currentLives = maxLives;
        RefreshHearts();
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

        Debug.Log("Player is temporarily invincible");

        yield return new WaitForSeconds(invincibleDuration);

        invincible = false;

        Debug.Log("Player can take damage again");
    }

    void PlayerDied()
    {
        Debug.Log("Player has died");
    }

    void RefreshHearts()
    {
        if (hearts == null || hearts.Length == 0)
            return;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null)
                continue;

            if (i < currentLives)
                hearts[i].sprite = fullHeart;
            else
                hearts[i].sprite = emptyHeart;
        }
    }
}