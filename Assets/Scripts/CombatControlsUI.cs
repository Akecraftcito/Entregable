using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CombatControlsUI : MonoBehaviour
{
    [Header("Referencias UI Móvil")]
    [SerializeField] private VirtualJoystick joystick;
    [SerializeField] private Button punchButton;
    [SerializeField] private Button abilityButton;
    [SerializeField] private TMP_Text abilityButtonLabel;
    [SerializeField] private Button ultimateButton;
    [SerializeField] private TMP_Text ultimateButtonLabel;
    [SerializeField] private Image ultimateChargeFill;
    [SerializeField] private EventTrigger blockButtonTrigger;

    [Header("Jugador Controlado")]
    [SerializeField] private FighterController playerFighter;

    private bool isHoldingBlockMobile = false;

    public void SetPlayerFighter(FighterController fighter)
    {
        playerFighter = fighter;

        if (joystick != null)
        {
            joystick.OnJumpTriggered -= OnMobileJump;
            joystick.OnJumpTriggered += OnMobileJump;
        }
    }

    private void Start()
    {
        SetupMobileButtons();
    }

    private void SetupMobileButtons()
    {
        if (punchButton != null)
        {
            punchButton.onClick.RemoveAllListeners();
            punchButton.onClick.AddListener(OnMobilePunch);
        }

        if (abilityButton != null)
        {
            abilityButton.onClick.RemoveListener(OnMobileAbility);
            abilityButton.onClick.AddListener(OnMobileAbility);
        }

        if (ultimateButton != null)
        {
            ultimateButton.onClick.RemoveListener(OnMobileUltimate);
            ultimateButton.onClick.AddListener(OnMobileUltimate);
        }

        if (blockButtonTrigger != null)
        {
            blockButtonTrigger.triggers.Clear();

            // Evento PointerDown (Empezar a bloquear)
            EventTrigger.Entry pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener((data) => {
                isHoldingBlockMobile = true;
                if (playerFighter != null) playerFighter.SetBlocking(true);
            });
            blockButtonTrigger.triggers.Add(pointerDown);

            // Evento PointerUp (Soltar bloqueo)
            EventTrigger.Entry pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener((data) => {
                isHoldingBlockMobile = false;
                if (playerFighter != null) playerFighter.SetBlocking(false);
            });
            blockButtonTrigger.triggers.Add(pointerUp);
        }

        if (joystick != null)
        {
            joystick.OnJumpTriggered -= OnMobileJump;
            joystick.OnJumpTriggered += OnMobileJump;
        }
    }

    private void Update()
    {
        UpdateAbilityButton();
        UpdateUltimateButton();
        if (playerFighter == null || playerFighter.isDefeated) return;
        if (playerFighter.IsPerformingUltimate)
        {
            if (playerFighter.isBlocking) playerFighter.SetBlocking(false);
            return;
        }

        // ============================================
        // 1. MOVIMIENTO HORIZONTAL (Móvil Joystick + PC WASD/Flechas)
        // ============================================
        float horizontal = 0f;

        // Entrada PC: Teclas A (-1) y D (+1)
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            horizontal -= 1f;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            horizontal += 1f;
        }

        // Si el joystick móvil está en uso, tiene prioridad o se suma
        if (joystick != null && Mathf.Abs(joystick.Horizontal) > 0.1f)
        {
            horizontal = joystick.Horizontal;
        }

        if (Mathf.Abs(horizontal) > 0.05f)
        {
            playerFighter.Move(horizontal);
        }

        // ============================================
        // 2. SALTO EN PC (Espacio o tecla W)
        // ============================================
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            playerFighter.Jump();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            playerFighter.TryUseAbility();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            playerFighter.TryUseUltimate();
        }

        // ============================================
        // 3. GOLPE EN PC (Click Izquierdo del ratón)
        // ============================================
        if (Input.GetMouseButtonDown(0))
        {
            // Solo atacar con el ratón si no se está pulsando un botón de UI
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                playerFighter.ExecutePunchCombo();
            }
        }

        // ============================================
        // 4. BLOQUEO EN PC (Mantener tecla F)
        // ============================================
        bool isHoldingF = Input.GetKey(KeyCode.F);
        if (isHoldingF || isHoldingBlockMobile)
        {
            if (!playerFighter.isBlocking)
            {
                playerFighter.SetBlocking(true);
            }
        }
        else
        {
            if (playerFighter.isBlocking)
            {
                playerFighter.SetBlocking(false);
            }
        }
    }

    private void OnMobilePunch()
    {
        if (playerFighter != null)
        {
            playerFighter.ExecutePunchCombo();
        }
    }

    private void OnMobileAbility()
    {
        if (playerFighter != null) playerFighter.TryUseAbility();
    }

    private void OnMobileUltimate()
    {
        if (playerFighter != null) playerFighter.TryUseUltimate();
    }

    private void UpdateAbilityButton()
    {
        if (abilityButton == null) return;

        FighterAbility ability = playerFighter != null ? playerFighter.Ability : null;
        float cooldownRemaining = ability != null ? ability.CooldownRemaining : 0f;
        abilityButton.interactable = ability != null
            && ability.IsReady
            && !playerFighter.isDefeated
            && !playerFighter.isStunned
            && !playerFighter.IsBeingGrabbed;

        if (abilityButtonLabel != null)
        {
            abilityButtonLabel.text = ability == null
                ? "HABILIDAD"
                : ability.IsExecuting
                    ? "EN USO"
                : cooldownRemaining > 0f
                    ? $"{cooldownRemaining:0.0}s"
                    : ability.AbilityName;
        }
    }

    private void UpdateUltimateButton()
    {
        if (ultimateButton == null || playerFighter == null) return;

        float charge = playerFighter.UltimateCharge;
        float required = playerFighter.UltimateChargeRequired;
        float normalizedCharge = required > 0f ? Mathf.Clamp01(charge / required) : 0f;

        if (ultimateChargeFill != null) ultimateChargeFill.fillAmount = normalizedCharge;
        ultimateButton.interactable = playerFighter.CanUseUltimate;

        if (ultimateButtonLabel != null)
        {
            ultimateButtonLabel.text = playerFighter.IsPerformingUltimate
                ? "ULT EN USO"
                : playerFighter.IsUltimateCharged && !playerFighter.HasUltimateConfiguration
                    ? "ASIGNA HITBOX"
                    : playerFighter.IsUltimateCharged
                        ? "ULT (G)"
                        : $"ULT {Mathf.FloorToInt(normalizedCharge * 100f)}%";
        }
    }

    private void OnMobileJump()
    {
        if (playerFighter != null)
        {
            playerFighter.Jump();
        }
    }
}
