using System.Collections;
using UnityEngine;

public class FighterAI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private FighterController myFighter;
    [SerializeField] private FighterController targetFighter;
    [SerializeField] private Transform stageReference;

    [Header("Dificultad")]
    public GameDifficulty difficulty = GameDifficulty.Medium;

    [Header("Parámetros de Distancia")]
    [SerializeField] private float attackRange = 0.75f;
    [SerializeField] private float retreatRange = 0.4f;

    private float nextDecisionTime = 0f;
    private float currentHorizontalInput = 0f;
    private bool isDefending = false;
    private Coroutine defenseCoroutine;

    private void Awake()
    {
        if (myFighter == null) myFighter = GetComponent<FighterController>();
    }

    public void Setup(FighterController target, GameDifficulty diff, Transform stage)
    {
        targetFighter = target;
        difficulty = diff;
        stageReference = stage;

        if (myFighter != null && stageReference != null)
        {
            myFighter.SetStageReference(stageReference);
        }
    }

    private void Update()
    {
        if (myFighter == null || myFighter.isDefeated || targetFighter == null || targetFighter.isDefeated)
        {
            if (myFighter != null && isDefending)
            {
                myFighter.SetBlocking(false);
                isDefending = false;
            }
            return;
        }

        if (myFighter.IsPerformingUltimate) return;

        // Siempre orientarse hacia el rival
        myFighter.FaceOpponent(targetFighter.transform.position);

        // Control de IA según temporizador de decisión (velocidad de reacción)
        if (Time.time >= nextDecisionTime)
        {
            MakeDecision();
            ScheduleNextDecision();
        }

        // Aplicar movimiento continuo
        if (!isDefending && !myFighter.isStunned)
        {
            myFighter.Move(currentHorizontalInput);
        }
    }

    private void ScheduleNextDecision()
    {
        float interval;
        switch (difficulty)
        {
            case GameDifficulty.Easy:
                // Reacción lenta y torpe: 0.8s a 1.3s
                interval = Random.Range(0.8f, 1.3f);
                break;
            case GameDifficulty.Medium:
                // Reacción normal: 0.35s a 0.5f
                interval = Random.Range(0.35f, 0.5f);
                break;
            case GameDifficulty.Hard:
            default:
                // Reacción muy rápida: 0.10s a 0.18s
                interval = Random.Range(0.10f, 0.18f);
                break;
        }
        nextDecisionTime = Time.time + interval;
    }

    private void MakeDecision()
    {
        Vector3 rightDir = stageReference != null ? stageReference.right : Vector3.right;
        Vector3 toTarget = targetFighter.transform.position - transform.position;
        float distanceAlongStage = Vector3.Dot(toTarget, rightDir);
        float absDistance = Mathf.Abs(distanceAlongStage);
        float directionToTarget = Mathf.Sign(distanceAlongStage);

        if (absDistance <= attackRange * 1.5f && myFighter.CanUseUltimate && myFighter.TryUseUltimate())
        {
            currentHorizontalInput = 0f;
            return;
        }

        switch (difficulty)
        {
            case GameDifficulty.Easy:
                ProcessEasyAI(absDistance, directionToTarget);
                break;

            case GameDifficulty.Medium:
                ProcessMediumAI(absDistance, directionToTarget);
                break;

            case GameDifficulty.Hard:
            default:
                ProcessHardAI(absDistance, directionToTarget);
                break;
        }
    }

    // ================================================
    // DIFICULTAD FÁCIL: Lento y Torpe
    // ================================================
    private void ProcessEasyAI(float absDistance, float directionToTarget)
    {
        // 35% de probabilidad de torpeza (quedarse quieto o alejarse)
        float roll = Random.value;
        if (roll < 0.35f)
        {
            currentHorizontalInput = roll < 0.2f ? 0f : -directionToTarget * 0.5f;
            return;
        }

        if (absDistance > attackRange)
        {
            // Acercarse despacio
            currentHorizontalInput = directionToTarget * 0.7f;
        }
        else
        {
            // En rango: golpear poco o bloquear raramente
            currentHorizontalInput = 0f;

            if (TryUseSpecialAbility(0.1f)) return;

            if (targetFighter.currentComboCount > 0 && Random.value < 0.15f)
            {
                TriggerTimedBlock(Random.Range(0.4f, 0.7f));
            }
            else if (Random.value < 0.5f && !myFighter.isOnComboCooldown)
            {
                myFighter.ExecutePunchCombo();
            }
        }
    }

    // ================================================
    // DIFICULTAD NORMAL: Preciso y Equilibrado
    // ================================================
    private void ProcessMediumAI(float absDistance, float directionToTarget)
    {
        if (absDistance > attackRange)
        {
            currentHorizontalInput = directionToTarget;

            // Salto ocasional para cerrar distancia
            if (absDistance > attackRange * 1.5f && Random.value < 0.2f && myFighter.isGrounded)
            {
                myFighter.Jump();
            }
        }
        else if (absDistance < retreatRange && Random.value < 0.3f)
        {
            // Reubicarse hacia atrás
            currentHorizontalInput = -directionToTarget * 0.8f;
        }
        else
        {
            currentHorizontalInput = 0f;

            if (TryUseSpecialAbility(0.25f)) return;

            // Reaccionar a golpes del jugador bloqueando con 50% de probabilidad
            if (targetFighter.currentComboCount > 0 && Random.value < 0.5f)
            {
                TriggerTimedBlock(Random.Range(0.3f, 0.5f));
            }
            else if (!myFighter.isOnComboCooldown)
            {
                myFighter.ExecutePunchCombo();
            }
        }
    }

    // ================================================
    // DIFICULTAD DIFÍCIL: Rápido, Agresivo y Preciso
    // ================================================
    private void ProcessHardAI(float absDistance, float directionToTarget)
    {
        // Reacción defensiva instantánea si el jugador ataca cerca
        if (targetFighter.currentComboCount > 0 && absDistance <= attackRange * 1.15f)
        {
            if (Random.value < 0.85f && !isDefending)
            {
                TriggerTimedBlock(Random.Range(0.25f, 0.4f));
                return;
            }
        }

        if (absDistance > attackRange)
        {
            // Persecución agresiva
            currentHorizontalInput = directionToTarget;

            // Salto táctico para acortar distancia o esquivar
            if (Random.value < 0.35f && myFighter.isGrounded)
            {
                myFighter.Jump();
            }
        }
        else
        {
            // En rango: combos continuos y presión
            currentHorizontalInput = 0f;

            if (TryUseSpecialAbility(0.5f)) return;

            if (!myFighter.isOnComboCooldown)
            {
                myFighter.ExecutePunchCombo();
            }
            else
            {
                // En enfriamiento de combo: esquivar o retroceder ligeramente
                if (Random.value < 0.5f && myFighter.isGrounded)
                {
                    myFighter.Jump();
                }
                currentHorizontalInput = -directionToTarget * 0.6f;
            }
        }
    }

    private bool TryUseSpecialAbility(float chance)
    {
        return Random.value < chance && myFighter.TryUseAbility();
    }

    private void TriggerTimedBlock(float duration)
    {
        if (defenseCoroutine != null) StopCoroutine(defenseCoroutine);
        defenseCoroutine = StartCoroutine(TimedBlockRoutine(duration));
    }

    private IEnumerator TimedBlockRoutine(float duration)
    {
        isDefending = true;
        currentHorizontalInput = 0f;
        myFighter.SetBlocking(true);

        yield return new WaitForSeconds(duration);

        if (myFighter != null) myFighter.SetBlocking(false);
        isDefending = false;
    }
}
