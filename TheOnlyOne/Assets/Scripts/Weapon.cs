using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    //atributos propios del arma
    public WeaponBlueprint ws;

    //atributos comunes de todas las armas
    public Camera weaponCam;
    public Camera playerCam;
    public PlayerMove playerMove;
    public AudioSource audioSource;
    private Recoil recoilScript;
    private VisualRecoil viusalRecoilScript;
    private WeaponChanger weaponChanger;

    private float nextFire = 0f;
    private float reloadTimer = 0f;
    public bool isReloading = false;
    public bool isAming;
    Transform anchor;
    Transform hipState;
    Transform aimState;
    public Transform prefabContainer;
    GameObject prefab;
    public ParticleSystem muzzleFlash;
    void Start()
    {
        ws.currentAmmo = ws.maxClipAmmo;
        ws.totalAmmo = 90;
          
        weaponChanger = FindObjectOfType<WeaponChanger>();
        recoilScript = FindObjectOfType<Recoil>();
        playerMove = FindObjectOfType<PlayerMove>();
        viusalRecoilScript = GetComponent<VisualRecoil>();
        anchor = transform.Find("Anchor");
        hipState = transform.Find("States/Hip");
        aimState = transform.Find("States/Aim");
        prefabContainer = transform.Find("Anchor/Design");
        //Equip
        prefab = Instantiate(ws.prefab, prefabContainer.position, prefabContainer.rotation, prefabContainer);
        muzzleFlash = prefab.GetComponentInChildren<ParticleSystem>();
    }
    //condiciones para poder disparar
    private bool CanShoot()
    {
        return !weaponChanger.IsChanging() && !isReloading && nextFire>ws.fireRate && ws.currentAmmo>0;
    }
    // Update is called once per frame
    void Update()
    {
        nextFire += Time.deltaTime;

        Sway();
        ListenReloadTime();
        ListenAimInput();
        ListenShootInput();

    }

    public void ListenReloadTime()
    {
        if ((Input.GetKeyDown(KeyCode.R) || ws.currentAmmo <= 0) && ws.currentAmmo < ws.maxClipAmmo && ws.totalAmmo > 0 && !isReloading)
        {
            Reload();
        }
        if (isReloading)
        {
            if (reloadTimer <= ws.reloadTime)
            {
                reloadTimer += Time.deltaTime;
            }
            else
            {
                isReloading = false;
                reloadTimer = 0;
            }
        }
    }
    public void Reload()
    {
        isReloading = true;
        if (ws.anim != null) ws.anim.SetTrigger("Reload");
        audioSource.PlayOneShot(ws.reloadSound, 0.2f);
        if (ws.totalAmmo + ws.currentAmmo < ws.maxClipAmmo)
        {
            ws.currentAmmo += ws.totalAmmo;
            ws.totalAmmo = 0;
        }
        else
        {
            ws.totalAmmo -= ws.maxClipAmmo - ws.currentAmmo;
            ws.currentAmmo = ws.maxClipAmmo;
        }
    }
    private void ListenAimInput()
    {
        isAming = Input.GetMouseButton(1);
        Aim(isAming);
    }
    private void ListenShootInput()
    {

        if (ws.autoShoot)
        {
            if (Input.GetMouseButton(0) && CanShoot())//Time.time >= nextFire)
            {
               // nextFire = Time.time + ws.fireRate;
                Shoot();
            }
        }
        else
        if (Input.GetMouseButtonDown(0) && CanShoot()) //Time.time >= nextFire)
        {
            //nextFire = Time.time + ws.fireRate;
            Shoot();
        }
    }
    private void Shoot()
    {

        if (ws.anim != null) ws.anim.SetTrigger("Shoot");
        audioSource.pitch = Random.Range(ws.pitch - ws.pitchRand, ws.pitch + ws.pitchRand);
        audioSource.PlayOneShot(ws.shootSound, 0.2f);
        muzzleFlash.Play();

        //recoil
        if (isAming)
        {
            recoilScript.RecoilFire(ws.aimRecoilRotation);
            viusalRecoilScript.VisualRecoilFire(ws.vRecoilRotationAim, ws.vRecoilKickBackAim);
        }
        else
        {
            recoilScript.RecoilFire(ws.recoilRotation);
            viusalRecoilScript.VisualRecoilFire(ws.vRecoilRotation, ws.vRecoilKickBack);
        }


        RaycastHit hit;
        if (Physics.Raycast(weaponCam.transform.position, weaponCam.transform.forward, out hit, ws.range))
        {
            GameObject decal = Instantiate(ws.bulletDecal, hit.point + (hit.normal * 0.025f), Quaternion.FromToRotation(Vector3.up, hit.normal)) as GameObject;//se instancia el decal
                                                                                                                                                               //Se rota el decal para adaptarse a la superficie
            decal.transform.parent = hit.transform;//el decal se "pega" al objeto con el que impacte
            Destroy(decal, 10f);//Se destruye el decal a los 10 segundos
        }
        ws.currentAmmo--;
        nextFire = 0;
        if (hit.transform != null)
            if (hit.transform.gameObject.tag == "Target")
            {
                Destroy(hit.transform.gameObject);
                FindObjectOfType<GameManager>().remainingTargets--;
            }

    }
    private void Aim(bool aiming)
    {

        if (aiming)
        {

            anchor.position = Vector3.Lerp(anchor.position, aimState.position, Time.deltaTime * ws.aimSpeed);
            playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, ws.aimFOV, ws.aimSpeed * Time.deltaTime);
        }
        else
        {

            anchor.position = Vector3.Lerp(anchor.position, hipState.position, Time.deltaTime * ws.aimSpeed);
            playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, ws.mainFOV, ws.aimSpeed * Time.deltaTime);

        }
    }
    public void Enable(bool enable)
    {
        this.gameObject.SetActive(enable);
    }

    public void Sway()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Quaternion xSway = Quaternion.AngleAxis(ws.swayIntensity * -mouseX, Vector3.up);//horizontal sway
        Quaternion ySway = Quaternion.AngleAxis(ws.swayIntensity * mouseY, Vector3.right);//vertical sway
        Quaternion target_rotation = xSway * ySway;

        transform.localRotation = Quaternion.Slerp(transform.localRotation, target_rotation, Time.deltaTime * ws.swaySpeed);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        //ver la direccion de las balas en el editor
        Debug.DrawRay(weaponCam.transform.position, weaponCam.transform.forward);
    }
}