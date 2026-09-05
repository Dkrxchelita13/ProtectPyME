using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MinigameFeedbackPresenter : MonoBehaviour
{
    private const float DefaultTimeoutSeconds = 12f;
    private const float CardWidthRatio = 0.82f;
    private const float CardHeightRatio = 0.74f;
    private const float FallbackCardWidth = 900f;
    private const float FallbackCardHeight = 520f;
    private const float HeaderHeight = 92f;
    private const float FooterHeight = 104f;
    private const float ButtonTouchSize = 68f;

    private RectTransform rootRect;
    private RectTransform cardRect;
    private RectTransform contentRect;
    private ScrollRect scrollRect;
    private CanvasGroup canvasGroup;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI messageText;
    private TextMeshProUGUI resultText;
    private TextMeshProUGUI reinforcementText;
    private TextMeshProUGUI strengthText;
    private TextMeshProUGUI motivationalText;
    private TextMeshProUGUI nextStepText;
    private TextMeshProUGUI recommendationText;

    private string expectedSessionId = "";
    private bool waitingStarted;
    private bool rendered;
    private bool dismissed;
    private Coroutine waitCoroutine;

    public static MinigameFeedbackPresenter AttachOrGet(Transform finalPanel)
    {
        if (finalPanel == null)
        {
            return null;
        }

        MinigameFeedbackPresenter existing =
            finalPanel.GetComponentInChildren<MinigameFeedbackPresenter>(true);

        if (existing != null && !existing.dismissed &&
            existing.gameObject.activeInHierarchy)
        {
            existing.transform.SetAsLastSibling();
            return existing;
        }

        GameObject presenterObject =
            new GameObject("MinigameFeedbackPresenter", typeof(RectTransform));
        presenterObject.transform.SetParent(finalPanel, false);

        MinigameFeedbackPresenter presenter =
            presenterObject.AddComponent<MinigameFeedbackPresenter>();
        presenter.BuildUi(finalPanel);
        presenter.transform.SetAsLastSibling();

        return presenter;
    }

    public void BeginWaitingForFeedback(
        string sessionId,
        float timeoutSeconds = DefaultTimeoutSeconds
    )
    {
        if (waitingStarted)
        {
            return;
        }

        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.Log("Feedback UI: omitido porque el flujo es legacy");
            return;
        }

        expectedSessionId = sessionId;
        waitingStarted = true;
        rendered = false;
        dismissed = false;

        RefreshSize();
        ShowWaitingState();

        Debug.Log("Feedback UI abierto id=" + sessionId);

        waitCoroutine = StartCoroutine(
            WaitForFeedback(sessionId, timeoutSeconds)
        );
    }

    private IEnumerator WaitForFeedback(
        string sessionId,
        float timeoutSeconds = DefaultTimeoutSeconds
    )
    {
        float startedAt = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - startedAt < timeoutSeconds)
        {
            if (dismissed)
            {
                yield break;
            }

            if (this == null || rootRect == null || !gameObject.activeInHierarchy)
            {
                yield break;
            }

            if (HasFeedbackForSession(sessionId))
            {
                if (dismissed)
                {
                    yield break;
                }

                RenderFeedback(MinigameLessonState.LastFeedback);
                waitCoroutine = null;
                yield break;
            }

            yield return null;
        }

        if (this != null && rootRect != null && !rendered && !dismissed)
        {
            ShowTimeoutState(sessionId);
        }

        waitCoroutine = null;
    }

    private void OnDestroy()
    {
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }
    }

    private void BuildUi(Transform finalPanel)
    {
        rootRect = GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.localScale = Vector3.one;

        Canvas localCanvas = gameObject.AddComponent<Canvas>();
        localCanvas.overrideSorting = true;
        localCanvas.sortingOrder = ResolveSortingOrder(finalPanel) + 20;
        gameObject.AddComponent<GraphicRaycaster>();

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        Image overlay = gameObject.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.72f);
        overlay.raycastTarget = true;

        TMP_FontAsset font = ResolveFont(finalPanel);

        cardRect = CreateRect("FeedbackCard", transform);
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.localScale = Vector3.one;

        Image cardImage = cardRect.gameObject.AddComponent<Image>();
        cardImage.color = new Color(0.06f, 0.12f, 0.20f, 0.97f);
        cardImage.raycastTarget = true;

        RectTransform headerRect = CreateHeader(cardRect, font);
        RectTransform footerRect = CreateFooter(cardRect, font);
        RectTransform bodyRect = CreateBody(cardRect);
        ConfigureBodyOffsets(bodyRect, headerRect, footerRect);

        contentRect = CreateRect("Content", scrollRect.viewport.transform);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layoutGroup =
            contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(24, 24, 18, 18);
        layoutGroup.spacing = 12f;
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        ContentSizeFitter contentFitter =
            contentRect.gameObject.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;

        Color textColor = new Color(0.96f, 0.98f, 1f, 1f);
        messageText = CreateText("Message", font, textColor, 28f, FontStyles.Normal);
        resultText = CreateText("Result", font, textColor, 30f, FontStyles.Bold);
        reinforcementText =
            CreateText("Reinforcement", font, textColor, 28f, FontStyles.Normal);
        strengthText = CreateText("Strength", font, textColor, 28f, FontStyles.Normal);
        motivationalText =
            CreateText(
                "MotivationalText",
                font,
                new Color(0.82f, 0.90f, 1f, 1f),
                24f,
                FontStyles.Italic
            );
        nextStepText = CreateText("NextStep", font, textColor, 28f, FontStyles.Normal);
        recommendationText =
            CreateText("Recommendation", font, textColor, 28f, FontStyles.Bold);

        RefreshSize();
    }

    private RectTransform CreateHeader(
        RectTransform parent,
        TMP_FontAsset font
    )
    {
        RectTransform headerRect = CreateRect("Header", parent);
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.offsetMin = new Vector2(0f, -HeaderHeight);
        headerRect.offsetMax = Vector2.zero;

        Image headerImage = headerRect.gameObject.AddComponent<Image>();
        headerImage.color = new Color(0.09f, 0.22f, 0.36f, 1f);
        headerImage.raycastTarget = true;

        RectTransform titleRect = CreateRect("Title", headerRect);
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(34f, 6f);
        titleRect.offsetMax = new Vector2(-96f, -6f);

        titleText = titleRect.gameObject.AddComponent<TextMeshProUGUI>();
        ConfigureText(
            titleText,
            font,
            new Color(1f, 1f, 1f, 1f),
            46f,
            FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft
        );

        CreateButton(
            "CloseButtonX",
            headerRect,
            "X",
            new Vector2(1f, 0.5f),
            new Vector2(-46f, 0f),
            new Vector2(ButtonTouchSize, ButtonTouchSize),
            font,
            38f
        );

        return headerRect;
    }

    private RectTransform CreateBody(RectTransform parent)
    {
        RectTransform bodyRect = CreateRect("BodyScroll", parent);

        Image bodyImage = bodyRect.gameObject.AddComponent<Image>();
        bodyImage.color = new Color(0.04f, 0.09f, 0.15f, 0.94f);
        bodyImage.raycastTarget = true;

        RectTransform viewportRect = CreateRect("Viewport", bodyRect);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        viewportRect.offsetMin = new Vector2(18f, 12f);
        viewportRect.offsetMax = new Vector2(-18f, -12f);
        viewportRect.gameObject.AddComponent<RectMask2D>();

        scrollRect = bodyRect.gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 28f;

        return bodyRect;
    }

    private RectTransform CreateFooter(
        RectTransform parent,
        TMP_FontAsset font
    )
    {
        RectTransform footerRect = CreateRect("Footer", parent);
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.offsetMin = Vector2.zero;
        footerRect.offsetMax = new Vector2(0f, FooterHeight);

        Image footerImage = footerRect.gameObject.AddComponent<Image>();
        footerImage.color = new Color(0.09f, 0.22f, 0.36f, 1f);
        footerImage.raycastTarget = true;

        CreateButton(
            "CloseButtonFooter",
            footerRect,
            "CERRAR",
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(260f, 70f),
            font,
            34f
        );

        return footerRect;
    }

    private void ConfigureBodyOffsets(
        RectTransform bodyRect,
        RectTransform headerRect,
        RectTransform footerRect
    )
    {
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.offsetMin = new Vector2(0f, FooterHeight);
        bodyRect.offsetMax = new Vector2(0f, -HeaderHeight);
    }

    private Button CreateButton(
        string objectName,
        RectTransform parent,
        string label,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size,
        TMP_FontAsset font,
        float fontSize
    )
    {
        RectTransform buttonRect = CreateRect(objectName, parent);
        buttonRect.anchorMin = anchor;
        buttonRect.anchorMax = anchor;
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = size;

        Image image = buttonRect.gameObject.AddComponent<Image>();
        image.color = new Color(0.96f, 0.88f, 0.22f, 1f);
        image.raycastTarget = true;

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(Dismiss);

        RectTransform labelRect = CreateRect("Label", buttonRect);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText =
            labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        ConfigureText(
            labelText,
            font,
            new Color(0.03f, 0.08f, 0.14f, 1f),
            fontSize,
            FontStyles.Bold,
            TextAlignmentOptions.Center
        );
        labelText.text = label;
        labelText.raycastTarget = false;

        return button;
    }

    private TextMeshProUGUI CreateText(
        string objectName,
        TMP_FontAsset font,
        Color color,
        float fontSize,
        FontStyles fontStyle
    )
    {
        RectTransform textRect = CreateRect(objectName, contentRect);
        TextMeshProUGUI text = textRect.gameObject.AddComponent<TextMeshProUGUI>();

        ConfigureText(
            text,
            font,
            color,
            fontSize,
            fontStyle,
            TextAlignmentOptions.Left
        );

        return text;
    }

    private void ConfigureText(
        TextMeshProUGUI text,
        TMP_FontAsset font,
        Color color,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment
    )
    {
        if (font != null)
        {
            text.font = font;
        }
        text.color = color;
        text.fontSize = fontSize;
        text.fontSizeMin = Mathf.Max(22f, fontSize - 6f);
        text.fontSizeMax = fontSize;
        text.enableAutoSizing = true;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
    }

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject rectObject =
            new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        rectObject.transform.localScale = Vector3.one;

        return rectObject.GetComponent<RectTransform>();
    }

    private void ShowWaitingState()
    {
        SetText(titleText, "Retroalimentacion personalizada");
        SetText(
            messageText,
            "Preparando tu retroalimentacion personalizada..."
        );

        SetText(resultText, "");
        SetText(reinforcementText, "");
        SetText(strengthText, "");
        SetText(motivationalText, "");
        SetText(nextStepText, "");
        SetText(recommendationText, "");

        rendered = false;
        UpdateLayout();
    }

    private void ShowTimeoutState(string sessionId)
    {
        bool completionPending = APIManager.Instance != null &&
            APIManager.Instance.HasPendingMinigameSessionCompletion(sessionId);

        SetText(
            titleText,
            completionPending
                ? "Sincronizacion pendiente"
                : "Resumen guardado"
        );
        SetText(
            messageText,
            completionPending
                ? "Tu progreso se sincronizara automaticamente. La recomendacion personalizada estara disponible despues."
                : "Tu resumen fue guardado. La recomendacion personalizada estara disponible mas adelante."
        );

        SetText(resultText, "");
        SetText(reinforcementText, "");
        SetText(strengthText, "");
        SetText(motivationalText, "");
        SetText(nextStepText, "");
        SetText(recommendationText, "");

        UpdateLayout();
        Debug.LogWarning(
            "Feedback UI: tiempo de espera agotado id=" + sessionId
        );
    }

    private void RenderFeedback(MinigameFeedbackResponse feedback)
    {
        if (dismissed || rendered || !IsValidFeedback(feedback))
        {
            return;
        }

        ConceptFeedbackResponse reinforcement =
            GetFirstValidConcept(feedback.reinforcement);
        ConceptFeedbackResponse strength =
            GetFirstValidConcept(feedback.strengths);
        bool withoutEvidence = ValuesMatch(
            feedback.performance_level,
            "sin_evidencia"
        );

        SetText(titleText, feedback.title);
        SetText(messageText, feedback.message);

        if (withoutEvidence)
        {
            SetText(resultText, "");
            SetText(reinforcementText, "");
            SetText(strengthText, "");
            SetText(
                motivationalText,
                MotivationalMessageProvider.GetMessage(
                    ResolveMotivationContext(feedback.performance_level)
                )
            );
            SetText(nextStepText, BuildNextStepText(feedback.next_step));
            SetText(recommendationText, "");
        }
        else
        {
            SetText(resultText, BuildResultText(feedback));
            SetText(reinforcementText, BuildReinforcementText(reinforcement));
            SetText(strengthText, BuildStrengthText(strength));
            SetText(
                motivationalText,
                MotivationalMessageProvider.GetMessage(
                    ResolveMotivationContext(feedback.performance_level)
                )
            );
            SetText(nextStepText, BuildNextStepText(feedback.next_step));
            SetText(recommendationText, BuildRecommendationText(feedback));
        }

        rendered = true;
        UpdateLayout();

        Debug.Log(
            "Feedback UI mostrado id=" +
            feedback.session_id +
            " level=" +
            feedback.performance_level
        );
    }

    private void Dismiss()
    {
        if (dismissed)
        {
            return;
        }

        dismissed = true;

        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (!rendered)
        {
            Debug.Log(
                "Feedback UI cerrado antes de recibir respuesta id=" +
                expectedSessionId
            );
        }

        Debug.Log("Feedback UI cerrado id=" + expectedSessionId);
        Destroy(gameObject);
    }

    private string BuildResultText(MinigameFeedbackResponse feedback)
    {
        return "Precision: " +
            feedback.accuracy.ToString("0.##") +
            "%\nAciertos: " +
            feedback.correct_attempts +
            " de " +
            feedback.total_attempts;
    }

    private string BuildReinforcementText(ConceptFeedbackResponse concept)
    {
        if (concept == null)
        {
            return "";
        }

        return "Refuerza: " +
            concept.term +
            "\n" +
            concept.recommendation;
    }

    private string BuildStrengthText(ConceptFeedbackResponse concept)
    {
        if (concept == null)
        {
            return "";
        }

        return "Fortaleza: " +
            concept.term +
            "\n" +
            concept.message;
    }

    private string BuildNextStepText(string nextStep)
    {
        if (string.IsNullOrEmpty(nextStep))
        {
            return "";
        }

        return "Siguiente paso\n" + nextStep;
    }

    private string BuildRecommendationText(MinigameFeedbackResponse feedback)
    {
        string visibleMinigame =
            GetVisibleMinigameName(feedback.recommended_minigame);

        if (string.IsNullOrEmpty(visibleMinigame))
        {
            return "";
        }

        return "Siguiente actividad recomendada: " + visibleMinigame;
    }

    private MotivationContext ResolveMotivationContext(string performanceLevel)
    {
        if (ValuesMatch(performanceLevel, "excelente") ||
            ValuesMatch(performanceLevel, "buen_progreso"))
        {
            return MotivationContext.PositiveReinforcement;
        }

        return MotivationContext.NeedsReinforcement;
    }

    private void SetText(TextMeshProUGUI target, string value)
    {
        if (target == null)
        {
            return;
        }

        string safeValue = value ?? "";
        target.text = safeValue;
        target.gameObject.SetActive(!string.IsNullOrEmpty(safeValue));
    }

    private void UpdateLayout()
    {
        RefreshSize();
        Canvas.ForceUpdateCanvases();

        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void RefreshSize()
    {
        if (cardRect == null || rootRect == null)
        {
            return;
        }

        RectTransform parentRect = rootRect.parent as RectTransform;
        Vector2 parentSize = parentRect != null
            ? parentRect.rect.size
            : Vector2.zero;

        float width = parentSize.x > 1f
            ? parentSize.x * CardWidthRatio
            : FallbackCardWidth;
        float height = parentSize.y > 1f
            ? parentSize.y * CardHeightRatio
            : FallbackCardHeight;

        cardRect.sizeDelta = new Vector2(width, height);
    }

    private bool HasFeedbackForSession(string sessionId)
    {
        return MinigameLessonState.HasLastFeedback &&
            MinigameLessonState.LastFeedback != null &&
            ValuesMatch(MinigameLessonState.LastFeedback.session_id, sessionId);
    }

    private bool IsValidFeedback(MinigameFeedbackResponse feedback)
    {
        return feedback != null &&
            ValuesMatch(feedback.session_id, expectedSessionId) &&
            !string.IsNullOrEmpty(feedback.title) &&
            !string.IsNullOrEmpty(feedback.message);
    }

    private ConceptFeedbackResponse GetFirstValidConcept(
        ConceptFeedbackResponse[] concepts
    )
    {
        if (concepts == null)
        {
            return null;
        }

        for (int i = 0; i < concepts.Length; i++)
        {
            ConceptFeedbackResponse concept = concepts[i];

            if (concept != null &&
                !string.IsNullOrEmpty(concept.term))
            {
                return concept;
            }
        }

        return null;
    }

    private string GetVisibleMinigameName(string minigame)
    {
        string value = (minigame ?? "").Trim().ToLowerInvariant();

        if (value == "quiz")
        {
            return "Quiz";
        }

        if (value == "wordsearch")
        {
            return "Sopa de letras";
        }

        if (value == "crossword")
        {
            return "Crucigrama";
        }

        return "";
    }

    private int ResolveSortingOrder(Transform finalPanel)
    {
        Canvas source = finalPanel.GetComponentInParent<Canvas>();
        return source != null ? source.sortingOrder : 0;
    }

    private TMP_FontAsset ResolveFont(Transform finalPanel)
    {
        TextMeshProUGUI source =
            finalPanel.GetComponentInChildren<TextMeshProUGUI>(true);

        return source != null ? source.font : null;
    }

    private bool ValuesMatch(string left, string right)
    {
        return string.Equals(
            (left ?? "").Trim(),
            (right ?? "").Trim(),
            System.StringComparison.OrdinalIgnoreCase
        );
    }
}
