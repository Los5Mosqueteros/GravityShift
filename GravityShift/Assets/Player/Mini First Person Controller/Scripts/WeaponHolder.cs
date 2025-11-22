using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    [SerializeField] private GameObject[] weapons;
    [SerializeField] private Animator animator;
    [SerializeField] private SwayNBobScript swayNBob;
    
    private int currentWeaponIndex = 0;
    private Vector3 defaultBobLimit;
    private Vector3 defaultMultiplier;
    private float defaultBobExaggeration;

    void Start()
    {
        ShowWeapon(0);
        
        if (swayNBob != null)
        {
            defaultBobLimit = swayNBob.bobLimit;
            defaultMultiplier = swayNBob.multiplier;
            defaultBobExaggeration = swayNBob.bobExaggeration;
        }
    }

    void Update()
    {
        HandleWeaponSwitch();
        HandleAiming();
    }

    private void HandleAiming()
    {
        bool isAiming = Input.GetMouseButton(1);
        animator.SetBool("IsAimingRifle", isAiming && currentWeaponIndex == 0);
        animator.SetBool("IsAimingPistol", isAiming && currentWeaponIndex == 1);
        
        UpdateSwayNBob(isAiming);
    }

    private void UpdateSwayNBob(bool isAiming)
    {
        if (swayNBob == null) return;
        
        if (isAiming)
        {
            swayNBob.bobExaggeration = 0f;
            swayNBob.multiplier = Vector3.zero;
            swayNBob.bobLimit = Vector3.one * 0.005f;
        }
        else
        {
            swayNBob.bobExaggeration = defaultBobExaggeration;
            swayNBob.multiplier = defaultMultiplier;
            swayNBob.bobLimit = defaultBobLimit;
        }
    }

    public void SetAiming(bool value)
    {
        animator.SetBool("IsAimingRifle", value && currentWeaponIndex == 0);
        animator.SetBool("IsAimingPistol", value && currentWeaponIndex == 1);
        UpdateSwayNBob(value);
    }

    private void HandleWeaponSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectWeapon(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectWeapon(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectWeapon(2);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) SelectWeapon((currentWeaponIndex - 1 + weapons.Length) % weapons.Length);
        if (scroll < 0f) SelectWeapon((currentWeaponIndex + 1) % weapons.Length);
    }

    private void SelectWeapon(int index)
    {
        currentWeaponIndex = Mathf.Clamp(index, 0, weapons.Length - 1);
        ShowWeapon(currentWeaponIndex);
    }

    private void ShowWeapon(int index)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].SetActive(i == index);
        }
    }
}
