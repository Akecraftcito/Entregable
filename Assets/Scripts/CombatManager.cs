using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Prefabs de Personajes")]
    [SerializeField] private GameObject[] characterPrefabs;

    [Header("Referencias UI")]
    [SerializeField] private CombatHUD combatHUD;
    [SerializeField] private CombatControlsUI combatControlsUI;

    [Header("Colocación AR")]
    [SerializeField] private float stageOffsetFromWall = 0.7f;
    [SerializeField, Range(0.1f, 1f)] private float stageScale = 0.5f;

    [Header("Estado Actual de Combate")]
    public int currentPlayerIndex = 0;
    public int currentEnemyIndex = 0;
    public GameDifficulty currentDifficulty = GameDifficulty.Medium;

    private CombatStage currentStage;
    private FighterController spawnedPlayer;
    private FighterController spawnedEnemy;
    private FighterAI spawnedAI;
    private Vector3 lastWallPosition;
    private Quaternion lastWallRotation;
    private AudioSource battleMusicSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Cargar prefabs de personajes si no están asignados en el inspector
        if (characterPrefabs == null || characterPrefabs.Length < 3 || characterPrefabs[0] == null)
        {
            LoadCharacterPrefabs();
        }

        // Crear/Cargar la UI en memoria pero asegurando que nazca OCULTA
        EnsureCombatUI();
        HideCombatUI();
    }

    private void LateUpdate()
    {
        if (spawnedPlayer == null || spawnedEnemy == null) return;

        spawnedPlayer.FaceOpponent(spawnedEnemy.transform.position);
        spawnedEnemy.FaceOpponent(spawnedPlayer.transform.position);
    }

    private void LoadCharacterPrefabs()
    {
        characterPrefabs = new GameObject[3];
        characterPrefabs[0] = Resources.Load<GameObject>("Prefabs/Personaje1") ?? Resources.Load<GameObject>("Personaje1");
        characterPrefabs[1] = Resources.Load<GameObject>("Prefabs/Personaje2") ?? Resources.Load<GameObject>("Personaje2");
        characterPrefabs[2] = Resources.Load<GameObject>("Prefabs/Personaje3") ?? Resources.Load<GameObject>("Personaje3");
    }

    public void SetCharacterPrefabs(GameObject[] prefabs)
    {
        characterPrefabs = prefabs;
    }

    public void SetBattleMusicSource(AudioSource source)
    {
        battleMusicSource = source;
        StopBattleMusic();
    }

    public void StartCombat(int playerIndex, int enemyIndex, int difficultyIdx, Vector3 wallPos, Quaternion wallRot)
    {
        currentPlayerIndex = Mathf.Clamp(playerIndex, 0, 2);
        currentEnemyIndex = Mathf.Clamp(enemyIndex, 0, 2);
        currentDifficulty = (GameDifficulty)difficultyIdx;
        lastWallPosition = wallPos;
        lastWallRotation = wallRot;

        Debug.Log($"Iniciando Combate AR: Jugador P{currentPlayerIndex + 1} vs Rival P{currentEnemyIndex + 1} (Dificultad: {currentDifficulty})");

        // 1. Limpiar combate previo si existe
        CleanupCombat();

        // 2. Alejar el escenario de la pared, hacia el lado desde el que se escaneó.
        Vector3 stagePosition = lastWallPosition + lastWallRotation * Vector3.back * stageOffsetFromWall;
        currentStage = CombatStage.CreateStage(stagePosition, lastWallRotation);
        currentStage.transform.localScale = Vector3.one * stageScale;

        // 3. Obtener prefabs
        GameObject playerPrefab = GetCharacterPrefab(currentPlayerIndex);
        GameObject enemyPrefab = GetCharacterPrefab(currentEnemyIndex);

        if (playerPrefab == null || enemyPrefab == null)
        {
            Debug.LogError("No se encontraron los prefabs de los personajes. Por favor verifica la carpeta Prefabs.");
            return;
        }

        // 4. Instanciar Jugador
        GameObject playerObj = Instantiate(playerPrefab, currentStage.PlayerSpawnPoint.position, currentStage.transform.rotation, currentStage.transform);
        playerObj.name = $"Player_P{currentPlayerIndex + 1}";
        playerObj.tag = "Player";
        spawnedPlayer = playerObj.GetComponent<FighterController>();
        if (spawnedPlayer == null) spawnedPlayer = playerObj.AddComponent<FighterController>();

        spawnedPlayer.SetStageReference(currentStage.transform);
        spawnedPlayer.SetEnemyTag("Enemy");
        spawnedPlayer.SetFacing(true); // Mira a la derecha

        // 5. Instanciar Rival
        GameObject enemyObj = Instantiate(enemyPrefab, currentStage.EnemySpawnPoint.position, currentStage.transform.rotation, currentStage.transform);
        enemyObj.name = $"Enemy_P{currentEnemyIndex + 1}";
        enemyObj.tag = "Enemy";
        spawnedEnemy = enemyObj.GetComponent<FighterController>();
        if (spawnedEnemy == null) spawnedEnemy = enemyObj.AddComponent<FighterController>();

        spawnedEnemy.SetStageReference(currentStage.transform);
        spawnedEnemy.SetEnemyTag("Player");
        spawnedEnemy.SetFacing(false); // Mira a la izquierda

        // 6. Configurar IA del Rival
        spawnedAI = enemyObj.GetComponent<FighterAI>();
        if (spawnedAI == null) spawnedAI = enemyObj.AddComponent<FighterAI>();
        spawnedAI.Setup(spawnedPlayer, currentDifficulty, currentStage.transform);

        // 7. Conectar eventos de derrota
        spawnedPlayer.OnDefeated -= OnFighterDefeated;
        spawnedPlayer.OnDefeated += OnFighterDefeated;

        spawnedEnemy.OnDefeated -= OnFighterDefeated;
        spawnedEnemy.OnDefeated += OnFighterDefeated;

        PlayBattleMusic();

        // 8. ACTIVAR Y MOSTRAR LA UI ÚNICAMENTE AHORA QUE EMPIEZA EL COMBATE
        EnsureCombatUI();

        if (combatControlsUI != null)
        {
            combatControlsUI.gameObject.SetActive(true);
            combatControlsUI.SetPlayerFighter(spawnedPlayer);
        }

        if (combatHUD != null)
        {
            combatHUD.gameObject.SetActive(true);
            string diffName = currentDifficulty switch
            {
                GameDifficulty.Easy => "FÁCIL",
                GameDifficulty.Hard => "DIFÍCIL",
                _ => "NORMAL"
            };
            combatHUD.Setup(spawnedPlayer, spawnedEnemy, $"JUGADOR (P{currentPlayerIndex + 1})", $"RIVAL ({diffName})");
        }
    }

    private GameObject GetCharacterPrefab(int index)
    {
        if (characterPrefabs != null && index >= 0 && index < characterPrefabs.Length && characterPrefabs[index] != null)
        {
            return characterPrefabs[index];
        }

        // Búsqueda de respaldo
        string prefabName = $"Personaje{index + 1}";
        return Resources.Load<GameObject>($"Prefabs/{prefabName}") ?? Resources.Load<GameObject>(prefabName);
    }

    private void OnFighterDefeated(FighterController defeated)
    {
        bool playerWon = (defeated == spawnedEnemy);
        Debug.Log($"Fin del combate: {(playerWon ? "¡VICTORIA!" : "¡DERROTA!")}");
        StopBattleMusic();

        if (combatHUD != null)
        {
            combatHUD.ShowResultScreen(playerWon);
        }
    }

    public void RestartCombat()
    {
        if (spawnedPlayer == null || spawnedEnemy == null || currentStage == null)
        {
            StartCombat(currentPlayerIndex, currentEnemyIndex, (int)currentDifficulty, lastWallPosition, lastWallRotation);
            return;
        }

        // Reiniciar posiciones y vida
        spawnedPlayer.ResetFighter(currentStage.PlayerSpawnPoint.position, currentStage.transform.rotation);
        spawnedPlayer.SetFacing(true);

        spawnedEnemy.ResetFighter(currentStage.EnemySpawnPoint.position, currentStage.transform.rotation);
        spawnedEnemy.SetFacing(false);

        PlayBattleMusic();

        if (combatHUD != null)
        {
            string diffName = currentDifficulty switch
            {
                GameDifficulty.Easy => "FÁCIL",
                GameDifficulty.Hard => "DIFÍCIL",
                _ => "NORMAL"
            };
            combatHUD.Setup(spawnedPlayer, spawnedEnemy, $"JUGADOR (P{currentPlayerIndex + 1})", $"RIVAL ({diffName})");
            combatHUD.HideResultScreen();
        }
    }

    public void ReturnToMenu()
    {
        StopBattleMusic();
        CleanupCombat();

        // Ocultar HUD y Controles de pelea
        if (combatHUD != null) combatHUD.gameObject.SetActive(false);
        if (combatControlsUI != null) combatControlsUI.gameObject.SetActive(false);

        // Volver a activar y mostrar el menú principal
        MenuUIController menuUI = FindAnyObjectByType<MenuUIController>(FindObjectsInactive.Include);
        if (menuUI != null)
        {
            menuUI.gameObject.SetActive(true);
            menuUI.OnClickBackToMain();
        }

        MenuManager menuMgr = FindAnyObjectByType<MenuManager>(FindObjectsInactive.Include);
        if (menuMgr != null)
        {
            menuMgr.gameObject.SetActive(true);
            menuMgr.ShowMainMenu();
        }
    }

    private void CleanupCombat()
    {
        if (spawnedPlayer != null)
        {
            Destroy(spawnedPlayer.gameObject);
            spawnedPlayer = null;
        }

        if (spawnedEnemy != null)
        {
            Destroy(spawnedEnemy.gameObject);
            spawnedEnemy = null;
        }

        if (currentStage != null)
        {
            Destroy(currentStage.gameObject);
            currentStage = null;
        }
    }

    private void EnsureCombatUI()
    {
        if (combatHUD == null) combatHUD = FindAnyObjectByType<CombatHUD>(FindObjectsInactive.Include);
        if (combatControlsUI == null) combatControlsUI = FindAnyObjectByType<CombatControlsUI>(FindObjectsInactive.Include);

        if (combatHUD == null || combatControlsUI == null)
        {
            CombatUIGenerator.CreateCombatUIIfMissing(out combatHUD, out combatControlsUI);
        }
    }
    private void PlayBattleMusic()
    {
        if (battleMusicSource == null || battleMusicSource.clip == null) return;
        if (!battleMusicSource.isPlaying) battleMusicSource.Play();
    }

    private void StopBattleMusic()
    {
        if (battleMusicSource != null && battleMusicSource.isPlaying)
        {
            battleMusicSource.Stop();
        }
    }
    private void HideCombatUI()
    {
        if (combatHUD != null) combatHUD.gameObject.SetActive(false);
        if (combatControlsUI != null) combatControlsUI.gameObject.SetActive(false);

        // Desactivar directamente el Canvas si existe
        Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas != null && canvas.gameObject.name == "CombatUI_Canvas")
        {
            canvas.gameObject.SetActive(false);
        }
    }
}
