using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TruckInventory : MonoBehaviour
{
    public List<GameObject> packagesInTruck = new List<GameObject>();

    public GameObject ConsumeRandomPackage()
{
    packagesInTruck.RemoveAll(item => item == null);

    if (packagesInTruck.Count == 0)
        return null;

    int randomIndex = Random.Range(0, packagesInTruck.Count);
    GameObject selectedPackage = packagesInTruck[randomIndex];

    packagesInTruck.RemoveAt(randomIndex);

    return selectedPackage;
}

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