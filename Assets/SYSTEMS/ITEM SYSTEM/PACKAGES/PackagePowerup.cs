using UnityEngine;

public class PackagePowerup : MonoBehaviour
{
    [Header("Powerup")]
    public GameObject itemPrefab;

    public GameObject GetItemPrefab()
    {
        return itemPrefab;
    }
}