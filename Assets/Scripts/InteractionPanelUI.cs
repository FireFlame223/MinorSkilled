using System.Collections;
using TMPro;
using UnityEngine;

public class InteractionPanelUI : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("Root object toggled on/off (e.g. your text box panel).")]
    [SerializeField] private GameObject panelRoot;

    [Header("Text")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Journal")]
    [SerializeField] private JournalUI journal;

    [Header("Typewriter")]
    [Tooltip("Delay after each character. Higher = slower typing.")]
    [Min(0.001f)]
    [SerializeField] private float secondsPerCharacter = 0.04f;

    private Coroutine _typewriterRoutine;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    public bool BlocksDismissal => journal != null && journal.IsOpen;

    private void Awake()
    {
        Hide();
    }

    public void Show(MapInteractable interactable)
    {
        if (interactable == null)
            return;

        if (journal != null)
            journal.Close();

        StopTypewriter();

        if (nameText != null)
            nameText.text = interactable.DisplayName;

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (descriptionText != null)
            _typewriterRoutine = StartCoroutine(RevealDescriptionByCharacter(interactable.Description));
    }

    public void Hide()
    {
        StopTypewriter();

        if (panelRoot != null)
            panelRoot.SetActive(false);
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
        if (string.IsNullOrEmpty(fullDescription))
            yield break;

        for (int i = 0; i < fullDescription.Length; i++)
        {
            descriptionText.text += fullDescription[i];
            yield return new WaitForSeconds(secondsPerCharacter);
        }

        _typewriterRoutine = null;
    }
}
