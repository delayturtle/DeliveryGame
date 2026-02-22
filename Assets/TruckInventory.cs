using System.Collections.Generic;
using UnityEngine;

public class TruckInventory : MonoBehaviour
{
    public List<GameObject> packagesInTruck = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Package"))
        {
            if (!packagesInTruck.Contains(other.gameObject))
            {
                packagesInTruck.Add(other.gameObject);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Package"))
        {
            if (packagesInTruck.Contains(other.gameObject))
            {
                packagesInTruck.Remove(other.gameObject);
            }
        }
    }

   public bool HasPackage()
{
    packagesInTruck.RemoveAll(item => item == null);
    return packagesInTruck.Count > 0;
}

public int GetPackageCount()
{
    packagesInTruck.RemoveAll(item => item == null);
    return packagesInTruck.Count;
}
    public GameObject GetFirstPackage()
    {
        if (HasPackage())
            return packagesInTruck[0];

        return null;
    }
}