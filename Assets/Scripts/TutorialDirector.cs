using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Left-side tutorial box. Movement plays first, then interact.
/// Later lessons wait in a queue until the box is free.
/// An action stays locked until its own lesson is on screen, then it stays available.
/// The third-token warning covers the current lesson, then that lesson comes back.
/// Put this on its own object. Assign the box and the text. Hiding the box must not disable this object.
/// </summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public class TutorialDirector : MonoBehaviour
{
    private enum Lesson
    {
        None,
        Move,
        Interact,
        Tokens,
        Journal,
        Deduction,
        NewToken,
        TwoTokens
    }

    [Header("Box")]
    [Tooltip("Panel to show and hide. Do not put this component on that object.")]
    [SerializeField] private GameObject boxRoot;

    [SerializeField] private TMP_Text messageText;

    [Header("Animation")]
    [Tooltip("Animator on the box. If empty, the box object is searched. States play once: appear when the box opens, disappear when it closes.")]
    [SerializeField] private Animator boxAnimator;

    [SerializeField] private string appearState = "HintAppear";

    [SerializeField] private string disappearState = "HintDisappear";

    [Header("Timing")]
    [Tooltip("How long the move lesson stays up after the first movement key.")]
    [Min(0f)]
    [SerializeField] private float delayAfterFirstMove = 3f;

    [Tooltip("How long the deduction lesson stays up after Space.")]
    [Min(0f)]
    [SerializeField] private float deductionDismissDelay = 5f;

    [Tooltip("How long the two-token limit warning stays up. It shows again on every later attempt.")]
    [Min(0f)]
    [SerializeField] private float thirdTokenMessageDuration = 3f;

    [Tooltip("Journal bullets required before the deduction lesson is queued.")]
    [Min(1)]
    [SerializeField] private int cluesBeforeDeduction = 3;

    [Header("Lessons")]
    [TextArea(2, 4)]
    [SerializeField] private string moveMessage = "Press WASD or arrow keys to Move";

    [TextArea(2, 4)]
    [SerializeField] private string interactMessage = "Press E to Interact with Objects, NPCs or Entrances/Exits";

    [TextArea(2, 5)]
    [SerializeField] private string tokensMessage = "Tokens represent topics that you can discuss. Press 1 or click on the Token to choose it and E to confirm your choice";

    [TextArea(2, 4)]
    [SerializeField] private string journalMessage = "When you discover a Clue, it appears in your Journal. Press TAB to open it";

    [TextArea(2, 5)]
    [SerializeField] private string deductionMessage = "When you gather enough Clues, you can try to solve the Deduction. Press SPACE to do so. Choose options from drop down menus. Some of them will be scrollable";

    [TextArea(2, 4)]
    [SerializeField] private string newTokenMessage = "You unlocked a new Token. You can try it out on an NPC";

    [TextArea(2, 4)]
    [SerializeField] private string twoTokensMessage = "Now you have two Tokens. You can still choose one or both at the same time to get intersecting information";

    [TextArea(2, 4)]
    [SerializeField] private string thirdTokenMessage = "You can only choose up to two tokens at the same time";

    private readonly Queue<Lesson> _queue = new Queue<Lesson>();

    private JournalDiscoveryLog _journalLog;
    private TokenUnlockManager _tokens;
    private Lesson _current = Lesson.None;
    private float _transientUntil;
    private float _interactLessonAt = -1f;
    private float _deductionDismissAt = -1f;
    private bool _journalSubscribed;
    private bool _bootstrapped;
    private bool _ended;
    private bool _moveDelayStarted;
    private bool _tokensEnqueued;
    private bool _journalEnqueued;
    private bool _deductionEnqueued;
    private bool _newTokenEnqueued;
    private bool _twoTokensEnqueued;
    private bool _deductionDismissStarted;
    private bool _interactShown;
    private bool _tokensShown;
    private bool _journalShown;
    private bool _deductionShown;
    private bool _twoTokensShown;
    private bool _warnedMissingText;
    private bool _warnedBoxIsHost;
    private bool _warnedMissingAnimator;
    private bool _warnedMissingState;
    private bool _boxOnScreen;
    private bool _transitioningOut;
    private bool _waitingToHide;
    private bool _holdAtEnd;
    private bool _sawClipAdvance;
    private bool _justActivated;
    private bool _introArmed;
    private int _introStep;
    private bool _warningOwnsBox;
    private bool _hasPendingLesson;
    private Lesson _pendingLesson;
    private string _deferredState;
    private bool _deferredHide;
    private int _deferAttempts;
    private float _hideAt = -1f;
    private int _playedStateHash;
    private CanvasGroup _canvasGroup;

    public static TutorialDirector Instance { get; private set; }

    public event System.Action GatesChanged;

    public bool AllowsInteract => _interactShown;

    public bool AllowsJournal => _journalShown;

    public bool AllowsDeduction => _deductionShown;

    public bool AllowsTokens => _tokensShown;

    /// <summary>0 before the token lesson, 1 after it, 2 once the two-token lesson is on screen.</summary>
    public int MaxSelectableTokens
    {
        get
        {
            if (_twoTokensShown)
                return 2;

            return _tokensShown ? 1 : 0;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("TutorialDirector: one director is enough. This duplicate is turned off.", this);
            enabled = false;
            return;
        }

        Instance = this;

        if (messageText == null && !_warnedMissingText)
        {
            _warnedMissingText = true;
            Debug.LogWarning("TutorialDirector: assign the message text. Lessons still lock input until they would appear.", this);
        }

        GameObject root = BoxObject();
        if (HidingBoxWouldStopTutorial(root))
            WarnBoxIsHost();
        else if (root != null)
        {
            EnsureAnimator(root);
            if (boxAnimator != null)
                boxAnimator.enabled = false;

            root.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (Instance != this)
            return;

        SubscribeJournal();

        if (_bootstrapped)
            return;

        _bootstrapped = true;
        _current = Lesson.Move;
        if (messageText != null)
            messageText.text = moveMessage;

        _introArmed = true;
        _introStep = 0;
    }

    private void OnDisable()
    {
        if (_journalSubscribed && _journalLog != null)
            _journalLog.BulletAdded -= OnBulletAdded;

        _journalSubscribed = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (Instance != this)
            return;

        TickIntro();
        PlayDeferredClip();
        HoldPlayedStateAtEnd();
        FinishHideIfDue();
        _justActivated = false;

        if (_transientUntil > Time.unscaledTime && messageText != null)
            messageText.text = thirdTokenMessage;

        if (_ended)
            return;

        if (_journalLog == null)
            SubscribeJournal();

        if (!_moveDelayStarted && _current == Lesson.Move && WasMovePressed())
        {
            _moveDelayStarted = true;
            _interactLessonAt = Time.unscaledTime + delayAfterFirstMove;
        }

        if (_interactLessonAt >= 0f && Time.unscaledTime >= _interactLessonAt)
        {
            _interactLessonAt = -1f;
            if (_current == Lesson.Move)
                Present(Lesson.Interact);
        }

        if (_deductionDismissAt >= 0f && Time.unscaledTime >= _deductionDismissAt)
        {
            _deductionDismissAt = -1f;
            if (_current == Lesson.Deduction)
                CompleteCurrent();
        }

        if (_transientUntil > 0f && Time.unscaledTime >= _transientUntil)
        {
            _transientUntil = 0f;
            bool closeWarning = _warningOwnsBox;
            _warningOwnsBox = false;

            if (closeWarning && _current == Lesson.None && !_hasPendingLesson)
                BeginDisappear();
            else if (_current == Lesson.None)
                ShowNextIfIdle();
            else
                RefreshBox();
        }
    }

    /// <summary>The player used an entrance or exit.</summary>
    public void NotifyMapSwitched()
    {
        if (_ended)
            return;

        if (_current == Lesson.Interact)
            CompleteCurrent();
    }

    /// <summary>The player opened any map interactable. Those count as both POIs and NPCs.</summary>
    public void NotifyInteractableOpened()
    {
        if (_ended)
            return;

        if (!_tokensEnqueued)
        {
            _tokensEnqueued = true;
            Enqueue(Lesson.Tokens);
        }

        if (!_twoTokensEnqueued && UnlockedTokenCount() >= 2)
        {
            _twoTokensEnqueued = true;
            Enqueue(Lesson.TwoTokens);
        }

        if (_current == Lesson.NewToken)
            CompleteCurrent();
    }

    public void NotifyTokensConfirmed(int count)
    {
        if (_ended || count <= 0)
            return;

        if (_current == Lesson.Tokens)
            CompleteCurrent();
        else if (_current == Lesson.TwoTokens && count >= 2)
            CompleteCurrent();
    }

    public void NotifyThirdTokenAttempt()
    {
        if (_ended)
            return;

        _transientUntil = Time.unscaledTime + thirdTokenMessageDuration;
        if (messageText != null)
            messageText.text = thirdTokenMessage;

        GameObject root = BoxObject();
        if (root == null || HidingBoxWouldStopTutorial(root))
            return;

        if (root.activeSelf && _boxOnScreen)
            return;

        _warningOwnsBox = _current == Lesson.None && !_hasPendingLesson;
        root.SetActive(true);
        _boxOnScreen = true;
        SetBoxAlpha(0f);
        EnsureAnimator(root);
        if (boxAnimator != null)
            boxAnimator.enabled = true;

        StartClip(appearState, false);
    }

    public void NotifyJournalTabPressed()
    {
        if (_ended)
            return;

        if (_current == Lesson.Journal)
            CompleteCurrent();
    }

    public void NotifyDeductionSpacePressed()
    {
        if (_ended || _current != Lesson.Deduction || _deductionDismissStarted)
            return;

        _deductionDismissStarted = true;

        if (deductionDismissDelay <= 0f)
            CompleteCurrent();
        else
            _deductionDismissAt = Time.unscaledTime + deductionDismissDelay;
    }

    public void NotifyFirstDeductionSolved()
    {
        if (_ended || _newTokenEnqueued)
            return;

        _newTokenEnqueued = true;
        Enqueue(Lesson.NewToken);
    }

    /// <summary>Clears the box when the ending takes over the screen.</summary>
    public void NotifyGameEnded()
    {
        if (_ended)
            return;

        _ended = true;
        _transientUntil = 0f;
        _interactLessonAt = -1f;
        _deductionDismissAt = -1f;
        _current = Lesson.None;
        _hasPendingLesson = false;
        _queue.Clear();
        if (_boxOnScreen)
            BeginDisappear();
    }

    private void SubscribeJournal()
    {
        if (_journalLog == null)
            _journalLog = FindFirstObjectByType<JournalDiscoveryLog>();

        if (_journalLog == null || _journalSubscribed)
            return;

        _journalLog.BulletAdded += OnBulletAdded;
        _journalSubscribed = true;
    }

    private void OnBulletAdded(int total)
    {
        if (_ended)
            return;

        if (!_journalEnqueued)
        {
            _journalEnqueued = true;
            Enqueue(Lesson.Journal);
        }

        if (!_deductionEnqueued && total >= cluesBeforeDeduction)
        {
            _deductionEnqueued = true;
            Enqueue(Lesson.Deduction);
        }
    }

    private void Enqueue(Lesson lesson)
    {
        _queue.Enqueue(lesson);
        ShowNextIfIdle();
    }

    private void CompleteCurrent()
    {
        _current = Lesson.None;
        ShowNextIfIdle();
    }

    private void ShowNextIfIdle()
    {
        if (_current != Lesson.None)
            return;

        if (_transientUntil > Time.unscaledTime)
        {
            RefreshBox();
            return;
        }

        if (_hasPendingLesson || _transitioningOut)
            return;

        if (_queue.Count == 0)
        {
            _hasPendingLesson = false;
            if (_boxOnScreen)
                BeginDisappear();
            return;
        }

        Present(_queue.Dequeue());
    }

    private void Present(Lesson lesson)
    {
        if (_boxOnScreen)
        {
            _pendingLesson = lesson;
            _hasPendingLesson = true;
            BeginDisappear();
            return;
        }

        Commit(lesson);
    }

    private void Commit(Lesson lesson)
    {
        _current = lesson;

        switch (lesson)
        {
            case Lesson.Interact:
                _interactShown = true;
                break;
            case Lesson.Tokens:
                _tokensShown = true;
                break;
            case Lesson.Journal:
                _journalShown = true;
                break;
            case Lesson.Deduction:
                _deductionShown = true;
                break;
            case Lesson.TwoTokens:
                _twoTokensShown = true;
                break;
        }

        GatesChanged?.Invoke();

        bool transient = _transientUntil > Time.unscaledTime;
        if (messageText != null && !transient)
            messageText.text = MessageFor(lesson);

        GameObject root = BoxObject();
        if (root == null || HidingBoxWouldStopTutorial(root))
        {
            if (root != null && HidingBoxWouldStopTutorial(root))
                WarnBoxIsHost();
            return;
        }

        if (!root.activeSelf)
        {
            root.SetActive(true);
            _justActivated = true;
        }

        _boxOnScreen = true;
        SetBoxAlpha(0f);
        EnsureAnimator(root);
        if (!StartClip(appearState, false))
            SetBoxAlpha(1f);
    }

    private void RefreshBox()
    {
        if (messageText == null)
            return;

        if (_transientUntil > Time.unscaledTime)
            messageText.text = thirdTokenMessage;
        else if (_current != Lesson.None)
            messageText.text = MessageFor(_current);
    }

    private void TickIntro()
    {
        if (!_introArmed)
            return;

        GameObject root = BoxObject();
        if (root == null || HidingBoxWouldStopTutorial(root))
        {
            _introArmed = false;
            return;
        }

        if (_introStep == 0)
        {
            _introStep = 1;
            return;
        }

        if (_introStep == 1)
        {
            root.SetActive(true);
            _justActivated = true;
            _boxOnScreen = true;
            SetBoxAlpha(0f);
            EnsureAnimator(root);
            if (boxAnimator != null)
                boxAnimator.enabled = false;

            _introStep = 2;
            return;
        }

        EnsureAnimator(root);
        if (boxAnimator != null)
        {
            boxAnimator.enabled = true;
            boxAnimator.Rebind();
            boxAnimator.Update(0f);
        }

        _introArmed = false;
        if (!StartClip(appearState, false))
            SetBoxAlpha(1f);
    }

    private void BeginDisappear()
    {
        if (_transitioningOut)
            return;

        GameObject root = BoxObject();
        if (root == null)
            return;

        if (HidingBoxWouldStopTutorial(root))
        {
            WarnBoxIsHost();
            return;
        }

        if (!root.activeSelf)
        {
            _boxOnScreen = false;
            _transitioningOut = false;
            return;
        }

        _transitioningOut = true;
        EnsureAnimator(root);
        if (!StartClip(disappearState, true))
            FinishTransition();
    }

    private bool StartClip(string stateName, bool hideAfter)
    {
        if (boxAnimator != null && boxAnimator.isActiveAndEnabled && boxAnimator.isInitialized && !_justActivated)
        {
            if (!PlayBoxNow(stateName, out float length))
                return false;

            if (hideAfter)
            {
                _waitingToHide = true;
                _hideAt = Time.unscaledTime + length;
            }

            return true;
        }

        _deferredState = stateName;
        _deferredHide = hideAfter;
        _waitingToHide = false;
        return true;
    }

    private void PlayDeferredClip()
    {
        if (string.IsNullOrEmpty(_deferredState))
            return;

        if (boxAnimator == null || !boxAnimator.isActiveAndEnabled || _justActivated)
            return;

        if (!boxAnimator.isInitialized && _deferAttempts < 5)
        {
            _deferAttempts++;
            return;
        }

        string stateName = _deferredState;
        bool hideAfter = _deferredHide;

        if (!PlayBoxNow(stateName, out float length))
        {
            _deferAttempts++;
            if (_deferAttempts < 5)
                return;

            _deferredState = null;
            _deferAttempts = 0;
            if (hideAfter)
                FinishTransition();
            else
                SetBoxAlpha(1f);
            return;
        }

        _deferredState = null;
        _deferAttempts = 0;

        if (hideAfter)
        {
            _waitingToHide = true;
            _hideAt = Time.unscaledTime + length;
        }
    }

    private string ResolveStateName(string stateName)
    {
        if (boxAnimator == null || string.IsNullOrEmpty(stateName))
            return stateName;

        if (boxAnimator.HasState(0, Animator.StringToHash(stateName)))
            return stateName;

        string alternate = null;
        if (stateName == "HintDisappear")
            alternate = "HintDissapear";
        else if (stateName == "HintDissapear")
            alternate = "HintDisappear";

        if (alternate != null && boxAnimator.HasState(0, Animator.StringToHash(alternate)))
            return alternate;

        return stateName;
    }

    private void EnsureAnimator(GameObject root)
    {
        if (boxAnimator != null)
            return;

        boxAnimator = root.GetComponent<Animator>();
        if (boxAnimator == null)
            boxAnimator = root.GetComponentInParent<Animator>();

        if (boxAnimator == null && !_warnedMissingAnimator)
        {
            _warnedMissingAnimator = true;
            Debug.LogWarning("TutorialDirector: no Animator on the box. It will show and hide without the fade.", this);
        }
    }

    private bool PlayBoxNow(string stateName, out float length)
    {
        length = 0f;

        if (boxAnimator == null || boxAnimator.runtimeAnimatorController == null || string.IsNullOrEmpty(stateName))
            return false;

        stateName = ResolveStateName(stateName);
        int hash = Animator.StringToHash(stateName);
        if (!boxAnimator.HasState(0, hash))
        {
            if (!_warnedMissingState)
            {
                _warnedMissingState = true;
                Debug.LogWarning($"TutorialDirector: the box animator has no state \"{stateName}\".", this);
            }

            return false;
        }

        boxAnimator.speed = 1f;
        boxAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        boxAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Play does not restart the state the animator is already in. Step through the other clip first.
        AnimatorStateInfo current = boxAnimator.GetCurrentAnimatorStateInfo(0);
        if (current.shortNameHash == hash && current.normalizedTime > 0.05f)
        {
            string otherName = ResolveStateName(stateName == appearState || stateName == "HintAppear" ? disappearState : appearState);
            int otherHash = Animator.StringToHash(otherName);
            if (boxAnimator.HasState(0, otherHash))
            {
                boxAnimator.Play(otherHash, 0, 1f);
                boxAnimator.Update(0f);
            }
        }

        boxAnimator.Play(hash, 0, 0f);
        boxAnimator.Update(0f);

        _playedStateHash = hash;
        _holdAtEnd = true;
        _sawClipAdvance = false;
        length = boxAnimator.GetCurrentAnimatorStateInfo(0).length;
        if (length <= 0f)
            length = 1f;

        return true;
    }

    private void HoldPlayedStateAtEnd()
    {
        if (!_holdAtEnd || boxAnimator == null || !boxAnimator.isActiveAndEnabled)
            return;

        AnimatorStateInfo info = boxAnimator.GetCurrentAnimatorStateInfo(0);
        if (info.shortNameHash != _playedStateHash)
            return;

        if (info.normalizedTime < 1f)
        {
            _sawClipAdvance = true;
            return;
        }

        if (!_sawClipAdvance)
            return;

        boxAnimator.Play(_playedStateHash, 0, 1f);
        boxAnimator.speed = 0f;
        _holdAtEnd = false;
    }

    private void FinishHideIfDue()
    {
        if (!_waitingToHide || Time.unscaledTime < _hideAt)
            return;

        FinishTransition();
    }

    private void FinishTransition()
    {
        _waitingToHide = false;
        _hideAt = -1f;
        _transitioningOut = false;

        if (_hasPendingLesson && !_ended)
        {
            Lesson next = _pendingLesson;
            _hasPendingLesson = false;
            Commit(next);
            return;
        }

        _boxOnScreen = false;
        GameObject root = BoxObject();
        if (root != null && root.activeSelf && !HidingBoxWouldStopTutorial(root))
            root.SetActive(false);
    }

    private void SetBoxAlpha(float alpha)
    {
        GameObject root = BoxObject();
        if (root == null)
            return;

        if (_canvasGroup == null)
            _canvasGroup = root.GetComponent<CanvasGroup>();

        if (_canvasGroup != null)
            _canvasGroup.alpha = alpha;
    }

    private GameObject BoxObject()
    {
        if (boxRoot != null)
            return boxRoot;

        return messageText != null ? messageText.gameObject : null;
    }

    private string MessageFor(Lesson lesson)
    {
        switch (lesson)
        {
            case Lesson.Move: return moveMessage;
            case Lesson.Interact: return interactMessage;
            case Lesson.Tokens: return tokensMessage;
            case Lesson.Journal: return journalMessage;
            case Lesson.Deduction: return deductionMessage;
            case Lesson.NewToken: return newTokenMessage;
            case Lesson.TwoTokens: return twoTokensMessage;
            default: return null;
        }
    }

    private int UnlockedTokenCount()
    {
        if (_tokens == null)
            _tokens = FindFirstObjectByType<TokenUnlockManager>();

        return _tokens != null ? _tokens.UnlockedCount : 0;
    }

    private bool HidingBoxWouldStopTutorial(GameObject root)
    {
        if (root == null)
            return false;

        return root == gameObject || transform.IsChildOf(root.transform);
    }

    private void WarnBoxIsHost()
    {
        if (_warnedBoxIsHost)
            return;

        _warnedBoxIsHost = true;
        Debug.LogError(
            "TutorialDirector: hiding the box would turn this component off. " +
            "Put it on its own object, outside the box, and assign the box as Box Root.",
            this
        );
    }

    private bool WasMovePressed()
    {
        if (GameplayInput.IsBlocked)
            return false;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        return keyboard.wKey.wasPressedThisFrame ||
               keyboard.aKey.wasPressedThisFrame ||
               keyboard.sKey.wasPressedThisFrame ||
               keyboard.dKey.wasPressedThisFrame ||
               keyboard.upArrowKey.wasPressedThisFrame ||
               keyboard.downArrowKey.wasPressedThisFrame ||
               keyboard.leftArrowKey.wasPressedThisFrame ||
               keyboard.rightArrowKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
}
