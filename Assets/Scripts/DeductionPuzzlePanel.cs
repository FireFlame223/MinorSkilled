using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// One blank in a deduction sentence. Options are this gap's dropdown list.
/// </summary>
[Serializable]
public class DeductionGap
{
    [Tooltip("Words offered in this gap.")]
    public string[] options = Array.Empty<string>();

    [Tooltip("Index of the correct word in Options. The first word is 0.")]
    public int correctOption;
}

/// <summary>
/// A sentence with gaps. Fixed Parts has one more entry than Gaps: text before the first gap, between gaps, and after the last gap.
/// </summary>
[Serializable]
public class DeductionPuzzle
{
    [Tooltip("Fixed words around the gaps. One more entry than gaps.")]
    public string[] fixedParts = Array.Empty<string>();

    public DeductionGap[] gaps = Array.Empty<DeductionGap>();

    [Tooltip("Token awarded when Confirm is correct. 1 Seen, 2 Heard, 3 Done, 4 Told.")]
    [Range(1, 4)]
    public int rewardToken = 1;
}

/// <summary>
/// Space slides the hypothesis panel up from the bottom of the screen. Confirm checks the dropdowns.
/// A correct answer awards that puzzle's token and shows the next puzzle. The last puzzle starts the ending.
/// </summary>
[DefaultExecutionOrder(-100)]
public class DeductionPuzzlePanel : MonoBehaviour
{
    [Header("Puzzles")]
    [Tooltip("Shown in each gap before a word is chosen. Leave this empty for a blank gap.")]
    [SerializeField] private string blankOption = "";
    [SerializeField] private DeductionPuzzle[] puzzles =
    {
        new DeductionPuzzle
        {
            fixedParts = new[] { "The Golden Skull ", " during the ", " between ", " and ", "." },
            gaps = new[]
            {
                new DeductionGap { options = new[] { "vanished", "returned", "slept" }, correctOption = 0 },
                new DeductionGap { options = new[] { "ritual", "storm", "silence" }, correctOption = 0 },
                new DeductionGap { options = new[] { "dusk", "noon" }, correctOption = 0 },
                new DeductionGap { options = new[] { "dawn", "midnight" }, correctOption = 0 }
            },
            rewardToken = 3
        },
        new DeductionPuzzle
        {
            fixedParts = new[] { "The bell ", " once." },
            gaps = new[]
            {
                new DeductionGap { options = new[] { "rang", "cracked" }, correctOption = 0 }
            },
            rewardToken = 4
        }
    };

    [Header("Panel")]
    [Tooltip("Seconds the panel takes to slide in or out.")]
    [Min(0f)]
    [SerializeField] private float slideDuration = 0.35f;

    [Header("Token notification")]
    [Tooltip("Seconds the unlock message stays fully visible. The message text is the Token Notification object. Include {0} where the token name goes.")]
    [Min(0f)]
    [SerializeField] private float notificationSeconds = 2f;

    [Header("Ending")]
    [TextArea(4, 12)]
    [Tooltip("Typed on the Credits object. If that object already has text, its text is used instead.")]
    [SerializeField] private string creditsText = "Credits\n\nThank you for playing.";

    [Tooltip("Seconds to wait after the screen has gone black, before the credits start.")]
    [Min(0f)]
    [SerializeField] private float creditsDelay = 1f;

    [Tooltip("Seconds the black screen takes to fade in, and the Exit button takes to fade in.")]
    [Min(0f)]
    [SerializeField] private float fadeDuration = 1.5f;

    [Tooltip("Seconds between characters while the credits type on, matching the interaction panel.")]
    [Min(0.001f)]
    [SerializeField] private float secondsPerCharacter = 0.04f;

    [Header("References")]
    [SerializeField] private TokenUnlockManager tokenUnlockManager;
    [SerializeField] private JournalUI journal;
    [SerializeField] private InteractionPanelUI interactionPanel;
    [SerializeField] private GridCursor cursor;

    [Header("Scene UI")]
    [SerializeField] private GameObject uiRoot;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private RectTransform sentenceRoot;
    [SerializeField] private TMP_Text wordTemplate;
    [SerializeField] private TMP_Dropdown gapTemplate;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text notificationLabel;
    [SerializeField] private CanvasGroup notificationGroup;
    [SerializeField] private CanvasGroup endingGroup;
    [SerializeField] private TMP_Text creditsLabel;
    [SerializeField] private Button exitButton;
    [SerializeField] private CanvasGroup exitGroup;

    public bool HasSceneUi => panelRect != null && sentenceRoot != null && wordTemplate != null && gapTemplate != null;

    readonly List<TMP_Dropdown> gapDropdowns = new List<TMP_Dropdown>();

    int puzzleIndex;
    int builtPuzzle = -1;
    float slide;
    bool isOpen;
    bool ending;
    bool warnedEmpty;
    bool warnedMissingUi;
    Coroutine notificationRoutine;
    Coroutine endingRoutine;
    Vector2 openPosition;
    Vector2 closedPosition;
    string creditsFull = string.Empty;
    string notificationFormat = "you unlocked a new token '{0}'";

    void Awake()
    {
        if (tokenUnlockManager == null)
            tokenUnlockManager = FindFirstObjectByType<TokenUnlockManager>();
        if (journal == null)
            journal = FindFirstObjectByType<JournalUI>();
        if (interactionPanel == null)
            interactionPanel = FindFirstObjectByType<InteractionPanelUI>();
        if (cursor == null)
            cursor = FindFirstObjectByType<GridCursor>();

        PrepareSceneUi();
    }

    void OnEnable()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
    }

    void OnDisable()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(Confirm);
        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitGame);
    }

    void OnValidate()
    {
        if (puzzles == null)
            return;

        for (int i = 0; i < puzzles.Length; i++)
        {
            DeductionPuzzle puzzle = puzzles[i];
            if (puzzle == null)
                continue;

            int gapCount = puzzle.gaps != null ? puzzle.gaps.Length : 0;
            int partCount = puzzle.fixedParts != null ? puzzle.fixedParts.Length : 0;
            if (partCount != gapCount + 1)
            {
                Debug.LogWarning(
                    $"Deduction puzzle {i + 1} has {gapCount} gaps, so it needs {gapCount + 1} fixed text parts (it has {partCount}).",
                    this);
            }
        }
    }

    void Update()
    {
        AnimateSlide();

        if (ending || !WasSpacePressed())
            return;

        if (AnyGapExpanded())
            return;

        if (isOpen)
            Close();
        else
            Open();
    }

    void LateUpdate()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || uiRoot == null)
            return;

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (selected == null || !selected.transform.IsChildOf(uiRoot.transform))
            return;

        TMP_Dropdown dropdown = selected.GetComponentInParent<TMP_Dropdown>();
        if (dropdown != null && dropdown.IsExpanded)
            return;

        eventSystem.SetSelectedGameObject(null);
    }

    void Open()
    {
        if (ending)
            return;

        if (!HasSceneUi)
        {
            if (!warnedMissingUi)
            {
                warnedMissingUi = true;
                Debug.LogError("Deduction puzzles: the scene UI is missing. In Unity, use Tools / Build Deduction UI.", this);
            }

            return;
        }

        if (puzzles == null || puzzles.Length == 0)
        {
            if (!warnedEmpty)
            {
                warnedEmpty = true;
                Debug.LogWarning("Deduction puzzles: add at least one puzzle on Deduction Puzzle Panel.", this);
            }

            return;
        }

        if (journal != null && journal.IsOpen)
            journal.Close();

        if (interactionPanel != null && interactionPanel.IsVisible)
        {
            interactionPanel.Hide();
            if (cursor != null)
                cursor.CanMove = true;
        }

        EnsurePuzzleBuilt();
        isOpen = true;
        GameplayInput.SetBlocked(true);
    }

    void Close()
    {
        isOpen = false;
        HideExpandedGaps();
        if (!ending)
            GameplayInput.SetBlocked(false);
    }

    void Confirm()
    {
        if (ending || puzzles == null || puzzleIndex < 0 || puzzleIndex >= puzzles.Length)
            return;

        DeductionPuzzle puzzle = puzzles[puzzleIndex];
        if (!IsSolved(puzzle))
            return;

        bool unlocked = tokenUnlockManager != null && tokenUnlockManager.UnlockToken(puzzle.rewardToken);
        bool last = puzzleIndex >= puzzles.Length - 1;
        puzzleIndex++;
        builtPuzzle = -1;

        if (last)
        {
            BeginEnding();
            return;
        }

        if (unlocked && tokenUnlockManager != null)
            ShowNotification(tokenUnlockManager.GetTokenName(puzzle.rewardToken));

        EnsurePuzzleBuilt();
    }

    void BeginEnding()
    {
        ending = true;
        GameplayInput.BlockPermanently();
        HideExpandedGaps();
        isOpen = false;

        if (journal != null && journal.IsOpen)
            journal.Close();

        if (interactionPanel != null && interactionPanel.IsVisible)
        {
            interactionPanel.Hide();
            if (cursor != null)
                cursor.CanMove = false;
        }

        if (notificationRoutine != null)
        {
            StopCoroutine(notificationRoutine);
            notificationRoutine = null;
        }

        if (notificationGroup != null)
        {
            notificationGroup.alpha = 0f;
            notificationGroup.blocksRaycasts = false;
        }

        if (endingRoutine == null)
            endingRoutine = StartCoroutine(PlayEnding());
    }

    IEnumerator PlayEnding()
    {
        if (endingGroup != null)
        {
            endingGroup.blocksRaycasts = true;
            endingGroup.interactable = true;
            yield return FadeAlpha(endingGroup, 1f, fadeDuration);
        }

        if (creditsDelay > 0f)
            yield return new WaitForSecondsRealtime(creditsDelay);

        yield return RevealCredits();

        if (exitGroup != null)
        {
            yield return FadeAlpha(exitGroup, 1f, fadeDuration);
            exitGroup.blocksRaycasts = true;
            exitGroup.interactable = true;
        }
    }

    IEnumerator RevealCredits()
    {
        if (creditsLabel == null)
            yield break;

        string fullText = creditsFull ?? string.Empty;
        creditsLabel.text = string.Empty;

        for (int i = 0; i < fullText.Length; i++)
        {
            creditsLabel.text += fullText[i];
            if (i < fullText.Length - 1)
                yield return new WaitForSecondsRealtime(Mathf.Max(0.001f, secondsPerCharacter));
        }
    }

    void ShowNotification(string tokenName)
    {
        if (notificationLabel == null || notificationGroup == null)
            return;

        notificationLabel.text = string.Format(notificationFormat, tokenName);

        if (notificationRoutine != null)
            StopCoroutine(notificationRoutine);

        notificationRoutine = StartCoroutine(PlayNotification());
    }

    IEnumerator PlayNotification()
    {
        yield return FadeAlpha(notificationGroup, 1f, 0.2f);

        if (notificationSeconds > 0f)
            yield return new WaitForSecondsRealtime(notificationSeconds);

        yield return FadeAlpha(notificationGroup, 0f, 0.35f);
        notificationGroup.blocksRaycasts = false;
        notificationRoutine = null;
    }

    static IEnumerator FadeAlpha(CanvasGroup group, float target, float duration)
    {
        float start = group.alpha;
        if (duration <= 0f)
        {
            group.alpha = target;
            yield break;
        }

        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            t = t * t * (3f - 2f * t);
            group.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }

        group.alpha = target;
    }

    void EnsurePuzzleBuilt()
    {
        if (sentenceRoot == null || builtPuzzle == puzzleIndex)
            return;

        BuildSentence(puzzles[puzzleIndex]);
        builtPuzzle = puzzleIndex;
    }

    bool IsSolved(DeductionPuzzle puzzle)
    {
        if (puzzle == null || puzzle.gaps == null || gapDropdowns.Count != puzzle.gaps.Length)
            return false;

        for (int i = 0; i < puzzle.gaps.Length; i++)
        {
            DeductionGap gap = puzzle.gaps[i];
            int selected = gapDropdowns[i].value - 1;
            if (gap == null || selected != gap.correctOption)
                return false;
        }

        return true;
    }

    void AnimateSlide()
    {
        if (panelRect == null)
            return;

        float target = isOpen ? 1f : 0f;
        if (Mathf.Approximately(slide, target))
            return;

        float step = slideDuration <= 0f ? 1f : Time.unscaledDeltaTime / slideDuration;
        slide = Mathf.MoveTowards(slide, target, step);
        float eased = slide * slide * (3f - 2f * slide);
        panelRect.anchoredPosition = Vector2.Lerp(closedPosition, openPosition, eased);
    }

    void PrepareSceneUi()
    {
        if (panelRect != null)
        {
            Canvas.ForceUpdateCanvases();
            openPosition = panelRect.anchoredPosition;
            float height = panelRect.rect.height;
            if (height < 1f)
                height = panelRect.sizeDelta.y;
            closedPosition = openPosition + new Vector2(0f, -(height + 8f));
            slide = 0f;
            panelRect.anchoredPosition = closedPosition;
        }

        if (creditsLabel != null)
        {
            if (string.IsNullOrWhiteSpace(creditsLabel.text))
                creditsLabel.text = creditsText;
            creditsFull = creditsLabel.text;
            creditsLabel.text = string.Empty;
        }
        else
        {
            creditsFull = creditsText;
        }

        if (notificationLabel != null)
        {
            if (!string.IsNullOrWhiteSpace(notificationLabel.text) && notificationLabel.text.Contains("{0}"))
                notificationFormat = notificationLabel.text;
            notificationLabel.text = string.Empty;
        }

        if (notificationGroup != null)
        {
            notificationGroup.alpha = 0f;
            notificationGroup.blocksRaycasts = false;
            notificationGroup.interactable = false;
        }

        if (endingGroup != null)
        {
            endingGroup.alpha = 0f;
            endingGroup.blocksRaycasts = false;
            endingGroup.interactable = false;
        }

        if (exitGroup != null)
        {
            exitGroup.alpha = 0f;
            exitGroup.blocksRaycasts = false;
            exitGroup.interactable = false;
        }
    }

    bool AnyGapExpanded()
    {
        for (int i = 0; i < gapDropdowns.Count; i++)
        {
            if (gapDropdowns[i] != null && gapDropdowns[i].IsExpanded)
                return true;
        }

        return false;
    }

    void HideExpandedGaps()
    {
        for (int i = 0; i < gapDropdowns.Count; i++)
        {
            if (gapDropdowns[i] != null && gapDropdowns[i].IsExpanded)
                gapDropdowns[i].Hide();
        }
    }

    void BuildSentence(DeductionPuzzle puzzle)
    {
        ClearSentence();

        if (puzzle == null)
            return;

        int gapCount = puzzle.gaps != null ? puzzle.gaps.Length : 0;

        for (int i = 0; i < gapCount; i++)
        {
            CreateSentenceText(PartAt(puzzle, i));
            gapDropdowns.Add(CreateGap(puzzle.gaps[i]));
        }

        CreateSentenceText(PartAt(puzzle, gapCount));

        SentenceFlowLayout flow = sentenceRoot.GetComponent<SentenceFlowLayout>();
        if (flow != null)
            flow.Reflow();
    }

    static string PartAt(DeductionPuzzle puzzle, int index)
    {
        if (puzzle.fixedParts == null || index < 0 || index >= puzzle.fixedParts.Length)
            return string.Empty;

        return puzzle.fixedParts[index] ?? string.Empty;
    }

    void ClearSentence()
    {
        gapDropdowns.Clear();

        if (sentenceRoot == null)
            return;

        for (int i = sentenceRoot.childCount - 1; i >= 0; i--)
            DestroyImmediate(sentenceRoot.GetChild(i).gameObject);
    }

    void CreateSentenceText(string content)
    {
        if (string.IsNullOrEmpty(content) || wordTemplate == null)
            return;

        TMP_Text text = Instantiate(wordTemplate, sentenceRoot);
        text.gameObject.SetActive(true);
        text.name = "Words";
        text.text = content;
        text.raycastTarget = false;
        text.ForceMeshUpdate();
        float width = text.GetPreferredValues(content).x;
        float height = text.rectTransform.sizeDelta.y > 1f ? text.rectTransform.sizeDelta.y : 48f;
        text.rectTransform.sizeDelta = new Vector2(width, height);
    }

    TMP_Dropdown CreateGap(DeductionGap gap)
    {
        var labels = new List<string> { blankOption ?? string.Empty };
        if (gap != null && gap.options != null)
        {
            if (gap.correctOption < 0 || gap.correctOption >= gap.options.Length)
            {
                Debug.LogWarning(
                    $"A deduction gap's correct option is {gap.correctOption}, but it has {gap.options.Length} options.",
                    this);
            }

            for (int i = 0; i < gap.options.Length; i++)
                labels.Add(gap.options[i] ?? string.Empty);
        }

        TMP_Dropdown dropdown = Instantiate(gapTemplate, sentenceRoot);
        dropdown.gameObject.SetActive(true);
        dropdown.name = "Gap";
        dropdown.ClearOptions();
        dropdown.AddOptions(labels);
        dropdown.value = 0;
        dropdown.RefreshShownValue();
        return dropdown;
    }

    static void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static bool WasSpacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
}
