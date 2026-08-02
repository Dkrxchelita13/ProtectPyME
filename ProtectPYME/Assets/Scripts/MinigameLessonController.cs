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
        lessonLoaded = false;
        currentLesson = null;

        ShowLoading();

        StartCoroutine(
            APIManager.Instance.GetMinigameLesson(
                MinigameLessonState.Topic,
                MinigameLessonState.Risk,
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
        LoadLesson();
    }

    public void OnBack()
    {
        MinigameLessonState.Clear();
        SceneManager.LoadScene(backSceneName);
    }

    private void OnLessonLoaded(MinigameLessonResponse response)
    {
        isLoading = false;

        if (!IsValidLesson(response))
        {
            ShowError("La leccion recibida no tiene el formato esperado.");
            return;
        }

        currentLesson = response;
        lessonLoaded = true;

        SetActive(loadingPanel, false);
        SetActive(errorPanel, false);
        SetActive(contentPanel, true);
        SetButtonInteractable(btnStart, false);
        SetButtonInteractable(btnRetry, true);

        SetText(txtTitle, response.title);
        SetText(txtVulnerability, response.vulnerability);
        SetText(txtLearningObjective, response.learning_objective);
        SetText(txtExplanation, response.explanation);
        SetText(txtTip1, FormatTip(response.tips[0]));
        SetText(txtTip2, FormatTip(response.tips[1]));
        SetText(txtTip3, FormatTip(response.tips[2]));
        SetText(txtRecommendedAction, response.recommended_action);

        RebuildLessonLayout();
        SetButtonInteractable(btnStart, true);
    }

    private void OnLessonError(string error)
    {
        isLoading = false;
        lessonLoaded = false;
        currentLesson = null;
        ShowError(error);
    }

    private bool HasValidState()
    {
        return !string.IsNullOrEmpty(MinigameLessonState.TargetScene)
            && !string.IsNullOrEmpty(MinigameLessonState.Topic)
            && !string.IsNullOrEmpty(MinigameLessonState.Risk);
    }

    private bool HasEssentialReferences()
    {
        bool hasReferences = true;

        hasReferences &= HasReference(txtTitle, nameof(txtTitle));
        hasReferences &= HasReference(txtVulnerability, nameof(txtVulnerability));
        hasReferences &= HasReference(txtLearningObjective, nameof(txtLearningObjective));
        hasReferences &= HasReference(txtExplanation, nameof(txtExplanation));
        hasReferences &= HasReference(txtTip1, nameof(txtTip1));
        hasReferences &= HasReference(txtTip2, nameof(txtTip2));
        hasReferences &= HasReference(txtTip3, nameof(txtTip3));
        hasReferences &= HasReference(txtRecommendedAction, nameof(txtRecommendedAction));
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
            && response.tips != null
            && response.tips.Length == 3;
    }

    private void ShowLoading()
    {
        SetActive(loadingPanel, true);
        SetActive(contentPanel, false);
        SetActive(errorPanel, false);
        SetButtonInteractable(btnStart, false);
        SetButtonInteractable(btnRetry, false);
    }

    private void ShowContent()
    {
        SetActive(loadingPanel, false);
        SetActive(errorPanel, false);
        SetActive(contentPanel, true);
        RebuildLessonLayout();
        SetButtonInteractable(btnStart, true);
        SetButtonInteractable(btnRetry, true);
    }

    private void RebuildLessonLayout()
    {
        ScrollRect lessonScrollRect = contentPanel != null
            ? contentPanel.GetComponentInChildren<ScrollRect>(true)
            : null;

        if (lessonScrollRect == null || lessonScrollRect.content == null)
        {
            Debug.LogWarning("MinigameLessonController: ScrollRect de leccion incompleto.");
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(lessonScrollRect.content);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(lessonScrollRect.content);
        lessonScrollRect.verticalNormalizedPosition = 1f;
    }

    private void ShowError(string error)
    {
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

        text.richText = false;
        text.enableWordWrapping = true;
        text.text = SanitizeText(value);
        text.ForceMeshUpdate();
    }

    private string SanitizeText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        char[] chars = value.ToCharArray();
        System.Text.StringBuilder builder =
            new System.Text.StringBuilder(chars.Length);

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
