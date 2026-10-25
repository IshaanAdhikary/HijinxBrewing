using UnityEngine;
using UnityEngine.InputSystem;

public class RebindHandler : MonoBehaviour
{
    private static InputActionMap paws = GameManager.Instance.playerInput.actions.FindActionMap("Rhythm");

    public void Start()
    {
        string savedRebinds = PlayerPrefs.GetString("CharacterControls", string.Empty);
        if (!string.IsNullOrEmpty(savedRebinds))
        {
            paws.LoadBindingOverridesFromJson(savedRebinds);
        }
    }
    
    // Function to rebind keys
    public void Rebind(string pawName)
    {
        InputAction action;

        switch (pawName)
        {
            case "LeftPaw":
                action = paws.FindAction("LeftPaw");
                break;
            case "RightPaw":
                action = paws.FindAction("RightPaw");
                break;
            default:
                Debug.Log("Attempting to bing nonexistent action");
                return;
        }
        
        // Disable the action map so it doesn't trigger actions while rebinding
        action.Disable();

        // Start the interactive rebind process
        var rebindOperation = action.PerformInteractiveRebinding()
            .WithControlsExcluding("/<keyboard>/escape") 
            .OnMatchWaitForAnother(0.1f) 
            .OnComplete(operation => 
            {
                Debug.Log($"Successfully bound to: {operation.selectedControl}");
                
                // Clean up the memory operation
                operation.Dispose(); 
                
                // Re-enable the action map for gameplay
                action.Enable(); 
                
                PlayerPrefs.SetString("CharacterControls", paws.SaveBindingOverridesAsJson());
                PlayerPrefs.Save();
            })
            .Start();
    }
}
