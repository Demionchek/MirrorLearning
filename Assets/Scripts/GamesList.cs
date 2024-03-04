using UnityEngine;
using UnityEngine.SceneManagement;

public class GamesList : MonoBehaviour
{
    public void LoadScene()
    {
        SceneManager.LoadScene("Menu");
    }
}
