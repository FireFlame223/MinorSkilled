using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// One journal page per roster POI; first visit and token outcomes append bullet lines to that page's body.
/// </summary>
public class JournalDiscoveryLog : MonoBehaviour
{
    [Header("POI roster")]
    [Tooltip("One journal page per entry, in roster order.")]
    [SerializeField] private MapInteractable[] poiRoster = new MapInteractable[0];

    [Header("Title page")]
    [SerializeField] private JournalPageView existingTitlePage;

    [SerializeField] private GameObject titlePagePrefab;

    [SerializeField] private string titlePageNumber = "0";

    [Header("Discovery pages")]
    [SerializeField] private GameObject pagePrefab;

    [Tooltip("Parent for spawned pages only — not prev/next buttons.")]
    [SerializeField] private Transform pageContainer;

    [SerializeField] private JournalUI journalUI;

    [Header("Copy")]
    [SerializeField] private string emptyPageTitle = "Empty";

    [SerializeField] private string emptyPageText = "Info coming soon...";

    [Tooltip("Discovery page numbers only. {0} = page number.")]
    [SerializeField] private string pageNumberFormat = "{0}";

    private JournalPageView _titlePageView;
    private readonly List<JournalPageView> _poiPageViews = new List<JournalPageView>();
    private readonly Dictionary<MapInteractable, JournalPageView> _pageByPoi = new Dictionary<MapInteractable, JournalPageView>();
    private readonly Dictionary<string, List<string>> _bulletsByJournalId = new Dictionary<string, List<string>>();
    private readonly HashSet<string> _discoveredPoiIntroIds = new HashSet<string>();
    private readonly List<string> _discoveredPoiIntroOrder = new List<string>();
    private readonly HashSet<string> _discoveredTokenOutcomeKeys = new HashSet<string>();

    public IReadOnlyList<string> DiscoveredJournalIdsInOrder => _discoveredPoiIntroOrder;

    private void Awake()
    {
        ValidateRoster();
        ClearGeneratedDiscoveryPages();
        EnsureTitlePage();
        CreateRosterPages();
        PushPagesToJournal();
    }

    /// <summary>First E interaction: first bullet from journal intro fields.</summary>
    public void RecordDiscovery(MapInteractable interactable)
    {
        if (interactable == null || !TryGetPoiPage(interactable, out JournalPageView page))
            return;

        string journalId = interactable.JournalId;
        if (_discoveredPoiIntroIds.Contains(journalId))
            return;

        _discoveredPoiIntroIds.Add(journalId);
        _discoveredPoiIntroOrder.Add(journalId);

        if (!string.IsNullOrWhiteSpace(interactable.JournalDescription))
            AppendBullet(journalId, interactable.JournalDescription);

        ApplyPageContent(interactable, page);
        PushPagesToJournal();
    }

    /// <summary>First time this token combo is confirmed: append that outcome's journal bullet line.</summary>
    public void RecordTokenOutcome(MapInteractable interactable, IReadOnlyList<int> tokenIndices)
    {
        if (interactable == null || !TryGetPoiPage(interactable, out JournalPageView page))
            return;

        string journalId = interactable.JournalId;
        if (!_discoveredPoiIntroIds.Contains(journalId))
            return;

        if (!interactable.TryGetTokenChoiceJournalBullet(tokenIndices, out string bulletLine))
            return;

        string outcomeKey = interactable.GetTokenChoiceJournalKey(tokenIndices);
        if (string.IsNullOrEmpty(outcomeKey) || _discoveredTokenOutcomeKeys.Contains(outcomeKey))
            return;

        _discoveredTokenOutcomeKeys.Add(outcomeKey);
        AppendBullet(journalId, bulletLine);
        ApplyPageContent(interactable, page);
        PushPagesToJournal();
    }

    private void AppendBullet(string journalId, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        if (!_bulletsByJournalId.TryGetValue(journalId, out List<string> bullets))
        {
            bullets = new List<string>();
            _bulletsByJournalId[journalId] = bullets;
        }

        bullets.Add(line.Trim());
    }

    private void ApplyPageContent(MapInteractable interactable, JournalPageView page)
    {
        if (page == null || interactable == null)
            return;

        string journalId = interactable.JournalId;
        if (!_discoveredPoiIntroIds.Contains(journalId))
            return;

        string title = interactable.JournalTitle;
        string body = FormatBullets(_bulletsByJournalId, journalId);
        page.ShowEntry(title, body);
    }

    private static string FormatBullets(Dictionary<string, List<string>> bulletsByJournalId, string journalId)
    {
        if (!bulletsByJournalId.TryGetValue(journalId, out List<string> bullets) || bullets.Count == 0)
            return string.Empty;

        var builder = new StringBuilder();
        for (int i = 0; i < bullets.Count; i++)
        {
            if (i > 0)
                builder.Append('\n');

            builder.Append("- ");
            builder.Append(bullets[i]);
        }

        return builder.ToString();
    }

    private void CreateRosterPages()
    {
        _poiPageViews.Clear();
        _pageByPoi.Clear();

        if (poiRoster == null)
            return;

        foreach (MapInteractable poi in poiRoster)
        {
            if (poi == null)
                continue;

            JournalPageView view = CreateDiscoveryPageView();
            if (view == null)
                continue;

            view.ShowPlaceholder(emptyPageTitle, emptyPageText);
            _poiPageViews.Add(view);
            _pageByPoi[poi] = view;
        }
    }

    private bool TryGetPoiPage(MapInteractable interactable, out JournalPageView page)
    {
        page = null;
        return interactable != null && _pageByPoi.TryGetValue(interactable, out page);
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

    private void PushPagesToJournal()
    {
        if (journalUI == null)
        {
            Debug.LogError("JournalDiscoveryLog: assign Journal UI.", this);
            return;
        }

        var pageObjects = new List<GameObject>();
        if (_titlePageView != null)
            pageObjects.Add(_titlePageView.gameObject);

        foreach (JournalPageView view in _poiPageViews)
        {
            if (view != null)
                pageObjects.Add(view.gameObject);
        }

        RefreshPageNumbers();
        journalUI.RefreshPages(pageObjects);
    }

    private void RefreshPageNumbers()
    {
        if (_titlePageView != null)
            _titlePageView.SetPageNumber(titlePageNumber);

        for (int i = 0; i < _poiPageViews.Count; i++)
        {
            JournalPageView view = _poiPageViews[i];
            if (view == null)
                continue;

            int discoveryPageNumber = i + 1;
            string label = string.Format(pageNumberFormat, discoveryPageNumber);
            view.SetPageNumber(label);
        }
    }

    private void ClearGeneratedDiscoveryPages()
    {
        _poiPageViews.Clear();
        _pageByPoi.Clear();
        _bulletsByJournalId.Clear();
        _discoveredPoiIntroIds.Clear();
        _discoveredPoiIntroOrder.Clear();
        _discoveredTokenOutcomeKeys.Clear();
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
