using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Journal page after the title page. Shows unlocked tokens and fades in one note at a time.
/// </summary>
public class JournalTokenPage : MonoBehaviour
{
    private const int TokenCount = TokenUnlockManager.TokenCount;

    [Tooltip("Seen, Heard, Done, Told. Each button needs InteractionTokenVisual for the normal and selected sprites.")]
    [SerializeField] private Button[] tokens = new Button[TokenCount];

    [Tooltip("Notes in the same order as Tokens. Each needs a Canvas Group.")]
    [SerializeField] private CanvasGroup[] notes = new CanvasGroup[TokenCount];

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text pageNumberText;

    [SerializeField] private TokenUnlockManager tokenUnlockManager;

    [Min(0f)]
    [SerializeField] private float noteFadeSeconds = 0.25f;

    private readonly UnityAction[] _clicks = new UnityAction[TokenCount];
    private InteractionTokenVisual[] _visuals = new InteractionTokenVisual[TokenCount];
    private CanvasGroup[] _tokenGroups = new CanvasGroup[TokenCount];
    private readonly float[] _noteAlpha = new float[TokenCount];
    private readonly float[] _noteTarget = new float[TokenCount];
    private int _selected = -1;

    private void Awake()
    {
        if (tokenUnlockManager == null)
            tokenUnlockManager = FindFirstObjectByType<TokenUnlockManager>();

        CacheVisuals();
        CacheTokenGroups();
        ClearSelection();
    }

    private void OnEnable()
    {
        WireTokens();
        RefreshUnlocked();
    }

    private void OnDisable()
    {
        UnwireTokens();
        ClearSelection();
    }

    private void Update()
    {
        float step = noteFadeSeconds <= 0f ? 1f : Time.unscaledDeltaTime / noteFadeSeconds;

        for (int i = 0; i < TokenCount; i++)
        {
            if (Mathf.Approximately(_noteAlpha[i], _noteTarget[i]))
                continue;

            _noteAlpha[i] = Mathf.MoveTowards(_noteAlpha[i], _noteTarget[i], step);
            ApplyNoteAlpha(i);
        }
    }

    public void SetTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title;
            return;
        }

        JournalPageView view = GetComponent<JournalPageView>();
        if (view != null)
            view.SetTitle(title);
    }

    public void SetPageNumber(string label)
    {
        if (pageNumberText != null)
            pageNumberText.text = label;
    }

    public void RefreshUnlocked()
    {
        if (tokenUnlockManager == null)
            tokenUnlockManager = FindFirstObjectByType<TokenUnlockManager>();

        for (int i = 0; i < TokenCount; i++)
        {
            bool unlocked = IsUnlocked(i);
            Button button = ButtonAt(i);
            if (button != null)
                button.interactable = unlocked;

            CanvasGroup group = i < _tokenGroups.Length ? _tokenGroups[i] : null;
            if (group != null)
            {
                group.alpha = unlocked ? 1f : 0f;
                group.interactable = unlocked;
                group.blocksRaycasts = unlocked;
            }

            if (!unlocked && _selected == i)
                _selected = -1;
        }

        ApplySelection(true);
    }

    /// <summary>Drops the note immediately. Used when the page is left or the journal opens again.</summary>
    public void ClearSelection()
    {
        _selected = -1;
        ApplySelection(true);
    }

    private void OnTokenClicked(int index)
    {
        if (!IsUnlocked(index))
            return;

        if (_selected == index)
        {
            _selected = -1;
            ApplySelection(false);
            PlayTokenSound(FMODEvents.Instance != null ? FMODEvents.Instance.tokenDeselect : default);
            return;
        }

        _selected = index;
        ApplySelection(false);
        PlayTokenSound(FMODEvents.Instance != null ? FMODEvents.Instance.tokenSelect : default);
    }

    private void ApplySelection(bool instant)
    {
        for (int i = 0; i < TokenCount; i++)
        {
            if (_visuals != null && i < _visuals.Length && _visuals[i] != null)
                _visuals[i].SetSelected(i == _selected);

            _noteTarget[i] = i == _selected ? 1f : 0f;
            if (!instant)
                continue;

            _noteAlpha[i] = _noteTarget[i];
            ApplyNoteAlpha(i);
        }
    }

    private void ApplyNoteAlpha(int index)
    {
        CanvasGroup note = NoteAt(index);
        if (note == null)
            return;

        bool visible = _noteAlpha[index] > 0f;
        note.alpha = _noteAlpha[index];
        note.interactable = false;
        note.blocksRaycasts = visible;

        if (!visible)
            return;

        Graphic[] graphics = note.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = true;
    }

    private void CacheVisuals()
    {
        for (int i = 0; i < TokenCount; i++)
        {
            Button button = ButtonAt(i);
            _visuals[i] = button != null ? button.GetComponentInChildren<InteractionTokenVisual>(true) : null;
        }
    }

    private void CacheTokenGroups()
    {
        for (int i = 0; i < TokenCount; i++)
        {
            Button button = ButtonAt(i);
            if (button == null)
                continue;

            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group == null)
                group = button.gameObject.AddComponent<CanvasGroup>();

            _tokenGroups[i] = group;
        }
    }

    private void WireTokens()
    {
        for (int i = 0; i < TokenCount; i++)
        {
            Button button = ButtonAt(i);
            if (button == null)
                continue;

            int index = i;
            _clicks[i] = () => OnTokenClicked(index);
            button.onClick.AddListener(_clicks[i]);
        }
    }

    private void UnwireTokens()
    {
        for (int i = 0; i < TokenCount; i++)
        {
            Button button = ButtonAt(i);
            if (button == null || _clicks[i] == null)
                continue;

            button.onClick.RemoveListener(_clicks[i]);
            _clicks[i] = null;
        }
    }

    private bool IsUnlocked(int index)
    {
        return tokenUnlockManager != null && tokenUnlockManager.IsTokenUnlocked(index);
    }

    private Button ButtonAt(int index)
    {
        if (tokens == null || index < 0 || index >= tokens.Length)
            return null;

        return tokens[index];
    }

    private CanvasGroup NoteAt(int index)
    {
        if (notes == null || index < 0 || index >= notes.Length)
            return null;

        return notes[index];
    }

    private static void PlayTokenSound(EventReference sound)
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlayOneShot(sound);
    }
}
