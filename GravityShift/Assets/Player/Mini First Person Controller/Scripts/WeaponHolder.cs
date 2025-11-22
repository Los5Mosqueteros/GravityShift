using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    [SerializeField] private GameObject[] weapons;
    private int currentWeaponIndex = 0;

    void Start()
    {
        ShowWeapon(0);
    }

    void Update()
    {
        HandleWeaponSwitch();
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
