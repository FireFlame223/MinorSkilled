using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Interaction panel: opening description, pick 1–2 tokens, confirm, then typewriter outcome text from the POI.
/// </summary>
public class InteractionPanelUI : MonoBehaviour
{
    private const int TokenCount = 4;
    private const int MaxSelectedTokens = 2;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Tokens")]
    [SerializeField] private GameObject interactionMenu;

    [SerializeField] private Transform availableTokensLayout;
    [SerializeField] private Transform chosenTokensLayout;

    [FormerlySerializedAs("menuButtons")]
    [SerializeField] private Button[] tokenButtons = new Button[TokenCount];

    [SerializeField] private Button confirmButton;

    [Header("Text")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Journal")]
    [SerializeField] private JournalUI journal;

    [Header("Typewriter")]
    [Min(0.001f)]
    [SerializeField] private float secondsPerCharacter = 0.04f;

    private Coroutine _typewriterRoutine;
    private MapInteractable _currentInteractable;
    private readonly UnityAction[] _tokenClickActions = new UnityAction[TokenCount];
    private UnityAction _confirmClickAction;

    private readonly List<int> _selectedTokenIndices = new List<int>(MaxSelectedTokens);
    private Transform[] _tokenHomeParents;
    private int[] _tokenHomeSiblingIndices;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    public bool BlocksDismissal => journal != null && journal.IsOpen;

    private void Awake()
    {
        CacheTokenHomeTransforms();
        WireTokenButtons();
        WireConfirmButton();
        Hide();
    }

    private void OnDestroy()
    {
        UnwireTokenButtons();
        UnwireConfirmButton();
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

        if (availableTokensLayout != null)
            availableTokensLayout.gameObject.SetActive(true);

        if (chosenTokensLayout != null)
            chosenTokensLayout.gameObject.SetActive(true);

        ShowBodyWithTypewriter(interactable.Description);
    }

    public void Hide()
    {
        StopTypewriter();
        ResetTokenSelection();
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

    private void CacheTokenHomeTransforms()
    {
        _tokenHomeParents = new Transform[TokenCount];
        _tokenHomeSiblingIndices = new int[TokenCount];

        if (tokenButtons == null)
            return;

        for (int i = 0; i < tokenButtons.Length && i < TokenCount; i++)
        {
            Button button = tokenButtons[i];
            if (button == null)
                continue;

            Transform t = button.transform;
            _tokenHomeParents[i] = t.parent;
            _tokenHomeSiblingIndices[i] = t.GetSiblingIndex();
        }
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
        if (_currentInteractable == null || tokenIndex < 0 || tokenIndex >= TokenCount)
            return;

        if (_selectedTokenIndices.Contains(tokenIndex))
        {
            _selectedTokenIndices.Remove(tokenIndex);
            MoveTokenToAvailable(tokenIndex);
        }
        else if (_selectedTokenIndices.Count < MaxSelectedTokens)
        {
            _selectedTokenIndices.Add(tokenIndex);
            MoveTokenToChosen(tokenIndex);
        }

        RefreshConfirmButton();
    }

    private void OnConfirmClicked()
    {
        if (_currentInteractable == null || _selectedTokenIndices.Count == 0)
            return;

        string body = _currentInteractable.GetTokenChoiceText(_selectedTokenIndices);
        ShowBodyWithTypewriter(body);
        ResetTokenSelection();
    }

    private void ResetTokenSelection()
    {
        _selectedTokenIndices.Clear();

        for (int i = 0; i < TokenCount; i++)
            MoveTokenToAvailable(i);

        RefreshConfirmButton();
    }

    private void MoveTokenToAvailable(int tokenIndex)
    {
        if (tokenButtons == null || tokenIndex < 0 || tokenIndex >= tokenButtons.Length)
            return;

        Button button = tokenButtons[tokenIndex];
        if (button == null)
            return;

        Transform home = _tokenHomeParents != null ? _tokenHomeParents[tokenIndex] : null;
        if (home == null)
            home = availableTokensLayout;

        if (home == null)
            return;

        Transform t = button.transform;
        t.SetParent(home, false);

        if (_tokenHomeSiblingIndices != null && tokenIndex < _tokenHomeSiblingIndices.Length)
            t.SetSiblingIndex(_tokenHomeSiblingIndices[tokenIndex]);
    }

    private void MoveTokenToChosen(int tokenIndex)
    {
        if (chosenTokensLayout == null || tokenButtons == null || tokenIndex < 0 || tokenIndex >= tokenButtons.Length)
            return;

        Button button = tokenButtons[tokenIndex];
        if (button == null)
            return;

        button.transform.SetParent(chosenTokensLayout, false);
    }

    private void RefreshConfirmButton()
    {
        if (confirmButton != null)
            confirmButton.interactable = _selectedTokenIndices.Count > 0;
    }

    private void ShowBodyWithTypewriter(string fullText)
    {
        StopTypewriter();

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        if (descriptionText == null || string.IsNullOrEmpty(fullText))
            return;

        _typewriterRoutine = StartCoroutine(RevealDescriptionByCharacter(fullText));
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
            yield return new WaitForSeconds(secondsPerCharacter);
        }

        _typewriterRoutine = null;
    }
}
