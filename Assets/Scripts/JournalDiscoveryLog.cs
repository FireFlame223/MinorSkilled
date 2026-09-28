using System.Collections.Generic;
using UnityEngine;

public class JournalDiscoveryLog : MonoBehaviour
{
    [Header("POI roster")]
    [Tooltip("Every POI that can appear in the journal, in any order. Discovery order sets page order.")]
    [SerializeField] private MapInteractable[] poiRoster = new MapInteractable[0];

    [Header("Title page")]
    [Tooltip("Optional instance already in the scene (under Page Container).")]
    [SerializeField] private JournalPageView existingTitlePage;

    [Tooltip("Spawned at runtime if no Existing Title Page is set.")]
    [SerializeField] private GameObject titlePagePrefab;

    [SerializeField] private string titlePageNumber = "0";

    [Header("Discovery pages")]
    [SerializeField] private GameObject pagePrefab;

    [Tooltip("Empty parent for spawned pages only. Do not put prev/next buttons here — they get deleted on play.")]
    [SerializeField] private Transform pageContainer;

    [SerializeField] private JournalUI journalUI;

    [Header("Copy")]
    [SerializeField] private string emptyPageTitle = "Empty";

    [SerializeField] private string emptyPageText = "Info coming soon...";

    [Tooltip("Discovery page numbers only (title uses Title Page Number). {0} = page number.")]
    [SerializeField] private string pageNumberFormat = "{0}";

    private JournalPageView _titlePageView;
    private readonly List<JournalPageView> _discoveryPageViews = new List<JournalPageView>();
    private readonly List<string> _discoveredJournalIds = new List<string>();

    public IReadOnlyList<string> DiscoveredJournalIdsInOrder => _discoveredJournalIds;

    private void Awake()
    {
        ValidateRoster();
        ClearGeneratedDiscoveryPages();
        EnsureTitlePage();
        AddPlaceholderPage();
        PushPagesToJournal(showPageIndex: 0);
    }

    public void RecordDiscovery(MapInteractable interactable)
    {
        if (interactable == null || !IsInRoster(interactable))
            return;

        string journalId = interactable.JournalId;
        if (_discoveredJournalIds.Contains(journalId))
            return;

        _discoveredJournalIds.Add(journalId);
        FillNextSlot(interactable);

        int undiscoveredCount = CountRosterEntries() - _discoveredJournalIds.Count;
        if (undiscoveredCount > 0)
            AddPlaceholderPage();

        int filledPageIndex = _discoveredJournalIds.Count;
        PushPagesToJournal(showPageIndex: filledPageIndex);
    }

    private void EnsureTitlePage()
    {
        if (_titlePageView != null)
            return;

        if (existingTitlePage != null)
        {
            _titlePageView = existingTitlePage;
        }
        else if (titlePagePrefab != null && pageContainer != null)
        {
            GameObject instance = Instantiate(titlePagePrefab, pageContainer);
            _titlePageView = instance.GetComponent<JournalPageView>();
            if (_titlePageView == null)
                Debug.LogError("JournalDiscoveryLog: title page prefab needs JournalPageView.", instance);
        }
        else
        {
            Debug.LogWarning("JournalDiscoveryLog: assign a title page prefab or existing title page.", this);
            return;
        }

        _titlePageView.ConfigureAsTitlePage(titlePageNumber);
        _titlePageView.transform.SetAsFirstSibling();
    }

    private void FillNextSlot(MapInteractable interactable)
    {
        if (_discoveryPageViews.Count > 0 && _discoveryPageViews[_discoveryPageViews.Count - 1].IsPlaceholder)
        {
            _discoveryPageViews[_discoveryPageViews.Count - 1].ShowEntry(
                interactable.JournalTitle,
                interactable.JournalDescription
            );
            return;
        }

        AddEntryPage(interactable);
    }

    private void AddEntryPage(MapInteractable interactable)
    {
        JournalPageView view = CreateDiscoveryPageView();
        if (view == null)
            return;

        view.ShowEntry(interactable.JournalTitle, interactable.JournalDescription);
        _discoveryPageViews.Add(view);
    }

    private void AddPlaceholderPage()
    {
        JournalPageView view = CreateDiscoveryPageView();
        if (view == null)
            return;

        view.ShowPlaceholder(emptyPageTitle, emptyPageText);
        _discoveryPageViews.Add(view);
    }

    private JournalPageView CreateDiscoveryPageView()
    {
        if (pagePrefab == null || pageContainer == null)
        {
            Debug.LogError("JournalDiscoveryLog: assign Page Prefab and Page Container.", this);
            return null;
        }

        GameObject instance = Instantiate(pagePrefab, pageContainer);
        JournalPageView view = instance.GetComponent<JournalPageView>();
        if (view == null)
            Debug.LogError("JournalDiscoveryLog: page prefab needs a JournalPageView component.", instance);

        return view;
    }

    private void PushPagesToJournal(int showPageIndex)
    {
        if (journalUI == null)
        {
            Debug.LogError("JournalDiscoveryLog: assign Journal UI.", this);
            return;
        }

        var pageObjects = new List<GameObject>();
        if (_titlePageView != null)
            pageObjects.Add(_titlePageView.gameObject);

        foreach (JournalPageView view in _discoveryPageViews)
        {
            if (view != null)
                pageObjects.Add(view.gameObject);
        }

        RefreshPageNumbers();
        journalUI.SetPages(pageObjects, showPageIndex);
    }

    private void RefreshPageNumbers()
    {
        if (_titlePageView != null)
            _titlePageView.SetPageNumber(titlePageNumber);

        for (int i = 0; i < _discoveryPageViews.Count; i++)
        {
            JournalPageView view = _discoveryPageViews[i];
            if (view == null)
                continue;

            int discoveryPageNumber = i + 1;
            string label = string.Format(pageNumberFormat, discoveryPageNumber);
            view.SetPageNumber(label);
        }
    }

    private void ClearGeneratedDiscoveryPages()
    {
        _discoveryPageViews.Clear();
        _discoveredJournalIds.Clear();
        _titlePageView = existingTitlePage;

        if (pageContainer == null)
            return;

        for (int i = pageContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = pageContainer.GetChild(i);
            JournalPageView view = child.GetComponent<JournalPageView>();
            if (view == null)
                continue;

            if (existingTitlePage != null && view == existingTitlePage)
                continue;

            Destroy(child.gameObject);
        }
    }

    private bool IsInRoster(MapInteractable interactable)
    {
        if (poiRoster == null)
            return false;

        foreach (MapInteractable entry in poiRoster)
        {
            if (entry == interactable)
                return true;
        }

        return false;
    }

    private int CountRosterEntries()
    {
        if (poiRoster == null)
            return 0;

        int count = 0;
        foreach (MapInteractable entry in poiRoster)
        {
            if (entry != null)
                count++;
        }

        return count;
    }

    private void ValidateRoster()
    {
        if (poiRoster == null)
            return;

        var seenIds = new HashSet<string>();
        foreach (MapInteractable entry in poiRoster)
        {
            if (entry == null)
                continue;

            string id = entry.JournalId;
            if (!seenIds.Add(id))
            {
                Debug.LogWarning(
                    $"JournalDiscoveryLog: duplicate journal id \"{id}\" in POI roster.",
                    entry
                );
            }
        }
    }
}
