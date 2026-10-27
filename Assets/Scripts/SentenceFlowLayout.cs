using UnityEngine;

/// <summary>
/// Places sentence words and gap dropdowns left to right, wrapping to the next line.
/// Child widths are left as they are, so a dropdown stays the size set on its template.
/// </summary>
public class SentenceFlowLayout : MonoBehaviour
{
    [Tooltip("Space between words and dropdowns.")]
    [SerializeField] private float spacing = 10f;

    [Tooltip("Distance from one line to the next.")]
    [SerializeField] private float lineHeight = 60f;

    [Tooltip("Put each word and each dropdown on its own row.")]
    [SerializeField] private bool oneColumn;

    public void Reflow()
    {
        RectTransform root = transform as RectTransform;
        if (root == null)
            return;

        float maxWidth = root.rect.width;
        if (maxWidth < 10f)
            maxWidth = 1600f;

        float x = 0f;
        float y = 0f;
        bool placedAny = false;

        for (int i = 0; i < root.childCount; i++)
        {
            RectTransform child = root.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf)
                continue;

            float width = child.sizeDelta.x;
            float height = child.sizeDelta.y > 1f ? child.sizeDelta.y : lineHeight;

            if (placedAny && (oneColumn || x + width > maxWidth))
            {
                x = 0f;
                y -= lineHeight;
            }

            child.anchorMin = new Vector2(0f, 1f);
            child.anchorMax = new Vector2(0f, 1f);
            child.pivot = new Vector2(0f, 1f);
            child.anchoredPosition = new Vector2(x, y);
            child.sizeDelta = new Vector2(width, height);

            x += width + spacing;
            placedAny = true;
        }
    }
}
