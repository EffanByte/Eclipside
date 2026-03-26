using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject achievementPanel;
    [SerializeField] private GameObject ChallengePanel;
    [SerializeField] private GameObject missionPanel;
    [SerializeField] private string gameplaySceneName = "Demo";

    private const string DefaultCharacterId = "D_Eryndor";

    private GameObject characterSelectPanel;
    private RectTransform characterListContent;
    private Text selectedCharacterLabel;
    private Button startRunButton;
    private readonly Dictionary<string, Button> characterButtons = new Dictionary<string, Button>();
    private Font fallbackFont;
    private string pendingCharacterSelectionId;
    private bool achievementPanelStyled;
    private bool challengePanelStyled;
    private bool missionPanelStyled;

    public void OnStartGameButtonPressed()
    {
        EnsureCharacterSelectPanel();
        RefreshCharacterSelectPanel();

        if (characterSelectPanel != null)
        {
            characterSelectPanel.SetActive(true);
        }
    }

    public void OnAchievementButtonPressed()
    {
        BeautifyTrackedPanel(achievementPanel, "Achievements", ref achievementPanelStyled);
        achievementPanel.SetActive(true);
        ChallengePanel.SetActive(false);
    }

    public void OnChallengeButtonPressed()
    {
        BeautifyTrackedPanel(ChallengePanel, "Challenges", ref challengePanelStyled);
        ChallengePanel.SetActive(true);
        achievementPanel.SetActive(false);
    }

    public void OnExitAchievementPanel()
    {
        achievementPanel.SetActive(false);
    }

    public void OnExitChallengePanel()
    {
        ChallengePanel.SetActive(false);
    }

    public void OnGachaButtonPressed()
    {
        SceneManager.LoadScene("Demo");
        SceneManager.LoadScene("GachaScene");
    }

    public void OnMissionButtonPressed()
    {
        BeautifyTrackedPanel(missionPanel, "Missions", ref missionPanelStyled);
        missionPanel.SetActive(true);
    }

    public void onExitMissionPanel()
    {
        missionPanel.SetActive(false);
    }

    public void OnCloseCharacterSelectPressed()
    {
        if (characterSelectPanel != null)
        {
            characterSelectPanel.SetActive(false);
        }
    }

    public void OnStartRunFromCharacterSelectPressed()
    {
        if (string.IsNullOrWhiteSpace(pendingCharacterSelectionId))
        {
            Debug.LogWarning("[MainMenu] Start Run blocked because no character is selected.");
            return;
        }

        SaveFile_Profile profile = SaveManager.Profile;
        EnsureDefaultCharacterUnlocked(profile);
        profile.characters.equipped_character_id = pendingCharacterSelectionId;
        SaveManager.SaveProfile();

        SceneManager.LoadScene(gameplaySceneName);
    }

    private void EnsureCharacterSelectPanel()
    {
        if (characterSelectPanel != null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogError("[MainMenu] Could not create character select menu because no Canvas was found.");
            return;
        }

        fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        characterSelectPanel = CreateUIObject("CharacterSelectPanel", canvas.transform);
        Image overlayImage = characterSelectPanel.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.82f);
        RectTransform overlayRect = characterSelectPanel.GetComponent<RectTransform>();
        StretchToParent(overlayRect);
        characterSelectPanel.SetActive(false);

        GameObject card = CreateUIObject("CharacterSelectCard", characterSelectPanel.transform);
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.12f, 0.12f, 0.16f, 0.98f);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(760f, 560f);
        cardRect.anchoredPosition = Vector2.zero;

        Text titleLabel = CreateText("Title", card.transform, "Select Character", 28, TextAnchor.MiddleCenter);
        RectTransform titleRect = titleLabel.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(24f, -68f);
        titleRect.offsetMax = new Vector2(-24f, -16f);

        selectedCharacterLabel = CreateText("SelectedLabel", card.transform, "Selected: None", 18, TextAnchor.MiddleLeft);
        RectTransform selectedRect = selectedCharacterLabel.rectTransform;
        selectedRect.anchorMin = new Vector2(0f, 1f);
        selectedRect.anchorMax = new Vector2(1f, 1f);
        selectedRect.pivot = new Vector2(0.5f, 1f);
        selectedRect.offsetMin = new Vector2(32f, -108f);
        selectedRect.offsetMax = new Vector2(-32f, -72f);

        GameObject viewport = CreateUIObject("CharacterViewport", card.transform);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0.08f, 0.08f, 0.11f, 1f);
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = true;
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.offsetMin = new Vector2(32f, 92f);
        viewportRect.offsetMax = new Vector2(-32f, -124f);

        GameObject content = CreateUIObject("CharacterListContent", viewport.transform);
        characterListContent = content.GetComponent<RectTransform>();
        characterListContent.anchorMin = new Vector2(0f, 1f);
        characterListContent.anchorMax = new Vector2(1f, 1f);
        characterListContent.pivot = new Vector2(0.5f, 1f);
        characterListContent.anchoredPosition = Vector2.zero;
        characterListContent.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = characterListContent;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;

        Button closeButton = CreateButton("CloseButton", card.transform, "Back");
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0f, 0f);
        closeRect.anchorMax = new Vector2(0f, 0f);
        closeRect.pivot = new Vector2(0f, 0f);
        closeRect.sizeDelta = new Vector2(180f, 48f);
        closeRect.anchoredPosition = new Vector2(32f, 24f);
        closeButton.onClick.AddListener(OnCloseCharacterSelectPressed);

        startRunButton = CreateButton("StartRunButton", card.transform, "Start Run");
        RectTransform startRect = startRunButton.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(1f, 0f);
        startRect.anchorMax = new Vector2(1f, 0f);
        startRect.pivot = new Vector2(1f, 0f);
        startRect.sizeDelta = new Vector2(220f, 48f);
        startRect.anchoredPosition = new Vector2(-32f, 24f);
        startRunButton.onClick.AddListener(OnStartRunFromCharacterSelectPressed);
    }

    private void BeautifyTrackedPanel(GameObject panel, string title, ref bool styledFlag)
    {
        if (panel == null)
        {
            return;
        }

        if (fallbackFont == null)
        {
            fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        if (styledFlag)
        {
            return;
        }

        styledFlag = true;
        BeautifyExistingPanel(panel, title);
    }

    private void BeautifyExistingPanel(GameObject panel, string title)
    {
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            return;
        }

        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        Image overlayImage = panel.GetComponent<Image>();
        if (overlayImage == null)
        {
            overlayImage = panel.AddComponent<Image>();
        }
        overlayImage.color = new Color(0f, 0f, 0f, 0.82f);
        overlayImage.raycastTarget = true;

        Transform existingCard = panel.transform.Find("PrettyCard");
        GameObject card = existingCard != null ? existingCard.gameObject : CreateUIObject("PrettyCard", panel.transform);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(1040f, 760f);
        cardRect.anchoredPosition = new Vector2(0f, -24f);
        card.transform.SetAsFirstSibling();

        Image cardImage = card.GetComponent<Image>();
        if (cardImage == null)
        {
            cardImage = card.AddComponent<Image>();
        }
        cardImage.color = new Color(0.12f, 0.12f, 0.16f, 0.98f);

        Outline cardOutline = card.GetComponent<Outline>();
        if (cardOutline == null)
        {
            cardOutline = card.AddComponent<Outline>();
        }
        cardOutline.effectColor = new Color(0.92f, 0.75f, 0.28f, 0.45f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        Transform headerTransform = panel.transform.Find("PrettyHeader");
        Text header = headerTransform != null ? headerTransform.GetComponent<Text>() : CreateText("PrettyHeader", panel.transform, title, 30, TextAnchor.MiddleCenter);
        header.font = fallbackFont;
        header.fontStyle = FontStyle.Bold;
        header.color = new Color(1f, 0.96f, 0.88f, 1f);
        header.text = title;

        RectTransform headerRect = header.rectTransform;
        headerRect.anchorMin = new Vector2(0.5f, 1f);
        headerRect.anchorMax = new Vector2(0.5f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(480f, 44f);
        headerRect.anchoredPosition = new Vector2(0f, -42f);
        header.transform.SetSiblingIndex(1);

        foreach (ScrollRect scrollRect in panel.GetComponentsInChildren<ScrollRect>(true))
        {
            StyleScrollRect(scrollRect);
        }

        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
        {
            StyleButton(button);
        }

        foreach (Toggle toggle in panel.GetComponentsInChildren<Toggle>(true))
        {
            StyleToggle(toggle);
        }

        foreach (Text text in panel.GetComponentsInChildren<Text>(true))
        {
            StyleLegacyText(text, text == header);
        }

        foreach (TMPro.TextMeshProUGUI tmp in panel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
        {
            StyleTmpText(tmp);
        }

        foreach (Image image in panel.GetComponentsInChildren<Image>(true))
        {
            StylePanelImage(image, panel, cardImage, overlayImage);
        }
    }

    private void StyleScrollRect(ScrollRect scrollRect)
    {
        if (scrollRect == null)
        {
            return;
        }

        scrollRect.horizontal = false;
        Image image = scrollRect.GetComponent<Image>();
        if (image == null)
        {
            image = scrollRect.gameObject.AddComponent<Image>();
        }
        image.color = new Color(0.08f, 0.08f, 0.11f, 0.94f);

        Outline outline = scrollRect.GetComponent<Outline>();
        if (outline == null)
        {
            outline = scrollRect.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(1f, 1f, 1f, 0.08f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private void StyleButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image == null)
        {
            image = button.gameObject.AddComponent<Image>();
        }

        bool isCloseButton = button.name.ToLowerInvariant().Contains("exit") || button.name.ToLowerInvariant().Contains("close");
        image.color = isCloseButton
            ? new Color(0.92f, 0.34f, 0.30f, 0.95f)
            : new Color(0.94f, 0.88f, 0.72f, 0.98f);

        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = isCloseButton
            ? new Color(0.97f, 0.42f, 0.38f, 1f)
            : new Color(0.98f, 0.92f, 0.78f, 1f);
        colors.pressedColor = isCloseButton
            ? new Color(0.82f, 0.26f, 0.24f, 1f)
            : new Color(0.84f, 0.78f, 0.62f, 1f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.95f);
        button.colors = colors;

        Outline outline = button.GetComponent<Outline>();
        if (outline == null)
        {
            outline = button.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0f, 0f, 0f, 0.25f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private void StyleToggle(Toggle toggle)
    {
        if (toggle == null)
        {
            return;
        }

        Image background = toggle.targetGraphic as Image;
        if (background == null)
        {
            background = toggle.GetComponent<Image>();
        }

        if (background != null)
        {
            background.color = new Color(0.18f, 0.18f, 0.24f, 0.98f);
        }

        if (toggle.graphic is Image checkmark)
        {
            checkmark.color = new Color(0.92f, 0.75f, 0.28f, 1f);
        }
    }

    private void StyleLegacyText(Text text, bool isHeader)
    {
        if (text == null)
        {
            return;
        }

        text.font = fallbackFont;
        if (isHeader)
        {
            text.color = new Color(1f, 0.96f, 0.88f, 1f);
        }
        else
        {
            text.color = text.GetComponentInParent<Button>() != null
                ? Color.black
                : new Color(0.92f, 0.92f, 0.94f, 1f);
        }
    }

    private void StyleTmpText(TMPro.TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        string lowerName = text.name.ToLowerInvariant();
        bool isButtonLabel = text.GetComponentInParent<Button>() != null;
        bool isTitleLike = lowerName.Contains("title") || lowerName.Contains("header");
        bool isProgress = lowerName.Contains("progress");

        if (isButtonLabel)
        {
            text.color = Color.black;
        }
        else if (isTitleLike)
        {
            text.color = new Color(1f, 0.96f, 0.88f, 1f);
        }
        else if (isProgress)
        {
            text.color = new Color(0.92f, 0.75f, 0.28f, 1f);
        }
        else
        {
            text.color = new Color(0.92f, 0.92f, 0.94f, 1f);
        }
    }

    private void StylePanelImage(Image image, GameObject rootPanel, Image cardImage, Image overlayImage)
    {
        if (image == null)
        {
            return;
        }

        if (image == overlayImage || image == cardImage)
        {
            return;
        }

        if (image.GetComponent<Button>() != null)
        {
            return;
        }

        if (image.transform == rootPanel.transform)
        {
            return;
        }

        string lowerName = image.name.ToLowerInvariant();
        if (lowerName.Contains("background") || lowerName.Contains("panel") || lowerName.Contains("viewport"))
        {
            image.color = new Color(0.14f, 0.14f, 0.18f, 0.94f);
        }
    }

    private void RefreshCharacterSelectPanel()
    {
        if (characterListContent == null)
        {
            return;
        }

        SaveFile_Profile profile = SaveManager.Profile;
        EnsureDefaultCharacterUnlocked(profile);

        List<CharacterData> characters = new List<CharacterData>(GameDatabase.Instance.allCharacters);
        characters.Sort(CompareCharactersForMenu);

        if (string.IsNullOrWhiteSpace(profile.characters.equipped_character_id) || !profile.characters.owned_character_ids.Contains(profile.characters.equipped_character_id))
        {
            profile.characters.equipped_character_id = DefaultCharacterId;
        }

        pendingCharacterSelectionId = profile.characters.equipped_character_id;

        foreach (Transform child in characterListContent)
        {
            Destroy(child.gameObject);
        }

        characterButtons.Clear();

        foreach (CharacterData character in characters)
        {
            if (character == null)
            {
                continue;
            }

            bool isUnlocked = profile.characters.owned_character_ids.Contains(character.characterID);
            Button button = CreateButton(character.characterID + "_Button", characterListContent, BuildCharacterLabel(character, isUnlocked));
            LayoutElement element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 56f;
            button.interactable = isUnlocked;

            if (isUnlocked)
            {
                string characterId = character.characterID;
                button.onClick.AddListener(() => OnCharacterSelected(characterId));
            }

            characterButtons[character.characterID] = button;
        }

        UpdateCharacterSelectionVisuals();
        SaveManager.SaveProfile();
    }

    private void OnCharacterSelected(string characterId)
    {
        pendingCharacterSelectionId = characterId;
        UpdateCharacterSelectionVisuals();
    }

    private void UpdateCharacterSelectionVisuals()
    {
        SaveFile_Profile profile = SaveManager.Profile;

        foreach (KeyValuePair<string, Button> entry in characterButtons)
        {
            if (entry.Value == null)
            {
                continue;
            }

            bool isUnlocked = profile.characters.owned_character_ids.Contains(entry.Key);
            bool isSelected = entry.Key == pendingCharacterSelectionId;

            ColorBlock colors = entry.Value.colors;
            colors.normalColor = isSelected ? new Color(0.92f, 0.75f, 0.28f, 1f) : new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = isSelected ? new Color(0.98f, 0.82f, 0.36f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.8f, 0.68f, 0.28f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.95f);
            entry.Value.colors = colors;
            entry.Value.interactable = isUnlocked;
        }

        CharacterData selectedCharacter = GameDatabase.Instance.GetCharacterByID(pendingCharacterSelectionId);
        selectedCharacterLabel.text = selectedCharacter != null
            ? $"Selected: {selectedCharacter.characterName} ({selectedCharacter.rarity})"
            : "Selected: None";

        if (startRunButton != null)
        {
            startRunButton.interactable = !string.IsNullOrWhiteSpace(pendingCharacterSelectionId)
                && profile.characters.owned_character_ids.Contains(pendingCharacterSelectionId);
        }
    }

    private void EnsureDefaultCharacterUnlocked(SaveFile_Profile profile)
    {
        if (!profile.characters.owned_character_ids.Contains(DefaultCharacterId))
        {
            profile.characters.owned_character_ids.Add(DefaultCharacterId);
        }
    }

    private int CompareCharactersForMenu(CharacterData a, CharacterData b)
    {
        SaveFile_Profile profile = SaveManager.Profile;
        bool aUnlocked = a != null && profile.characters.owned_character_ids.Contains(a.characterID);
        bool bUnlocked = b != null && profile.characters.owned_character_ids.Contains(b.characterID);

        if (aUnlocked != bUnlocked)
        {
            return aUnlocked ? -1 : 1;
        }

        if (a != null && b != null && a.rarity != b.rarity)
        {
            return a.rarity.CompareTo(b.rarity);
        }

        if (a == null || b == null)
        {
            return 0;
        }

        return string.Compare(a.characterName, b.characterName, System.StringComparison.OrdinalIgnoreCase);
    }

    private string BuildCharacterLabel(CharacterData character, bool isUnlocked)
    {
        string status = isUnlocked ? "Unlocked" : "Locked";
        return $"{character.characterName}  [{character.rarity}]  -  {status}";
    }

    private GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private Text CreateText(string objectName, Transform parent, string message, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = CreateUIObject(objectName, parent);
        Text text = textObject.AddComponent<Text>();
        text.font = fallbackFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = message;
        return text;
    }

    private Button CreateButton(string objectName, Transform parent, string label)
    {
        GameObject buttonObject = CreateUIObject(objectName, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.95f);
        button.colors = colors;

        Text labelText = CreateText("Label", buttonObject.transform, label, 18, TextAnchor.MiddleCenter);
        labelText.color = Color.black;
        RectTransform labelRect = labelText.rectTransform;
        StretchToParent(labelRect, 12f, 12f);

        return button;
    }

    private void StretchToParent(RectTransform rectTransform, float horizontalPadding = 0f, float verticalPadding = 0f)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        rectTransform.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
    }
}
