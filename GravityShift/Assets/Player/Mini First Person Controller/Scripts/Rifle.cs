using UnityEngine;

public class Rifle : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 30;
    private int currentAmmo;

    [Header("Shooting")]
    [SerializeField] private float fireRate = 0.1f;
    private float lastFireTime;

    [Header("Effects")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 50f;

    void Start()
    {
        currentAmmo = maxAmmo;
    }

    void Update()
    {
        HandleShooting();
        HandleReload();
    }

    private void HandleShooting()
    {
        if (Input.GetMouseButton(0) && Time.time >= lastFireTime + fireRate)
        {
            Shoot();
            lastFireTime = Time.time;
        }
    }

    private void HandleReload()
    {
        if (Input.GetKeyDown(KeyCode.R))
            Reload();
    }

    private void Shoot()
    {
        if (currentAmmo <= 0)
            return;

        currentAmmo--;
        
        if (muzzleFlash != null)
            muzzleFlash.Play();

        Vector3 spawnPos = muzzle != null ? muzzle.position : transform.position;
        Quaternion spawnRot = muzzle != null ? muzzle.rotation : transform.rotation;
        
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, spawnRot);
        
        Projectile projectile = bullet.GetComponent<Projectile>();
        
        if (projectile != null)
            projectile.speed = bulletSpeed;
    }

    private void Reload()
    {
        currentAmmo = maxAmmo;
    }

    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmo;
}
