using UnityEngine;

public class Rifle : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 30;
    private int currentAmmo;

    [Header("Shooting")]
    [SerializeField] private float fireRate = 0.1f;
    private float lastFireTime;
    [SerializeField] private float raycastDistance = 10f;

    [Header("Effects")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 50f;
    
    private Camera mainCamera;

    void Start()
    {
        currentAmmo = maxAmmo;
        mainCamera = Camera.main;
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

        Vector3 spawnPos = muzzle != null ? muzzle.position : transform.position;
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint = ray.origin + ray.direction * raycastDistance;
        Vector3 shootDirection = (targetPoint - spawnPos).normalized;
        
        Debug.DrawLine(ray.origin, targetPoint, Color.red, 1f);
        
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        bullet.transform.rotation = Quaternion.FromToRotation(Vector3.left, shootDirection);
        
        Debug.DrawLine(spawnPos, spawnPos + shootDirection * raycastDistance, Color.green, 1f);
        
        Projectile projectile = bullet.GetComponent<Projectile>();
        
        if (projectile != null)
        {
            projectile.speed = bulletSpeed;
            projectile.SetConvergenceData(targetPoint, ray.direction);
        }
    }

    private Vector3 GetShootDirection()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint = ray.origin + ray.direction * raycastDistance;
        
        Debug.DrawLine(ray.origin, targetPoint, Color.red, 0.1f);
        
        return (targetPoint - muzzle.position).normalized;
    }

    private void Reload()
    {
        currentAmmo = maxAmmo;
    }

    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmo;
}
