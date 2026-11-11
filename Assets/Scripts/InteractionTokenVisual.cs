using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Selected/unselected and locked look for a token button (sprite, scale, optional marker).
/// </summary>
[DisallowMultipleComponent]
public class InteractionTokenVisual : MonoBehaviour
{
    [SerializeField] private Image targetImage;

    [Tooltip("Sprite shown while this token is not selected.")]
    [SerializeField] private Sprite inactiveSprite;

    [Tooltip("Sprite shown while this token is selected.")]
    [SerializeField] private Sprite selectedSprite;

    [Tooltip("Optional checkmark, border, or glow shown while selected.")]
    [SerializeField] private GameObject selectedIndicator;

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

            ApplySprite(inactiveSprite);

            if (targetImage != null)
                targetImage.color = lockedColor;

            transform.localScale = Vector3.one * normalScale;
            return;
        }

        if (selectedIndicator != null)
            selectedIndicator.SetActive(_selected);

        ApplySprite(_selected ? selectedSprite : inactiveSprite);

        if (targetImage != null)
            targetImage.color = Color.white;

        transform.localScale = Vector3.one * (_selected ? selectedScale : normalScale);
    }

    private void ApplySprite(Sprite sprite)
    {
        if (targetImage == null || sprite == null)
            return;

        targetImage.sprite = sprite;
    }
}
