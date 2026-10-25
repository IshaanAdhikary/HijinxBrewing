using UnityEngine;

public class SceneClick : MonoBehaviour
{
    void OnMouseDown()
    {
        Debug.Log("Mouse Clicked");
        SceneSwitcher.Instance.NextScene();
    }
}
