using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class PlayerScript : NetworkBehaviour
{

    [Header("References")]
    public TextMesh playerNameText;
    public GameObject playerNameObj;
    public GameObject[] weaponArray;
    public Renderer playerRenderer;
    public Transform cameraPivot;

    [Space(10)]

    [Header("Controlls")]
    public float movementSpeed = 4f;
    public float rotationSpeedY = 10f;
    public float rotationSpeedX = 10f;
    public LayerMask groundLayers;


    [HideInInspector, SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;

    [HideInInspector, SyncVar(hook = nameof(OnColorChanged))]
    public Color playerColor = Color.white;

    [HideInInspector, SyncVar(hook = nameof(OnWeaponChanged))]
    public int activeWeaponSynced = 1;

    private Material playerMaterial;
    private SceneScript sceneScript;
    private Weapon activeWeapon;
    private CharacterController characterController;
    private NetworkAnimator networkAnimator;
    private Vector3 verticalVelocity;

    private float weaponCooldownTime;
    public float groundedOffset = -0.14f;

    private int selectedWeaponLocal = 0;
    private bool isGrounded;
    private float gravityModyfier = 2;
    private float terminalVelocity = -50f;
    private float mouseInputX = 0.0f;
    private float mouseInputY = 0.0f;


    [Command]
    public void CmdSendPlayerMessage()
    {
        if (sceneScript)
            sceneScript.statusText = $"{playerName} says hello {UnityEngine.Random.Range(10, 99)}";
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

        characterController = GetComponent<CharacterController>();
        networkAnimator = GetComponent<NetworkAnimator>();

        if (characterController == null) Debug.LogError("characterController is null!");

        playerNameObj.transform.localPosition = new Vector3(0, -1f, 0.6f);
        playerNameObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        string name = "Player" + UnityEngine.Random.Range(100, 999);
        Color color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
        CmdSetupPlayer(name, color);
    }

    void Awake()
    {
        //allow all players to run this
        sceneScript = GameObject.Find("SceneReference").GetComponent<SceneReference>().sceneScript;

        gameObject.name = "Player" + UnityEngine.Random.Range(0,100);

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

    void OnWeaponChanged(int oldIndex, int newIndex)
    {
        if (0 < oldIndex && oldIndex < weaponArray.Length && weaponArray[oldIndex] != null)
            weaponArray[oldIndex].SetActive(false);


        if (0 < newIndex && newIndex < weaponArray.Length && weaponArray[newIndex] != null)
        {
            weaponArray[newIndex].SetActive(true);
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

        LookInput();
        GroundedCheck();
        Movement();
        AttackInput();
    }

    private void FixedUpdate() {
        if (!isLocalPlayer) return;
        
    }

    private void LookInput() {
        mouseInputX += Input.GetAxis("Mouse X1") * rotationSpeedX;
        mouseInputY -= Input.GetAxis("Mouse Y1") * rotationSpeedY;

        transform.rotation = Quaternion.Euler(0, mouseInputX, 0);

        mouseInputY = Math.Clamp(mouseInputY, -60, 40);

        cameraPivot.transform.eulerAngles = new Vector3(mouseInputY, mouseInputX,0);
    }

    private void AttackInput()
    {
        if (Input.GetButtonDown("Fire2"))
        {
            selectedWeaponLocal++;

            if (selectedWeaponLocal > weaponArray.Length - 1)
                selectedWeaponLocal = 0;

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
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");

        if (networkAnimator != null) {
            networkAnimator.animator.SetFloat("InputFwd", inputZ);
            networkAnimator.animator.SetFloat("InputRight", inputX);
        }

        inputX *= Time.deltaTime * movementSpeed;
        inputZ *= Time.deltaTime * movementSpeed;

        if (isGrounded)
        {
            verticalVelocity = new Vector3(0,-2f,0);
        }
        else
        {
            if (verticalVelocity.y > terminalVelocity)
            {
                verticalVelocity += Physics.gravity * gravityModyfier * Time.deltaTime;
            }
        }

        Vector3 moveDir = Vector3.ClampMagnitude(transform.forward * inputZ + cameraPivot.transform.right * inputX, movementSpeed);

        moveDir += verticalVelocity;

        if (characterController != null)
        {
            characterController.Move(moveDir);
        }
    }

    private void GroundedCheck()
    {
        float groundedRadius = characterController.radius - 0.1f;
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
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
