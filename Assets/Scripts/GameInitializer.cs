using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Script colocado en un GameObject persistente de la escena.
/// Se encarga de inicializar el CombatManager con los prefabs correctos
/// y asegurar que la escena esté lista para el flujo completo de juego.
/// Coloca este script en un GameObject vacío llamado "GameInitializer" en la escena.
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("Prefabs de Personajes (Asignar desde la carpeta Prefabs)")]
    [SerializeField] private GameObject personaje1Prefab;
    [SerializeField] private GameObject personaje2Prefab;
    [SerializeField] private GameObject personaje3Prefab;

    [Header("Configuración del CombatManager")]
    [SerializeField] private CombatManager combatManagerPrefab; // Opcional, puede dejarse vacío

    [Header("Música de combate")]
    [SerializeField] private AudioClip battleMusic;
    [SerializeField] private AudioMixerGroup battleMusicOutput;
    [SerializeField, Range(0f, 1f)] private float battleMusicVolume = 1f;

    private AudioSource battleMusicSource;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        ConfigureBattleMusic();
        InitializeCombatManager();
    }

    private void ConfigureBattleMusic()
    {
        battleMusicSource = GetComponent<AudioSource>();
        if (battleMusicSource == null) battleMusicSource = gameObject.AddComponent<AudioSource>();

        battleMusicSource.clip = battleMusic;
        battleMusicSource.loop = true;
        battleMusicSource.playOnAwake = false;
        battleMusicSource.spatialBlend = 0f;
        battleMusicSource.outputAudioMixerGroup = battleMusicOutput;
        battleMusicSource.volume = battleMusicVolume;
        battleMusicSource.Stop();
    }

    private void Start()
    {
        // Asegurar que el texto de instrucción en el CharacterPanel esté visible
        FixCharacterPanelText();
    }

    private void InitializeCombatManager()
    {
        // Verificar si ya existe un CombatManager
        CombatManager existingManager = FindAnyObjectByType<CombatManager>();

        if (existingManager == null)
        {
            GameObject mgrObj = new GameObject("CombatManager");
            existingManager = mgrObj.AddComponent<CombatManager>();
            Debug.Log("[GameInitializer] CombatManager creado automáticamente.");
        }

        // Asignar prefabs si están asignados en el inspector
        if (personaje1Prefab != null || personaje2Prefab != null || personaje3Prefab != null)
        {
            GameObject[] prefabs = new GameObject[3];
            prefabs[0] = personaje1Prefab;
            prefabs[1] = personaje2Prefab;
            prefabs[2] = personaje3Prefab;

            // Intentar asignar los prefabs via búsqueda de respaldo si alguno es nulo
            for (int i = 0; i < 3; i++)
            {
                if (prefabs[i] == null)
                {
                    prefabs[i] = Resources.Load<GameObject>($"Prefabs/Personaje{i + 1}")
                                ?? Resources.Load<GameObject>($"Personaje{i + 1}");
                }
            }

            existingManager.SetCharacterPrefabs(prefabs);
            Debug.Log("[GameInitializer] Prefabs asignados al CombatManager.");
        }
        else
        {
            Debug.Log("[GameInitializer] Prefabs no asignados en inspector. CombatManager buscará en Resources/Prefabs/.");
        }

        existingManager.SetBattleMusicSource(battleMusicSource);
    }

    /// <summary>
    /// Busca y activa el texto de instrucción del CharacterPanel que puede estar desactivado.
    /// </summary>
    private void FixCharacterPanelText()
    {
        // Buscar el CharacterPanel en la jerarquía
        GameObject characterPanel = GameObject.Find("CharacterPanel");
        if (characterPanel == null)
        {
            // Buscar también en objetos inactivos
            var allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t.name == "CharacterPanel")
                {
                    characterPanel = t.gameObject;
                    break;
                }
            }
        }

        if (characterPanel != null)
        {
            // Activar el Text (TMP) dentro del CharacterPanel
            Transform tmpText = characterPanel.transform.Find("Text (TMP)");
            if (tmpText != null && !tmpText.gameObject.activeSelf)
            {
                tmpText.gameObject.SetActive(true);
                Debug.Log("[GameInitializer] Text (TMP) del CharacterPanel activado.");
            }
        }
    }
}
