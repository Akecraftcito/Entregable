using UnityEngine;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject characterSelectionPanel;
    public GameObject difficultyPanel;

    [Header("Instruction Text")]
    public Text selectionInstructionText;

    [Header("Buttons")]
    public Button playButton;
    public Button optionsButton;
    public Button backFromOptionsButton;
    public Button[] characterButtons;
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;

    public static int SelectedPlayerIndex { get; private set; } = -1;
    public static int SelectedEnemyIndex { get; private set; } = -1;
    public static int SelectedDifficulty { get; private set; } = 1; // 0: Easy, 1: Medium, 2: Hard

    private bool isSelectingPlayer = true;

    private void Awake()
    {
        ShowPanel(mainPanel);
    }

    private void Start()
    {
        // Vincular los eventos directamente a las referencias dentro de esta instancia
        if (playButton) playButton.onClick.AddListener(OnClickPlay);
        if (optionsButton) optionsButton.onClick.AddListener(OnClickOptions);
        if (backFromOptionsButton) backFromOptionsButton.onClick.AddListener(OnClickBackToMain);

        if (characterButtons != null)
        {
            for (int i = 0; i < characterButtons.Length; i++)
            {
                int index = i;
                if (characterButtons[i] != null)
                {
                    characterButtons[i].onClick.AddListener(() => OnSelectCharacter(index));
                }
            }
        }

        if (easyButton) easyButton.onClick.AddListener(() => OnSelectDifficulty(0));
        if (mediumButton) mediumButton.onClick.AddListener(() => OnSelectDifficulty(1));
        if (hardButton) hardButton.onClick.AddListener(() => OnSelectDifficulty(2));
    }

    public void OnClickPlay()
    {
        isSelectingPlayer = true;
        SelectedPlayerIndex = -1;
        SelectedEnemyIndex = -1;

        if (selectionInstructionText)
            selectionInstructionText.text = "SELECCIONA TU PERSONAJE";

        ShowPanel(characterSelectionPanel);
    }

    public void OnClickOptions()
    {
        ShowPanel(optionsPanel);
    }

    public void OnClickBackToMain()
    {
        ShowPanel(mainPanel);
    }

    public void OnSelectCharacter(int characterIndex)
    {
        if (isSelectingPlayer)
        {
            SelectedPlayerIndex = characterIndex;
            isSelectingPlayer = false;

            if (selectionInstructionText)
                selectionInstructionText.text = "SELECCIONA A TU RIVAL";
        }
        else
        {
            SelectedEnemyIndex = characterIndex;
            ShowPanel(difficultyPanel);
        }
    }

    public void OnSelectDifficulty(int difficulty)
    {
        SelectedDifficulty = difficulty;
        Debug.Log($"Configuración lista: Jugador {SelectedPlayerIndex}, Rival {SelectedEnemyIndex}, Dificultad {SelectedDifficulty}");

        // Iniciar combate y destruir el menú
        Destroy(gameObject);
    }

    private void ShowPanel(GameObject panelToKeep)
    {
        if (mainPanel) mainPanel.SetActive(mainPanel == panelToKeep);
        if (optionsPanel) optionsPanel.SetActive(optionsPanel == panelToKeep);
        if (characterSelectionPanel) characterSelectionPanel.SetActive(characterSelectionPanel == panelToKeep);
        if (difficultyPanel) difficultyPanel.SetActive(difficultyPanel == panelToKeep);
    }
}