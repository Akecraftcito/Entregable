using UnityEngine;

public sealed class HeavyStrikeAbility : FighterAbility
{
    [SerializeField] private AbilityHitbox strikeHitbox;
    [SerializeField, Min(0f)] private float damage = 30f;
    [SerializeField, Min(0.02f)] private float hitboxActiveDuration = 0.2f;

    public override string AbilityName => "GOLPE FUERTE";

    protected override void Awake()
    {
        base.Awake();
        if (strikeHitbox == null) strikeHitbox = GetComponentInChildren<AbilityHitbox>(true);
    }

    protected override bool ActivateAbility()
    {
        return strikeHitbox != null && strikeHitbox.ActivateStrike(Owner, damage, true, true, hitboxActiveDuration);
    }
}