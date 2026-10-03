using UnityEngine;

public sealed class GrabAbility : FighterAbility
{
    [SerializeField] private AbilityHitbox grabHitbox;
    [SerializeField, Min(0f)] private float holdDuration = 3f;
    [SerializeField, Min(0f)] private float damagePerSecond = 8f;
    [SerializeField, Min(0.1f)] private float damageInterval = 1f;
    [SerializeField, Min(0f)] private float throwForce = 5f;
    [SerializeField, Min(0.02f)] private float hitboxActiveDuration = 0.2f;

    public override string AbilityName => "AGARRE";
    protected override bool StartCooldownAfterExecution => true;

    protected override void Awake()
    {
        base.Awake();
        if (grabHitbox == null) grabHitbox = GetComponentInChildren<AbilityHitbox>(true);
    }

    protected override bool ActivateAbility()
    {
        return grabHitbox != null && grabHitbox.ActivateGrab(
            this,
            Owner,
            damagePerSecond,
            holdDuration,
            damageInterval,
            throwForce,
            hitboxActiveDuration);
    }
}