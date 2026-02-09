using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton that stores player session data (gamepad assignments, character selections)
/// Persists across scenes via DontDestroyOnLoad
/// Auto-cleared when game closes
/// </summary>
public class PlayerSessionData : MonoBehaviour
{
    public static PlayerSessionData Instance { get; private set; }

    [Header("Gamepad Assignments")]
    public InputDevice player1Device;
    public InputDevice player2Device;

    [Header("Character Selections")]
    public int player1CharacterIndex = 0; // 0=None, 1=Left, 2=Right
    public int player2CharacterIndex = 0;

    [Header("Login Status")]
    public bool player1LoggedIn = false;
    public bool player2LoggedIn = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("<color=cyan>PlayerSessionData Instance created</color>");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Save all player data (call after character selection is confirmed)
    /// </summary>
    public void SavePlayerData(InputDevice p1Device, InputDevice p2Device, int p1CharIndex, int p2CharIndex)
    {
        player1Device = p1Device;
        player2Device = p2Device;
        player1CharacterIndex = p1CharIndex;
        player2CharacterIndex = p2CharIndex;
        player1LoggedIn = (p1Device != null);
        player2LoggedIn = (p2Device != null);

        Debug.Log($"<color=green>PlayerSessionData saved:</color>\n" +
                  $"P1: Device={p1Device?.deviceId}, Character={GetCharacterName(p1CharIndex)}\n" +
                  $"P2: Device={p2Device?.deviceId}, Character={GetCharacterName(p2CharIndex)}");
    }

    /// <summary>
    /// Check if session data is valid
    /// </summary>
    public bool IsValid()
    {
        bool valid = player1Device != null && player2Device != null &&
                     player1CharacterIndex > 0 && player2CharacterIndex > 0;
        
        if (!valid)
        {
            Debug.LogWarning($"<color=orange>PlayerSessionData incomplete: P1Device={player1Device!=null}, P2Device={player2Device!=null}, P1Char={player1CharacterIndex}, P2Char={player2CharacterIndex}</color>");
        }
        
        return valid;
    }

    /// <summary>
    /// Clear all session data
    /// </summary>
    public void ClearData()
    {
        player1Device = null;
        player2Device = null;
        player1CharacterIndex = 0;
        player2CharacterIndex = 0;
        player1LoggedIn = false;
        player2LoggedIn = false;
        Debug.Log("<color=yellow>PlayerSessionData cleared</color>");
    }

    private string GetCharacterName(int index)
    {
        switch (index)
        {
            case 0: return "None";
            case 1: return "Left";
            case 2: return "Right";
            default: return "Unknown";
        }
    }
}
