using UnityEngine;

public class HitboxVisual : MonoBehaviour
{
    private float damage;
    private bool isFinalHit;
    private Vector3 attackerPosition;
    private string targetTag;

    /// <summary>
    /// Configura los parámetros de impacto antes de encender la hitbox visual.
    /// </summary>
    public void Setup(float damageAmount, bool finalHit, Vector3 attackerPos, string enemyTag)
    {
        damage = damageAmount;
        isFinalHit = finalHit;
        attackerPosition = attackerPos;
        targetTag = enemyTag;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Evitar golpear a objetos con el mismo Tag (compañeros/uno mismo)
        if (other.CompareTag(targetTag))
        {
            FighterController target = other.GetComponent<FighterController>();
            if (target != null)
            {
                target.TakeHit(damage, isFinalHit, attackerPosition);
            }
        }
    }
}