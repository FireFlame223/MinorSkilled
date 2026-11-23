using FMODUnity;
using UnityEngine;

public enum CharacterVoiceId
{
    None,
    Osiris,
    Horus,
    Seth,
    Thoth,
    Anubis,
}

/// <summary>
/// One character's voice blips and speaking rhythm. The event is a multi instrument; FMOD picks each blip.
/// </summary>
[System.Serializable]
public struct CharacterVoice
{
    [Tooltip("Short voice blips. Leave this empty for a silent character.")]
    public EventReference Event;

    [Tooltip("Visible letters between blips. 2 plays a blip on every second letter.")]
    [Min(1)]
    public int lettersPerBleep;

    [Tooltip("Minimum seconds between blips. 0 uses the letter count only. A blip keeps playing if the next one starts.")]
    [Min(0f)]
    public float minimumBleepInterval;

    [Tooltip("Seconds between revealed characters. Lower is a faster talker.")]
    [Min(0.001f)]
    public float secondsPerCharacter;

    public static CharacterVoice WithDefaults(CharacterVoice voice)
    {
        if (voice.lettersPerBleep < 1)
        {
            voice.lettersPerBleep = 2;
            voice.minimumBleepInterval = 0.06f;
        }

        if (voice.secondsPerCharacter < 0.001f)
            voice.secondsPerCharacter = 0.04f;

        if (voice.minimumBleepInterval < 0f)
            voice.minimumBleepInterval = 0f;

        return voice;
    }
}

/// <summary>
/// This class holds all FMOD eventreferences to easily play sounds from anywhere using the AudioManager
/// Is a singleton so getting a reference is easy from anywhere in the project
/// </summary>
public class FMODEvents : MonoBehaviour
{
    public static FMODEvents Instance;

    [field: Header("Voices")]
    [field: Tooltip("Voice, blip rate, and typing speed. Assign this character on a point of interest.")]
    [field: SerializeField] public CharacterVoice Osiris { get; private set; }

    [field: Tooltip("Voice, blip rate, and typing speed. Assign this character on a point of interest.")]
    [field: SerializeField] public CharacterVoice Horus { get; private set; }

    [field: Tooltip("Voice, blip rate, and typing speed. Assign this character on a point of interest.")]
    [field: SerializeField] public CharacterVoice Seth { get; private set; }

    [field: Tooltip("Voice, blip rate, and typing speed. Assign this character on a point of interest.")]
    [field: SerializeField] public CharacterVoice Thoth { get; private set; }

    [field: Tooltip("Voice, blip rate, and typing speed. Assign this character on a point of interest.")]
    [field: SerializeField] public CharacterVoice Anubis { get; private set; }

    [field: Tooltip("Voice, blip rate, and typing speed for every location clue. Points of interest cannot use this voice.")]
    [field: SerializeField] public CharacterVoice Detective { get; private set; }

    [field: Header("SFX")]
    [field: Tooltip("One shot played when the journal opens.")]
    [field: SerializeField] public EventReference journalOpen { get; private set; }

    [field: Tooltip("One shot played when the journal closes.")]
    [field: SerializeField] public EventReference journalClose { get; private set; }

    [field: Tooltip("One shot played each time the journal changes page.")]
    [field: SerializeField] public EventReference journalPageFlip { get; private set; }

    [field: Tooltip("One shot played when a new line is written into the journal.")]
    [field: SerializeField] public EventReference journalEntry { get; private set; }

    [field: Tooltip("One shot played when the deduction panel opens.")]
    [field: SerializeField] public EventReference deductionOpen { get; private set; }

    [field: Tooltip("One shot played when the deduction panel closes.")]
    [field: SerializeField] public EventReference deductionClose { get; private set; }

    [field: Tooltip("One shot played when the pause menu opens or closes.")]
    [field: SerializeField] public EventReference pauseMenu { get; private set; }

    [field: Tooltip("One shot played when the player uses a door named Enter.")]
    [field: SerializeField] public EventReference locationEnter { get; private set; }

    [field: Tooltip("One shot played when the player uses a door named Exit.")]
    [field: SerializeField] public EventReference locationExit { get; private set; }

    [field: Tooltip("One shot played when a token is selected.")]
    [field: SerializeField] public EventReference tokenSelect { get; private set; }

    [field: Tooltip("One shot played when a token is deselected.")]
    [field: SerializeField] public EventReference tokenDeselect { get; private set; }

    [field: Tooltip("One shot played when the token selection is confirmed.")]
    [field: SerializeField] public EventReference tokenConfirm { get; private set; }

    [field: Tooltip("One shot played when a solved deduction awards a new token.")]
    [field: SerializeField] public EventReference tokenNotification { get; private set; }

    [field: Header("Music")]
    [field: Tooltip("Plays on the Main Menu scene.")]
    [field: SerializeField] public EventReference mainMenuTheme { get; private set; }

    [field: Tooltip("Plays on the Tutorial and Level scenes.")]
    [field: SerializeField] public EventReference gameTheme { get; private set; }

    [field: Tooltip("Plays from the start of the credits until the scene changes.")]
    [field: SerializeField] public EventReference creditsTheme { get; private set; }

    public CharacterVoice GetCharacterVoice(CharacterVoiceId id)
    {
        switch (id)
        {
            case CharacterVoiceId.Osiris: return CharacterVoice.WithDefaults(Osiris);
            case CharacterVoiceId.Horus: return CharacterVoice.WithDefaults(Horus);
            case CharacterVoiceId.Seth: return CharacterVoice.WithDefaults(Seth);
            case CharacterVoiceId.Thoth: return CharacterVoice.WithDefaults(Thoth);
            case CharacterVoiceId.Anubis: return CharacterVoice.WithDefaults(Anubis);
            default: return default;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return;} // Singleton
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnValidate()
    {
        Osiris = CharacterVoice.WithDefaults(Osiris);
        Horus = CharacterVoice.WithDefaults(Horus);
        Seth = CharacterVoice.WithDefaults(Seth);
        Thoth = CharacterVoice.WithDefaults(Thoth);
        Anubis = CharacterVoice.WithDefaults(Anubis);
        Detective = CharacterVoice.WithDefaults(Detective);
    }
}
