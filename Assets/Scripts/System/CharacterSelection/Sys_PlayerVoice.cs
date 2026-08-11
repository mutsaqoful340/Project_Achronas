using UnityEngine;
using UnityEngine.InputSystem;

public class Sys_PlayerVoice : MonoBehaviour
{
    [Header("Voice Lines per Character")]
    public AudioClip[] leftCharacterVoiceLines;   // dipakai kalau CharacterIndex == 1
    public AudioClip[] rightCharacterVoiceLines;  // dipakai kalau CharacterIndex == 2

    [Header("Audio Sources")]
    public AudioSource player1AudioSource;
    public AudioSource player2AudioSource;

    void Update()
    {
        var session = Sys_PlayerSessionData.Instance;
        if (session == null) return;

        // Player 1
        if (session.player1Device is Gamepad gamepad1)
        {
            if (gamepad1.leftShoulder.wasPressedThisFrame)
            {
                PlayVoice(player1AudioSource, session.player1CharacterIndex, "Player 1");
            }
        }

        // Player 2
        if (session.player2Device is Gamepad gamepad2)
        {
            if (gamepad2.leftShoulder.wasPressedThisFrame)
            {
                PlayVoice(player2AudioSource, session.player2CharacterIndex, "Player 2");
            }
        }
    }

    void PlayVoice(AudioSource source, int characterIndex, string debugLabel)
    {
        if (source == null)
        {
            Debug.LogWarning($"{debugLabel}: AudioSource belum di-assign di Inspector!");
            return;
        }

        AudioClip[] clips = null;

        if (characterIndex == 1) clips = leftCharacterVoiceLines;
        else if (characterIndex == 2) clips = rightCharacterVoiceLines;

        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"{debugLabel}: Tidak ada voice clip untuk characterIndex={characterIndex}");
            return;
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        source.PlayOneShot(clip);
        Debug.Log($"{debugLabel} (character {characterIndex}) memainkan: {clip.name}");
    }
}
