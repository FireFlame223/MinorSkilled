using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JournalPageView : MonoBehaviour
{
    [Header("Page Content")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text pageNumberText;

    public bool IsPlaceholder { get; private set; }

    private void OnEnable()
    {
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
    }

    public void SetPageNumber(string label)
    {
        if (pageNumberText != null)
            pageNumberText.text = label;
    }

    public void ConfigureAsTitlePage(string pageNumberLabel)
    {
        IsPlaceholder = false;
        SetPageNumber(pageNumberLabel);
    }

    public void ShowPlaceholder(string title, string placeholderText)
    {
        IsPlaceholder = true;

        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
            bodyText.text = placeholderText;
    }

    public void ShowEntry(string title, string body)
    {
        IsPlaceholder = false;

        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
            bodyText.text = body;
    }
}
