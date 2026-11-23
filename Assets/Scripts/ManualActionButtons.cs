using UnityEngine;

/// <summary>
/// Hides the three action buttons until the tutorial unlocks them, then fades each one in.
/// With no tutorial in the scene, the buttons stay as they are.
/// </summary>
public class ManualActionButtons : MonoBehaviour
{
    [SerializeField] private GameObject interactButton;
    [SerializeField] private GameObject journalButton;
    [SerializeField] private GameObject deductionButton;

    [Min(0f)]
    [SerializeField] private float fadeSeconds = 0.4f;

    private bool _followTutorial;

    private void Start()
    {
        TutorialDirector tutorial = TutorialDirector.Instance;
        if (tutorial == null)
            return;

        _followTutorial = true;
        Hide(interactButton);
        Hide(journalButton);
        Hide(deductionButton);
        tutorial.GatesChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (TutorialDirector.Instance != null)
            TutorialDirector.Instance.GatesChanged -= Refresh;
    }

    private void Refresh()
    {
        TutorialDirector tutorial = TutorialDirector.Instance;
        if (tutorial == null)
            return;

        if (tutorial.AllowsInteract)
            Show(interactButton);

        if (tutorial.AllowsJournal)
            Show(journalButton);

        if (tutorial.AllowsDeduction)
            Show(deductionButton);
    }

    private void Update()
    {
        if (!_followTutorial)
            return;

        Fade(interactButton);
        Fade(journalButton);
        Fade(deductionButton);
    }

    private static void Hide(GameObject button)
    {
        if (button == null)
            return;

        CanvasGroup group = Group(button);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        button.SetActive(false);
    }

    private static void Show(GameObject button)
    {
        if (button == null || button.activeSelf)
            return;

        CanvasGroup group = Group(button);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        button.SetActive(true);
    }

    private void Fade(GameObject button)
    {
        if (button == null || !button.activeSelf)
            return;

        CanvasGroup group = button.GetComponent<CanvasGroup>();
        if (group == null || group.alpha >= 1f)
            return;

        float step = fadeSeconds <= 0f ? 1f : Time.unscaledDeltaTime / fadeSeconds;
        group.alpha = Mathf.MoveTowards(group.alpha, 1f, step);
        bool shown = group.alpha >= 1f;
        group.interactable = shown;
        group.blocksRaycasts = shown;
    }

    private static CanvasGroup Group(GameObject button)
    {
        CanvasGroup group = button.GetComponent<CanvasGroup>();
        return group != null ? group : button.AddComponent<CanvasGroup>();
    }
}
