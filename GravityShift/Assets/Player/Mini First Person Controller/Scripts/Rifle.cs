using UnityEngine;

public class Rifle : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 30;
    private int currentAmmo;

    [Header("Shooting")]
    [SerializeField] private float raycastDistance = 10f;

    [Header("Effects")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 50f;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] shootSounds;
    [SerializeField] private AudioClip reloadSound;
    
    private Camera mainCamera;
    private bool isReloading = false;
    private WeaponHolder weaponHolder;

    void Start()
    {
        currentAmmo = maxAmmo;
        mainCamera = Camera.main;
        weaponHolder = GetComponentInParent<WeaponHolder>();
    }

    void OnDisable()
    {
        if (isReloading)
        {
            isReloading = false;
            animator.ResetTrigger("Reload");
        }
        animator.SetBool("IsShooting", false);
    }

    void Update()
    {
        animator.SetBool("IsShooting", Input.GetMouseButton(0) && currentAmmo > 0 && !isReloading);
        
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo && !isReloading)
        {
            isReloading = true;
            weaponHolder.SetAiming(false);
            animator.SetTrigger("Reload");
        }
    }

    public void Shoot()
    {
        if (currentAmmo <= 0) return;

        currentAmmo--;
        
        if (audioSource != null && shootSounds.Length > 0)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(shootSounds[0]);
        }
        
        CameraShake.Instance?.Shake(0.08f, 0.04f);

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint = ray.origin + ray.direction * raycastDistance;
        Vector3 shootDirection = (targetPoint - muzzle.position).normalized;
        
        // Tienes que usar el projectile manager, que para algo lo tienes, sino no se puede enviar la informacion de la bala a los demas
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

    public void PlayReloadSound()
    {
        if (audioSource != null && reloadSound != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(reloadSound);
        }
    }

    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmo;
}
