using UnityEngine;

public sealed class ProjectileAbility : FighterAbility
{
    [SerializeField] private AbilityProjectile projectilePrefab;
    [SerializeField] private Transform launchPoint;
    [SerializeField, Min(0f)] private float damage = 20f;
    [SerializeField, Min(0f)] private float projectileSpeed = 0.1f;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 3f;

    public override string AbilityName => "PROYECTIL";

    protected override bool ActivateAbility()
    {
        if (projectilePrefab == null) return false;

        Vector3 direction = Owner.StageRight * (Owner.FacingRight ? 1f : -1f);
        Vector3 spawnPosition = launchPoint != null
            ? launchPoint.position
            : Owner.transform.position + direction * 0.35f + Owner.StageUp * 0.25f;
        Quaternion rotation = Quaternion.LookRotation(direction, Owner.StageUp);

        AbilityProjectile projectile = Instantiate(projectilePrefab, spawnPosition, rotation);
        projectile.transform.SetParent(null, true);
        projectile.transform.localScale = Vector3.Scale(projectile.transform.localScale, Owner.transform.lossyScale);
        projectile.gameObject.SetActive(true);
        projectile.Initialize(Owner, damage, direction, projectileSpeed, projectileLifetime);
        return true;
    }
}