using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Interaction panel: opening description, pick 1–2 tokens (visual highlight), confirm, then typewriter outcome text.
/// </summary>
public class InteractionPanelUI : MonoBehaviour
{
    private const int TokenCount = 4;
    private const int MaxSelectedTokens = 2;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Tokens")]
    [SerializeField] private GameObject interactionMenu;

    [FormerlySerializedAs("menuButtons")]
    [SerializeField] private Button[] tokenButtons = new Button[TokenCount];

    [Tooltip("Optional. If empty, reads InteractionTokenVisual from each token button.")]
    [SerializeField] private InteractionTokenVisual[] tokenVisuals = new InteractionTokenVisual[TokenCount];

    [SerializeField] private Button confirmButton;

    [Header("Text")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Journal")]
    [SerializeField] private JournalUI journal;

    [SerializeField] private JournalDiscoveryLog journalDiscoveryLog;

    [SerializeField] private TokenUnlockManager tokenUnlockManager;

    [Header("Typewriter")]
    [Min(0.001f)]
    [SerializeField] private float secondsPerCharacter = 0.04f;

    private Coroutine _typewriterRoutine;
    private string _typewriterFullText = string.Empty;
    private bool _revealTokensWhenTypewriterCompletes;
    private bool _tokensRevealed;
    private MapInteractable _currentInteractable;
    private readonly UnityAction[] _tokenClickActions = new UnityAction[TokenCount];
    private UnityAction _confirmClickAction;

    private readonly List<int> _selectedTokenIndices = new List<int>(MaxSelectedTokens);

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    public bool BlocksDismissal => journal != null && journal.IsOpen;

    /// <summary>True when one or two tokens are selected but not yet confirmed.</summary>
    public bool HasPendingTokenSelection => _selectedTokenIndices.Count > 0;

    /// <summary>True while description or token-outcome text is still revealing.</summary>
    public bool IsTypewriterRunning => _typewriterRoutine != null;

    private void Awake()
    {
        if (tokenUnlockManager == null)
            tokenUnlockManager = FindFirstObjectByType<TokenUnlockManager>();

        WireTokenButtons();
        WireConfirmButton();
        RefreshTokenAvailability();
        Hide();
    }

    private void OnDestroy()
    {
        UnwireTokenButtons();
        UnwireConfirmButton();
    }

    private void Update()
    {
        if (!IsVisible || BlocksDismissal || !_tokensRevealed)
            return;

        if (TryGetTokenIndexFromNumberKey(out int tokenIndex))
            OnTokenClicked(tokenIndex);
    }

    public void Show(MapInteractable interactable)
    {
        if (interactable == null || panelRoot == null)
            return;

        _currentInteractable = interactable;

        if (journal != null)
            journal.Close();

        StopTypewriter();
        ResetTokenSelection();

        if (nameText != null)
            nameText.text = interactable.DisplayName;

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        panelRoot.SetActive(true);

        if (interactionMenu != null)
            interactionMenu.SetActive(true);

        SetTokensVisible(false);
        ShowBodyWithTypewriter(interactable.Description, true);
    }

    public void Hide()
    {
        StopTypewriter();
        _revealTokensWhenTypewriterCompletes = false;
        ResetTokenSelection();
        SetTokensVisible(false);
        _currentInteractable = null;

        if (interactionMenu != null &&
            panelRoot != null &&
            !interactionMenu.transform.IsChildOf(panelRoot.transform))
        {
            interactionMenu.SetActive(false);
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void WireTokenButtons()
    {
        if (tokenButtons == null)
            return;

        for (int i = 0; i < tokenButtons.Length && i < TokenCount; i++)
        {
            Button button = tokenButtons[i];
            if (button == null)
                continue;

            int tokenIndex = i;
            _tokenClickActions[i] = () => OnTokenClicked(tokenIndex);
            button.onClick.AddListener(_tokenClickActions[i]);
        }
    }

    private void UnwireTokenButtons()
    {
        if (tokenButtons == null)
            return;

        for (int i = 0; i < tokenButtons.Length && i < TokenCount; i++)
        {
            Button button = tokenButtons[i];
            if (button == null || _tokenClickActions[i] == null)
                continue;

            button.onClick.RemoveListener(_tokenClickActions[i]);
            _tokenClickActions[i] = null;
        }
    }

    private void WireConfirmButton()
    {
        if (confirmButton == null)
            return;

        _confirmClickAction = OnConfirmClicked;
        confirmButton.onClick.AddListener(_confirmClickAction);
    }

    private void UnwireConfirmButton()
    {
        if (confirmButton == null || _confirmClickAction == null)
            return;

        confirmButton.onClick.RemoveListener(_confirmClickAction);
        _confirmClickAction = null;
    }

    private void OnTokenClicked(int tokenIndex)
    {
        if (!_tokensRevealed || _currentInteractable == null || tokenIndex < 0 || tokenIndex >= TokenCount)
            return;

        if (!IsTokenUnlocked(tokenIndex))
            return;

        if (_selectedTokenIndices.Contains(tokenIndex))
            _selectedTokenIndices.Remove(tokenIndex);
        else if (_selectedTokenIndices.Count < MaxSelectedTokens)
            _selectedTokenIndices.Add(tokenIndex);

        RefreshTokenVisuals();
        RefreshConfirmButton();
    }

    private void OnConfirmClicked()
    {
        ConfirmTokenSelection();
    }

    /// <summary>Applies token outcome text and clears selection. Panel stays open.</summary>
    public void ConfirmTokenSelection()
    {
        if (!_tokensRevealed || _currentInteractable == null || _selectedTokenIndices.Count == 0)
            return;

        var confirmedIndices = new List<int>(_selectedTokenIndices);

        if (journalDiscoveryLog != null)
            journalDiscoveryLog.RecordTokenOutcome(_currentInteractable, confirmedIndices);

        if (tokenUnlockManager != null)
            tokenUnlockManager.EvaluateUnlocks(_currentInteractable, confirmedIndices);

        string body = _currentInteractable.GetTokenChoiceText(confirmedIndices);
        ShowBodyWithTypewriter(body, false);
        ResetTokenSelection();
        RefreshTokenAvailability();
    }

    private void ResetTokenSelection()
    {
        _selectedTokenIndices.Clear();
        RefreshTokenVisuals();
        RefreshConfirmButton();
    }

    private void RefreshTokenVisuals()
    {
        for (int i = 0; i < TokenCount; i++)
        {
            bool unlocked = IsTokenUnlocked(i);
            InteractionTokenVisual visual = GetTokenVisual(i);
            if (visual != null)
            {
                visual.SetLocked(!unlocked);
                visual.SetSelected(unlocked && _selectedTokenIndices.Contains(i));
            }
        }
    }

    private void RefreshTokenAvailability()
    {
        for (int i = 0; i < TokenCount; i++)
        {
            bool unlocked = IsTokenUnlocked(i);
            if (tokenButtons == null || i >= tokenButtons.Length || tokenButtons[i] == null)
                continue;

            Button button = tokenButtons[i];
            button.gameObject.SetActive(unlocked);
            if (unlocked)
                button.interactable = true;
        }

        _selectedTokenIndices.RemoveAll(index => !IsTokenUnlocked(index));
        RefreshTokenVisuals();
        RefreshConfirmButton();
    }

    private bool IsTokenUnlocked(int tokenIndex)
    {
        if (tokenUnlockManager == null)
            return true;

        return tokenUnlockManager.IsTokenUnlocked(tokenIndex);
    }

    private InteractionTokenVisual GetTokenVisual(int index)
    {
        if (tokenVisuals != null && index < tokenVisuals.Length && tokenVisuals[index] != null)
            return tokenVisuals[index];

        if (tokenButtons == null || index < 0 || index >= tokenButtons.Length)
            return null;

        Button button = tokenButtons[index];
        return button != null ? button.GetComponent<InteractionTokenVisual>() : null;
    }

    private void RefreshConfirmButton()
    {
        if (confirmButton != null)
            confirmButton.interactable = _selectedTokenIndices.Count > 0;
    }

    /// <summary>Fills in the text that is still typing. Does not close the panel.</summary>
    public void CompleteTypewriter()
    {
        if (!IsTypewriterRunning)
            return;

        string fullText = _typewriterFullText;
        bool revealTokens = _revealTokensWhenTypewriterCompletes;
        StopTypewriter();

        if (descriptionText != null)
            descriptionText.text = fullText;

        if (revealTokens)
            SetTokensVisible(true);
    }

    private void ShowBodyWithTypewriter(string fullText, bool revealTokensOnComplete)
    {
        StopTypewriter();
        _typewriterFullText = fullText ?? string.Empty;
        _revealTokensWhenTypewriterCompletes = revealTokensOnComplete;

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        if (descriptionText == null || string.IsNullOrEmpty(_typewriterFullText))
        {
            if (revealTokensOnComplete)
                SetTokensVisible(true);

            return;
        }

        _typewriterRoutine = StartCoroutine(RevealDescriptionByCharacter(_typewriterFullText));
    }

    private void SetTokensVisible(bool visible)
    {
        _tokensRevealed = visible;
        _revealTokensWhenTypewriterCompletes = false;

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(visible);

        if (tokenButtons != null)
        {
            for (int i = 0; i < tokenButtons.Length && i < TokenCount; i++)
            {
                if (tokenButtons[i] != null)
                    tokenButtons[i].gameObject.SetActive(visible);
            }
        }

        if (visible)
            RefreshTokenAvailability();
    }

    private void StopTypewriter()
    {
        if (_typewriterRoutine == null)
            return;

        StopCoroutine(_typewriterRoutine);
        _typewriterRoutine = null;
    }

    private IEnumerator RevealDescriptionByCharacter(string fullDescription)
    {
        for (int i = 0; i < fullDescription.Length; i++)
        {
            descriptionText.text += fullDescription[i];

            if (i < fullDescription.Length - 1)
                yield return new WaitForSeconds(secondsPerCharacter);
        }

        _typewriterRoutine = null;

        if (_revealTokensWhenTypewriterCompletes)
            SetTokensVisible(true);
    }

    private bool TryGetTokenIndexFromNumberKey(out int tokenIndex)
    {
        tokenIndex = -1;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            tokenIndex = 0;
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            tokenIndex = 1;
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            tokenIndex = 2;
        else if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame)
            tokenIndex = 3;
        else
            return false;

        return true;

#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            tokenIndex = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            tokenIndex = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            tokenIndex = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            tokenIndex = 3;
        else
            return false;

        return true;
#else
        return false;
#endif
    }
}
