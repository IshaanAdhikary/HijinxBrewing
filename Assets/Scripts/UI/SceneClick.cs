using UnityEngine;

public class SceneClick : MonoBehaviour
{
    void OnMouseDown()
    {
        SceneSwitcher.Instance.NextScene();
    }
}
