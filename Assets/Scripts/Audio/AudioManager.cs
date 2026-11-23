using UnityEngine;
using FMOD.Studio;
using FMODUnity;
using UnityEngine.SceneManagement;

/// <summary>
/// Plays one-shots and the scene music, and applies the bus volumes.
/// Lives across scene loads.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private enum Theme
    {
        None,
        MainMenu,
        Game,
        Credits
    }

    private EventInstance musicInstance;
    private Theme currentTheme;
    private bool warnedMissingTheme;
    private bool gameThemeQuieted;
    private float quietGameThemeVolume = 1f;

    [Range(0,1)] public float masterVolume = 1;
    [Range(0,1)] public float musicVolume = 1;
    [Range(0,1)] public float sfxVolume = 1;
    [Range(0,1)] public float ambienceVolume = 1;
    
    private Bus masterBus;
    private Bus musicBus;
    private Bus sfxBus;
    private Bus ambienceBus;
    private bool hasMasterBus;
    private bool hasMusicBus;
    private bool hasSfxBus;
    private bool hasAmbienceBus;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return;} // Singleton
        Instance = this;
        DontDestroyOnLoad(gameObject);

        hasMasterBus = TryGetBus("bus:/", out masterBus);
        hasMusicBus = TryGetBus("bus:/Music", out musicBus);
        hasSfxBus = TryGetBus("bus:/SFX", out sfxBus);
        hasAmbienceBus = TryGetBus("bus:/Ambience", out ambienceBus);
    }
    
    private void OnEnable()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded += OnSceneLoad;
    }

    private void Start()
    {
        if (Instance != this)
            return;

        PlayThemeForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoad;

        if (Instance == this)
            Instance = null;

        ReleaseTheme(ref musicInstance);
    }
    
    private void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        PlayThemeForScene(scene.name);
    }
    
    private void Update()
    {
        if (hasMasterBus)
            masterBus.setVolume(masterVolume);
        if (hasMusicBus)
            musicBus.setVolume(musicVolume);
        if (hasSfxBus)
            sfxBus.setVolume(sfxVolume);
        if (hasAmbienceBus)
            ambienceBus.setVolume(ambienceVolume);
    }

    public void PlayOneShot(EventReference sound, Vector3 position)
    {
        if (sound.IsNull)
            return;

        RuntimeManager.PlayOneShot(sound, position);
    }

    public void PlayOneShot(EventReference sound)
    {
        if (sound.IsNull)
            return;

        RuntimeManager.PlayOneShot(sound);
    }

    public void PlayCreditsTheme()
    {
        CrossfadeTo(Theme.Credits);
    }

    public void SetGameThemeQuieted(bool quiet, float volume)
    {
        gameThemeQuieted = quiet;
        quietGameThemeVolume = Mathf.Clamp01(volume);
        ApplyGameThemeVolume();
    }

    private void PlayThemeForScene(string sceneName)
    {
        Theme theme = ThemeForScene(sceneName);
        if (theme == Theme.None)
        {
            if (musicInstance.isValid())
                CrossfadeTo(Theme.None);
            return;
        }

        if (theme == currentTheme && musicInstance.isValid())
            return;

        CrossfadeTo(theme);
    }

    private static Theme ThemeForScene(string sceneName)
    {
        if (sceneName == "MainMenu")
            return Theme.MainMenu;
        if (sceneName == "Tutorial" || sceneName == "Level")
            return Theme.Game;
        return Theme.None;
    }

    private void CrossfadeTo(Theme theme)
    {
        if (musicInstance.isValid())
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            musicInstance.release();
            musicInstance.clearHandle();
        }

        currentTheme = theme;
        if (!TryGetThemeEvent(theme, out EventReference themeEvent))
            return;

        musicInstance = RuntimeManager.CreateInstance(themeEvent);
        if (!musicInstance.isValid())
            return;

        ApplyGameThemeVolume();
        musicInstance.start();
    }

    private void ApplyGameThemeVolume()
    {
        if (currentTheme != Theme.Game || !musicInstance.isValid())
            return;

        musicInstance.setVolume(gameThemeQuieted ? quietGameThemeVolume : 1f);
    }

    private bool TryGetThemeEvent(Theme theme, out EventReference themeEvent)
    {
        themeEvent = default;
        if (FMODEvents.Instance == null)
            return false;

        switch (theme)
        {
            case Theme.MainMenu:
                themeEvent = FMODEvents.Instance.mainMenuTheme;
                break;
            case Theme.Game:
                themeEvent = FMODEvents.Instance.gameTheme;
                break;
            case Theme.Credits:
                themeEvent = FMODEvents.Instance.creditsTheme;
                break;
            default:
                return false;
        }

        if (!themeEvent.IsNull)
            return true;

        if (!warnedMissingTheme)
        {
            warnedMissingTheme = true;
            Debug.LogWarning("AudioManager: assign the Main Menu, Game, and Credits themes on FMOD Events.", this);
        }

        return false;
    }

    private static bool TryGetBus(string path, out Bus bus)
    {
        bus = default;
        if (!RuntimeManager.IsInitialized)
            return false;

        return RuntimeManager.StudioSystem.getBus(path, out bus) == FMOD.RESULT.OK && bus.isValid();
    }

    private static void ReleaseTheme(ref EventInstance instance)
    {
        if (instance.isValid())
        {
            instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            instance.release();
        }

        instance.clearHandle();
    }
}
