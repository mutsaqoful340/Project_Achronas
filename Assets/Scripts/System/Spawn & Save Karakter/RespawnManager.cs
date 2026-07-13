using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class RespawnManager : MonoBehaviour
{
    [Header("Players")]
    public Player_Components player1;
    public Player_Components player2;

    [Header("Respawn Settings")]
    public float respawnDelay = 3f;
    [Tooltip("If true, fully reload the scene from checkpoint. If false, soft-reset enemies in place.")]
    public bool reloadSceneOnRespawn = false;

    [Header("Default Spawn (jika belum ada save)")]
    public Transform defaultSpawnP1;
    public Transform defaultSpawnP2;

    [Header("Reset Managers")]
    public Sys_ObjResetManager[] resetManager;

    [Header("Transition")]
    [Tooltip("Fade to black transition sebelum respawn diproses")]
    public DeathTransition deathTransition;

    [Header("Audio")]
    [SerializeField] private AudioSource deathAudioSource;
    [SerializeField] private AudioClip deathClip;

    [Header("Events")]
    public UnityEvent onRespawn;

    private bool isRespawning = false;

    private void Update()
    {
        if (isRespawning) return;
        if (player1.IsDead || player2.IsDead)
        {
            isRespawning = true;

            if (deathAudioSource != null && deathClip != null)
                deathAudioSource.PlayOneShot(deathClip);

            StartCoroutine(RespawnAll());
        }
    }

    private IEnumerator RespawnAll()
    {
        // Fade ke hitam dulu sebelum proses respawn dimulai
        if (deathTransition != null)
            yield return StartCoroutine(deathTransition.PlayDeathTransition());
        else
            yield return new WaitForSeconds(respawnDelay);

        if (reloadSceneOnRespawn)
        {
            // ═══════════════════════════════════════════════════════════
            // MODE 1: FULL SCENE RELOAD (resets all enemies)
            // ═══════════════════════════════════════════════════════════
            Debug.Log("[RESPAWN] Reloading scene from last checkpoint...");

            // Get last saved slot and room
            if (!PlayerPrefs.HasKey("lastRoomID"))
            {
                Debug.LogWarning("[RESPAWN] No checkpoint found!");
                yield break;
            }

            string lastRoomID = PlayerPrefs.GetString("lastRoomID");

            if (SaveManager.Instance != null)
            {
                string slot = SaveManager.Instance.FindSlotByRoomID(lastRoomID);
                if (slot != null)
                {
                    // Load from checkpoint (resets player positions and room state)
                    SaveManager.Instance.Load(slot, player1.transform, player2.transform);

                    // Reload scene asynchronously and wait for it to complete
                    string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    AsyncOperation sceneLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

                    // Wait for scene to fully load
                    while (!sceneLoad.isDone)
                    {
                        yield return null;
                    }

                    // Scene is now loaded - reassign devices to the new player instances
                    yield return new WaitForEndOfFrame(); // Extra frame to ensure all Start() methods have run

                    Sys_CharacterAssignmentManager assignmentManager = FindAnyObjectByType<Sys_CharacterAssignmentManager>();
                    if (assignmentManager != null)
                    {
                        assignmentManager.AssignCharacters();
                        Debug.Log("[RESPAWN] Device assignments restored after scene reload");
                    }
                    else
                    {
                        Debug.LogWarning("[RESPAWN] CharacterAssignmentManager not found in scene!");
                    }

                    yield break;
                }
            }

            Debug.LogWarning("[RESPAWN] SaveManager not found or checkpoint slot not found!");
        }
        else
        {
            // ═══════════════════════════════════════════════════════════
            // MODE 2: SOFT RESET (respawn in place + reset enemies)
            // ═══════════════════════════════════════════════════════════
            Vector3 spawnP1, spawnP2;

            // Kalau belum ada save, pakai default spawn point
            if (!PlayerPrefs.HasKey("spawnP1") || !PlayerPrefs.HasKey("spawnP2"))
            {
                if (defaultSpawnP1 == null || defaultSpawnP2 == null)
                {
                    Debug.LogWarning("[RESPAWN] Tidak ada data spawn point dan default spawn belum di-set!");
                    isRespawning = false;
                    yield break;
                }

                Debug.Log("[RESPAWN] Pakai default spawn point.");
                spawnP1 = defaultSpawnP1.position;
                spawnP2 = defaultSpawnP2.position;
            }
            else
            {
                spawnP1 = JsonUtility.FromJson<Vector3>(PlayerPrefs.GetString("spawnP1"));
                spawnP2 = JsonUtility.FromJson<Vector3>(PlayerPrefs.GetString("spawnP2"));
                Debug.Log($"[RESPAWN] Loaded spawn positions from PlayerPrefs - P1: {spawnP1}, P2: {spawnP2}");

                // Clamp spawn Y to minimum 0.5 to prevent low spawn points
                if (spawnP1.y < 0.5f)
                {
                    spawnP1.y = 0.5f;
                    Debug.Log($"[RESPAWN] Clamped P1 Y to 0.5 (was too low)");
                }
                if (spawnP2.y < 0.5f)
                {
                    spawnP2.y = 0.5f;
                    Debug.Log($"[RESPAWN] Clamped P2 Y to 0.5 (was too low)");
                }
            }

            Debug.Log($"[RESPAWN] About to warp P1 to Y={spawnP1.y}, P2 to Y={spawnP2.y}");
            WarpPlayer(player1, spawnP1);
            WarpPlayer(player2, spawnP2);

            player1.IsDead = false;
            player1.currentActionState = ActionState.Idle;
            player1.HandleIdle();
            player2.IsDead = false;
            player2.currentActionState = ActionState.Idle;
            player2.HandleIdle();
            player2.ResetAfterRespawn();
            player1.ResetAfterRespawn();

            // Reset all enemies in scene to initial state
            ResetAllEnemies();

            // Reset all camera trigger areas to clear player references and deactivate cameras
            ResetAllTriggerAreas();

            // Switch to Player mode so players can control their characters again
            if (Sys_GameModeSwitch.Instance != null)
            {
                Sys_GameModeSwitch.Instance.SetMode(Sys_GameModeSwitch.GameMode.Player);
                Debug.Log("[RESPAWN] Switched to Player mode - players can control now");
            }
            else
            {
                Debug.LogWarning("[RESPAWN] GameModeSwitch instance not found!");
            }

            isRespawning = false;

            Debug.Log("[RESPAWN] Kedua player respawn!");

            // Fade balik terang setelah semua state selesai di-reset
            if (deathTransition != null)
                yield return StartCoroutine(deathTransition.FadeBackIn());
        }

        for (int i = 0; i < resetManager.Length; i++)
        {
            if (resetManager[i] != null)
            {
                resetManager[i].OnResetObjects();
                Debug.Log($"[RESPAWN] Reset manager {resetManager[i].gameObject.name} triggered");
            }
        }
    }

    private void WarpPlayer(Player_Components player, Vector3 spawnPos)
    {
        // Ensure player is detached from any boss/grab slot before repositioning
        if (player.transform.parent != null)
        {
            player.transform.SetParent(null, true);
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            // Disable CC, set position, then re-enable to clear internal physics state
            cc.enabled = false;
            player.transform.position = spawnPos;
            cc.enabled = true;
            Debug.Log($"[RESPAWN] Warped {player.gameObject.name} to {spawnPos}");
        }
        else
        {
            // No CharacterController, just set position
            player.transform.position = spawnPos;
            Debug.Log($"[RESPAWN] Warped {player.gameObject.name} to {spawnPos} (no CharacterController)");
        }
    }

    /// <summary>
    /// Reset all enemy AI states to initial/idle state (for soft respawn)
    /// </summary>
    private void ResetAllEnemies()
    {
        // Find all bosses in scene
        _Enemy_Boss[] bosses = FindObjectsByType<_Enemy_Boss>();
        foreach (var boss in bosses)
        {
            if (boss != null)
            {
                // Reset boss to idle state
                boss.ResetToInitialState();
                Debug.Log($"[RESPAWN] Reset {boss.gameObject.name} to initial state");
            }
        }

        // Find all mannequins in scene  
        _Enemy_Mannequin[] mannequins = FindObjectsByType<_Enemy_Mannequin>();
        foreach (var mannequin in mannequins)
        {
            if (mannequin != null)
            {
                // Reset mannequin to idle state
                mannequin.ResetToInitialState();
                Debug.Log($"[RESPAWN] Reset {mannequin.gameObject.name} to initial state");
            }
        }
    }

    /// <summary>
    /// Reset all camera trigger areas to clear player references and deactivate cameras
    /// </summary>
    private void ResetAllTriggerAreas()
    {
        _Sys_VCamPriorityTriggerArea[] triggerAreas = FindObjectsByType<_Sys_VCamPriorityTriggerArea>();
        foreach (var triggerArea in triggerAreas)
        {
            if (triggerArea != null)
            {
                triggerArea.ResetTriggerArea();
                Debug.Log($"[RESPAWN] Reset trigger area {triggerArea.gameObject.name}");
            }
        }

        // Refresh all trigger areas to re-detect players now at spawn positions
        foreach (var triggerArea in triggerAreas)
        {
            if (triggerArea != null)
            {
                triggerArea.RefreshTriggerArea();
            }
        }
    }
}