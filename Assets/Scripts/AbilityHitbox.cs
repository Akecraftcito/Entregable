using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class AbilityHitbox : MonoBehaviour
{
    private readonly HashSet<FighterController> hitTargets = new HashSet<FighterController>();

    private FighterController ownerFighter;
    private GrabAbility grabAbility;
    private float damage;
    private bool ignoresBlocking;
    private bool isFinalHit;
    private bool isGrabAttack;
    private bool isConfigured;
    private bool grabConnected;
    private float grabDuration;
    private float grabDamageInterval;
    private float throwForce;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        ownerFighter = GetComponentInParent<FighterController>();
    }

    public bool ActivateStrike(FighterController owner, float strikeDamage, bool bypassBlocking, bool finalHit, float activeDuration)
    {
        if (!Prepare(owner, strikeDamage, activeDuration)) return false;

        ignoresBlocking = bypassBlocking;
        isFinalHit = finalHit;
        isGrabAttack = false;
        return true;
    }

    public bool ActivateGrab(GrabAbility ability, FighterController owner, float damagePerSecond, float duration, float damageInterval, float launchForce, float activeDuration)
    {
        if (!Prepare(owner, damagePerSecond, activeDuration)) return false;

        grabAbility = ability;
        grabConnected = false;
        ignoresBlocking = true;
        isFinalHit = false;
        isGrabAttack = true;
        grabDuration = Mathf.Max(0f, duration);
        grabDamageInterval = Mathf.Max(0.1f, damageInterval);
        throwForce = Mathf.Max(0f, launchForce);
        return true;
    }

    private bool Prepare(FighterController owner, float hitDamage, float activeDuration)
    {
        if (owner == null || owner.isDefeated || hitDamage < 0f) return false;

        ownerFighter = owner;
        damage = hitDamage;
        hitTargets.Clear();
        isConfigured = true;
        gameObject.SetActive(true);
        owner.StartCoroutine(DisableAfter(Mathf.Max(0.02f, activeDuration)));
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isConfigured || ownerFighter == null || ownerFighter.isDefeated) return;
        if (other.transform == ownerFighter.transform || other.transform.IsChildOf(ownerFighter.transform)) return;

        FighterController target = other.GetComponentInParent<FighterController>();
        if (target == null || target == ownerFighter || !hitTargets.Add(target)) return;

        if (isGrabAttack)
        {
            if (target.TryBeginGrab(ownerFighter))
            {
                grabConnected = true;
                ownerFighter.StartCoroutine(GrabTarget(target, ownerFighter));
            }
            return;
        }

        float appliedDamage = target.TakeHit(damage, isFinalHit, ownerFighter.transform.position, ignoresBlocking);
        ownerFighter.AddDamageDealt(appliedDamage);
    }

    private IEnumerator GrabTarget(FighterController target, FighterController attacker)
    {
        float elapsed = 0f;
        while (elapsed < grabDuration && target != null && attacker != null && !target.isDefeated && !attacker.isDefeated && target.IsBeingGrabbed)
        {
            float interval = Mathf.Min(grabDamageInterval, grabDuration - elapsed);
            yield return new WaitForSeconds(interval);
            elapsed += interval;

            if (target != null && attacker != null && !target.isDefeated && !attacker.isDefeated && target.IsBeingGrabbed)
            {
                float appliedDamage = target.TakeHit(damage * interval, false, attacker.transform.position, ignoresBlocking);
                attacker.AddDamageDealt(appliedDamage);
            }
        }

        if (target != null)
        {
            target.ReleaseFromGrab(attacker, attacker != null && !attacker.isDefeated ? throwForce : 0f);
        }

        if (grabAbility != null) grabAbility.CompleteExecution();
    }

    private IEnumerator DisableAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (gameObject != null)
        {
            isConfigured = false;
            gameObject.SetActive(false);
            if (isGrabAttack && !grabConnected && grabAbility != null)
            {
                grabAbility.CompleteExecution();
            }
        }
    }
}