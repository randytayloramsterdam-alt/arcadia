using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public List<InventoryItem> items = new List<InventoryItem>();
    public int maxSlots = 20;

    public System.Action OnInventoryChanged;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool AddItem(InventoryItem item)
    {
        if (items.Count >= maxSlots)
            return false;
        items.Add(item);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void RemoveItem(InventoryItem item)
    {
        if (items.Remove(item))
            OnInventoryChanged?.Invoke();
    }
}
