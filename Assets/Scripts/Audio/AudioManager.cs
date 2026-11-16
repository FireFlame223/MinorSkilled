using System;
using System.Collections.Generic;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;
using UnityEngine.SceneManagement;
using Random = System.Random;

/// <summary>
/// This class manages all FMOD audio event instances, emitters and oneshots.
/// Makes sure all audio is instantiated and cleaned up propperly
/// also takes care of audio volume
/// is a singleton so referencing from anywhere in the project is easy
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    public static AudioManager instance => Instance;

    private List<EventInstance> eventInstances;
    private List<StudioEventEmitter> eventEmitters;
    
    private EventInstance musicInstance;
    
    [Range(0,1)] public float masterVolume = 1;
    [Range(0,1)] public float musicVolume = 1;
    [Range(0,1)] public float sfxVolume = 1;
    [Range(0,1)] public float ambienceVolume = 1;
    
    private Bus masterBus;
    private Bus musicBus;
    private Bus sfxBus;
    private Bus ambienceBus;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return;} // Singleton
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        eventInstances = new List<EventInstance>();
        eventEmitters = new List<StudioEventEmitter>();
        
        masterBus = RuntimeManager.GetBus("bus:/");
        musicBus = RuntimeManager.GetBus("bus:/Music");
        sfxBus = RuntimeManager.GetBus("bus:/SFX");
        ambienceBus = RuntimeManager.GetBus("bus:/Ambience");
    }
    
    private void OnEnable()
    {
        SceneManager.sceneUnloaded += OnSceneUnLoaded;
        SceneManager.sceneLoaded += OnSceneLoad;
    }
    
    private void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        
    }
    
    private void OnSceneUnLoaded(Scene scene)
    {
        CleanUp();
    }
    
    private void Update()
    {
        masterBus.setVolume(masterVolume);
        musicBus.setVolume(musicVolume);
        sfxBus.setVolume(sfxVolume);
        ambienceBus.setVolume(ambienceVolume);
    }

    public void PlayOneShot(EventReference sound, Vector3 position)
    {
        RuntimeManager.PlayOneShot(sound, position);
        //eventInstance.getPitch(out float pitch);
        //eventInstance.setPitch(pitch + UnityEngine.Random.Range(-1.0f, 1.0f));
    }
    public EventInstance CreateInstance(EventReference eventReference)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        eventInstances.Add(eventInstance);
        return eventInstance;
    }

    public StudioEventEmitter InitializeEventEmitter(EventReference eventReference, GameObject emitterObject)
    {
        StudioEventEmitter emitter = emitterObject.GetComponent<StudioEventEmitter>();
        emitter.EventReference = eventReference;
        eventEmitters.Add(emitter);
        return emitter;
    }

    private void CleanUp()
    {
        foreach (EventInstance eventInstance in eventInstances)
        {
            eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            eventInstance.release();
        }

        foreach (StudioEventEmitter emitter in eventEmitters)
        {
            emitter.Stop();
        }
    }

    private void PauseSounds()
    {
        foreach (EventInstance eventInstance in eventInstances)
        {
            eventInstance.setPaused(true);
        }
    }
    private void UnPauseSounds()
    {
        foreach (EventInstance eventInstance in eventInstances)
        {
            eventInstance.setPaused(false);
        }
    }
}
