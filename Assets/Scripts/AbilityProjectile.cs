using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class AbilityProjectile : MonoBehaviour
{
    private FighterController ownerFighter;
    private Rigidbody projectileBody;
    private Vector3 direction;
    private float damage;
    private float speed;
    private bool isInitialized;
    private bool hasHit;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        projectileBody = GetComponent<Rigidbody>();
        if (projectileBody != null)
        {
            projectileBody.useGravity = false;
            projectileBody.isKinematic = true;
        }
    }

    public void Initialize(FighterController owner, float projectileDamage, Vector3 travelDirection, float travelSpeed, float lifetime)
    {
        ownerFighter = owner;
        damage = projectileDamage;
        direction = travelDirection.normalized;
        speed = Mathf.Max(0f, travelSpeed);
        isInitialized = ownerFighter != null;
        Destroy(gameObject, Mathf.Max(0.1f, lifetime));
    }

    private void FixedUpdate()
    {
        if (!isInitialized || hasHit) return;

        Vector3 nextPosition = transform.position + direction * speed * Time.fixedDeltaTime;
        if (projectileBody != null) projectileBody.MovePosition(nextPosition);
        else transform.position = nextPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInitialized || hasHit || ownerFighter == null) return;
        if (other.transform == ownerFighter.transform || other.transform.IsChildOf(ownerFighter.transform)) return;

        FighterController target = other.GetComponentInParent<FighterController>();
        if (target == null || target == ownerFighter) return;

        hasHit = true;
        float appliedDamage = target.TakeHit(damage, false, ownerFighter.transform.position);
        ownerFighter.AddDamageDealt(appliedDamage);
        Destroy(gameObject);
    }
}