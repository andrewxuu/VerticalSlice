using UnityEngine;

public class PickupDrop : MonoBehaviour
{
    [HideInInspector] public ItemData item;

    [Tooltip("Radius of the pickup trigger. Tune in the Inspector.")]
    public float pickupRadius = 0.6f;

    void Awake()
    {
        SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius    = pickupRadius;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) Pickup();
    }

    void Pickup()
    {
        if (item != null)
            InventoryManager.Instance.AddItem(item);
        else
            Debug.LogWarning("[PickupDrop] item is null — was it assigned by SpawnDrop?");

        Destroy(gameObject);
    }
}