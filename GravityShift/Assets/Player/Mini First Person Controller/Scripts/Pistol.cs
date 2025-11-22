using UnityEngine;

public class Pistol : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 12;
    private int currentAmmo;

    [Header("Shooting")]
    [SerializeField] private float raycastDistance = 10f;

    [Header("Effects")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 60f;
    [SerializeField] private Animator animator;
    
    private Camera mainCamera;
    private bool isReloading = false;

    void Start()
    {
        currentAmmo = maxAmmo;
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && currentAmmo > 0 && !isReloading)
            animator.SetTrigger("Shoot");
        
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo && !isReloading)
        {
            isReloading = true;
            animator.SetTrigger("Reload");
        }
    }

    public void Shoot()
    {
        if (currentAmmo <= 0) return;

        currentAmmo--;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint = ray.origin + ray.direction * raycastDistance;
        Vector3 shootDirection = (targetPoint - muzzle.position).normalized;
        
        GameObject bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.FromToRotation(Vector3.left, shootDirection));
        Projectile projectile = bullet.GetComponent<Projectile>();
        
        projectile.speed = bulletSpeed;
        projectile.SetConvergenceData(targetPoint, ray.direction);
    }

    public void Reload()
    {
        currentAmmo = maxAmmo;
        isReloading = false;
    }

    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmo;
}
