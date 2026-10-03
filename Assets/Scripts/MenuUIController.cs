using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuUIController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject characterSelectionPanel;
    public GameObject difficultyPanel;

    [Header("Instruction Text")]
    public TMP_Text selectionInstructionText;

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
    private GameObject wallScanPanel;
    private TMP_Text wallScanStatusText;
    private Button startCombatButton;

    private void Awake()
    {
        ConfigureOverlayCanvas();
        AutoFindReferences();
        ShowPanel(mainPanel);
        CreateWallScanPanel();
    }

    private void Start()
    {
        SetupButtonListeners();
    }

    private void Update()
    {
        if (wallScanPanel == null || !wallScanPanel.activeSelf || startCombatButton == null || startCombatButton.interactable)
        {
            return;
        }

        if (ARWallSpawner.HasWallPose)
        {
            wallScanStatusText.text = "Pared detectada. Pulsa para iniciar el combate.";
            startCombatButton.interactable = true;
        }
    }

    private void ConfigureOverlayCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) return;

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10;
        transform.localScale = Vector3.one;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void CreateWallScanPanel()
    {
        wallScanPanel = new GameObject("WallScanPanel", typeof(RectTransform), typeof(Image));
        wallScanPanel.transform.SetParent(transform, false);

        RectTransform panelRect = wallScanPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(560f, 260f);

        Image panelImage = wallScanPanel.GetComponent<Image>();
        panelImage.color = new Color(0.06f, 0.09f, 0.12f, 0.94f);

        GameObject statusObject = new GameObject("WallScanStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusObject.transform.SetParent(wallScanPanel.transform, false);
        RectTransform statusRect = statusObject.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.08f, 0.42f);
        statusRect.anchorMax = new Vector2(0.92f, 0.94f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;
        wallScanStatusText = statusObject.GetComponent<TextMeshProUGUI>();
        wallScanStatusText.text = "Apunta a una pared y tócala para escanearla.";
        wallScanStatusText.fontSize = 28f;
        wallScanStatusText.alignment = TextAlignmentOptions.Center;
        wallScanStatusText.color = Color.white;
        wallScanStatusText.raycastTarget = false;

        GameObject buttonObject = new GameObject("StartCombatButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(wallScanPanel.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.16f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.16f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(320f, 64f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.12f, 0.55f, 0.3f, 1f);
        startCombatButton = buttonObject.GetComponent<Button>();
        startCombatButton.targetGraphic = buttonImage;
        startCombatButton.interactable = false;
        startCombatButton.onClick.AddListener(OnClickStartCombat);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TMP_Text label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "INICIAR COMBATE";
        label.fontSize = 24f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        wallScanPanel.SetActive(false);
    }

    private void AutoFindReferences()
    {
        // Auto-encontrar paneles si no están asignados
        if (mainPanel == null) mainPanel = transform.Find("MainPanel")?.gameObject;
        if (optionsPanel == null) optionsPanel = transform.Find("OptionsPanel")?.gameObject;
        if (characterSelectionPanel == null) characterSelectionPanel = transform.Find("CharacterPanel")?.gameObject;
        if (difficultyPanel == null) difficultyPanel = transform.Find("DifficultyPanel")?.gameObject;

        // Auto-encontrar texto de instrucción en CharacterPanel
        if (selectionInstructionText == null && characterSelectionPanel != null)
        {
            selectionInstructionText = characterSelectionPanel.GetComponentInChildren<TMP_Text>(true);
        }

        if (selectionInstructionText != null)
        {
            selectionInstructionText.gameObject.SetActive(true);
        }

        // Auto-encontrar botones si no están asignados
        if (mainPanel != null)
        {
            if (playButton == null) playButton = mainPanel.transform.Find("Jugar")?.GetComponent<Button>();
            if (optionsButton == null) optionsButton = mainPanel.transform.Find("Opciones")?.GetComponent<Button>();
        }

        if (characterSelectionPanel != null && (characterButtons == null || characterButtons.Length == 0))
        {
            characterButtons = new Button[3];
            characterButtons[0] = characterSelectionPanel.transform.Find("Personaje1")?.GetComponent<Button>();
            characterButtons[1] = characterSelectionPanel.transform.Find("Personaje2")?.GetComponent<Button>();
            characterButtons[2] = characterSelectionPanel.transform.Find("Personaje3")?.GetComponent<Button>();
        }

        if (difficultyPanel != null)
        {
            if (easyButton == null) easyButton = difficultyPanel.transform.Find("Facil")?.GetComponent<Button>();
            if (mediumButton == null) mediumButton = difficultyPanel.transform.Find("Normal")?.GetComponent<Button>();
            if (hardButton == null) hardButton = difficultyPanel.transform.Find("Dificil")?.GetComponent<Button>();
        }
    }

    private void SetupButtonListeners()
    {
        if (playButton)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnClickPlay);
        }

        if (optionsButton)
        {
            optionsButton.onClick.RemoveAllListeners();
            optionsButton.onClick.AddListener(OnClickOptions);
        }

        if (backFromOptionsButton)
        {
            backFromOptionsButton.onClick.RemoveAllListeners();
            backFromOptionsButton.onClick.AddListener(OnClickBackToMain);
        }

        if (characterButtons != null)
        {
            for (int i = 0; i < characterButtons.Length; i++)
            {
                int index = i;
                if (characterButtons[i] != null)
                {
                    characterButtons[i].onClick.RemoveAllListeners();
                    characterButtons[i].onClick.AddListener(() => OnSelectCharacter(index));
                }
            }
        }

        if (easyButton)
        {
            easyButton.onClick.RemoveAllListeners();
            easyButton.onClick.AddListener(() => OnSelectDifficulty(0));
        }

        if (mediumButton)
        {
            mediumButton.onClick.RemoveAllListeners();
            mediumButton.onClick.AddListener(() => OnSelectDifficulty(1));
        }

        if (hardButton)
        {
            hardButton.onClick.RemoveAllListeners();
            hardButton.onClick.AddListener(() => OnSelectDifficulty(2));
        }
    }

    public void OnClickPlay()
    {
        isSelectingPlayer = true;
        SelectedPlayerIndex = -1;
        SelectedEnemyIndex = -1;

        if (selectionInstructionText != null)
        {
            selectionInstructionText.gameObject.SetActive(true);
            selectionInstructionText.text = "Escoge tu personaje";
        }

        ShowPanel(characterSelectionPanel);
    }

    public void OnClickOptions()
    {
        ShowPanel(optionsPanel);
    }

    public void OnClickBackToMain()
    {
        isSelectingPlayer = true;
        SelectedPlayerIndex = -1;
        SelectedEnemyIndex = -1;
        ARWallSpawner.CancelWallScan();
        if (wallScanPanel != null) wallScanPanel.SetActive(false);
        ShowPanel(mainPanel);
    }

    public void OnSelectCharacter(int characterIndex)
    {
        if (isSelectingPlayer)
        {
            // 1. Selección del Personaje del Jugador
            SelectedPlayerIndex = characterIndex;
            Debug.Log($"Personaje Jugador Seleccionado: Personaje {characterIndex + 1}");

            isSelectingPlayer = false;

            // Reutiliza el mismo panel y cambia el texto a "Escoge tu rival"
            if (selectionInstructionText != null)
            {
                selectionInstructionText.gameObject.SetActive(true);
                selectionInstructionText.text = "Escoge tu rival";
            }
        }
        else
        {
            // 2. Selección del Personaje del Rival
            SelectedEnemyIndex = characterIndex;
            Debug.Log($"Personaje Rival Seleccionado: Personaje {characterIndex + 1}");

            // Pasar al panel de dificultad
            ShowPanel(difficultyPanel);
        }
    }

    public void OnSelectDifficulty(int difficulty)
    {
        SelectedDifficulty = difficulty;
        Debug.Log($"Configuración de Combate: Jugador {SelectedPlayerIndex + 1}, Rival {SelectedEnemyIndex + 1}, Dificultad {SelectedDifficulty}");

        if (SelectedPlayerIndex < 0 || SelectedEnemyIndex < 0) return;

        ShowPanel(null);
        wallScanStatusText.text = "Apunta a una pared y tócala para escanearla.";
        startCombatButton.interactable = false;
        wallScanPanel.SetActive(true);
        ARWallSpawner.BeginWallScan();
    }

    private void OnClickStartCombat()
    {
        if (!ARWallSpawner.HasWallPose) return;

        Pose wallPose = ARWallSpawner.LastWallPose;
        CombatManager manager = CombatManager.Instance;
        if (manager == null)
        {
            GameObject mgrObj = new GameObject("CombatManager");
            manager = mgrObj.AddComponent<CombatManager>();
        }

        manager.StartCombat(SelectedPlayerIndex, SelectedEnemyIndex, SelectedDifficulty, wallPose.position, wallPose.rotation);
        ARWallSpawner.CancelWallScan();

        gameObject.SetActive(false);
    }

    private void ShowPanel(GameObject panelToKeep)
    {
        if (mainPanel) mainPanel.SetActive(mainPanel == panelToKeep);
        if (optionsPanel) optionsPanel.SetActive(optionsPanel == panelToKeep);
        if (characterSelectionPanel) characterSelectionPanel.SetActive(characterSelectionPanel == panelToKeep);
        if (difficultyPanel) difficultyPanel.SetActive(difficultyPanel == panelToKeep);
    }
}