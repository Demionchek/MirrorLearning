using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class PlayerScript : NetworkBehaviour {

    public TextMesh playerNameText;
    public GameObject playerNameObj;
    public GameObject[] weaponArray;
    
    public float movementSpeed = 4f;
    public float rotationSpeed = 110f;

    [SyncVar(hook = nameof(OnNameChanged))] 
    public string playerName;
    
    [SyncVar(hook = nameof(OnColorChanged))] 
    public Color playerColor = Color.white;
    
    [SyncVar(hook = nameof(OnWeaponChanged))]
    public int activeWeaponSynced = 1;
    
    private Material playerMaterial;
    private Renderer playerRenderer;
    private SceneScript sceneScript;
    
    private int selectedWeaponLocal = 1;

    [Command]
    public void CmdSendPlayerMessage()
    {
        if (sceneScript) 
            sceneScript.statusText = $"{playerName} says hello {Random.Range(10, 99)}";
    }
    
    [Command]
    public void CmdSetupPlayer(string name, Color color) {
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
    
    public override void OnStartLocalPlayer() {
        sceneScript.playerScript = this;
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0, 0, 0);
        
        playerNameObj.transform.localPosition = new Vector3(0, -1f, 0.6f);
        playerNameObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        string name = "Player" + Random.Range(100, 999);
        Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        CmdSetupPlayer(name, color);
    }
    
    void Awake()
    {
        //allow all players to run this
        sceneScript = GameObject.FindObjectOfType<SceneScript>();
        
        // disable all weapons
        foreach (var item in weaponArray)
            if (item != null)
                item.SetActive(false);
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
            weaponArray[_New].SetActive(true);
    }

    public void OnNameChanged(string oldName, string newName) {
        playerNameText.text = playerName;
    }

    public void OnColorChanged(Color oldColor, Color newColor) {
        playerNameText.color = newColor;
        
        if (playerRenderer == null) playerRenderer = GetComponent<Renderer>();

        if (playerRenderer != null) {
            
            playerMaterial = new Material(playerRenderer.material);
            if (playerMaterial != null) {
                playerMaterial.color = newColor;
                playerRenderer.material = playerMaterial;
            }
        }
    }

    void Update() {
        if (!isLocalPlayer)  {
            // make non-local players run this
            playerNameObj.transform.LookAt(Camera.main.transform);
            return;
        }

        float moveX = Input.GetAxis("Horizontal") * Time.deltaTime * rotationSpeed;
        float moveZ = Input.GetAxis("Vertical") * Time.deltaTime * movementSpeed;

        transform.Rotate(0, moveX, 0);
        transform.Translate(0, 0, moveZ);
        
        if (Input.GetButtonDown("Fire2")) //Fire2 is mouse 2nd click and left alt
        {
            selectedWeaponLocal += 1;

            if (selectedWeaponLocal > weaponArray.Length) 
                selectedWeaponLocal = 1; 

            CmdChangeActiveWeapon(selectedWeaponLocal);
        }
    }
}
