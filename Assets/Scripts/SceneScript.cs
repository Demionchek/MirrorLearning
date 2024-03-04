using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class SceneScript : NetworkBehaviour
{
    public Text canvasStatusText;
    public Text canvasAmmoText;
    public PlayerScript playerScript;
    public SceneReference sceneReference;
    
    [SyncVar(hook = nameof(OnStatusTextChanged))]
    public string statusText;

    private void OnStatusTextChanged(string oldText, string newText)
    {
        //called from sync var hook, to update info on screen for all players
        canvasStatusText.text = statusText;
    }

    public void ButtonSendMessage()
    {
        if (playerScript != null)  
            playerScript.CmdSendPlayerMessage();
    }
    
    public void UIAmmo(int _value)
    {
        canvasAmmoText.text = "Ammo: " + _value;
    }
    
    public void ButtonChangeScene()
    {
        if (isServer)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name == "MyScene")
                NetworkManager.singleton.ServerChangeScene("MyOtherScene");
            else
                NetworkManager.singleton.ServerChangeScene("MyScene");
        }
        else
            Debug.Log("You are not Host.");
    }
}

