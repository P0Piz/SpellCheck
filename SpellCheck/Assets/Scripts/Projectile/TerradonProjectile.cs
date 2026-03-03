using UnityEngine;

public class TerradonProjectile : HomingProjectileBase
{
    protected override void Start()
    {
        moveSpeed = 12f;
        turnSpeed = 260f;
        maxLifetime = 7f;

        base.Start();
    }
}