using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class MenuManager : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private GameObject worldSpaceMenuPrefab;

    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject characterSelectionPanel;
    public GameObject difficultyPanel;

    [Header("Character Selection UI")]
    public TMP_Text selectionInstructionText;
    public Button[] characterButtons;

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
        AutoFindReferences();
        ResetPanelsState();
    }

    private void Start()
    {
        SetupAllButtons();
    }

    private void AutoFindReferences()
    {
        if (mainPanel == null) mainPanel = transform.Find("MainPanel")?.gameObject;
        if (optionsPanel == null) optionsPanel = transform.Find("OptionsPanel")?.gameObject;
        if (characterSelectionPanel == null) characterSelectionPanel = transform.Find("CharacterPanel")?.gameObject;
        if (difficultyPanel == null) difficultyPanel = transform.Find("DifficultyPanel")?.gameObject;

        if (selectionInstructionText == null && characterSelectionPanel != null)
        {
            selectionInstructionText = characterSelectionPanel.GetComponentInChildren<TMP_Text>(true);
        }

        if (selectionInstructionText != null)
        {
            selectionInstructionText.gameObject.SetActive(true);
        }

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

    void Update()
    {
        if (spawnedMenuInstance != null || raycastManager == null) return;

        Vector2 touchPos = Vector2.zero;
        bool hasInput = false;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            touchPos = Input.GetTouch(0).position;
            hasInput = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            touchPos = Input.mousePosition;
            hasInput = true;
        }

        if (hasInput)
        {
            if (raycastManager.Raycast(touchPos, hits, TrackableType.Planes))
            {
                Pose hitPose = hits[0].pose;
                SpawnMenuAtPose(hitPose);
            }
        }
    }

    private void SpawnMenuAtPose(Pose hitPose)
    {
        if (worldSpaceMenuPrefab == null) return;

        spawnedMenuInstance = Instantiate(worldSpaceMenuPrefab, hitPose.position, hitPose.rotation);
        spawnedMenuInstance.transform.Rotate(0f, 180f, 0f, Space.Self);

        MenuManager instanceMenu = spawnedMenuInstance.GetComponent<MenuManager>();
        if (instanceMenu != null)
        {
            instanceMenu.ResetPanelsState();
            instanceMenu.SetupAllButtons();
            instanceMenu.ShowMainMenu();
        }
    }

    public void ResetPanelsState()
    {
        if (mainPanel) mainPanel.SetActive(true);
        if (optionsPanel) optionsPanel.SetActive(false);
        if (characterSelectionPanel) characterSelectionPanel.SetActive(false);
        if (difficultyPanel) difficultyPanel.SetActive(false);
    }

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

    public void ShowMainMenu()
    {
        isSelectingPlayer = true;
        SelectedPlayerCharacterIndex = -1;
        SelectedEnemyCharacterIndex = -1;
        SetPanelActive(mainPanel);
    }

    public void OnClickPlay()
    {
        isSelectingPlayer = true;
        SelectedPlayerCharacterIndex = -1;
        SelectedEnemyCharacterIndex = -1;

        if (selectionInstructionText != null)
        {
            selectionInstructionText.gameObject.SetActive(true);
            selectionInstructionText.text = "Escoge tu personaje";
        }

        SetPanelActive(characterSelectionPanel);
    }

    public void OnClickOptions()
    {
        SetPanelActive(optionsPanel);
    }

    public void OnClickBackToMain()
    {
        ShowMainMenu();
    }

    public void OnSelectCharacter(int characterIndex)
    {
        if (isSelectingPlayer)
        {
            SelectedPlayerCharacterIndex = characterIndex;
            isSelectingPlayer = false;

            if (selectionInstructionText != null)
            {
                selectionInstructionText.gameObject.SetActive(true);
                selectionInstructionText.text = "Escoge tu rival";
            }
        }
        else
        {
            SelectedEnemyCharacterIndex = characterIndex;
            ShowDifficultyPanel();
        }
    }

    public void ShowDifficultyPanel()
    {
        SetPanelActive(difficultyPanel);
    }

    public void OnSelectDifficulty(int difficultyIndex)
    {
        SelectedDifficulty = (GameDifficulty)difficultyIndex;

        Vector3 wallPos = transform.position;
        Quaternion wallRot = transform.rotation;

        if (ARWallSpawner.HasWallPose)
        {
            wallPos = ARWallSpawner.LastWallPose.position;
            wallRot = ARWallSpawner.LastWallPose.rotation;
        }

        CombatManager manager = CombatManager.Instance;
        if (manager == null)
        {
            GameObject mgrObj = new GameObject("CombatManager");
            manager = mgrObj.AddComponent<CombatManager>();
        }

        manager.StartCombat(SelectedPlayerCharacterIndex, SelectedEnemyCharacterIndex, (int)SelectedDifficulty, wallPos, wallRot);

        if (spawnedMenuInstance != null)
        {
            spawnedMenuInstance.SetActive(false);
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