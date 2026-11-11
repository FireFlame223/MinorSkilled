using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Journal shell: Tab toggles slide in/out; pages and prev/next are driven by <see cref="JournalDiscoveryLog"/>.
/// </summary>
public class JournalUI : MonoBehaviour
{
    [Header("Slide")]
    [SerializeField] private RectTransform slideRoot;

    [SerializeField] private Vector2 hiddenOffset = new Vector2(2400f, 0f);

    [Min(1f)]
    [SerializeField] private float moveSpeed = 6000f;

    [Header("Navigation")]
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button nextPageButton;

    private readonly List<GameObject> _pages = new List<GameObject>();

    private Vector2 _openAnchoredPosition;
    private Vector2 _closedAnchoredPosition;
    private Vector2 _moveTarget;
    private int _currentPageIndex;
    private int _openOnPageIndex = -1;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (slideRoot == null)
        {
            Debug.LogError("JournalUI: assign Slide Root.", this);
            enabled = false;
            return;
        }

        _openAnchoredPosition = slideRoot.anchoredPosition;
        _closedAnchoredPosition = _openAnchoredPosition + hiddenOffset;

        if (previousPageButton != null)
            previousPageButton.onClick.AddListener(ShowPreviousPage);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(ShowNextPage);
    }

    private void Start()
    {
        IsOpen = false;
        _moveTarget = _closedAnchoredPosition;
        slideRoot.anchoredPosition = _closedAnchoredPosition;
    }

    private void OnDestroy()
    {
        if (previousPageButton != null)
            previousPageButton.onClick.RemoveListener(ShowPreviousPage);

        if (nextPageButton != null)
            nextPageButton.onClick.RemoveListener(ShowNextPage);
    }

    private void Update()
    {
        if (!GameplayInput.IsBlocked)
        {
            if (IsOpen && GameplayInput.TryConsumeEscape())
                Close();

            if (WasTabPressed())
            {
                TutorialDirector tutorial = TutorialDirector.Instance;
                if (tutorial == null || tutorial.AllowsJournal)
                {
                    tutorial?.NotifyJournalTabPressed();

                    if (IsOpen)
                        Close();
                    else
                        Open();
                }
            }

            if (IsOpen)
            {
                if (WasPreviousPagePressed())
                    ShowPreviousPage();

                if (WasNextPagePressed())
                    ShowNextPage();
            }
        }

        if (slideRoot == null)
            return;

        slideRoot.anchoredPosition = Vector2.MoveTowards(
            slideRoot.anchoredPosition,
            _moveTarget,
            moveSpeed * Time.unscaledDeltaTime
        );
    }

    public void SetPages(IReadOnlyList<GameObject> pages, int showPageIndex)
    {
        _pages.Clear();

        if (pages != null)
        {
            foreach (GameObject page in pages)
            {
                if (page != null)
                    _pages.Add(page);
            }
        }

        ShowPage(showPageIndex);
    }

    /// <summary>Updates page list without jumping to a new page (keeps current index clamped).</summary>
    public void RefreshPages(IReadOnlyList<GameObject> pages)
    {
        int index = _currentPageIndex;
        SetPages(pages, index);
    }

    /// <summary>
    /// Next time the journal opens, show this page. A later update replaces it until the journal is opened.
    /// If the journal is already open, show the page immediately.
    /// </summary>
    public void ShowPageOnNextOpen(GameObject page)
    {
        if (page == null)
            return;

        int index = _pages.IndexOf(page);
        if (index < 0)
            return;

        if (IsOpen)
        {
            ShowPage(index);
            _openOnPageIndex = -1;
            return;
        }

        _openOnPageIndex = index;
    }

    public void Open()
    {
        if (_openOnPageIndex >= 0)
        {
            ShowPage(_openOnPageIndex);
            _openOnPageIndex = -1;
        }

        IsOpen = true;
        _moveTarget = _openAnchoredPosition;
    }

    public void Close()
    {
        IsOpen = false;
        _moveTarget = _closedAnchoredPosition;
    }

    public void ShowPreviousPage()
    {
        if (_currentPageIndex <= 0)
            return;

        ShowPage(_currentPageIndex - 1);
    }

    public void ShowNextPage()
    {
        if (_currentPageIndex >= _pages.Count - 1)
            return;

        ShowPage(_currentPageIndex + 1);
    }

    private void ShowPage(int index)
    {
        if (_pages.Count == 0)
        {
            _currentPageIndex = 0;
            RefreshPageButtons();
            return;
        }

        index = Mathf.Clamp(index, 0, _pages.Count - 1);
        _currentPageIndex = index;

        for (int i = 0; i < _pages.Count; i++)
            _pages[i].SetActive(i == index);

        RefreshPageButtons();
    }

    private void RefreshPageButtons()
    {
        int lastIndex = Mathf.Max(0, _pages.Count - 1);

        if (previousPageButton != null)
            previousPageButton.gameObject.SetActive(_currentPageIndex > 0);

        if (nextPageButton != null)
            nextPageButton.gameObject.SetActive(_currentPageIndex < lastIndex);

        BringNavigationToFront();
    }

    private void BringNavigationToFront()
    {
        if (previousPageButton != null)
        {
            Graphic target = previousPageButton.targetGraphic;
            if (target != null)
                target.raycastTarget = true;

            previousPageButton.transform.SetAsLastSibling();
        }

        if (nextPageButton != null)
        {
            Graphic target = nextPageButton.targetGraphic;
            if (target != null)
                target.raycastTarget = true;

            nextPageButton.transform.SetAsLastSibling();
        }
    }

    private bool WasTabPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.tabKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    private bool WasPreviousPagePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame);
#else
        return false;
#endif
    }

    private bool WasNextPagePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame);
#else
        return false;
#endif
    }
}
