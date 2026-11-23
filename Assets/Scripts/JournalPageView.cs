using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One journal page: title, body, and page number text.
/// </summary>
public class JournalPageView : MonoBehaviour
{
    [Header("Page Content")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text pageNumberText;

    private void OnEnable()
    {
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.GetComponentInParent<Selectable>(true) != null)
                continue;

            graphic.raycastTarget = false;
        }
    }

    public void SetTitle(string title)
    {
        if (titleText != null)
            titleText.text = title;
    }

    public void SetPageNumber(string label)
    {
        if (pageNumberText != null)
            pageNumberText.text = label;
    }

    public void ConfigureAsTitlePage(string pageNumberLabel)
    {
        SetPageNumber(pageNumberLabel);
    }

    public void ShowPlaceholder(string title, string placeholderText)
    {
        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
            bodyText.text = placeholderText;
    }

    public void ShowEntry(string title, string body)
    {
        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
            bodyText.text = body;
    }
}
