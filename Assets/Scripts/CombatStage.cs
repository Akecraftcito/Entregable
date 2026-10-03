using UnityEngine;

public class CombatStage : MonoBehaviour
{
    [Header("Dimensiones del Escenario")]
    [SerializeField] private float stageWidth = 3.6f;
    [SerializeField] private float floorHeight = 0.12f;
    [SerializeField] private float floorDepth = 0.6f;
    [SerializeField] private float barrierHeight = 3.0f;

    [Header("Puntos de Aparición")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform enemySpawnPoint;

    public Transform PlayerSpawnPoint => playerSpawnPoint;
    public Transform EnemySpawnPoint => enemySpawnPoint;

    public static CombatStage CreateStage(Vector3 position, Quaternion rotation)
    {
        GameObject stageObj = new GameObject("CombatStage_AR");
        stageObj.transform.position = position;
        stageObj.transform.rotation = rotation;

        CombatStage stage = stageObj.AddComponent<CombatStage>();
        stage.BuildStageGeometry();
        return stage;
    }

    public void BuildStageGeometry()
    {
        // 1. Piso Recto
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(transform, false);
        floor.transform.localPosition = new Vector3(0f, -floorHeight * 0.5f, 0f);
        floor.transform.localScale = new Vector3(stageWidth, floorHeight, floorDepth);

        // Material estilizado para el piso (plataforma oscura con brillo)
        Renderer rend = floor.GetComponent<Renderer>();
        if (rend != null)
        {
            Material floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            floorMat.color = new Color(0.12f, 0.14f, 0.18f); // Gris oscuro metálico
            rend.material = floorMat;
        }

        // 2. Línea de neón / borde del ring
        GameObject neonLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        neonLine.name = "NeonBorder";
        neonLine.transform.SetParent(transform, false);
        neonLine.transform.localPosition = new Vector3(0f, 0.005f, 0f);
        neonLine.transform.localScale = new Vector3(stageWidth * 0.98f, 0.02f, 0.04f);

        Renderer lineRend = neonLine.GetComponent<Renderer>();
        if (lineRend != null)
        {
            Material lineMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            lineMat.color = new Color(0f, 0.85f, 1f); // Cian neón
            lineRend.material = lineMat;
        }
        Collider lineCol = neonLine.GetComponent<Collider>();
        if (lineCol != null) Destroy(lineCol);

        // 3. Paredes invisibles alrededor del ring
        float halfWidth = stageWidth * 0.5f;
        float halfDepth = floorDepth * 0.5f;
        float barrierCenterY = barrierHeight * 0.5f;
        CreateBarrier("LeftBarrier", new Vector3(-halfWidth - 0.05f, barrierCenterY, 0f),
            new Vector3(0.1f, barrierHeight, floorDepth + 0.1f));
        CreateBarrier("RightBarrier", new Vector3(halfWidth + 0.05f, barrierCenterY, 0f),
            new Vector3(0.1f, barrierHeight, floorDepth + 0.1f));
        CreateBarrier("FrontBarrier", new Vector3(0f, barrierCenterY, -halfDepth - 0.05f),
            new Vector3(stageWidth + 0.2f, barrierHeight, 0.1f));
        CreateBarrier("BackBarrier", new Vector3(0f, barrierCenterY, halfDepth + 0.05f),
            new Vector3(stageWidth + 0.2f, barrierHeight, 0.1f));

        // 4. Puntos de Spawn
        GameObject pSpawn = new GameObject("PlayerSpawn");
        pSpawn.transform.SetParent(transform, false);
        pSpawn.transform.localPosition = new Vector3(-0.9f, 0.35f, 0f);
        playerSpawnPoint = pSpawn.transform;

        GameObject eSpawn = new GameObject("EnemySpawn");
        eSpawn.transform.SetParent(transform, false);
        eSpawn.transform.localPosition = new Vector3(0.9f, 0.35f, 0f);
        enemySpawnPoint = eSpawn.transform;
    }

    private void CreateBarrier(string barrierName, Vector3 localPos, Vector3 size)
    {
        GameObject barrier = new GameObject(barrierName);
        barrier.transform.SetParent(transform, false);
        barrier.transform.localPosition = localPos;

        BoxCollider col = barrier.AddComponent<BoxCollider>();
        col.size = size;
    }
}
