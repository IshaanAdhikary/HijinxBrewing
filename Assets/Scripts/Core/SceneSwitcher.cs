using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{

    public static SceneSwitcher Instance { get; private set; }

    Image fadeImage;
    private int nextScene = 1;

    private void Awake()
    {
        // Check if an instance already exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instance
            return;
        }

        // Set the active instance
        Instance = this;

        // Keep this object alive across scene changes
        DontDestroyOnLoad(gameObject);
    }

    public void ToTitle()
    {
        SceneManager.LoadScene("Title");
        nextScene = 1;
    }

    

    public void NextScene()
    {

        switch (nextScene)
        {
            case 0:
                SceneManager.LoadScene("Title");
                nextScene++;
                break;
            case 1:
                SceneManager.LoadScene("Crushing");
                nextScene++;
                break;
            case 2:
                SceneManager.LoadScene("Mixing");
                nextScene++;
                break;
            case 3:
                SceneManager.LoadScene("Credits");
                nextScene = 0;
                break;
            default:
                Debug.Log("Tried to go to invalid scene");
                break;
        }
    }

}
