using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MinigameFeedbackPresenter : MonoBehaviour
{
    private const float DefaultTimeoutSeconds = 12f;
    private const float WidthRatio = 0.85f;
    private const float MaxHeightRatio = 0.48f;
    private const float MinWidth = 420f;
    private const float FallbackWidth = 760f;
    private const float FallbackHeight = 330f;

    private RectTransform rootRect;
    private RectTransform contentRect;
    private ScrollRect scrollRect;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI messageText;
    private TextMeshProUGUI resultText;
    private TextMeshProUGUI reinforcementText;
    private TextMeshProUGUI strengthText;
    private TextMeshProUGUI nextStepText;
    private TextMeshProUGUI recommendationText;

    private string expectedSessionId = "";
    private bool waitingStarted;
    private bool rendered;
    private Coroutine waitCoroutine;

    public static MinigameFeedbackPresenter AttachOrGet(Transform finalPanel)
    {
        if (finalPanel == null)
        {
            return null;
        }

        MinigameFeedbackPresenter existing =
            finalPanel.GetComponentInChildren<MinigameFeedbackPresenter>(true);

        if (existing != null)
        {
            return existing;
        }

        GameObject presenterObject =
            new GameObject("MinigameFeedbackPresenter", typeof(RectTransform));
        presenterObject.transform.SetParent(finalPanel, false);

        MinigameFeedbackPresenter presenter =
            presenterObject.AddComponent<MinigameFeedbackPresenter>();
        presenter.BuildUi(finalPanel);

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

        RefreshSize();
        ShowWaitingState();

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
            if (this == null || rootRect == null || !gameObject.activeInHierarchy)
            {
                yield break;
            }

            if (HasFeedbackForSession(sessionId))
            {
                RenderFeedback(MinigameLessonState.LastFeedback);
                waitCoroutine = null;
                yield break;
            }

            yield return null;
        }

        if (this != null && rootRect != null && !rendered)
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
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.localScale = Vector3.one;
        rootRect.anchoredPosition = new Vector2(0f, -20f);

        Image background = gameObject.AddComponent<Image>();
        background.color = ResolvePanelColor(finalPanel);

        GameObject viewportObject =
            new GameObject("Viewport", typeof(RectTransform));
        viewportObject.transform.SetParent(transform, false);

        RectTransform viewportRect =
            viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        viewportRect.offsetMin = new Vector2(18f, 14f);
        viewportRect.offsetMax = new Vector2(-18f, -14f);

        viewportObject.AddComponent<RectMask2D>();

        GameObject contentObject =
            new GameObject("Content", typeof(RectTransform));
        contentObject.transform.SetParent(viewportObject.transform, false);

        contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layoutGroup =
            contentObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(0, 0, 0, 0);
        layoutGroup.spacing = 5f;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        ContentSizeFitter contentFitter =
            contentObject.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect = gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = false;

        TMP_FontAsset font = ResolveFont(finalPanel);
        Color textColor = ResolveTextColor(finalPanel);

        titleText = CreateText("Title", font, textColor, 22f, FontStyles.Bold);
        titleText.alignment = TextAlignmentOptions.Center;

        messageText = CreateText("Message", font, textColor, 16f, FontStyles.Normal);
        resultText = CreateText("Result", font, textColor, 15f, FontStyles.Normal);
        reinforcementText =
            CreateText("Reinforcement", font, textColor, 15f, FontStyles.Normal);
        strengthText = CreateText("Strength", font, textColor, 15f, FontStyles.Normal);
        nextStepText = CreateText("NextStep", font, textColor, 15f, FontStyles.Normal);
        recommendationText =
            CreateText("Recommendation", font, textColor, 15f, FontStyles.Normal);

        RefreshSize();
    }

    private TextMeshProUGUI CreateText(
        string objectName,
        TMP_FontAsset font,
        Color color,
        float fontSize,
        FontStyles fontStyle
    )
    {
        GameObject textObject =
            new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(contentRect, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.color = color;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        return text;
    }

    private void ShowWaitingState()
    {
        SetText(titleText, "Retroalimentacion");
        SetText(
            messageText,
            "Preparando tu retroalimentacion personalizada..."
        );

        SetText(resultText, "");
        SetText(reinforcementText, "");
        SetText(strengthText, "");
        SetText(nextStepText, "");
        SetText(recommendationText, "");

        rendered = false;
        UpdateLayout();
    }

    private void ShowTimeoutState(string sessionId)
    {
        SetText(titleText, "Resumen guardado");
        SetText(
            messageText,
            "Tu resumen fue guardado. La recomendacion personalizada estara disponible mas adelante."
        );

        SetText(resultText, "");
        SetText(reinforcementText, "");
        SetText(strengthText, "");
        SetText(nextStepText, "");
        SetText(recommendationText, "");

        UpdateLayout();
        Debug.LogWarning(
            "Feedback UI: tiempo de espera agotado id=" + sessionId
        );
    }

    private void RenderFeedback(MinigameFeedbackResponse feedback)
    {
        if (rendered || !IsValidFeedback(feedback))
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
            SetText(nextStepText, feedback.next_step);
            SetText(recommendationText, "");
        }
        else
        {
            SetText(resultText, BuildResultText(feedback));
            SetText(reinforcementText, BuildReinforcementText(reinforcement));
            SetText(strengthText, BuildStrengthText(strength));
            SetText(nextStepText, feedback.next_step);
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
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        UpdateScrollState();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void UpdateScrollState()
    {
        if (scrollRect == null || scrollRect.viewport == null ||
            contentRect == null)
        {
            return;
        }

        float contentHeight = LayoutUtility.GetPreferredHeight(contentRect);
        float viewportHeight = scrollRect.viewport.rect.height;
        bool shouldScroll = contentHeight > viewportHeight + 1f;

        scrollRect.enabled = shouldScroll;
        scrollRect.vertical = shouldScroll;
    }

    private void RefreshSize()
    {
        if (rootRect == null || rootRect.parent == null)
        {
            return;
        }

        RectTransform parentRect = rootRect.parent as RectTransform;
        Vector2 parentSize = parentRect != null
            ? parentRect.rect.size
            : Vector2.zero;

        float width = parentSize.x > 1f
            ? Mathf.Max(MinWidth, parentSize.x * WidthRatio)
            : FallbackWidth;
        float height = parentSize.y > 1f
            ? Mathf.Max(210f, parentSize.y * MaxHeightRatio)
            : FallbackHeight;

        rootRect.sizeDelta = new Vector2(width, height);
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

    private TMP_FontAsset ResolveFont(Transform finalPanel)
    {
        TextMeshProUGUI source =
            finalPanel.GetComponentInChildren<TextMeshProUGUI>(true);

        return source != null ? source.font : null;
    }

    private Color ResolveTextColor(Transform finalPanel)
    {
        TextMeshProUGUI source =
            finalPanel.GetComponentInChildren<TextMeshProUGUI>(true);

        return source != null ? source.color : Color.white;
    }

    private Color ResolvePanelColor(Transform finalPanel)
    {
        Image source = finalPanel.GetComponent<Image>();

        if (source == null)
        {
            source = finalPanel.GetComponentInChildren<Image>(true);
        }

        Color color = source != null ? source.color : Color.black;
        color.a = Mathf.Clamp(color.a, 0.58f, 0.82f);

        return color;
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
