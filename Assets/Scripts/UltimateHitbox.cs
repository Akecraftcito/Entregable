using UnityEngine;

public class UltimateHitbox : MonoBehaviour
{
    private FighterUltimate ultimateSystem;
    private FighterController detectedVictim;

    public void SetOwner(FighterUltimate system)
    {
        ultimateSystem = system;
    }

    private void OnEnable()
    {
        detectedVictim = null;
        // Al activarse, busca de inmediato si ya hay alguien dentro del Collider
        CheckForVictimInstant();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryRegisterVictim(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (detectedVictim == null)
        {
            TryRegisterVictim(other);
        }
    }

    private void TryRegisterVictim(Collider other)
    {
        if (ultimateSystem != null && other.gameObject == ultimateSystem.gameObject) return;
        if (other.transform.root == transform.root) return;

        FighterController victim = other.GetComponentInParent<FighterController>();
        if (victim != null && !victim.isDefeated)
        {
            detectedVictim = victim;
            Debug.Log($"[UltimateHitbox] Oponente impactado: {victim.gameObject.name}");
        }
    }

    public void CheckForVictimInstant()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        // Búsqueda instantánea con física sin esperar frames
        Collider[] hits = Physics.OverlapBox(
            col.bounds.center, 
            col.bounds.extents, 
            transform.rotation
        );

        foreach (var hit in hits)
        {
            TryRegisterVictim(hit);
            if (detectedVictim != null) break;
        }
    }

    public bool HasHitVictim(out FighterController victim)
    {
        if (detectedVictim == null)
        {
            CheckForVictimInstant();
        }
        victim = detectedVictim;
        return detectedVictim != null;
    }
}