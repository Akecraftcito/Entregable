using System.Collections;
using UnityEngine;

public class FighterController : MonoBehaviour
{
    [Header("Estadísticas y Estados del Luchador")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isBlocking = false;
    
    // 💡 AGREGA ESTA LÍNEA PARA CORREGIR EL ERROR:
    public bool isStunned = false; 

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

    [Header("Referencias Físicas")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameObject blockShield;

    public System.Action<float, float> OnHealthChanged;
    private Coroutine comboResetCoroutine;

    private void Awake()
    {
        currentHealth = maxHealth;
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (punchHitboxCube) punchHitboxCube.SetActive(false);
        if (blockShield) blockShield.SetActive(false);
    }

    // ================================================
    // EJECUCIÓN DEL COMBO
    // ================================================

    public void ExecutePunchCombo()
    {
        if (isStunned || isOnComboCooldown) return;

        currentComboCount++;

        if (comboResetCoroutine != null) StopCoroutine(comboResetCoroutine);

        if (currentComboCount < 4)
        {
            StartCoroutine(ShowHitboxRoutine(punchDamage, false));
            comboResetCoroutine = StartCoroutine(ResetComboTimer());
        }
        else if (currentComboCount == 4)
        {
            StartCoroutine(ShowHitboxRoutine(punchDamage, true));
            currentComboCount = 0;
            StartCoroutine(StartComboCooldownRoutine());
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
        if (isStunned) return;

        isBlocking = active;
        if (blockShield) blockShield.SetActive(active);
    }

    // ================================================
    // RECIBIR DAÑO, STUN Y EMPUJE
    // ================================================

    public void TakeHit(float damage, bool isFinalHit, Vector3 attackDirection)
    {
        if (isBlocking)
        {
            damage *= 0.2f;
            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Aplicar Stun de 0.5s
        StartCoroutine(ApplyStunRoutine(stunDuration));

        // Si fue el 4.º golpe del combo, aplicar Knockback
        if (isFinalHit && rb != null)
        {
            Vector3 pushDirection = (transform.position - attackDirection).normalized;
            pushDirection.y = 0.2f;
            rb.AddForce(pushDirection * knockbackForce, ForceMode.Impulse);
        }

        if (currentHealth <= 0)
        {
            Debug.Log($"{gameObject.name} ha sido derrotado.");
        }
    }

    private IEnumerator ApplyStunRoutine(float duration)
    {
        isStunned = true;
        
        // Si estaba bloqueando, cancelar escudo al ser stuneado
        if (blockShield) blockShield.SetActive(false);

        yield return new WaitForSeconds(duration);
        
        isStunned = false;
    }
}