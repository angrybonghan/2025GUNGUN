using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public interface IDamageable
{
    void TakeDamage(float amount);
}

public class RaycastWeapon : MonoBehaviour
{

    [Header("����")]
    public PlayerCoverController player;
    public Camera shootCam;
    public Image crosshair;

    [Header("ź/�߻�")]
    public int magSize = 12;
    public float reloadTime = 1.6f;
    public float damage = 25f;
    public float fireRate = 8f;
    public float range = 200f;
    public LayerMask hitMask = ~0; // �⺻: ���� ����(�ʿ�� Player ���̾� ����)

    float nextFireTime;
    int ammoInMag;
    bool reloading;

    void Awake()
    {
        if (!shootCam) shootCam = Camera.main;
        ammoInMag = magSize;
        if (crosshair) crosshair.enabled = false;

        // (����) Player ���̾� �ڵ� ���� ����:
        // int playerLayer = LayerMask.NameToLayer("Player");
        // if (playerLayer >= 0) hitMask &= ~(1 << playerLayer);
    }

    void Update()
    {
        bool aiming = player && player.IsAiming();

        if (crosshair)
            crosshair.enabled = aiming;

        if (reloading) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(ReloadCR());
            return;
        }

        if (aiming && Input.GetMouseButtonDown(0))
            TryFireOnce();
    }

    void TryFireOnce()
    {
        if (Time.time < nextFireTime) return;
        if (ammoInMag <= 0)
        {
            StartCoroutine(ReloadCR());
            return;
        }

        ammoInMag--;
        nextFireTime = Time.time + (1f / fireRate);

        // ��� ����: PlayerCoverController�� ��ġ/�߿츦 �ݿ�
        Vector3 origin = player ? player.GetShootOrigin() : shootCam.transform.position;
        Quaternion rot = player ? player.GetShootRotation() : shootCam.transform.rotation;
        Vector3 dir = rot * Vector3.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            var dmg = hit.collider.GetComponentInParent<IDamageable>() ?? hit.collider.GetComponent<IDamageable>();
            if (dmg != null) dmg.TakeDamage(damage);
            Debug.DrawLine(origin, hit.point, Color.red, 0.25f);
        }
        else
        {
            Debug.DrawRay(origin, dir * range, Color.gray, 0.25f);
        }
    }

    System.Collections.IEnumerator ReloadCR()
    {
        reloading = true;
        yield return new WaitForSeconds(reloadTime);
        ammoInMag = magSize;
        reloading = false;
    }
}
