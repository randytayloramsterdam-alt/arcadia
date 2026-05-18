using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public int currentAmmo;
    public int reserveAmmo;

    [HideInInspector] public InventoryItem itemData;

    public Transform muzzlePoint;
    public GameObject muzzleFlashPrefab;

    private bool isADS;
    private bool isReloading;
    private float lastFireTime;
    private AudioSource audioSrc;

    // ADS transition
    private Vector3 hipPos;
    private Vector3 adsPos;
    private float adsSpeed = 10f;

    // Recoil
    private Vector3 recoilOffset;
    private float recoilRecoverySpeed = 8f;

    public bool IsADS => isADS;
    public bool IsReloading => isReloading;
    public bool CanFire => !isReloading && currentAmmo > 0 && Time.time - lastFireTime >= (itemData != null ? itemData.fireRate : 0.15f);

    void Awake()
    {
        audioSrc = GetComponent<AudioSource>();
        if (audioSrc == null)
            audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;
    }

    void Start()
    {
        hipPos = transform.localPosition;
        if (itemData != null)
        {
            currentAmmo = itemData.maxAmmo;
            reserveAmmo = itemData.reserveAmmo;
            adsPos = itemData.adsPosition;
        }
        else
        {
            adsPos = hipPos + new Vector3(0f, -0.18f, 0.35f);
        }
    }

    void Update()
    {
        // Smooth ADS position transition
        Vector3 target = isADS ? adsPos : hipPos;
        transform.localPosition = Vector3.Lerp(transform.localPosition, target + recoilOffset, Time.deltaTime * adsSpeed);

        // Recoil recovery
        recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);

        // ADS toggle input (forwarded from WeaponHolder)
        // WeaponHolder handles input and calls SetADS
    }

    public void SetADS(bool aiming)
    {
        if (isReloading)
            isADS = false;
        else
            isADS = aiming;
    }

    public void Fire(Camera cam)
    {
        if (!CanFire || !isADS) return;

        lastFireTime = Time.time;
        currentAmmo--;

        // Muzzle flash
        if (muzzleFlashPrefab != null && muzzlePoint != null)
        {
            var flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);
            Destroy(flash, 0.05f);
        }

        // Sound
        if (itemData != null && itemData.fireSound != null)
            audioSrc.PlayOneShot(itemData.fireSound);

        // Recoil pushback
        recoilOffset += new Vector3(0f, 0.002f, -0.015f);

        // Hitscan raycast
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            var enemy = hit.collider.GetComponentInParent<ShootableTarget>();
            if (enemy != null)
                enemy.TakeDamage(itemData != null ? itemData.damage : 25f, hit.point, hit.normal);

            // Bullet hole / impact effect
            if (enemy == null)
                SpawnImpact(hit.point, hit.normal);
        }

        // Auto-reload when empty
        if (currentAmmo <= 0 && reserveAmmo > 0)
            StartCoroutine(ReloadRoutine());
    }

    void SpawnImpact(Vector3 point, Vector3 normal)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.position = point + normal * 0.01f;
        go.transform.localScale = Vector3.one * 0.03f;
        go.GetComponent<MeshRenderer>().material.color = new Color(0.2f, 0.2f, 0.2f);
        Destroy(go.GetComponent<Collider>());
        Destroy(go, 3f);
    }

    public void Reload()
    {
        if (isReloading) return;
        if (reserveAmmo <= 0) return;
        if (currentAmmo >= (itemData != null ? itemData.maxAmmo : 7)) return;

        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;
        isADS = false;

        float reloadTime = itemData != null ? itemData.reloadTime : 1.6f;

        // Reload animation: move weapon down
        Vector3 reloadPos = hipPos + new Vector3(0f, -0.15f, 0.05f);
        float elapsed = 0f;
        float moveDownTime = reloadTime * 0.3f;
        float stayTime = reloadTime * 0.4f;
        float moveUpTime = reloadTime * 0.3f;

        while (elapsed < moveDownTime)
        {
            elapsed += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(hipPos, reloadPos, elapsed / moveDownTime);
            yield return null;
        }
        transform.localPosition = reloadPos;

        // Play reload sound mid-way
        if (itemData != null && itemData.reloadSound != null)
            audioSrc.PlayOneShot(itemData.reloadSound);

        yield return new WaitForSeconds(stayTime);

        // Refill ammo
        int need = (itemData != null ? itemData.maxAmmo : 7) - currentAmmo;
        int transfer = Mathf.Min(need, reserveAmmo);
        currentAmmo += transfer;
        reserveAmmo -= transfer;

        elapsed = 0f;
        while (elapsed < moveUpTime)
        {
            elapsed += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(reloadPos, hipPos, elapsed / moveUpTime);
            yield return null;
        }

        isReloading = false;
    }

    // Called when weapon is unequipped
    public void OnUnequip()
    {
        isADS = false;
        StopAllCoroutines();
        isReloading = false;
    }

    // Add ammo from pickup
    public void AddReserveAmmo(int amount)
    {
        reserveAmmo += amount;
    }

    public int GetMaxAmmo() => itemData != null ? itemData.maxAmmo : 7;
}
