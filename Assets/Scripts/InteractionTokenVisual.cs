using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Selected/unselected and locked look for a token button (color, scale, optional marker).
/// </summary>
[DisallowMultipleComponent]
public class InteractionTokenVisual : MonoBehaviour
{
    [SerializeField] private Image targetImage;

    [Tooltip("Optional checkmark, border, or glow shown while selected.")]
    [SerializeField] private GameObject selectedIndicator;

    [SerializeField] private Color normalColor = Color.white;

    [SerializeField] private Color selectedColor = new Color(1f, 0.92f, 0.55f, 1f);

    [SerializeField] private Color lockedColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);

    [SerializeField] private float normalScale = 1f;

    [SerializeField] private float selectedScale = 1.08f;

    private bool _selected;
    private bool _locked;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        ApplyVisual();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        ApplyVisual();
    }

    public void SetLocked(bool locked)
    {
        _locked = locked;
        if (_locked)
            _selected = false;

        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (_locked)
        {
            if (selectedIndicator != null)
                selectedIndicator.SetActive(false);

            if (targetImage != null)
                targetImage.color = lockedColor;

            transform.localScale = Vector3.one * normalScale;
            return;
        }

        if (selectedIndicator != null)
            selectedIndicator.SetActive(_selected);

        if (targetImage != null)
            targetImage.color = _selected ? selectedColor : normalColor;

        transform.localScale = Vector3.one * (_selected ? selectedScale : normalScale);
    }
}
