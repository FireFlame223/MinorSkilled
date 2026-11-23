using UnityEngine;
using FMODUnity;

public class PlayOneShotAudio : MonoBehaviour
{
    [SerializeField] private EventReference soundToPlay;

    public void PlaySound()
    {
        AudioManager.Instance.PlayOneShot(soundToPlay, this.transform.position);
    }
}
