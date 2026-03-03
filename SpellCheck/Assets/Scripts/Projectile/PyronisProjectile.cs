using UnityEngine;

public class PyronisProjectile : HomingProjectileBase
{
    protected override void Start()
    {
        moveSpeed = 20f;
        turnSpeed = 720f;
        maxLifetime = 5f;

        base.Start();
    }
}