using UnityEngine;

public class HitboxVisual : MonoBehaviour
{
    private float damage;
    private bool isFinalHit;
    private Vector3 attackerPosition;
    private string targetTag;
    private FighterController ownerFighter;

    private void Awake()
    {
        ownerFighter = GetComponentInParent<FighterController>();
    }

    /// <summary>
    /// Configura los parámetros de impacto antes de encender la hitbox visual.
    /// </summary>
    public void Setup(float damageAmount, bool finalHit, Vector3 attackerPos, string enemyTag)
    {
        damage = damageAmount;
        isFinalHit = finalHit;
        attackerPosition = attackerPos;
        targetTag = enemyTag;

        if (ownerFighter == null)
        {
            ownerFighter = GetComponentInParent<FighterController>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignorar colisiones consigo mismo o sus componentes hijos
        if (ownerFighter != null && other.transform.IsChildOf(ownerFighter.transform))
        {
            return;
        }

        bool tagMatches = !string.IsNullOrEmpty(targetTag) && other.CompareTag(targetTag);
        FighterController target = other.GetComponentInParent<FighterController>();

        if (target != null && target != ownerFighter)
        {
            // Golpear si coincide el tag o si no tiene tag asignado
            if (tagMatches || string.IsNullOrEmpty(targetTag))
            {
                float appliedDamage = target.TakeHit(damage, isFinalHit, attackerPosition);
                if (ownerFighter != null) ownerFighter.AddDamageDealt(appliedDamage);
            }
        }
    }
}