using UnityEngine;

public class PistolRaycast : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 12;
    private int currentAmmo;

    [Header("Shooting")]
    [SerializeField] private float damage = 15f;
    [SerializeField] private float range = 50f;

    [Header("Effects")]
    [SerializeField] private Transform muzzle;
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
        animator.ResetTrigger("Shoot");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && currentAmmo > 0 && !isReloading)
            animator.SetTrigger("Shoot");
        
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
        
        CameraShake.Instance?.Shake(0.1f, 0.06f);

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, range))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);
            
            HitBox hitBox = hit.collider.GetComponent<HitBox>();
            if (hitBox != null)
                hitBox.OnHit(damage);
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * range, Color.yellow, 1f);
        }
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
