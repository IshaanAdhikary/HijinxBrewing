using UnityEngine;
using UnityEngine.UI;

/// <summary>Goes to the next scene in the flow when this button is clicked.</summary>
[RequireComponent(typeof(Button))]
public class NextSceneButton : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => SceneSwitcher.Instance.NextScene());
    }
}
