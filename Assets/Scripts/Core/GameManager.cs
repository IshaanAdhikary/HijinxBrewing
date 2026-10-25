using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [SerializeField] public PlayerInput playerInput;

    public static GameManager Instance { get; private set; }

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
}