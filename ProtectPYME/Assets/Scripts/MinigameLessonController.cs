using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MinigameLessonController : MonoBehaviour
{
    [SerializeField] private TMP_Text txtTitle;
    [SerializeField] private TMP_Text txtVulnerability;
    [SerializeField] private TMP_Text txtLearningObjective;
    [SerializeField] private TMP_Text txtExplanation;
    [SerializeField] private TMP_Text txtTip1;
    [SerializeField] private TMP_Text txtTip2;
    [SerializeField] private TMP_Text txtTip3;
    [SerializeField] private TMP_Text txtRecommendedAction;
    [SerializeField] private TMP_Text txtError;
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnRetry;
    [SerializeField] private Button btnBack;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject contentPanel;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private string backSceneName = "MenuMinijuegos";

    private bool isLoading;
    private bool lessonLoaded;
    private bool lessonLoadInProgress;
    private bool layoutRebuildPending;
    private Coroutine rebuildCoroutine;
    private MinigameLessonResponse currentLesson;

    private void Start()
    {
        SetButtonInteractable(btnStart, false);

        if (APIManager.Instance == null)
        {
            ShowError("No se encontro APIManager.");
            return;
        }

        if (!MinigameLessonState.IsPending)
        {
            ShowError("No hay una leccion pendiente para mostrar.");
            return;
        }

        if (!HasValidState())
        {
            ShowError("La informacion del minijuego esta incompleta.");
            return;
        }

        LoadLesson();
    }

    public void LoadLesson()
    {
        Debug.Log("Lesson: inicio LoadLesson");

        if (lessonLoadInProgress)
        {
            Debug.LogWarning(
                "Lesson: se ignoro una solicitud duplicada mientras la leccion esta cargando."
            );
            return;
        }

        if (isLoading)
        {
            return;
        }

        if (!HasEssentialReferences())
        {
            ShowError("Faltan referencias esenciales de la leccion.");
            return;
        }

        isLoading = true;
        lessonLoadInProgress = true;
        lessonLoaded = false;
        currentLesson = null;
        SetButtonInteractable(btnStart, false);

        ShowLoading();

        StartCoroutine(
            APIManager.Instance.GetMinigameLesson(
                MinigameLessonState.Topic,
                MinigameLessonState.Risk,
                MinigameLessonState.MinigameKey,
                OnLessonLoaded,
                OnLessonError
            )
        );
    }

    public void OnStartMinigame()
    {
        if (isLoading || !lessonLoaded || currentLesson == null)
        {
            Debug.LogWarning("No se puede iniciar: la leccion aun no esta lista.");
            return;
        }

        if (string.IsNullOrEmpty(MinigameLessonState.TargetScene))
        {
            ShowError("No hay escena destino configurada.");
            return;
        }

        string targetScene = MinigameLessonState.TargetScene;
        MinigameLessonState.Clear();
        SceneManager.LoadScene(targetScene);
    }

    public void OnRetry()
    {
        if (lessonLoadInProgress)
        {
            return;
        }

        LoadLesson();
    }

    public void OnBack()
    {
        MinigameLessonState.Clear();
        SceneManager.LoadScene(backSceneName);
    }

    private void OnLessonLoaded(MinigameLessonResponse response)
    {
        Debug.Log("Lesson: respuesta recibida");
        isLoading = false;
        lessonLoadInProgress = false;

        if (!IsValidLesson(response))
        {
            ShowError("La leccion recibida no tiene el formato esperado.");
            return;
        }

        Debug.Log("Lesson: respuesta validada");

        currentLesson = response;
        lessonLoaded = true;

        SetButtonInteractable(btnStart, false);
        SetButtonInteractable(btnRetry, true);

        SetText(txtTitle, response.title);
        SetText(txtVulnerability, response.vulnerability);
        SetText(txtLearningObjective, "");
        SetText(txtExplanation, BuildCompleteLessonBody(response));
        SetText(txtTip1, "");
        SetText(txtTip2, "");
        SetText(txtTip3, "");
        SetText(txtRecommendedAction, "");
        HideRedundantSections();

        Debug.Log("Lesson: textos asignados");

        SetActive(loadingPanel, false);
        SetActive(errorPanel, false);
        SetActive(contentPanel, true);

        Debug.Log("Lesson: panel activado");

        ScheduleLessonLayoutRebuild();
        SetButtonInteractable(btnStart, true);
    }

    private void OnLessonError(string error)
    {
        isLoading = false;
        lessonLoadInProgress = false;
        lessonLoaded = false;
        currentLesson = null;
        ShowError(error);
    }

    private bool HasValidState()
    {
        return !string.IsNullOrEmpty(MinigameLessonState.TargetScene)
            && !string.IsNullOrEmpty(MinigameLessonState.Topic)
            && !string.IsNullOrEmpty(MinigameLessonState.Risk)
            && !string.IsNullOrEmpty(MinigameLessonState.MinigameKey);
    }

    private bool HasEssentialReferences()
    {
        bool hasReferences = true;

        hasReferences &= HasReference(txtTitle, nameof(txtTitle));
        hasReferences &= HasReference(txtExplanation, nameof(txtExplanation));
        hasReferences &= HasReference(txtTip1, nameof(txtTip1));
        hasReferences &= HasReference(txtTip2, nameof(txtTip2));
        hasReferences &= HasReference(txtTip3, nameof(txtTip3));
        hasReferences &= HasReference(btnStart, nameof(btnStart));

        if (!hasReferences)
        {
            Debug.LogError("MinigameLessonController tiene referencias esenciales incompletas.");
        }

        return hasReferences;
    }

    private bool HasReference(Object reference, string referenceName)
    {
        if (reference != null)
        {
            return true;
        }

        Debug.LogError("MinigameLessonController: falta referencia " + referenceName + ".");
        return false;
    }

    private bool IsValidLesson(MinigameLessonResponse response)
    {
        return response != null
            && !string.IsNullOrEmpty(response.title)
            && !string.IsNullOrEmpty(response.explanation)
            && !string.IsNullOrEmpty(response.minigame)
            && !string.IsNullOrEmpty(response.visual_key)
            && response.tips != null
            && response.tips.Length == 3
            && response.key_concepts != null
            && response.key_concepts.Length >= 2
            && response.key_concepts.Length <= 4
            && response.practical_example != null
            && response.practical_example.steps != null
            && response.practical_example.steps.Length >= 3
            && response.practical_example.steps.Length <= 5
            && response.common_mistake != null
            && response.quick_check != null
            && response.quick_check.options != null
            && response.quick_check.options.Length == 3
            && response.quick_check.correct_option >= 0
            && response.quick_check.correct_option <= 2;
    }

    private void ShowLoading()
    {
        CancelPendingLayoutRebuild();
        SetActive(loadingPanel, true);
        SetActive(contentPanel, false);
        SetActive(errorPanel, false);
        SetButtonInteractable(btnStart, false);
        SetButtonInteractable(btnRetry, false);
    }

    private void ScheduleLessonLayoutRebuild()
    {
        if (layoutRebuildPending && rebuildCoroutine != null)
        {
            StopCoroutine(rebuildCoroutine);
        }

        layoutRebuildPending = true;
        rebuildCoroutine = StartCoroutine(RebuildLessonLayoutNextFrame());
        Debug.Log("Lesson: rebuild programado");
    }

    private IEnumerator RebuildLessonLayoutNextFrame()
    {
        yield return null;

        ScrollRect lessonScrollRect = contentPanel != null
            ? contentPanel.GetComponentInChildren<ScrollRect>(true)
            : null;

        if (lessonScrollRect == null || lessonScrollRect.content == null)
        {
            Debug.LogWarning("MinigameLessonController: ScrollRect de leccion incompleto.");
            layoutRebuildPending = false;
            rebuildCoroutine = null;
            yield break;
        }

        RectTransform contentRect = lessonScrollRect.content;
        RectTransform explanationSection = GetParentSection(txtExplanation);

        ConfigureContentFitter(contentRect);
        DisableSectionFitter(explanationSection);
        HideStaticExplanationLabel(explanationSection);

        ApplyPreferredTextHeight(txtExplanation, 40f);

        ApplyPreferredSectionHeight(explanationSection, 36f);

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        lessonScrollRect.StopMovement();
        lessonScrollRect.verticalNormalizedPosition = 1f;

        Debug.Log(
            $"Lesson: layout final texto={GetRectHeight(txtExplanation != null ? txtExplanation.rectTransform : null)} "
            + $"seccion={GetRectHeight(explanationSection)} "
            + $"content={GetRectHeight(contentRect)}"
        );

        layoutRebuildPending = false;
        rebuildCoroutine = null;
    }

    private void CancelPendingLayoutRebuild()
    {
        if (rebuildCoroutine != null)
        {
            StopCoroutine(rebuildCoroutine);
            rebuildCoroutine = null;
        }

        layoutRebuildPending = false;
    }

    private void ShowError(string error)
    {
        CancelPendingLayoutRebuild();
        SetActive(loadingPanel, false);
        SetActive(contentPanel, false);
        SetActive(errorPanel, true);
        SetButtonInteractable(btnStart, false);
        SetButtonInteractable(btnRetry, true);

        string message = string.IsNullOrEmpty(error)
            ? "No se pudo cargar la leccion."
            : error;

        SetText(txtError, message);
        Debug.LogWarning("MinigameLessonController: " + message);
    }

    private string BuildCompleteLessonBody(MinigameLessonResponse lesson)
    {
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("QU\u00c9 APRENDER\u00c1S");
        builder.AppendLine();
        builder.AppendLine(SanitizeText(lesson.learning_objective));
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("EXPLICACI\u00d3N");
        builder.AppendLine();
        builder.AppendLine(SanitizeText(lesson.explanation));
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("CONCEPTOS CLAVE");
        builder.AppendLine();

        for (int i = 0; i < lesson.key_concepts.Length; i++)
        {
            LessonConcept concept = lesson.key_concepts[i];

            if (concept == null)
            {
                continue;
            }

            builder.AppendLine(SanitizeText(concept.term));
            builder.AppendLine("Definicion: " + SanitizeText(concept.definition));
            builder.AppendLine("\u00bfPor qu\u00e9 importa?: " + SanitizeText(concept.why_it_matters));
            builder.AppendLine("Ejemplo: " + SanitizeText(concept.example));
            builder.AppendLine();
            builder.AppendLine();
        }

        builder.AppendLine("EJEMPLO PR\u00c1CTICO");
        builder.AppendLine();
        builder.AppendLine(SanitizeText(lesson.practical_example.title));
        builder.AppendLine();

        for (int i = 0; i < lesson.practical_example.steps.Length; i++)
        {
            builder.Append(i + 1);
            builder.Append(". ");
            builder.AppendLine(SanitizeText(lesson.practical_example.steps[i]));
        }

        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("ERROR FRECUENTE");
        builder.AppendLine();
        builder.AppendLine(SanitizeText(lesson.common_mistake.title));
        builder.AppendLine(SanitizeText(lesson.common_mistake.explanation));
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("RECOMENDACIONES");
        builder.AppendLine();
        builder.AppendLine(FormatTip(lesson.tips[0]));
        builder.AppendLine(FormatTip(lesson.tips[1]));
        builder.AppendLine(FormatTip(lesson.tips[2]));
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("COMPROBACI\u00d3N R\u00c1PIDA");
        builder.AppendLine();
        builder.AppendLine(SanitizeText(lesson.quick_check.question));
        builder.AppendLine();
        builder.AppendLine("A) " + SanitizeText(lesson.quick_check.options[0]));
        builder.AppendLine("B) " + SanitizeText(lesson.quick_check.options[1]));
        builder.AppendLine("C) " + SanitizeText(lesson.quick_check.options[2]));
        builder.AppendLine();
        builder.Append("Piensa tu respuesta antes de comenzar el minijuego.");
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("ANTES DE COMENZAR");
        builder.AppendLine();
        builder.Append(SanitizeText(lesson.recommended_action));

        return builder.ToString();
    }

    private void HideRedundantSections()
    {
        SetActive(GetParentGameObject(txtLearningObjective), false);
        SetActive(GetParentGameObject(txtTip1), false);
        SetActive(GetParentGameObject(txtRecommendedAction), false);
    }

    private float ApplyPreferredTextHeight(TMP_Text textComponent, float extraPadding)
    {
        if (textComponent == null)
        {
            return 0f;
        }

        RectTransform textRect = textComponent.rectTransform;
        if (textRect == null)
        {
            return 0f;
        }

        float availableWidth = textRect.rect.width;

        if (availableWidth <= 10f && textRect.parent is RectTransform parentRect)
        {
            availableWidth = parentRect.rect.width;
        }

        availableWidth = Mathf.Max(10f, availableWidth - 8f);

        Vector2 preferredSize = textComponent.GetPreferredValues(
            textComponent.text,
            availableWidth,
            Mathf.Infinity
        );

        LayoutElement layoutElement =
            textComponent.GetComponent<LayoutElement>();

        if (layoutElement == null)
        {
            layoutElement =
                textComponent.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.ignoreLayout = false;
        layoutElement.minHeight = 0;
        layoutElement.preferredHeight =
            Mathf.Ceil(preferredSize.y + extraPadding);
        layoutElement.flexibleHeight = 0;

        textComponent.richText = false;
        textComponent.enableWordWrapping = true;
        textComponent.overflowMode = TextOverflowModes.Overflow;

        return layoutElement.preferredHeight;
    }

    private float ApplyPreferredSectionHeight(RectTransform section, float extraMargin)
    {
        if (section == null)
        {
            return 0f;
        }

        VerticalLayoutGroup layoutGroup =
            section.GetComponent<VerticalLayoutGroup>();

        float calculatedHeight = 0f;
        int activeChildren = 0;

        if (layoutGroup != null)
        {
            calculatedHeight += layoutGroup.padding.top;
            calculatedHeight += layoutGroup.padding.bottom;
        }

        for (int i = 0; i < section.childCount; i++)
        {
            RectTransform child =
                section.GetChild(i) as RectTransform;

            if (child == null || !child.gameObject.activeInHierarchy)
            {
                continue;
            }

            calculatedHeight += LayoutUtility.GetPreferredHeight(child);
            activeChildren++;
        }

        if (layoutGroup != null && activeChildren > 1)
        {
            calculatedHeight += layoutGroup.spacing * (activeChildren - 1);
        }

        calculatedHeight += extraMargin;

        LayoutElement sectionLayout =
            section.GetComponent<LayoutElement>();

        if (sectionLayout == null)
        {
            sectionLayout = section.gameObject.AddComponent<LayoutElement>();
        }

        sectionLayout.ignoreLayout = false;
        sectionLayout.minHeight = 0;
        sectionLayout.preferredHeight = Mathf.Ceil(calculatedHeight);
        sectionLayout.flexibleHeight = 0;

        return sectionLayout.preferredHeight;
    }

    private void ConfigureContentFitter(RectTransform contentRect)
    {
        if (contentRect == null)
        {
            return;
        }

        ContentSizeFitter contentFitter =
            contentRect.GetComponent<ContentSizeFitter>();

        if (contentFitter == null)
        {
            return;
        }

        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void DisableSectionFitter(RectTransform section)
    {
        if (section == null)
        {
            return;
        }

        ContentSizeFitter sectionFitter =
            section.GetComponent<ContentSizeFitter>();

        if (sectionFitter != null && sectionFitter.enabled)
        {
            sectionFitter.enabled = false;
        }
    }

    private void HideStaticExplanationLabel(RectTransform explanationSection)
    {
        if (explanationSection == null || txtExplanation == null)
        {
            return;
        }

        for (int i = 0; i < explanationSection.childCount; i++)
        {
            TMP_Text childText =
                explanationSection.GetChild(i).GetComponent<TMP_Text>();

            if (childText == null || childText == txtExplanation)
            {
                continue;
            }

            childText.gameObject.SetActive(false);
            return;
        }
    }

    private float GetRectHeight(RectTransform rectTransform)
    {
        return rectTransform != null
            ? rectTransform.rect.height
            : 0f;
    }

    private RectTransform GetParentSection(TMP_Text textComponent)
    {
        if (textComponent == null ||
            textComponent.rectTransform == null)
        {
            return null;
        }

        return textComponent.rectTransform.parent as RectTransform;
    }

    private GameObject GetParentGameObject(TMP_Text textComponent)
    {
        RectTransform section = GetParentSection(textComponent);
        return section != null
            ? section.gameObject
            : null;
    }

    private string FormatTip(string tip)
    {
        return "\u2022 " + SanitizeText(tip);
    }

    private void SetText(TMP_Text text, string value)
    {
        if (text == null)
        {
            Debug.LogWarning("Referencia TMP_Text no asignada.");
            return;
        }

        text.text = SanitizeText(value);
        text.richText = false;
        text.enableWordWrapping = true;
    }

    private string SanitizeText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        char[] chars = value.ToCharArray();
        StringBuilder builder = new StringBuilder(chars.Length);

        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsControl(chars[i]) || chars[i] == '\n')
            {
                builder.Append(chars[i]);
            }
        }

        return builder.ToString();
    }

    private void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }
}
