using UnityEngine;
using UnityEngine.UI;

/// <summary>Goes back to the title screen when this button is clicked.</summary>
[RequireComponent(typeof(Button))]
public class TitleButton : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => SceneSwitcher.Instance.ToTitle());
    }
}
