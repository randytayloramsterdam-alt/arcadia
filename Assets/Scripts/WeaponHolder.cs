using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    [Header("Sway")]
    public float swayAmount = 0.02f;
    public float swaySmooth = 6f;

    [Header("ADS")]
    public float adsTransitionSpeed = 8f;
    public float defaultFOV = 60f;

    private Camera cam;
    private InventoryUI inventoryUI;
    private GameObject weaponInstance;
    private Weapon currentWeapon;
    private bool isAiming;
    private float baseFOV;

    // Sway
    private Vector3 swayOffset;
    private Vector2 swayInput;

    public bool IsAiming => isAiming;
    public bool HasWeapon => currentWeapon != null;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        baseFOV = cam != null ? cam.fieldOfView : defaultFOV;
    }

    void Start()
    {
        inventoryUI = FindObjectOfType<InventoryUI>();
    }

    void Update()
    {
        if (currentWeapon == null) return;

        // Disable weapon input when inventory is open
        if (inventoryUI != null && inventoryUI.IsOpen) return;

        // Right-click hold to aim
        isAiming = Input.GetMouseButton(1);
        currentWeapon.SetADS(isAiming);

        // Fire — only when ADS
        if (isAiming && Input.GetMouseButtonDown(0))
            currentWeapon.Fire(cam);

        // Reload
        if (Input.GetKeyDown(KeyCode.R))
            currentWeapon.Reload();

        // -- ADS FOV zoom --
        float targetFOV = isAiming && currentWeapon.itemData != null ? currentWeapon.itemData.adsFOV : baseFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * adsTransitionSpeed);

        // Weapon sway is handled by Weapon.cs ADS/holstering lerp
    }

    public void EquipWeapon(InventoryItem item)
    {
        if (item == null) return;

        // Unequip current
        UnequipWeapon();

        if (item.equipPrefab != null)
        {
            weaponInstance = Instantiate(item.equipPrefab, transform);
        }
        else
        {
            // Fallback: build a low-poly pistol from primitives at runtime
            weaponInstance = BuildRuntimePistol();
            weaponInstance.transform.SetParent(transform);
        }

        weaponInstance.transform.localPosition = new Vector3(0.2f, -0.2f, 0.5f);
        weaponInstance.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

        currentWeapon = weaponInstance.GetComponent<Weapon>();
        if (currentWeapon == null)
            currentWeapon = weaponInstance.AddComponent<Weapon>();
        currentWeapon.itemData = item;

        // Set up muzzle point
        if (currentWeapon.muzzlePoint == null)
        {
            var mp = new GameObject("MuzzlePoint");
            mp.transform.SetParent(weaponInstance.transform);
            mp.transform.localPosition = new Vector3(0f, 0.055f, 0.10f);
            currentWeapon.muzzlePoint = mp.transform;
        }

        // Configure layer so weapon renders on top of everything
        int weaponLayer = LayerMask.NameToLayer("Weapon");
        if (weaponLayer >= 0)
            SetLayerRecursive(weaponInstance, weaponLayer);
    }

    GameObject BuildRuntimePistol()
    {
        var root = new GameObject("Pistol_Runtime");
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.12f, 0.12f, 0.15f);

        AddPrimitive(root, "Slide",      PrimitiveType.Cube, new Vector3(0.18f, 0.035f, 0.025f), new Vector3(0f, 0.065f, 0.01f), Quaternion.identity, mat);
        AddPrimitive(root, "Barrel",     PrimitiveType.Cube, new Vector3(0.055f, 0.018f, 0.018f), new Vector3(0f, 0.055f, 0.10f), Quaternion.identity, mat);
        AddPrimitive(root, "Frame",      PrimitiveType.Cube, new Vector3(0.10f, 0.028f, 0.025f), new Vector3(0f, 0.042f, 0.005f), Quaternion.identity, mat);
        AddPrimitive(root, "TrigGuard",  PrimitiveType.Cube, new Vector3(0.020f, 0.018f, 0.010f), new Vector3(0f, 0.032f, 0.025f), Quaternion.identity, mat);
        AddPrimitive(root, "Grip",       PrimitiveType.Cube, new Vector3(0.032f, 0.065f, 0.028f), new Vector3(0f, 0.008f, -0.015f), Quaternion.Euler(12f, 0f, 0f), mat);
        AddPrimitive(root, "Mag",        PrimitiveType.Cube, new Vector3(0.022f, 0.015f, 0.020f), new Vector3(0f, -0.025f, -0.010f), Quaternion.identity, mat);
        AddPrimitive(root, "FrontSight", PrimitiveType.Cube, new Vector3(0.006f, 0.008f, 0.004f), new Vector3(0f, 0.088f, 0.100f), Quaternion.identity, mat);
        AddPrimitive(root, "RearSight",  PrimitiveType.Cube, new Vector3(0.015f, 0.006f, 0.005f), new Vector3(0f, 0.085f, 0.005f), Quaternion.identity, mat);

        return root;
    }

    void AddPrimitive(GameObject parent, string name, PrimitiveType type, Vector3 scale, Vector3 pos, Quaternion rot, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.localScale = scale;
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Destroy(go.GetComponent<Collider>());
    }

    public void UnequipWeapon()
    {
        if (currentWeapon != null)
        {
            currentWeapon.OnUnequip();
            currentWeapon = null;
        }
        if (weaponInstance != null)
        {
            Destroy(weaponInstance);
            weaponInstance = null;
        }
        isAiming = false;
    }

    public Weapon GetCurrentWeapon() => currentWeapon;

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
