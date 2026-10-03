using UnityEngine;

public abstract class FighterAbility : MonoBehaviour
{
    [SerializeField, Min(0f)] private float cooldownSeconds = 5f;

    private float nextReadyTime;
    private bool isExecuting;

    protected FighterController Owner { get; private set; }

    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);
    public bool IsExecuting => isExecuting;
    public bool IsReady => !isExecuting && CooldownRemaining <= 0f;
    public abstract string AbilityName { get; }
    protected virtual bool StartCooldownAfterExecution => false;

    protected virtual void Awake()
    {
        Owner = GetComponent<FighterController>();
    }

    public bool TryActivate()
    {
        if (Owner == null) Owner = GetComponent<FighterController>();
        if (Owner == null || !IsReady || Owner.isDefeated || Owner.isStunned || Owner.IsBeingGrabbed)
        {
            return false;
        }

        if (Owner.isBlocking) Owner.SetBlocking(false);
        if (!ActivateAbility()) return false;

        if (StartCooldownAfterExecution)
        {
            isExecuting = true;
        }
        else
        {
            nextReadyTime = Time.time + cooldownSeconds;
        }

        Owner.NotifySpecialSkillExecuted();
        return true;
    }

    public void CompleteExecution()
    {
        if (!isExecuting) return;

        isExecuting = false;
        nextReadyTime = Time.time + cooldownSeconds;
    }

    public void ResetCooldown()
    {
        isExecuting = false;
        nextReadyTime = 0f;
    }

    protected abstract bool ActivateAbility();
}