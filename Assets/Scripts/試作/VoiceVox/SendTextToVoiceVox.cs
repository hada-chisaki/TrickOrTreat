using UnityEngine;
using VoicevoxBridge;

public class SendTextToVoiceVox : MonoBehaviour
{
    [SerializeField] VOICEVOX voicevox;
    async void Start()
    {
        int speaker = 1; // ずんだもん あまあま
        string text = "ずんだもんなのだ";
        await voicevox.PlayOneShot(speaker, text);
        Debug.Log("ボイス再生終了");
    }
}
