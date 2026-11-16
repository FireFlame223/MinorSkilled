using UnityEngine;
using FMODUnity;

public class PlayOneShotAudio : MonoBehaviour
{
    [SerializeField] private EventReference soundToPlay;

    public void PlaySound()
    {
        AudioManager.instance.PlayOneShot(soundToPlay, this.transform.position);
    }
}
