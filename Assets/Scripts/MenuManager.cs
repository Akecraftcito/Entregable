using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public enum GameDifficulty { Easy, Medium, Hard }

public class MenuManager : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private GameObject worldSpaceMenuPrefab; // Prefab del Canvas World Space

    [Header("UI Panels (Asignar en el Prefab)")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject characterSelectionPanel;
    public GameObject difficultyPanel;

    [Header("Character Selection UI")]
    public Text selectionInstructionText; // Texto de guía
    public Button[] characterButtons;     // Botones de personajes

    [Header("Main Menu Buttons")]
    public Button playButton;
    public Button optionsButton;

    [Header("Difficulty Buttons")]
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;

    [Header("Game Data Stored")]
    public static int SelectedPlayerCharacterIndex { get; private set; } = -1;
    public static int SelectedEnemyCharacterIndex { get; private set; } = -1;
    public static GameDifficulty SelectedDifficulty { get; private set; } = GameDifficulty.Medium;

    private GameObject spawnedMenuInstance;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool isSelectingPlayer = true;

    private void Awake()
    {
        // Asegurarnos de que al instanciar este objeto, solo el panel principal esté activo
        ResetPanelsState();
    }

    void Update()
    {
        // Si el menú aún no se ha colocado en la pared, escuchamos el toque
        if (spawnedMenuInstance == null)
        {
            Vector2 touchPos = Vector2.zero;
            bool hasInput = false;

            // Detección táctil en Móvil
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                touchPos = Input.GetTouch(0).position;
                hasInput = true;
            }
            // Detección en Editor PC
            else if (Input.GetMouseButtonDown(0))
            {
                touchPos = Input.mousePosition;
                hasInput = true;
            }

            if (hasInput && raycastManager != null)
            {
                if (raycastManager.Raycast(touchPos, hits, TrackableType.Planes))
                {
                    Pose hitPose = hits[0].pose;

                    // 1. Instanciar el Canvas usando la posición y la rotación EXACTA de la pared
                    spawnedMenuInstance = Instantiate(worldSpaceMenuPrefab, hitPose.position, hitPose.rotation);

                    // 2. Ajuste de alineación para que quede pegado plano contra la pared y mirando al frente
                    // Rotamos 180 grados en Y por si la cara del Canvas quedó mirando hacia la pared interna
                    spawnedMenuInstance.transform.Rotate(0f, 180f, 0f, Space.Self);

                    // 3. Inicializar los paneles y eventos de botones en la nueva instancia
                    MenuManager instanceMenu = spawnedMenuInstance.GetComponent<MenuManager>();
                    if (instanceMenu != null)
                    {
                        instanceMenu.ResetPanelsState();
                        instanceMenu.SetupAllButtons();
                        instanceMenu.ShowMainMenu();
                    }

                    Debug.Log("¡Canvas pegado correctamente a la pared!");
                }
            }
        }
    }

    /// <summary>
    /// Desactiva todos los paneles excepto el panel principal.
    /// </summary>
    public void ResetPanelsState()
    {
        if (mainPanel) mainPanel.SetActive(true);
        if (optionsPanel) optionsPanel.SetActive(false);
        if (characterSelectionPanel) characterSelectionPanel.SetActive(false);
        if (difficultyPanel) difficultyPanel.SetActive(false);
    }

    /// <summary>
    /// Asigna dinámicamente los eventos OnClick de los botones.
    /// </summary>
    public void SetupAllButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnClickPlay);
        }

        if (optionsButton != null)
        {
            optionsButton.onClick.RemoveAllListeners();
            optionsButton.onClick.AddListener(OnClickOptions);
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

        if (easyButton != null)
        {
            easyButton.onClick.RemoveAllListeners();
            easyButton.onClick.AddListener(() => OnSelectDifficulty(0));
        }

        if (mediumButton != null)
        {
            mediumButton.onClick.RemoveAllListeners();
            mediumButton.onClick.AddListener(() => OnSelectDifficulty(1));
        }

        if (hardButton != null)
        {
            hardButton.onClick.RemoveAllListeners();
            hardButton.onClick.AddListener(() => OnSelectDifficulty(2));
        }
    }

    // ================================================
    // NAVEGACIÓN ENTRE PANELES
    // ================================================

    public void ShowMainMenu()
    {
        SetPanelActive(mainPanel);
    }

    public void OnClickPlay()
    {
        Debug.Log("Pulsado botón JUGAR");
        isSelectingPlayer = true;
        SelectedPlayerCharacterIndex = -1;
        SelectedEnemyCharacterIndex = -1;

        if (selectionInstructionText != null)
            selectionInstructionText.text = "SELECCIONA TU PERSONAJE";

        SetPanelActive(characterSelectionPanel);
    }

    public void OnClickOptions()
    {
        Debug.Log("Pulsado botón OPCIONES");
        SetPanelActive(optionsPanel);
    }

    public void OnClickBackToMain()
    {
        ShowMainMenu();
    }

    // ================================================
    // SELECCIÓN DE PERSONAJES
    // ================================================

    public void OnSelectCharacter(int characterIndex)
    {
        if (isSelectingPlayer)
        {
            SelectedPlayerCharacterIndex = characterIndex;
            Debug.Log($"Personaje Jugador Seleccionado: {characterIndex}");

            isSelectingPlayer = false;

            if (selectionInstructionText != null)
                selectionInstructionText.text = "SELECCIONA A TU RIVAL";
        }
        else
        {
            SelectedEnemyCharacterIndex = characterIndex;
            Debug.Log($"Personaje Rival Seleccionado: {characterIndex}");

            ShowDifficultyPanel();
        }
    }

    // ================================================
    // SELECCIÓN DE DIFICULTAD E INICIO DE PELEA
    // ================================================

    public void ShowDifficultyPanel()
    {
        SetPanelActive(difficultyPanel);
    }

    public void OnSelectDifficulty(int difficultyIndex)
    {
        SelectedDifficulty = (GameDifficulty)difficultyIndex;
        Debug.Log($"Dificultad Seleccionada: {SelectedDifficulty}");

        StartGameCombat();
    }

    private void StartGameCombat()
    {
        Debug.Log("¡Pelea Iniciada! Destruyendo menú e iniciando combate.");

        if (spawnedMenuInstance != null)
        {
            Destroy(spawnedMenuInstance);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void SetPanelActive(GameObject targetPanel)
    {
        if (mainPanel) mainPanel.SetActive(mainPanel == targetPanel);
        if (optionsPanel) optionsPanel.SetActive(optionsPanel == targetPanel);
        if (characterSelectionPanel) characterSelectionPanel.SetActive(characterSelectionPanel == targetPanel);
        if (difficultyPanel) difficultyPanel.SetActive(difficultyPanel == targetPanel);
    }
}