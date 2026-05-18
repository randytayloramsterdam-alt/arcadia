using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class InventoryItem : ScriptableObject
{
    public string itemName = "Item";
    [TextArea(2, 5)]
    public string description = "";
    public GameObject inspectPrefab;

    [Header("Weapon (leave false for non-weapons)")]
    public bool isWeapon;
    public GameObject equipPrefab;
    public int maxAmmo = 7;
    public int reserveAmmo = 21;
    public float damage = 25f;
    public float fireRate = 0.15f;
    public float reloadTime = 1.6f;
    public float adsFOV = 40f;
    public Vector3 adsPosition = new Vector3(0f, -0.18f, 0.35f);
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
}
