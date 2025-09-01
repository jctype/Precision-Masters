using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BulletData", menuName = "Ballistics/Bullet Data")]
public class BulletData : ScriptableObject
{
    public float mass; // in kilograms
    public float muzzleVelocity; // in meters per second
    public float diameter; // in meters
    public GameObject bulletPrefab; // Prefab for visual representation
}
