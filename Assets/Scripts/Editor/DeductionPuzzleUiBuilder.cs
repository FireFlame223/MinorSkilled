using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates the deduction UI under the scene Canvas the first time the scripts compile.
/// After that, edit the objects under Canvas / Deduction UI.
/// </summary>
[InitializeOnLoad]
static class DeductionPuzzleUiBuilder
{
    const string MenuPath = "Tools/Build Deduction UI";

    static DeductionPuzzleUiBuilder()
    {
        EditorApplication.delayCall += EnsureInOpenScene;
        EditorSceneManager.sceneOpened += (_, _) => EditorApplication.delayCall += EnsureInOpenScene;
    }

    [MenuItem(MenuPath)]
    static void BuildFromMenu()
    {
        DeductionPuzzlePanel panel = Object.FindFirstObjectByType<DeductionPuzzlePanel>();
        if (panel == null)
        {
            Debug.LogError("Build Deduction UI: the scene needs a Deduction Puzzles object.");
            return;
        }

        if (panel.HasSceneUi && !EditorUtility.DisplayDialog(
                "Deduction UI",
                "The deduction UI is already in the scene. Build it again? The current Deduction UI object will be replaced.",
                "Build again",
                "Cancel"))
        {
            return;
        }

        Build(panel, replaceExisting: true);
    }

    static void EnsureInOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        DeductionPuzzlePanel panel = Object.FindFirstObjectByType<DeductionPuzzlePanel>();
        if (panel == null || panel.HasSceneUi)
            return;

        Build(panel, replaceExisting: false);
    }

    static void Build(DeductionPuzzlePanel panel, bool replaceExisting)
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Build Deduction UI: the scene needs a Canvas.");
            return;
        }

        Transform existing = canvas.transform.Find("Deduction UI");
        if (existing != null && replaceExisting)
            Object.DestroyImmediate(existing.gameObject);
        else if (existing != null)
            return;

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/KIWISODA SDF.asset");
        GameObject root = CreateUi(canvas.transform, font, out UiRefs refs);

        Undo.RegisterCreatedObjectUndo(root, "Build Deduction UI");

        SerializedObject serialized = new SerializedObject(panel);
        string credits = serialized.FindProperty("creditsText").stringValue;
        if (!string.IsNullOrEmpty(credits))
            refs.Credits.text = credits;

        string notification = refs.Notification.text;
        refs.Notification.text = string.IsNullOrEmpty(notification)
            ? "you unlocked a new token '{0}'"
            : notification;

        Assign(serialized, "uiRoot", root);
        Assign(serialized, "panelRect", refs.Panel);
        Assign(serialized, "sentenceRoot", refs.Sentence);
        Assign(serialized, "wordTemplate", refs.WordTemplate);
        Assign(serialized, "gapTemplate", refs.GapTemplate);
        Assign(serialized, "confirmButton", refs.Confirm);
        Assign(serialized, "notificationLabel", refs.Notification);
        Assign(serialized, "notificationGroup", refs.NotificationGroup);
        Assign(serialized, "endingGroup", refs.EndingGroup);
        Assign(serialized, "creditsLabel", refs.Credits);
        Assign(serialized, "exitButton", refs.Exit);
        Assign(serialized, "exitGroup", refs.ExitGroup);
        serialized.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
    }

    static void Assign(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    static GameObject CreateUi(Transform canvas, TMP_FontAsset font, out UiRefs refs)
    {
        var root = new GameObject("Deduction UI", typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        Stretch(root.GetComponent<RectTransform>());
        root.transform.SetAsLastSibling();

        RectTransform panel = CreatePanel(root.transform);
        RectTransform sentence = CreateSentence(panel);
        Button confirm = CreateButton(panel, "Confirm", "Confirm", font, new Vector2(0f, 28f), new Vector2(220f, 48f), new Color(0.93f, 0.78f, 0.4f, 1f), new Color(0.12f, 0.08f, 0.05f, 1f));

        var templates = new GameObject("Templates", typeof(RectTransform));
        templates.transform.SetParent(root.transform, false);

        TMP_Text word = CreateText(templates.transform, "Word Template", "Word", 32, TextAlignmentOptions.MidlineLeft, font, Paper);
        word.rectTransform.sizeDelta = new Vector2(120f, 48f);
        word.raycastTarget = false;
        word.textWrappingMode = TextWrappingModes.NoWrap;
        word.overflowMode = TextOverflowModes.Overflow;

        TMP_Dropdown gap = CreateGapTemplate(templates.transform, font);
        word.gameObject.SetActive(false);
        gap.gameObject.SetActive(false);

        TMP_Text notification = CreateText(root.transform, "Token Notification", "you unlocked a new token '{0}'", 34, TextAlignmentOptions.Center, font, Paper);
        RectTransform notificationRect = notification.rectTransform;
        notificationRect.anchorMin = new Vector2(0.5f, 1f);
        notificationRect.anchorMax = new Vector2(0.5f, 1f);
        notificationRect.pivot = new Vector2(0.5f, 1f);
        notificationRect.sizeDelta = new Vector2(1100f, 80f);
        notificationRect.anchoredPosition = new Vector2(0f, -36f);
        notification.raycastTarget = false;
        CanvasGroup notificationGroup = notification.gameObject.AddComponent<CanvasGroup>();
        notificationGroup.alpha = 0f;
        notificationGroup.blocksRaycasts = false;
        notificationGroup.interactable = false;

        CreateEnding(root.transform, font, out CanvasGroup endingGroup, out TMP_Text credits, out Button exit, out CanvasGroup exitGroup);

        refs = new UiRefs
        {
            Panel = panel,
            Sentence = sentence,
            WordTemplate = word,
            GapTemplate = gap,
            Confirm = confirm,
            Notification = notification,
            NotificationGroup = notificationGroup,
            EndingGroup = endingGroup,
            Credits = credits,
            Exit = exit,
            ExitGroup = exitGroup
        };
        return root;
    }

    static RectTransform CreatePanel(Transform parent)
    {
        var panel = new GameObject("Hypothesis Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(0f, 340f);
        rect.anchoredPosition = Vector2.zero;
        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.1f, 0.08f, 0.07f, 0.96f);
        ApplySprite(image);
        return rect;
    }

    static RectTransform CreateSentence(RectTransform panel)
    {
        var sentence = new GameObject("Sentence", typeof(RectTransform), typeof(SentenceFlowLayout));
        sentence.transform.SetParent(panel, false);
        RectTransform rect = sentence.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(48f, 96f);
        rect.offsetMax = new Vector2(-48f, -28f);
        return rect;
    }

    static TMP_Dropdown CreateGapTemplate(Transform parent, TMP_FontAsset font)
    {
        var root = new GameObject("Gap Template", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(300f, 48f);
        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.minWidth = 300f;
        layout.preferredWidth = 300f;
        layout.minHeight = 48f;
        Image background = root.GetComponent<Image>();
        background.color = new Color(0.18f, 0.15f, 0.12f, 1f);
        ApplySprite(background);

        TMP_Text caption = CreateText(root.transform, "Label", string.Empty, 26, TextAlignmentOptions.MidlineLeft, font, Paper);
        Stretch(caption.rectTransform, new Vector2(12f, 2f), new Vector2(-28f, -2f));
        caption.raycastTarget = false;
        caption.textWrappingMode = TextWrappingModes.NoWrap;
        caption.overflowMode = TextOverflowModes.Overflow;

        TMP_Text arrow = CreateText(root.transform, "Arrow", "v", 22, TextAlignmentOptions.Center, font, Paper);
        RectTransform arrowRect = arrow.rectTransform;
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.sizeDelta = new Vector2(24f, 24f);
        arrowRect.anchoredPosition = new Vector2(-6f, 0f);
        arrow.raycastTarget = false;

        RectTransform template = CreateDropdownTemplate(root.transform, font, out TMP_Text itemLabel);

        TMP_Dropdown dropdown = root.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = background;
        dropdown.template = template;
        dropdown.captionText = caption;
        dropdown.itemText = itemLabel;
        dropdown.navigation = new Navigation { mode = Navigation.Mode.None };
        template.gameObject.SetActive(false);
        return dropdown;
    }

    static RectTransform CreateDropdownTemplate(Transform parent, TMP_FontAsset font, out TMP_Text itemLabel)
    {
        var template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        template.transform.SetParent(parent, false);
        RectTransform templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.sizeDelta = new Vector2(0f, 280f);
        Image templateImage = template.GetComponent<Image>();
        templateImage.color = new Color(0.18f, 0.15f, 0.12f, 1f);
        ApplySprite(templateImage);

        ScrollRect scroll = template.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(template.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = Color.white;
        ApplySprite(viewportImage);
        viewport.GetComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = viewport.GetComponent<RectTransform>();

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 36f);
        scroll.content = contentRect;

        var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
        item.transform.SetParent(content.transform, false);
        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 0.5f);
        itemRect.anchorMax = new Vector2(1f, 0.5f);
        itemRect.sizeDelta = new Vector2(0f, 36f);

        var itemBackgroundObject = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
        itemBackgroundObject.transform.SetParent(item.transform, false);
        Stretch(itemBackgroundObject.GetComponent<RectTransform>());
        Image itemBackground = itemBackgroundObject.GetComponent<Image>();
        itemBackground.color = new Color(0.24f, 0.2f, 0.16f, 1f);
        ApplySprite(itemBackground);

        var checkObject = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
        checkObject.transform.SetParent(item.transform, false);
        RectTransform checkRect = checkObject.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0f, 0.5f);
        checkRect.anchorMax = new Vector2(0f, 0.5f);
        checkRect.sizeDelta = new Vector2(12f, 12f);
        checkRect.anchoredPosition = new Vector2(16f, 0f);
        Image check = checkObject.GetComponent<Image>();
        check.color = new Color(0.93f, 0.78f, 0.4f, 1f);
        ApplySprite(check);

        itemLabel = CreateText(item.transform, "Item Label", "Option", 24, TextAlignmentOptions.MidlineLeft, font, Paper);
        Stretch(itemLabel.rectTransform, new Vector2(28f, 0f), new Vector2(-8f, 0f));
        itemLabel.textWrappingMode = TextWrappingModes.NoWrap;
        itemLabel.overflowMode = TextOverflowModes.Overflow;

        Toggle toggle = item.GetComponent<Toggle>();
        toggle.targetGraphic = itemBackground;
        toggle.graphic = check;
        toggle.isOn = true;
        return templateRect;
    }

    static void CreateEnding(Transform parent, TMP_FontAsset font, out CanvasGroup endingGroup, out TMP_Text credits, out Button exit, out CanvasGroup exitGroup)
    {
        var endingObject = new GameObject("Ending", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        endingObject.transform.SetParent(parent, false);
        Stretch(endingObject.GetComponent<RectTransform>());
        Image black = endingObject.GetComponent<Image>();
        black.color = Color.black;
        black.raycastTarget = true;
        endingGroup = endingObject.GetComponent<CanvasGroup>();
        endingGroup.alpha = 0f;
        endingGroup.blocksRaycasts = false;
        endingGroup.interactable = false;

        credits = CreateText(endingObject.transform, "Credits", "Tanks for playing!", 36, TextAlignmentOptions.Center, font, Paper);
        RectTransform creditsRect = credits.rectTransform;
        creditsRect.anchorMin = new Vector2(0.12f, 0.22f);
        creditsRect.anchorMax = new Vector2(0.88f, 0.82f);
        creditsRect.offsetMin = Vector2.zero;
        creditsRect.offsetMax = Vector2.zero;
        credits.textWrappingMode = TextWrappingModes.Normal;
        credits.alignment = TextAlignmentOptions.Center;
        credits.raycastTarget = false;

        exit = CreateButton(endingObject.transform, "Exit", "Exit", font, new Vector2(0f, 80f), new Vector2(220f, 56f), Paper, Color.black);
        exitGroup = exit.gameObject.AddComponent<CanvasGroup>();
        exitGroup.alpha = 0f;
        exitGroup.blocksRaycasts = false;
        exitGroup.interactable = false;
    }

    static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font, Vector2 position, Vector2 size, Color backgroundColor, Color labelColor)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = buttonObject.GetComponent<Image>();
        image.color = backgroundColor;
        ApplySprite(image);

        Button button = buttonObject.GetComponent<Button>();
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        TMP_Text text = CreateText(buttonObject.transform, "Label", label, 28, TextAlignmentOptions.Center, font, labelColor);
        Stretch(text.rectTransform);
        text.raycastTarget = false;
        return button;
    }

    static TMP_Text CreateText(Transform parent, string name, string content, float size, TextAlignmentOptions alignment, TMP_FontAsset font, Color color)
    {
        var textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        return text;
    }

    static void Stretch(RectTransform rect)
    {
        Stretch(rect, Vector2.zero, Vector2.zero);
    }

    static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    static void ApplySprite(Image image)
    {
        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (sprite == null)
            return;

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
    }

    static readonly Color Paper = new Color(0.95f, 0.91f, 0.82f, 1f);

    struct UiRefs
    {
        public RectTransform Panel;
        public RectTransform Sentence;
        public TMP_Text WordTemplate;
        public TMP_Dropdown GapTemplate;
        public Button Confirm;
        public TMP_Text Notification;
        public CanvasGroup NotificationGroup;
        public CanvasGroup EndingGroup;
        public TMP_Text Credits;
        public Button Exit;
        public CanvasGroup ExitGroup;
    }
}
