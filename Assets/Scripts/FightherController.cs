using System.Collections;
using UnityEngine;

public class FighterController : MonoBehaviour
{
    [Header("Estadísticas y Estados del Luchador")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isBlocking = false;
    public bool isStunned = false;
    public bool isDefeated = false;

    [Header("Movimiento y Salto 2D")]
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float jumpForce = 3.8f; // Salto ligero
    [SerializeField] private float groundCheckDistance = 0.25f;
    [SerializeField] private LayerMask groundLayer = ~0; // Por defecto detecta todo excepto triggers
    public bool isGrounded = true;

    [Header("Identificación de Bando")]
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Cubo de Hitbox Único")]
    [SerializeField] private GameObject punchHitboxCube;

    [Header("Ajustes de Golpe")]
    [SerializeField] private float punchDamage = 8f;
    [SerializeField] private float punchVisualDuration = 0.2f;
    [SerializeField] private float stunDuration = 0.5f;   // Duración del aturdimiento
    [SerializeField] private float knockbackForce = 3f;   // Fuerza de empuje del 4.º golpe

    [Header("Sistema de Combo de 4 Golpes")]
    public int currentComboCount = 0;
    public float comboResetTime = 1.5f;
    public float comboCooldown = 2.0f;
    public bool isOnComboCooldown = false;

    [Header("Carga de Ultimate")]
    [SerializeField, Min(1f)] private float ultimateChargeRequired = 50f;

    [Header("Referencias Físicas y Visuales")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameObject blockShield;
    [SerializeField] private Transform stageReference;

    public System.Action<float, float> OnHealthChanged;
    public System.Action<float, float> OnUltimateChargeChanged;
    public System.Action<FighterController> OnDefeated;
    public System.Action OnSpecialSkillExecuted;

    private Coroutine comboResetCoroutine;
    private Coroutine stunCoroutine;
    private Coroutine comboCooldownCoroutine;
    private FighterAbility fighterAbility;
    private FighterUltimate fighterUltimate;
    private float ultimateCharge;
    private FighterController grabHolder;
    private bool isBeingGrabbed;
    private bool facingRight = true;

    public bool FacingRight => facingRight;
    public bool IsBeingGrabbed => isBeingGrabbed;
    public bool IsPerformingUltimate => fighterUltimate != null && fighterUltimate.IsExecuting;
    public bool HasUltimateConfiguration => fighterUltimate != null && fighterUltimate.HasRequiredAssets;
    public bool IsUltimateCharged => ultimateCharge >= ultimateChargeRequired;
    public bool CanUseUltimate => fighterUltimate != null && fighterUltimate.CanActivate;
    public float UltimateCharge => ultimateCharge;
    public float UltimateChargeRequired => ultimateChargeRequired;
    public FighterAbility Ability => fighterAbility != null ? fighterAbility : GetComponent<FighterAbility>();
    public Vector3 StageRight => stageReference != null ? stageReference.right : Vector3.right;
    public Vector3 StageUp => stageReference != null ? stageReference.up : Vector3.up;

    private void Awake()
    {
        currentHealth = maxHealth;
        fighterAbility = GetComponent<FighterAbility>();
        fighterUltimate = GetComponent<FighterUltimate>();

        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        // Configuración de Rigidbody para combate 2D estable
        rb.mass = 1f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 5f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Auto-encontrar Hitbox si no está asignado
        if (punchHitboxCube == null)
        {
            HitboxVisual visual = GetComponentInChildren<HitboxVisual>(true);
            if (visual != null) punchHitboxCube = visual.gameObject;
        }

        if (punchHitboxCube != null)
        {
            punchHitboxCube.SetActive(false);
        }

        // Crear escudo visual procedural si no existe
        if (blockShield == null)
        {
            CreateDefaultBlockShield();
        }

        if (blockShield != null)
        {
            blockShield.SetActive(false);
        }
    }

    private void Update()
    {
        CheckGrounded();
    }

    private void FixedUpdate()
    {
        if (!isBeingGrabbed) return;

        if (grabHolder == null || grabHolder.isDefeated)
        {
            ReleaseFromGrab(grabHolder, 0f);
            return;
        }

        Vector3 side = grabHolder.FacingRight ? grabHolder.StageRight : -grabHolder.StageRight;
        Vector3 heldPosition = grabHolder.transform.position + side * 0.35f + StageUp * 0.15f;
        if (rb != null) rb.MovePosition(heldPosition);
        else transform.position = heldPosition;
    }

    // ================================================
    // CONFIGURACIÓN Y ORIENTACIÓN
    // ================================================

    public void SetStageReference(Transform stage)
    {
        stageReference = stage;
    }

    public void SetEnemyTag(string tag)
    {
        enemyTag = tag;
    }

    public void SetFacing(bool faceRight)
    {
        facingRight = faceRight;
        // Rotación 0 para mirar a la derecha, 180 en Y para mirar a la izquierda
        transform.localRotation = faceRight ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
    }

    public void FaceOpponent(Vector3 opponentPosition)
    {
        Vector3 rightDir = stageReference != null ? stageReference.right : Vector3.right;
        Vector3 toOpponent = opponentPosition - transform.position;
        float dot = Vector3.Dot(toOpponent, rightDir);

        if (dot > 0.05f && !facingRight)
        {
            SetFacing(true);
        }
        else if (dot < -0.05f && facingRight)
        {
            SetFacing(false);
        }
    }

    // ================================================
    // MOVIMIENTO Y SALTO
    // ================================================

    public void Move(float horizontalInput)
    {
        if (isStunned || isDefeated || isBeingGrabbed || IsPerformingUltimate) return;

        // Si está bloqueando se mueve mucho más lento
        float actualSpeed = isBlocking ? moveSpeed * 0.3f : moveSpeed;
        Vector3 moveAxis = stageReference != null ? stageReference.right : Vector3.right;

        Vector3 moveDelta = moveAxis * (horizontalInput * actualSpeed * Time.deltaTime);
        transform.position += moveDelta;

        // Mantener plano Z relativo al escenario para no separarse de la pared
        if (stageReference != null)
        {
            Vector3 localPos = stageReference.InverseTransformPoint(transform.position);
            localPos.z = 0f;
            transform.position = stageReference.TransformPoint(localPos);
        }
    }

    public void Jump()
    {
        if (isStunned || isDefeated || isBeingGrabbed || IsPerformingUltimate || !isGrounded) return;

        Vector3 jumpDir = stageReference != null ? stageReference.up : Vector3.up;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(jumpDir * jumpForce, ForceMode.Impulse);
        isGrounded = false;
    }

    private void CheckGrounded()
    {
        Vector3 downDir = stageReference != null ? -stageReference.up : Vector3.down;
        Vector3 rayStart = transform.position + (stageReference != null ? stageReference.up : Vector3.up) * 0.05f;

        // Raycast corto hacia abajo para detectar el piso
        if (Physics.Raycast(rayStart, downDir, out RaycastHit hit, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    // ================================================
    // EJECUCIÓN DEL COMBO
    // ================================================

    public void ExecutePunchCombo()
    {
        if (isStunned || isOnComboCooldown || isDefeated || isBeingGrabbed || IsPerformingUltimate) return;

        // Si estaba bloqueando, deja de bloquear para golpear
        if (isBlocking) SetBlocking(false);

        currentComboCount++;

        if (comboResetCoroutine != null) StopCoroutine(comboResetCoroutine);

        if (currentComboCount < 4)
        {
            StartCoroutine(ShowHitboxRoutine(punchDamage, false));
            comboResetCoroutine = StartCoroutine(ResetComboTimer());
        }
        else if (currentComboCount == 4)
        {
            StartCoroutine(ShowHitboxRoutine(punchDamage * 1.5f, true));
            currentComboCount = 0;
            comboCooldownCoroutine = StartCoroutine(StartComboCooldownRoutine());
        }
    }

    private IEnumerator ShowHitboxRoutine(float damage, bool isFinalHit)
    {
        if (punchHitboxCube == null) yield break;

        HitboxVisual hitboxScript = punchHitboxCube.GetComponent<HitboxVisual>();
        if (hitboxScript != null)
        {
            hitboxScript.Setup(damage, isFinalHit, transform.position, enemyTag);
        }

        punchHitboxCube.SetActive(true);
        yield return new WaitForSeconds(punchVisualDuration);
        punchHitboxCube.SetActive(false);
    }

    private IEnumerator ResetComboTimer()
    {
        yield return new WaitForSeconds(comboResetTime);
        currentComboCount = 0;
    }

    private IEnumerator StartComboCooldownRoutine()
    {
        isOnComboCooldown = true;
        yield return new WaitForSeconds(comboCooldown);
        isOnComboCooldown = false;
    }

    // ================================================
    // BLOQUEO
    // ================================================

    public void SetBlocking(bool active)
    {
        if (isStunned || isDefeated || isBeingGrabbed) return;

        isBlocking = active;
        if (blockShield) blockShield.SetActive(active);
    }

    // ================================================
    // RECIBIR DAÑO, STUN Y EMPUJE
    // ================================================

    public float TakeHit(float damage, bool isFinalHit, Vector3 attackDirection, bool ignoreBlocking = false, float customStunDuration = -1f)
    {
        if (isDefeated) return 0f;

        if (isBlocking && !ignoreBlocking)
        {
            float healthBeforeHit = currentHealth;
            damage *= 0.2f;
            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            float appliedDamage = healthBeforeHit - currentHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            AddUltimateCharge(appliedDamage);

            if (rb != null)
            {
                Vector3 pushDirection = (transform.position - attackDirection).normalized;
                rb.AddForce(pushDirection * (knockbackForce * 0.3f), ForceMode.Impulse);
            }

            CheckDefeat();
            return appliedDamage;
        }

        if (isBlocking && ignoreBlocking)
        {
            isBlocking = false;
            if (blockShield != null) blockShield.SetActive(false);
        }

        float healthBeforeUnblockedHit = currentHealth;
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        float unblockedDamage = healthBeforeUnblockedHit - currentHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        AddUltimateCharge(unblockedDamage);

        // Si se pasa una duración personalizada de Stun (ej. 2s para la Ultimate), se usa esa.
        // De lo contrario, se usa la duración predeterminada del personaje.
        float durationToApply = customStunDuration > 0f ? customStunDuration : stunDuration;

        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(ApplyStunRoutine(durationToApply));

        // Empuje del 4.º golpe
        if (isFinalHit && rb != null)
        {
            Vector3 rightDir = stageReference != null ? stageReference.right : Vector3.right;
            Vector3 pushDir = Vector3.Dot(transform.position - attackDirection, rightDir) > 0 ? rightDir : -rightDir;
            pushDir += (stageReference != null ? stageReference.up : Vector3.up) * 0.25f;
            rb.AddForce(pushDir.normalized * knockbackForce, ForceMode.Impulse);
        }

        CheckDefeat();
        return unblockedDamage;
    }

    private void CheckDefeat()
    {
        if (currentHealth <= 0f && !isDefeated)
        {
            isDefeated = true;
            if (isBeingGrabbed) ReleaseFromGrab(grabHolder, 0f);
            if (blockShield) blockShield.SetActive(false);
            if (punchHitboxCube) punchHitboxCube.SetActive(false);
            Debug.Log($"¡{gameObject.name} ha sido derrotado!");
            OnDefeated?.Invoke(this);
        }
    }

    private IEnumerator ApplyStunRoutine(float duration)
    {
        isStunned = true;
        if (blockShield) blockShield.SetActive(false);

        yield return new WaitForSeconds(duration);

        isStunned = false;
    }

    // ================================================
    // HABILIDAD ESPECIAL (EXTENSIBLE)
    // ================================================

    public virtual void ExecuteSpecialSkill()
    {
        TryUseAbility();
    }

    public bool TryUseAbility()
    {
        if (fighterAbility == null) fighterAbility = GetComponent<FighterAbility>();
        return fighterAbility != null && fighterAbility.TryActivate();
    }

    public bool TryUseUltimate()
    {
        if (fighterUltimate == null) fighterUltimate = GetComponent<FighterUltimate>();
        return fighterUltimate != null && fighterUltimate.TryActivate();
    }

    public void AddDamageDealt(float damage)
    {
        AddUltimateCharge(damage);
    }

    public bool ConsumeUltimateCharge()
    {
        if (!IsUltimateCharged || isDefeated) return false;

        ultimateCharge = 0f;
        OnUltimateChargeChanged?.Invoke(ultimateCharge, ultimateChargeRequired);
        return true;
    }

    private void AddUltimateCharge(float damage)
    {
        if (damage <= 0f || isDefeated || IsPerformingUltimate) return;

        ultimateCharge = Mathf.Min(ultimateChargeRequired, ultimateCharge + damage);
        OnUltimateChargeChanged?.Invoke(ultimateCharge, ultimateChargeRequired);
    }

    public void NotifySpecialSkillExecuted()
    {
        OnSpecialSkillExecuted?.Invoke();
    }

    public bool TryBeginGrab(FighterController attacker)
    {
        if (attacker == null || isDefeated || isBeingGrabbed) return false;

        isBlocking = false;
        if (blockShield != null) blockShield.SetActive(false);
        grabHolder = attacker;
        isBeingGrabbed = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        return true;
    }

    public void ReleaseFromGrab(FighterController attacker, float throwForce)
    {
        if (!isBeingGrabbed || (attacker != null && grabHolder != attacker)) return;

        grabHolder = null;
        isBeingGrabbed = false;
        if (rb == null) return;

        rb.isKinematic = false;
        if (throwForce <= 0f || isDefeated) return;

        Vector3 awayDirection = attacker != null
            ? Vector3.Project(transform.position - attacker.transform.position, StageRight)
            : StageRight;
        if (awayDirection.sqrMagnitude < 0.0001f)
        {
            awayDirection = StageRight * (attacker != null && attacker.FacingRight ? 1f : -1f);
        }

        Vector3 launchDirection = (awayDirection.normalized + StageUp * 0.2f).normalized;
        rb.AddForce(launchDirection * throwForce, ForceMode.Impulse);
    }

    // ================================================
    // REINICIO DE ESTADO
    // ================================================

    public void ResetFighter(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (isBeingGrabbed) ReleaseFromGrab(grabHolder, 0f);

        currentHealth = maxHealth;
        isDefeated = false;
        isStunned = false;
        isBlocking = false;
        currentComboCount = 0;
        isOnComboCooldown = false;
        ultimateCharge = 0f;

        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        if (comboResetCoroutine != null) StopCoroutine(comboResetCoroutine);
        if (comboCooldownCoroutine != null) StopCoroutine(comboCooldownCoroutine);
        if (Ability != null) Ability.ResetCooldown();

        if (punchHitboxCube) punchHitboxCube.SetActive(false);
        if (blockShield) blockShield.SetActive(false);

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnUltimateChargeChanged?.Invoke(ultimateCharge, ultimateChargeRequired);
    }

    private void CreateDefaultBlockShield()
    {
        GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shield.name = "BlockShield";
        shield.transform.SetParent(transform, false);
        shield.transform.localPosition = new Vector3(0.4f, 0f, 0f);
        shield.transform.localScale = new Vector3(0.35f, 0.9f, 0.7f);

        Collider col = shield.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer rend = shield.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.1f, 0.8f, 1f, 0.5f);
            rend.material = mat;
        }

        blockShield = shield;
        blockShield.SetActive(false);
    }
}