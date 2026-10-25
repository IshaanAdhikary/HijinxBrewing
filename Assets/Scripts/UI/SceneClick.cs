using UnityEngine;

public class SceneClick : MonoBehaviour
{
    [SerializeField, Tooltip("Optional. Played when clicked.")] AudioClip sound;

    void OnMouseDown()
    {
        SceneSwitcher.Instance.NextScene(sound);
    }
}
