using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum JournalRosterEntryKind
{
    Poi,
    Location
}

/// <summary>One journal page. A row is either a single POI or a location page that collects clues.</summary>
[Serializable]
public class JournalRosterEntry
{
    [SerializeField] private JournalRosterEntryKind kind;

    [SerializeField] private MapInteractable poi;

    [Tooltip("Page title. Each LocationClue filed here must use this same text in its Location Page field.")]
    [SerializeField] private string locationPage;

    public JournalRosterEntryKind Kind => kind;

    public MapInteractable Poi => poi;

    public string LocationPage => locationPage;

    public JournalRosterEntry()
    {
    }

    public JournalRosterEntry(MapInteractable source)
    {
        kind = JournalRosterEntryKind.Poi;
        poi = source;
    }
}

/// <summary>
/// One journal page per roster row. POI rows gain bullets from that POI. Location rows gain a bullet each time a clue for that page is read.
/// </summary>
public class JournalDiscoveryLog : MonoBehaviour
{
    [Header("Roster")]
    [Tooltip("Journal order. Each row is one POI page or one location page. Location page text must match the Location Page field on each clue.")]
    [SerializeField] private JournalRosterEntry[] roster = new JournalRosterEntry[0];

    [SerializeField, HideInInspector] private MapInteractable[] poiRoster = new MapInteractable[0];

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
    private readonly List<JournalPageView> _rosterPageViews = new List<JournalPageView>();
    private readonly Dictionary<MapInteractable, JournalPageView> _pageByPoi = new Dictionary<MapInteractable, JournalPageView>();
    private readonly Dictionary<string, JournalPageView> _pageByLocation = new Dictionary<string, JournalPageView>();
    private readonly Dictionary<string, string> _locationTitleByKey = new Dictionary<string, string>();
    private readonly Dictionary<string, List<string>> _bulletsByJournalId = new Dictionary<string, List<string>>();
    private readonly HashSet<string> _discoveredPoiIntroIds = new HashSet<string>();
    private readonly HashSet<string> _discoveredTokenOutcomeKeys = new HashSet<string>();
    private readonly HashSet<string> _discoveredLocationKeys = new HashSet<string>();
    private readonly HashSet<LocationClue> _recordedClues = new HashSet<LocationClue>();

    private void OnValidate()
    {
        MigrateLegacyRoster();
    }

    private void Awake()
    {
        MigrateLegacyRoster();
        ValidateRoster();
        ClearGeneratedDiscoveryPages();
        EnsureTitlePage();
        CreateRosterPages();
        WarnAboutUnassignedClues();
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

        if (!string.IsNullOrWhiteSpace(interactable.JournalDescription))
            AppendBullet(journalId, interactable.JournalDescription);

        ApplyPageContent(interactable, page);
        PushPagesToJournal();
        FocusUpdatedPage(page);
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
        FocusUpdatedPage(page);
    }

    /// <summary>First time this clue is read: append its journal line to its location page, in find order.</summary>
    public void RecordLocationClue(LocationClue clue)
    {
        if (clue == null || _recordedClues.Contains(clue))
            return;

        string key = NormalizeLocation(clue.LocationPage);
        if (string.IsNullOrEmpty(key) || !_pageByLocation.TryGetValue(key, out JournalPageView page))
        {
            Debug.LogWarning(
                $"JournalDiscoveryLog: \"{clue.name}\" uses location page \"{clue.LocationPage}\", which is not in the roster.",
                clue
            );
            return;
        }

        _recordedClues.Add(clue);
        _discoveredLocationKeys.Add(key);

        if (!string.IsNullOrWhiteSpace(clue.JournalLine))
            AppendBullet(LocationBulletKey(key), clue.JournalLine);

        ApplyLocationPageContent(key, page);
        PushPagesToJournal();
        FocusUpdatedPage(page);
    }

    private void FocusUpdatedPage(JournalPageView page)
    {
        if (journalUI != null && page != null)
            journalUI.ShowPageOnNextOpen(page.gameObject);
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

    private void ApplyLocationPageContent(string locationKey, JournalPageView page)
    {
        if (page == null || !_discoveredLocationKeys.Contains(locationKey))
            return;

        string title = _locationTitleByKey.TryGetValue(locationKey, out string rosterTitle)
            ? rosterTitle
            : locationKey;
        string body = FormatBullets(_bulletsByJournalId, LocationBulletKey(locationKey));
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
        _rosterPageViews.Clear();
        _pageByPoi.Clear();
        _pageByLocation.Clear();
        _locationTitleByKey.Clear();

        if (roster == null)
            return;

        foreach (JournalRosterEntry entry in roster)
        {
            if (entry == null)
                continue;

            if (entry.Kind == JournalRosterEntryKind.Location)
            {
                CreateLocationPage(entry);
                continue;
            }

            CreatePoiPage(entry);
        }
    }

    private void CreatePoiPage(JournalRosterEntry entry)
    {
        MapInteractable poi = entry.Poi;
        if (poi == null)
            return;

        JournalPageView view = CreateDiscoveryPageView();
        if (view == null)
            return;

        view.ShowPlaceholder(emptyPageTitle, emptyPageText);
        _rosterPageViews.Add(view);
        _pageByPoi[poi] = view;
    }

    private void CreateLocationPage(JournalRosterEntry entry)
    {
        string key = NormalizeLocation(entry.LocationPage);
        if (string.IsNullOrEmpty(key))
            return;

        if (_pageByLocation.ContainsKey(key))
            return;

        JournalPageView view = CreateDiscoveryPageView();
        if (view == null)
            return;

        view.ShowPlaceholder(emptyPageTitle, emptyPageText);
        _rosterPageViews.Add(view);
        _pageByLocation[key] = view;
        _locationTitleByKey[key] = entry.LocationPage.Trim();
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

        foreach (JournalPageView view in _rosterPageViews)
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

        for (int i = 0; i < _rosterPageViews.Count; i++)
        {
            JournalPageView view = _rosterPageViews[i];
            if (view == null)
                continue;

            int discoveryPageNumber = i + 1;
            string label = string.Format(pageNumberFormat, discoveryPageNumber);
            view.SetPageNumber(label);
        }
    }

    private void ClearGeneratedDiscoveryPages()
    {
        _rosterPageViews.Clear();
        _pageByPoi.Clear();
        _pageByLocation.Clear();
        _locationTitleByKey.Clear();
        _bulletsByJournalId.Clear();
        _discoveredPoiIntroIds.Clear();
        _discoveredTokenOutcomeKeys.Clear();
        _discoveredLocationKeys.Clear();
        _recordedClues.Clear();
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

    private void MigrateLegacyRoster()
    {
        if (roster != null && roster.Length > 0)
            return;

        if (poiRoster == null || poiRoster.Length == 0)
            return;

        var migrated = new List<JournalRosterEntry>();
        foreach (MapInteractable poi in poiRoster)
        {
            if (poi == null)
                continue;

            migrated.Add(new JournalRosterEntry(poi));
        }

        if (migrated.Count == 0)
            return;

        roster = migrated.ToArray();
        poiRoster = new MapInteractable[0];
    }

    private void ValidateRoster()
    {
        if (roster == null)
            return;

        var seenPoiIds = new HashSet<string>();
        var seenLocations = new HashSet<string>();

        foreach (JournalRosterEntry entry in roster)
        {
            if (entry == null)
                continue;

            if (entry.Kind == JournalRosterEntryKind.Location)
            {
                string key = NormalizeLocation(entry.LocationPage);
                if (string.IsNullOrEmpty(key))
                {
                    Debug.LogWarning("JournalDiscoveryLog: a location row has no page name.", this);
                    continue;
                }

                if (!seenLocations.Add(key))
                {
                    Debug.LogWarning(
                        $"JournalDiscoveryLog: duplicate location page \"{entry.LocationPage.Trim()}\" in the roster.",
                        this
                    );
                }

                continue;
            }

            MapInteractable poi = entry.Poi;
            if (poi == null)
                continue;

            string id = poi.JournalId;
            if (!seenPoiIds.Add(id))
            {
                Debug.LogWarning(
                    $"JournalDiscoveryLog: duplicate journal id \"{id}\" in the roster.",
                    poi
                );
            }
        }
    }

    private void WarnAboutUnassignedClues()
    {
        LocationClue[] clues = FindObjectsByType<LocationClue>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (LocationClue clue in clues)
        {
            if (clue == null)
                continue;

            string key = NormalizeLocation(clue.LocationPage);
            if (!string.IsNullOrEmpty(key) && _pageByLocation.ContainsKey(key))
                continue;

            Debug.LogWarning(
                $"JournalDiscoveryLog: \"{clue.name}\" uses location page \"{clue.LocationPage}\", which is not in the roster.",
                clue
            );
        }
    }

    private static string NormalizeLocation(string locationPage)
    {
        return string.IsNullOrWhiteSpace(locationPage)
            ? string.Empty
            : locationPage.Trim().ToLowerInvariant();
    }

    private static string LocationBulletKey(string locationKey)
    {
        return "location:" + locationKey;
    }
}
