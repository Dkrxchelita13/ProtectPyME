using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum PilotStatusViewState
{
    ConsentRequired,
    PrePending,
    PreStarted,
    InterventionPending,
    PostAvailable,
    PostStarted,
    AllCompleted,
    InvalidPreStatus,
    InvalidPostStatus
}

public class PilotController : MonoBehaviour
{
    private const string ProfileSceneName = "MiPerfil";
    private const string LoginSceneName = "Login";
    private const int QuestionCount = 12;

    private readonly PilotState pilotState = new PilotState();

    private GameObject loadingPanel;
    private GameObject statusPanel;
    private GameObject consentPanel;
    private GameObject assessmentPanel;
    private GameObject completedPanel;
    private GameObject errorPanel;

    private TextMeshProUGUI loadingText;
    private TextMeshProUGUI statusTitle;
    private TextMeshProUGUI statusBody;
    private TextMeshProUGUI statusProgress;
    private Button statusPrimaryButton;
    private TextMeshProUGUI statusPrimaryText;
    private Button statusRevokeButton;

    private TextMeshProUGUI assessmentTitle;
    private TextMeshProUGUI assessmentProgress;
    private TextMeshProUGUI assessmentPrompt;
    private TextMeshProUGUI assessmentMessage;
    private RectTransform optionContainer;
    private Button nextButton;
    private TextMeshProUGUI nextButtonText;

    private TextMeshProUGUI completedBody;
    private TextMeshProUGUI errorTitle;
    private TextMeshProUGUI errorBody;
    private Button errorPrimaryButton;
    private TextMeshProUGUI errorPrimaryText;
    private Button errorSecondaryButton;
    private TextMeshProUGUI errorSecondaryText;

    private float questionStartedAt;
    private bool requestInProgress;

    private void Start()
    {
        Time.timeScale = 1f;
        EnsureEventSystem();
        EnsureApiManager();
        BuildInterface();
        StartCoroutine(RefreshPilotFlow());
    }

    public void GoBackToProfile()
    {
        SceneManager.LoadScene(ProfileSceneName);
    }

    private System.Collections.IEnumerator RefreshPilotFlow()
    {
        if (requestInProgress)
        {
            yield break;
        }

        requestInProgress = true;
        ShowLoading("Cargando estado del piloto...");

        PilotConsentResponse consent = null;
        string error = "";

        yield return StartCoroutine(
            APIManager.Instance.GetPilotConsent(
                response => consent = response,
                message => error = message
            )
        );

        if (!string.IsNullOrEmpty(error))
        {
            requestInProgress = false;
            ShowRequestError(error, RefreshPilotFlow);
            yield break;
        }

        if (!IsConsentActive(consent))
        {
            requestInProgress = false;
            ShowConsentPanel();
            yield break;
        }

        PilotAssessmentStatusResponse status = null;
        error = "";

        yield return StartCoroutine(
            APIManager.Instance.GetPilotAssessmentStatus(
                response => status = response,
                message => error = message
            )
        );

        requestInProgress = false;

        if (!string.IsNullOrEmpty(error))
        {
            ShowRequestError(error, RefreshPilotFlow);
            yield break;
        }

        RenderStatus(status);
    }

    private void ShowConsentPanel()
    {
        ShowOnly(consentPanel);
    }

    private void AcceptConsent()
    {
        if (!requestInProgress)
        {
            StartCoroutine(AcceptConsentCoroutine());
        }
    }

    private System.Collections.IEnumerator AcceptConsentCoroutine()
    {
        requestInProgress = true;
        ShowLoading("Registrando consentimiento...");

        string error = "";

        yield return StartCoroutine(
            APIManager.Instance.AcceptPilotConsent(
                response => { },
                message => error = message
            )
        );

        requestInProgress = false;

        if (!string.IsNullOrEmpty(error))
        {
            ShowRequestError(error, RefreshPilotFlow);
            yield break;
        }

        StartCoroutine(RefreshPilotFlow());
    }

    private void RenderStatus(PilotAssessmentStatusResponse status)
    {
        PilotStatusViewState viewState = ResolveStatusViewState(status);

        if (viewState == PilotStatusViewState.ConsentRequired)
        {
            ShowConsentPanel();
            return;
        }

        ShowOnly(statusPanel);
        statusRevokeButton.gameObject.SetActive(true);

        switch (viewState)
        {
            case PilotStatusViewState.AllCompleted:
                ShowCompletedPanel();
                return;

            case PilotStatusViewState.PreStarted:
                ConfigureStatus(
                    "Piloto academico",
                    "Continua la evaluacion inicial desde la primera pregunta pendiente.",
                    "",
                    "CONTINUAR EVALUACION INICIAL",
                    () => StartAssessment("PRE")
                );
                return;

            case PilotStatusViewState.PrePending:
                ConfigureStatus(
                    "Piloto academico",
                    "Tu participacion esta activa. Inicia la evaluacion inicial para el piloto academico.",
                    "",
                    "INICIAR EVALUACION INICIAL",
                    () => StartAssessment("PRE")
                );
                return;

            case PilotStatusViewState.PostStarted:
                ConfigureStatus(
                    "Evaluacion final",
                    "Continua la evaluacion final desde la primera pregunta pendiente.",
                    BuildInterventionText(status.intervention_progress),
                    "CONTINUAR EVALUACION FINAL",
                    () => StartAssessment("POST")
                );
                return;

            case PilotStatusViewState.PostAvailable:
                ConfigureStatus(
                    "Evaluacion final disponible",
                    "Ya puedes iniciar la evaluacion final del piloto academico.",
                    BuildInterventionText(status.intervention_progress),
                    "INICIAR EVALUACION FINAL",
                    () => StartAssessment("POST")
                );
                return;

            case PilotStatusViewState.InterventionPending:
                ConfigureStatus(
                    "Evaluacion inicial completada",
                    "Continua utilizando ProtectPYME antes de realizar la evaluacion final.",
                    BuildInterventionText(status.intervention_progress),
                    "ACTUALIZAR ESTADO",
                    () => StartCoroutine(RefreshPilotFlow())
                );
                return;

            case PilotStatusViewState.InvalidPreStatus:
            case PilotStatusViewState.InvalidPostStatus:
                ConfigureInvalidStatus(status);
                return;
        }
    }

    private void ConfigureStatus(
        string title,
        string body,
        string progress,
        string buttonLabel,
        UnityEngine.Events.UnityAction action
    )
    {
        statusTitle.text = title;
        statusBody.text = body;
        statusProgress.text = progress;
        statusPrimaryText.text = buttonLabel;
        statusPrimaryButton.onClick.RemoveAllListeners();
        statusPrimaryButton.onClick.AddListener(action);
        statusPrimaryButton.interactable = true;
    }

    private void StartAssessment(string phase)
    {
        if (!requestInProgress)
        {
            StartCoroutine(StartAssessmentCoroutine(phase));
        }
    }

    private System.Collections.IEnumerator StartAssessmentCoroutine(string phase)
    {
        requestInProgress = true;
        ShowLoading("Preparando evaluacion...");

        PilotAssessmentStartResponse response = null;
        string error = "";

        yield return StartCoroutine(
            APIManager.Instance.StartPilotAssessment(
                phase,
                data => response = data,
                message => error = message
            )
        );

        requestInProgress = false;

        if (!string.IsNullOrEmpty(error))
        {
            if (IsHttp(error, "409") || IsHttp(error, "403"))
            {
                StartCoroutine(RefreshPilotFlow());
                yield break;
            }

            ShowRequestError(error, () => StartAssessmentCoroutine(phase));
            yield break;
        }

        pilotState.Load(response);

        if (pilotState.IsComplete())
        {
            StartCoroutine(CompleteCurrentAssessment());
            yield break;
        }

        ShowCurrentQuestion();
    }

    private void ShowCurrentQuestion()
    {
        PilotAssessmentQuestion question = pilotState.GetCurrentQuestion();

        if (question == null)
        {
            StartCoroutine(CompleteCurrentAssessment());
            return;
        }

        ShowOnly(assessmentPanel);
        ClearOptionButtons();

        assessmentTitle.text =
            IsPostPhase(pilotState.Phase)
                ? "Evaluacion final"
                : "Evaluacion inicial";

        assessmentProgress.text =
            "Pregunta " +
            (pilotState.CurrentQuestionIndex + 1) +
            " de " +
            Mathf.Max(QuestionCount, pilotState.TotalQuestions);

        assessmentPrompt.text = question.prompt;
        assessmentMessage.text = "";
        pilotState.CurrentSelection = "";
        nextButton.interactable = true;
        nextButtonText.text = "SIGUIENTE";

        string[] options = question.options ?? new string[0];

        for (int i = 0; i < options.Length; i++)
        {
            string optionKey = ((char)('A' + i)).ToString();
            Button optionButton = CreateOptionButton(optionKey, options[i]);
            optionButton.transform.SetParent(optionContainer, false);
        }

        Canvas.ForceUpdateCanvases();
        questionStartedAt = Time.realtimeSinceStartup;
    }

    private Button CreateOptionButton(string optionKey, string optionText)
    {
        Button button = CreateButton(
            "Option" + optionKey,
            optionKey + ". " + optionText,
            optionContainer,
            new Color(0.94f, 0.97f, 1f),
            new Color(0.07f, 0.25f, 0.42f),
            34
        );

        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 84f;
        layout.preferredHeight = 112f;
        layout.flexibleWidth = 1f;

        button.onClick.AddListener(() => SelectOption(optionKey));
        return button;
    }

    private void SelectOption(string optionKey)
    {
        pilotState.CurrentSelection = optionKey;
        assessmentMessage.text = "";

        for (int i = 0; i < optionContainer.childCount; i++)
        {
            Image image = optionContainer.GetChild(i).GetComponent<Image>();
            Button button = optionContainer.GetChild(i).GetComponent<Button>();
            TextMeshProUGUI label =
                optionContainer.GetChild(i).GetComponentInChildren<TextMeshProUGUI>();

            if (image == null || button == null || label == null)
            {
                continue;
            }

            bool selected = label.text.StartsWith(optionKey + ".");
            image.color = selected
                ? new Color(1f, 0.88f, 0.20f)
                : new Color(0.94f, 0.97f, 1f);
        }
    }

    private void SubmitCurrentAnswer()
    {
        if (string.IsNullOrEmpty(pilotState.CurrentSelection))
        {
            assessmentMessage.text = "Selecciona una opcion para continuar.";
            return;
        }

        if (!requestInProgress)
        {
            StartCoroutine(SubmitCurrentAnswerCoroutine());
        }
    }

    private System.Collections.IEnumerator SubmitCurrentAnswerCoroutine()
    {
        PilotAssessmentQuestion question = pilotState.GetCurrentQuestion();

        if (question == null)
        {
            yield break;
        }

        requestInProgress = true;
        nextButton.interactable = false;
        nextButtonText.text = "ENVIANDO...";

        int responseTimeMs = Mathf.Clamp(
            Mathf.RoundToInt((Time.realtimeSinceStartup - questionStartedAt) * 1000f),
            0,
            3600000
        );

        PilotAssessmentAnswerRequest payload =
            new PilotAssessmentAnswerRequest(
                question.question_id,
                pilotState.CurrentSelection,
                responseTimeMs
            );

        PilotAssessmentAnswerResponse response = null;
        string error = "";

        yield return StartCoroutine(
            APIManager.Instance.SendPilotAssessmentAnswer(
                pilotState.CurrentAssessmentId,
                payload,
                data => response = data,
                message => error = message
            )
        );

        requestInProgress = false;

        if (!string.IsNullOrEmpty(error))
        {
            nextButton.interactable = true;
            nextButtonText.text = "SIGUIENTE";

            if (IsHttp(error, "409"))
            {
                StartAssessment(pilotState.Phase);
                yield break;
            }

            assessmentMessage.text = ToFriendlyError(error);
            yield break;
        }

        pilotState.MarkAnswered(response.question_id);

        if (pilotState.IsComplete())
        {
            StartCoroutine(CompleteCurrentAssessment());
            yield break;
        }

        ShowCurrentQuestion();
    }

    private System.Collections.IEnumerator CompleteCurrentAssessment()
    {
        if (requestInProgress)
        {
            yield break;
        }

        requestInProgress = true;
        ShowLoading("Guardando evaluacion...");

        string phase = pilotState.Phase;
        string error = "";

        yield return StartCoroutine(
            APIManager.Instance.CompletePilotAssessment(
                pilotState.CurrentAssessmentId,
                response => { },
                message => error = message
            )
        );

        requestInProgress = false;

        if (!string.IsNullOrEmpty(error))
        {
            ShowRequestError(error, () => CompleteCurrentAssessment());
            yield break;
        }

        if (IsPostPhase(phase))
        {
            ShowCompletedPanel();
        }
        else
        {
            StartCoroutine(RefreshPilotFlow());
        }
    }

    private void ShowRevokeConfirmation()
    {
        ShowErrorPanel(
            "Retirar participacion",
            "Al retirar tu participacion no podras continuar nuevas actividades del piloto mientras el consentimiento permanezca revocado. Esto no cerrara tu cuenta ni limitara el uso normal de ProtectPYME.",
            "RETIRAR",
            RevokeConsent,
            "CANCELAR",
            () => StartCoroutine(RefreshPilotFlow())
        );
    }

    private void RevokeConsent()
    {
        if (!requestInProgress)
        {
            StartCoroutine(RevokeConsentCoroutine());
        }
    }

    private System.Collections.IEnumerator RevokeConsentCoroutine()
    {
        requestInProgress = true;
        ShowLoading("Actualizando participacion...");

        string error = "";

        yield return StartCoroutine(
            APIManager.Instance.RevokePilotConsent(
                response => { },
                message => error = message
            )
        );

        requestInProgress = false;

        if (!string.IsNullOrEmpty(error))
        {
            ShowRequestError(error, RefreshPilotFlow);
            yield break;
        }

        StartCoroutine(RefreshPilotFlow());
    }

    private void ShowCompletedPanel()
    {
        ShowOnly(completedPanel);
        completedBody.text =
            "Evaluacion completada\n\nGracias por participar en el piloto academico de ProtectPYME.";
    }

    private void ShowLoading(string message)
    {
        ShowOnly(loadingPanel);
        loadingText.text = message;
    }

    private void ShowRequestError(
        string error,
        Func<System.Collections.IEnumerator> retryAction
    )
    {
        if (IsHttp(error, "401") || error == "NO_TOKEN")
        {
            ShowErrorPanel(
                "Sesion expirada",
                "Tu sesion expiro.",
                "INICIAR SESION",
                () => SceneManager.LoadScene(LoginSceneName),
                "REGRESAR",
                GoBackToProfile
            );
            return;
        }

        if (IsHttp(error, "403"))
        {
            ShowErrorPanel(
                "Consentimiento requerido",
                "Se requiere consentimiento activo para continuar con el piloto.",
                "ACTUALIZAR",
                () => StartCoroutine(RefreshPilotFlow()),
                "REGRESAR",
                GoBackToProfile
            );
            return;
        }

        if (IsHttp(error, "409"))
        {
            ShowErrorPanel(
                "Estado actualizado",
                "No fue posible continuar con ese paso. Actualiza el estado del piloto e intenta nuevamente.",
                "ACTUALIZAR",
                () => StartCoroutine(RefreshPilotFlow()),
                "REGRESAR",
                GoBackToProfile
            );
            return;
        }

        ShowErrorPanel(
            "Sin conexion",
            ToFriendlyError(error),
            "REINTENTAR",
            () => StartCoroutine(retryAction()),
            "REGRESAR",
            GoBackToProfile
        );
    }

    private string ToFriendlyError(string error)
    {
        if (string.IsNullOrEmpty(error) ||
            error.StartsWith("HTTP_0", StringComparison.OrdinalIgnoreCase) ||
            error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0 ||
            error.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Sin conexion. Revisa tu conexion e intenta nuevamente.";
        }

        if (IsHttp(error, "500"))
        {
            return "No fue posible completar la solicitud. Intenta nuevamente.";
        }

        return "No fue posible completar la solicitud. Intenta nuevamente.";
    }

    private void ShowErrorPanel(
        string title,
        string body,
        string primaryLabel,
        UnityEngine.Events.UnityAction primaryAction,
        string secondaryLabel,
        UnityEngine.Events.UnityAction secondaryAction
    )
    {
        ShowOnly(errorPanel);
        errorTitle.text = title;
        errorBody.text = body;
        errorPrimaryText.text = primaryLabel;
        errorSecondaryText.text = secondaryLabel;

        errorPrimaryButton.onClick.RemoveAllListeners();
        errorPrimaryButton.onClick.AddListener(primaryAction);
        errorSecondaryButton.onClick.RemoveAllListeners();
        errorSecondaryButton.onClick.AddListener(secondaryAction);
    }

    private bool IsConsentActive(PilotConsentResponse consent)
    {
        return consent != null &&
            consent.accepted &&
            string.IsNullOrEmpty(consent.revoked_at);
    }

    private bool IsStarted(PilotAssessmentStatusItem item)
    {
        return item != null &&
            string.Equals(item.status, "started", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCompleted(PilotAssessmentStatusItem item)
    {
        return item != null &&
            string.Equals(item.status, "completed", StringComparison.OrdinalIgnoreCase);
    }

    public static PilotStatusViewState ResolveStatusViewState(
        PilotAssessmentStatusResponse status
    )
    {
        if (status == null || !status.consent_active)
        {
            return PilotStatusViewState.ConsentRequired;
        }

        if (status.pre == null)
        {
            return PilotStatusViewState.PrePending;
        }

        if (IsStartedStatus(status.pre))
        {
            return PilotStatusViewState.PreStarted;
        }

        if (!IsCompletedStatus(status.pre))
        {
            return PilotStatusViewState.InvalidPreStatus;
        }

        if (status.post == null)
        {
            return status.post_eligible
                ? PilotStatusViewState.PostAvailable
                : PilotStatusViewState.InterventionPending;
        }

        if (IsStartedStatus(status.post))
        {
            return PilotStatusViewState.PostStarted;
        }

        if (IsCompletedStatus(status.post))
        {
            return PilotStatusViewState.AllCompleted;
        }

        return PilotStatusViewState.InvalidPostStatus;
    }

    private static bool IsStartedStatus(PilotAssessmentStatusItem item)
    {
        return item != null &&
            string.Equals(item.status, "started", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCompletedStatus(PilotAssessmentStatusItem item)
    {
        return item != null &&
            string.Equals(item.status, "completed", StringComparison.OrdinalIgnoreCase);
    }

    private void ConfigureInvalidStatus(PilotAssessmentStatusResponse status)
    {
        ConfigureStatus(
            "Estado del piloto no reconocido",
            "Actualiza el estado del piloto. Si el problema continua, reporta este paso.",
            BuildInterventionText(
                status == null ? null : status.intervention_progress
            ),
            "ACTUALIZAR ESTADO",
            () => StartCoroutine(RefreshPilotFlow())
        );
    }

    private bool IsPostPhase(string phase)
    {
        return string.Equals(phase, "POST", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsHttp(string error, string code)
    {
        return !string.IsNullOrEmpty(error) &&
            error.StartsWith("HTTP_" + code, StringComparison.OrdinalIgnoreCase);
    }

    private string BuildInterventionText(PilotInterventionProgress progress)
    {
        if (progress == null)
        {
            return "";
        }

        return "Requisitos minimos de participacion/exposicion:\n" +
            "Escenarios distintos realizados: " +
            progress.distinct_scenarios_completed +
            " / " +
            progress.required_distinct_scenarios +
            "\nCapacitaciones completadas: " +
            progress.completed_minigame_sessions +
            " / " +
            progress.required_minigame_sessions;
    }

    private void ClearOptionButtons()
    {
        for (int i = optionContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(optionContainer.GetChild(i).gameObject);
        }
    }

    private void ShowOnly(GameObject activePanel)
    {
        loadingPanel.SetActive(activePanel == loadingPanel);
        statusPanel.SetActive(activePanel == statusPanel);
        consentPanel.SetActive(activePanel == consentPanel);
        assessmentPanel.SetActive(activePanel == assessmentPanel);
        completedPanel.SetActive(activePanel == completedPanel);
        errorPanel.SetActive(activePanel == errorPanel);
    }

    private void BuildInterface()
    {
        Canvas canvas = CreateCanvas();
        RectTransform root = CreateRoot(canvas.transform);
        CreateHeader(root);

        RectTransform contentRoot =
            CreateRect("ContentRoot", root, new Vector2(0f, 0f), new Vector2(1f, 1f));
        contentRoot.offsetMin = new Vector2(42f, 36f);
        contentRoot.offsetMax = new Vector2(-42f, -112f);

        loadingPanel = CreatePanel("LoadingPanel", contentRoot);
        loadingText = CreateText("LoadingText", loadingPanel.transform, "", 38, FontStyles.Bold);

        statusPanel = CreatePanel("StatusPanel", contentRoot);
        statusTitle = CreateText("StatusTitle", statusPanel.transform, "", 46, FontStyles.Bold);
        statusBody = CreateText("StatusBody", statusPanel.transform, "", 34, FontStyles.Normal);
        statusProgress = CreateText("StatusProgress", statusPanel.transform, "", 32, FontStyles.Bold);
        statusPrimaryButton = CreateButton("StatusPrimaryButton", "", statusPanel.transform);
        statusPrimaryText = statusPrimaryButton.GetComponentInChildren<TextMeshProUGUI>();
        statusRevokeButton = CreateButton(
            "StatusRevokeButton",
            "RETIRAR PARTICIPACION",
            statusPanel.transform,
            new Color(0.85f, 0.91f, 0.97f),
            new Color(0.07f, 0.25f, 0.42f),
            28
        );
        statusRevokeButton.onClick.AddListener(ShowRevokeConfirmation);

        consentPanel = CreatePanel("ConsentPanel", contentRoot);
        CreateText(
            "ConsentTitle",
            consentPanel.transform,
            "Piloto academico",
            46,
            FontStyles.Bold
        );
        CreateScrollableText(
            "ConsentScroll",
            consentPanel.transform,
            "Texto provisional pendiente de revision institucional.\n\n" +
            "Te invitamos a participar voluntariamente en el piloto academico de ProtectPYME. Si aceptas, se registraran datos de uso educativo como tus evaluaciones, decisiones en escenarios y resultados de actividades. Para el analisis academico se utilizara un codigo de participante pseudonimizado. Puedes retirar tu participacion posteriormente. No participar no limita el uso normal de ProtectPYME."
        );
        CreateButton(
            "AcceptConsentButton",
            "ACEPTAR Y PARTICIPAR",
            consentPanel.transform
        ).onClick.AddListener(AcceptConsent);
        CreateButton(
            "DeclineConsentButton",
            "NO PARTICIPAR",
            consentPanel.transform,
            new Color(0.85f, 0.91f, 0.97f),
            new Color(0.07f, 0.25f, 0.42f),
            32
        ).onClick.AddListener(GoBackToProfile);

        BuildAssessmentPanel(contentRoot);
        BuildCompletedPanel(contentRoot);
        BuildErrorPanel(contentRoot);

        ShowOnly(loadingPanel);
    }

    private void BuildAssessmentPanel(RectTransform contentRoot)
    {
        assessmentPanel = CreatePanel("AssessmentPanel", contentRoot);
        assessmentTitle =
            CreateText("AssessmentTitle", assessmentPanel.transform, "", 44, FontStyles.Bold);
        assessmentProgress =
            CreateText("AssessmentProgress", assessmentPanel.transform, "", 30, FontStyles.Bold);

        RectTransform scrollRectTransform =
            CreateRect("QuestionScroll", assessmentPanel.transform, Vector2.zero, Vector2.one);
        LayoutElement scrollLayout = scrollRectTransform.gameObject.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.minHeight = 420f;

        ScrollRect scrollRect = scrollRectTransform.gameObject.AddComponent<ScrollRect>();
        Image scrollBackground = scrollRectTransform.gameObject.AddComponent<Image>();
        scrollBackground.color = new Color(1f, 1f, 1f, 0.08f);

        RectTransform viewport =
            CreateRect("Viewport", scrollRectTransform, Vector2.zero, Vector2.one);
        viewport.offsetMin = new Vector2(12f, 12f);
        viewport.offsetMax = new Vector2(-12f, -12f);
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content =
            CreateRect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
        content.pivot = new Vector2(0.5f, 1f);
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 18f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        assessmentPrompt =
            CreateText("AssessmentPrompt", content, "", 34, FontStyles.Bold);
        optionContainer = CreateRect(
            "Options",
            content,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        );
        VerticalLayoutGroup optionLayout =
            optionContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        optionLayout.spacing = 14f;
        optionLayout.childControlWidth = true;
        optionLayout.childControlHeight = true;
        optionLayout.childForceExpandWidth = true;
        optionLayout.childForceExpandHeight = false;
        ContentSizeFitter optionFitter =
            optionContainer.gameObject.AddComponent<ContentSizeFitter>();
        optionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        assessmentMessage =
            CreateText("AssessmentMessage", assessmentPanel.transform, "", 28, FontStyles.Bold);
        nextButton = CreateButton("NextButton", "SIGUIENTE", assessmentPanel.transform);
        nextButtonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
        nextButton.onClick.AddListener(SubmitCurrentAnswer);
    }

    private void BuildCompletedPanel(RectTransform contentRoot)
    {
        completedPanel = CreatePanel("CompletedPanel", contentRoot);
        completedBody =
            CreateText("CompletedBody", completedPanel.transform, "", 42, FontStyles.Bold);
        CreateButton(
            "CompletedBackButton",
            "REGRESAR A MI PERFIL",
            completedPanel.transform
        ).onClick.AddListener(GoBackToProfile);
        CreateButton(
            "CompletedRevokeButton",
            "RETIRAR PARTICIPACION",
            completedPanel.transform,
            new Color(0.85f, 0.91f, 0.97f),
            new Color(0.07f, 0.25f, 0.42f),
            28
        ).onClick.AddListener(ShowRevokeConfirmation);
    }

    private void BuildErrorPanel(RectTransform contentRoot)
    {
        errorPanel = CreatePanel("ErrorPanel", contentRoot);
        errorTitle = CreateText("ErrorTitle", errorPanel.transform, "", 44, FontStyles.Bold);
        errorBody = CreateText("ErrorBody", errorPanel.transform, "", 32, FontStyles.Normal);
        errorPrimaryButton = CreateButton("ErrorPrimaryButton", "", errorPanel.transform);
        errorPrimaryText = errorPrimaryButton.GetComponentInChildren<TextMeshProUGUI>();
        errorSecondaryButton = CreateButton(
            "ErrorSecondaryButton",
            "",
            errorPanel.transform,
            new Color(0.85f, 0.91f, 0.97f),
            new Color(0.07f, 0.25f, 0.42f),
            32
        );
        errorSecondaryText = errorSecondaryButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private RectTransform CreateRoot(Transform parent)
    {
        RectTransform root = CreateRect("PilotRoot", parent, Vector2.zero, Vector2.one);
        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.03f, 0.17f, 0.30f);
        return root;
    }

    private void CreateHeader(RectTransform root)
    {
        RectTransform header = CreateRect("Header", root, new Vector2(0f, 1f), new Vector2(1f, 1f));
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, 96f);
        header.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.padding = new RectOffset(42, 42, 18, 18);
        headerLayout.spacing = 24f;
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = true;

        Button backButton = CreateButton(
            "BackButton",
            "REGRESAR",
            header,
            new Color(0.85f, 0.91f, 0.97f),
            new Color(0.07f, 0.25f, 0.42f),
            28
        );
        LayoutElement backLayout = backButton.gameObject.GetComponent<LayoutElement>();
        backLayout.preferredWidth = 240f;
        backButton.onClick.AddListener(GoBackToProfile);

        TextMeshProUGUI title =
            CreateText("HeaderTitle", header, "Piloto academico", 44, FontStyles.Bold);
        LayoutElement titleLayout = title.gameObject.GetComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;
    }

    private GameObject CreatePanel(string name, Transform parent)
    {
        RectTransform panel = CreateRect(name, parent, Vector2.zero, Vector2.one);
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(1f, 1f, 1f, 0.08f);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(56, 56, 48, 48);
        layout.spacing = 24f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return panel.gameObject;
    }

    private TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string text,
        int fontSize,
        FontStyles style
    )
    {
        GameObject textObject = new GameObject(name);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Overflow;

        LayoutElement layout = textObject.AddComponent<LayoutElement>();
        layout.minHeight = fontSize + 18f;
        layout.flexibleWidth = 1f;

        return label;
    }

    private void CreateScrollableText(string name, Transform parent, string text)
    {
        RectTransform scrollRectTransform =
            CreateRect(name, parent, Vector2.zero, Vector2.one);
        LayoutElement scrollLayout = scrollRectTransform.gameObject.AddComponent<LayoutElement>();
        scrollLayout.minHeight = 260f;
        scrollLayout.flexibleHeight = 1f;

        ScrollRect scrollRect = scrollRectTransform.gameObject.AddComponent<ScrollRect>();
        Image image = scrollRectTransform.gameObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.06f);

        RectTransform viewport =
            CreateRect("Viewport", scrollRectTransform, Vector2.zero, Vector2.one);
        viewport.offsetMin = new Vector2(18f, 18f);
        viewport.offsetMax = new Vector2(-18f, -18f);
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content =
            CreateRect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
        content.pivot = new Vector2(0.5f, 1f);
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI label = CreateText("ConsentText", content, text, 32, FontStyles.Normal);
        label.alignment = TextAlignmentOptions.TopLeft;

        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
    }

    private Button CreateButton(
        string name,
        string label,
        Transform parent,
        Color? background = null,
        Color? textColor = null,
        int fontSize = 32
    )
    {
        RectTransform buttonRect = CreateRect(name, parent, Vector2.zero, Vector2.one);
        LayoutElement layout = buttonRect.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 72f;
        layout.preferredHeight = 82f;
        layout.flexibleWidth = 1f;

        Image image = buttonRect.gameObject.AddComponent<Image>();
        image.color = background ?? new Color(1f, 0.86f, 0.12f);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(1f, 0.93f, 0.38f);
        colors.pressedColor = new Color(0.89f, 0.72f, 0.08f);
        colors.disabledColor = new Color(0.55f, 0.57f, 0.58f);
        button.colors = colors;

        TextMeshProUGUI buttonText =
            CreateText("Text", buttonRect, label, fontSize, FontStyles.Bold);
        buttonText.color = textColor ?? new Color(0.07f, 0.25f, 0.42f);
        buttonText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = buttonText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 10f);
        textRect.offsetMax = new Vector2(-18f, -10f);

        return button;
    }

    private RectTransform CreateRect(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax
    )
    {
        GameObject gameObject = new GameObject(name);
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        return rect;
    }

    private void EnsureApiManager()
    {
        if (APIManager.Instance != null)
        {
            return;
        }

        GameObject apiManager = new GameObject("APIManager");
        apiManager.AddComponent<APIManager>();
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}
