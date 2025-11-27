using System.Text;
using UnityEngine;

public class RifleRaycast : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 30;
    private int currentAmmo;

    [Header("Shooting")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range = 100f;

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
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, range))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);
            
            PlayerShoot shoot = new PlayerShoot
            {
                shooterGuid = Client.Instance.GetGUID(),
                origin = ray.origin,
                direction = ray.direction,
                maxDistance = range,
                damage = damage,
                timestamp = Time.time
            };

            string json = JsonUtility.ToJson(shoot);
            byte[] packet = Encoding.UTF8.GetBytes("SHOOT|" + json);

            Client.Instance.PublicSendPacket(packet, Client.Instance.GetServerEndPoint());
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
