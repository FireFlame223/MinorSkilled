using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Selected/unselected look for a token button (color, scale, optional marker).
/// </summary>
[DisallowMultipleComponent]
public class InteractionTokenVisual : MonoBehaviour
{
    [SerializeField] private Image targetImage;

    [Tooltip("Optional checkmark, border, or glow shown while selected.")]
    [SerializeField] private GameObject selectedIndicator;

    [SerializeField] private Color normalColor = Color.white;

    [SerializeField] private Color selectedColor = new Color(1f, 0.92f, 0.55f, 1f);

    [SerializeField] private float normalScale = 1f;

    [SerializeField] private float selectedScale = 1.08f;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        ApplySelectedState(false);
    }

    public void SetSelected(bool selected)
    {
        ApplySelectedState(selected);
    }

    private void ApplySelectedState(bool selected)
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(selected);

        if (targetImage != null)
            targetImage.color = selected ? selectedColor : normalColor;

        transform.localScale = Vector3.one * (selected ? selectedScale : normalScale);
    }
}
