using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class PlayerScript : NetworkBehaviour
{

    public TextMesh playerNameText;
    public GameObject playerNameObj;
    public GameObject[] weaponArray;
    public Renderer playerRenderer;
    public Transform cameraPivot;

    public float movementSpeed = 4f;
    public float rotationSpeed = 110f;

    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;

    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;

    [SyncVar(hook = nameof(OnColorChanged))]
    public Color playerColor = Color.white;

    [SyncVar(hook = nameof(OnWeaponChanged))]
    public int activeWeaponSynced = 1;

    private Material playerMaterial;
    private SceneScript sceneScript;
    private Weapon activeWeapon;
    private CharacterController characterController;

    private float weaponCooldownTime;

    private int selectedWeaponLocal = 1;

    [Command]
    public void CmdSendPlayerMessage()
    {
        if (sceneScript)
            sceneScript.statusText = $"{playerName} says hello {Random.Range(10, 99)}";
    }

    [Command]
    public void CmdSetupPlayer(string name, Color color)
    {
        // player info sent to server, then server updates sync vars which handles it on all clients
        playerName = name;
        playerColor = color;
        sceneScript.statusText = $"{playerName} joined.";
    }

    [Command]
    public void CmdChangeActiveWeapon(int newIndex)
    {
        activeWeaponSynced = newIndex;
    }

    public override void OnStartLocalPlayer()
    {
        sceneScript.playerScript = this;
        Camera.main.transform.SetParent(cameraPivot);
        Camera.main.transform.localPosition = new Vector3(0, 0, -0.5f);

        playerNameObj.transform.localPosition = new Vector3(0, -1f, 0.6f);
        playerNameObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        string name = "Player" + Random.Range(100, 999);
        Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        CmdSetupPlayer(name, color);
    }

    void Awake()
    {
        //allow all players to run this
        sceneScript = GameObject.Find("SceneReference").GetComponent<SceneReference>().sceneScript;

        characterController = GetComponent<CharacterController>();

        if (characterController == null) Debug.LogError("characterController is null!");

        // disable all weapons
        foreach (var item in weaponArray)
            if (item != null)
                item.SetActive(false);

        if (selectedWeaponLocal < weaponArray.Length && weaponArray[selectedWeaponLocal] != null)
        {
            activeWeapon = weaponArray[selectedWeaponLocal].GetComponent<Weapon>();
            sceneScript.UIAmmo(activeWeapon.weaponAmmo);
        }
    }

    void OnWeaponChanged(int _Old, int _New)
    {
        // disable old weapon
        // in range and not null
        if (0 < _Old && _Old < weaponArray.Length && weaponArray[_Old] != null)
            weaponArray[_Old].SetActive(false);

        // enable new weapon
        // in range and not null
        if (0 < _New && _New < weaponArray.Length && weaponArray[_New] != null)
        {

            weaponArray[_New].SetActive(true);
            activeWeapon = weaponArray[activeWeaponSynced].GetComponent<Weapon>();

            if (isLocalPlayer && activeWeapon != null)
                sceneScript.UIAmmo(activeWeapon.weaponAmmo);
        }
    }

    public void OnNameChanged(string oldName, string newName)
    {
        playerNameText.text = playerName;
    }

    public void OnColorChanged(Color oldColor, Color newColor)
    {
        playerNameText.color = newColor;

        if (playerRenderer == null) return;

        playerMaterial = new Material(playerRenderer.material);
        if (playerMaterial != null)
        {
            playerMaterial.color = newColor;
            playerRenderer.material = playerMaterial;
        }

    }

    private void Update()
    {
        if (!isLocalPlayer)
        {
            // make non-local players run this
            playerNameObj.transform.LookAt(Camera.main.transform);
            return;
        }

        Movement();
        AttackInput();
    }

    private void FixedUpdate() {
        if (!isLocalPlayer) return;

        LookInput();
    }

    private void LookInput() {
        float mouseInputX = Input.GetAxis("Mouse X1");
        float mouseInputY = Input.GetAxis("Mouse Y1");

        Vector3 currPivotRotation = cameraPivot.transform.rotation.eulerAngles;

        currPivotRotation.y += mouseInputX;
        currPivotRotation.x += mouseInputY;

        // clamp our rotations so our values are limited 360 degrees
        // mouseInputX = ClampAngle( mouseInputX, float.MinValue, float.MaxValue);
        // mouseInputY = ClampAngle(mouseInputY, BottomClamp, TopClamp);

        cameraPivot.transform.rotation = Quaternion.Euler(currPivotRotation);
    }

    private void AttackInput()
    {
        if (Input.GetButtonDown("Fire2"))
        {
            selectedWeaponLocal += 1;

            if (selectedWeaponLocal > weaponArray.Length)
                selectedWeaponLocal = 1;

            CmdChangeActiveWeapon(selectedWeaponLocal);
        }

        if (Input.GetButtonDown("Fire1"))
        {
            if (activeWeapon && Time.time > weaponCooldownTime && activeWeapon.weaponAmmo > 0)
            {
                weaponCooldownTime = Time.time + activeWeapon.weaponCooldown;
                activeWeapon.weaponAmmo -= 1;
                sceneScript.UIAmmo(activeWeapon.weaponAmmo);
                CmdShootRay();
            }
        }
    }

    private void Movement()
    {
        float moveX = Input.GetAxis("Horizontal") * Time.deltaTime * rotationSpeed;
        float moveZ = Input.GetAxis("Vertical") * Time.deltaTime * movementSpeed;

        Vector3 moveDir = new Vector3(moveX, 0, moveZ);

        characterController.Move(moveDir);
        transform.Rotate(0, moveX, 0);
        transform.Translate(0, 0, moveZ);
    }

    [Command]
    void CmdShootRay()
    {
        RpcFireWeapon();
    }

    [ClientRpc]
    void RpcFireWeapon()
    {
        GameObject bullet = Instantiate(activeWeapon.weaponBullet, activeWeapon.weaponFirePosition.position, activeWeapon.weaponFirePosition.rotation);
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * activeWeapon.weaponSpeed;
        Destroy(bullet, activeWeapon.weaponLife);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}
