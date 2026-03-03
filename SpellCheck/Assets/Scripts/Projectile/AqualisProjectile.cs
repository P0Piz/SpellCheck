using UnityEngine;

public class AqualisProjectile : HomingProjectileBase
{
    protected override void Start()
    {
        moveSpeed = 16f;
        turnSpeed = 520f;
        maxLifetime = 6f;

        base.Start();
    }
}