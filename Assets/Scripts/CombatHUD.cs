using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatHUD : MonoBehaviour
{
    [Header("UI Barras de Vida")]
    [SerializeField] private Image playerHealthFill;
    [SerializeField] private TMP_Text playerHealthText;
    [SerializeField] private TMP_Text playerNameText;

    [SerializeField] private Image enemyHealthFill;
    [SerializeField] private TMP_Text enemyHealthText;
    [SerializeField] private TMP_Text enemyNameText;

    [Header("Pantalla de Resultado")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultSubtitleText;
    [SerializeField] private Button restartFightButton;
    [SerializeField] private Button returnToMenuButton;

    private FighterController playerFighter;
    private FighterController enemyFighter;

    private float targetPlayerFill = 1f;
    private float targetEnemyFill = 1f;

    private void Awake()
    {
        if (resultPanel != null) resultPanel.SetActive(false);

        BindResultButtons();
    }

    private void BindResultButtons()
    {
        if (restartFightButton != null)
        {
            restartFightButton.onClick.RemoveListener(OnClickRestart);
            restartFightButton.onClick.AddListener(OnClickRestart);
        }

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveListener(OnClickReturnToMenu);
            returnToMenuButton.onClick.AddListener(OnClickReturnToMenu);
        }
    }

    public void Setup(FighterController player, FighterController enemy, string playerName, string enemyName)
    {
        if (playerFighter != null) playerFighter.OnHealthChanged -= OnPlayerHealthChanged;
        if (enemyFighter != null) enemyFighter.OnHealthChanged -= OnEnemyHealthChanged;

        playerFighter = player;
        enemyFighter = enemy;
        BindResultButtons();

        if (playerNameText != null) playerNameText.text = playerName;
        if (enemyNameText != null) enemyNameText.text = enemyName;

        targetPlayerFill = 1f;
        targetEnemyFill = 1f;

        if (playerHealthFill != null) playerHealthFill.fillAmount = 1f;
        if (enemyHealthFill != null) enemyHealthFill.fillAmount = 1f;

        if (playerHealthText != null) playerHealthText.text = $"{Mathf.CeilToInt(player.maxHealth)} / {Mathf.CeilToInt(player.maxHealth)}";
        if (enemyHealthText != null) enemyHealthText.text = $"{Mathf.CeilToInt(enemy.maxHealth)} / {Mathf.CeilToInt(enemy.maxHealth)}";

        // Suscribirse a eventos de salud
        player.OnHealthChanged -= OnPlayerHealthChanged;
        player.OnHealthChanged += OnPlayerHealthChanged;

        enemy.OnHealthChanged -= OnEnemyHealthChanged;
        enemy.OnHealthChanged += OnEnemyHealthChanged;

        if (resultPanel != null) resultPanel.SetActive(false);
    }

    private void Update()
    {
        // Animación suave de barra de vida
        if (playerHealthFill != null)
        {
            playerHealthFill.fillAmount = Mathf.Lerp(playerHealthFill.fillAmount, targetPlayerFill, Time.deltaTime * 8f);
        }

        if (enemyHealthFill != null)
        {
            enemyHealthFill.fillAmount = Mathf.Lerp(enemyHealthFill.fillAmount, targetEnemyFill, Time.deltaTime * 8f);
        }
    }

    private void OnPlayerHealthChanged(float current, float max)
    {
        targetPlayerFill = Mathf.Clamp01(current / max);
        if (playerHealthText != null)
        {
            playerHealthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }

    private void OnEnemyHealthChanged(float current, float max)
    {
        targetEnemyFill = Mathf.Clamp01(current / max);
        if (enemyHealthText != null)
        {
            enemyHealthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }

    public void ShowResultScreen(bool playerWon)
    {
        if (resultPanel == null) return;

        resultPanel.SetActive(true);

        if (resultTitleText != null)
        {
            if (playerWon)
            {
                resultTitleText.text = "¡VICTORIA!";
                resultTitleText.color = new Color(1f, 0.84f, 0f); // Dorado brillante
            }
            else
            {
                resultTitleText.text = "¡DERROTA!";
                resultTitleText.color = new Color(0.9f, 0.2f, 0.2f); // Rojo intenso
            }
        }

        if (resultSubtitleText != null)
        {
            resultSubtitleText.text = playerWon
                ? "¡Has derrotado a tu oponente con éxito!"
                : "Has caído en combate. ¡Inténtalo de nuevo!";
        }
    }

    public void HideResultScreen()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    private void OnClickRestart()
    {
        HideResultScreen();
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.RestartCombat();
        }
    }

    private void OnClickReturnToMenu()
    {
        HideResultScreen();
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.ReturnToMenu();
        }
    }

    private void OnDestroy()
    {
        if (playerFighter != null) playerFighter.OnHealthChanged -= OnPlayerHealthChanged;
        if (enemyFighter != null) enemyFighter.OnHealthChanged -= OnEnemyHealthChanged;
    }
}
