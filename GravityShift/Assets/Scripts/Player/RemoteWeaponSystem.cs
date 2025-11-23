using UnityEngine;

public class RemoteWeaponSystem : MonoBehaviour
{
    public GameObject[] weapons;

    public void SetWeapon(int index)
    {
        for(int i = 0; i < weapons.Length; i++)
        {
            weapons[i].SetActive(i == index);
        }
    }

    public void SetAiming(bool aiming)
    {
        
    }

    public void SetShooting(bool shooting)
    {
        
    }
}
