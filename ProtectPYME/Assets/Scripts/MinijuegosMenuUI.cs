using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class MinijuegosMenuUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI txtTituloReforzamiento;

    private const string LessonSceneName = "LeccionMinijuego";
    private bool lessonRiskRequestInProgress;

    void Start()
    {
        ActualizarTituloMinijuegos();
    }

    public void OpenQuizLesson()
    {
        OpenLesson("quiz", "Kahoot");
    }

    public void OpenWordsearchLesson()
    {
        OpenLesson("wordsearch", "SopaLetras");
    }

    public void OpenCrosswordLesson()
    {
        OpenLesson("crossword", "Crucigrama");
    }

    void ActualizarTituloMinijuegos()
    {
        if (txtTituloReforzamiento == null) return;

        // Leemos la sugerencia almacenada en la memoria global de la IA
        string temaReforzamiento = AIState.RecommendedTraining;

        if (!string.IsNullOrEmpty(temaReforzamiento))
        {
            // Traducimos el tema que viene de la IA a un título legible para el jugador
            txtTituloReforzamiento.text = FormatearTitulo(temaReforzamiento);
        }
        else
        {
            // Si por alguna razón la IA no tiene datos aún, hacemos una consulta rápida al API
            CargarRiesgoDesdeAPI();
        }
    }

    void CargarRiesgoDesdeAPI()
    {
        if (APIManager.Instance == null) return;

        StartCoroutine(APIManager.Instance.GetAIRisk(
            onSuccess: (data) =>
            {
                if (txtTituloReforzamiento != null)
                {
                    txtTituloReforzamiento.text = FormatearTitulo(data.recommended_training);
                }
            },
            onError: (error) =>
            {
                // Título por defecto en caso de no tener internet/error
                txtTituloReforzamiento.text = "REFORZAMIENTO GENERAL";
            }
        ));
    }

    private string FormatearTitulo(string tema)
    {
        if (string.IsNullOrEmpty(tema)) return "REFORZAMIENTO GENERAL";

        string temaLiso = tema.ToLower().Trim();

        // Mapeo adaptativo según las temáticas de tus 3 escenarios
        if (temaLiso.Contains("phishing") || temaLiso.Contains("correo") || temaLiso.Contains("1"))
        {
            return "REFORZAMIENTO: DETECCIÓN DE PHISHING";
        }
        else if (temaLiso.Contains("contraseña") || temaLiso.Contains("password") || temaLiso.Contains("acceso") || temaLiso.Contains("2"))
        {
            return "REFORZAMIENTO: GESTIÓN DE CONTRASEÑAS";
        }
        else if (temaLiso.Contains("usb") || temaLiso.Contains("baiting") || temaLiso.Contains("extraible") || temaLiso.Contains("3"))
        {
            return "REFORZAMIENTO: SEGURIDAD EN MEDIOS EXTRAÍBLES";
        }

        // Si la IA devuelve la categoría directamente (ej. "Phishing"), la ponemos en mayúsculas
        return "REFORZAMIENTO: " + tema.ToUpper();
    }

    private void OpenLesson(string minigameKey, string targetScene)
    {
        if (TryOpenLessonWithCurrentState(minigameKey, targetScene))
        {
            return;
        }

        if (lessonRiskRequestInProgress)
        {
            Debug.LogWarning(
                "No se abrira la leccion porque ya se esta recuperando el estado de riesgo."
            );
            return;
        }

        if (APIManager.Instance == null)
        {
            Debug.LogError(
                "No se puede abrir la leccion: AIState no contiene topic/risk validos y APIManager no esta disponible."
            );
            return;
        }

        lessonRiskRequestInProgress = true;

        StartCoroutine(APIManager.Instance.GetAIRisk(
            onSuccess: (data) =>
            {
                lessonRiskRequestInProgress = false;

                if (!TryOpenLessonWithCurrentState(minigameKey, targetScene))
                {
                    Debug.LogError(
                        "No se puede abrir la leccion: /ai/risk/me no devolvio topic/risk validos."
                    );
                }
            },
            onError: (error) =>
            {
                lessonRiskRequestInProgress = false;
                MinigameLessonState.Clear();
                Debug.LogError(
                    "No se pudo recuperar el estado de riesgo para abrir la leccion: "
                    + error
                );
            }
        ));
    }

    private bool TryOpenLessonWithCurrentState(string minigameKey, string targetScene)
    {
        string topic = NormalizeTopic(AIState.RecommendedTraining);
        string risk = NormalizeRisk(AIState.RiskLevel);

        if (!IsValidTopic(topic) || !IsValidRisk(risk))
        {
            Debug.LogError(
                "No se abrira la leccion: topic o risk invalido. "
                + "topic='"
                + SafeLogValue(topic)
                + "', risk='"
                + SafeLogValue(risk)
                + "'."
            );
            return false;
        }

        if (LooksLikeInitialAiState(topic, risk))
        {
            Debug.LogError(
                "No se abrira la leccion: AIState parece no estar inicializado; se intentara recuperar /ai/risk/me."
            );
            return false;
        }

        try
        {
            MinigameLessonState.Prepare(
                minigameKey,
                targetScene,
                topic,
                risk
            );

            SceneManager.LoadScene(LessonSceneName);
            return true;
        }
        catch (System.Exception exception)
        {
            MinigameLessonState.Clear();
            Debug.LogError(
                "No se pudo preparar la leccion del minijuego: "
                + exception.Message
            );
            return true;
        }
    }

    private string NormalizeTopic(string topic)
    {
        return string.IsNullOrEmpty(topic)
            ? ""
            : topic.Trim().ToLowerInvariant();
    }

    private string NormalizeRisk(string risk)
    {
        return string.IsNullOrEmpty(risk)
            ? ""
            : risk.Trim().ToLowerInvariant();
    }

    private bool IsValidTopic(string topic)
    {
        return topic == "phishing"
            || topic == "passwords"
            || topic == "malware"
            || topic == "wifi";
    }

    private bool IsValidRisk(string risk)
    {
        return risk == "alto"
            || risk == "medio"
            || risk == "bajo";
    }

    private bool LooksLikeInitialAiState(string topic, string risk)
    {
        return topic == "phishing"
            && risk == "alto"
            && string.IsNullOrEmpty(AIState.RiskSource)
            && AIState.BehavioralDecisions == 0
            && !AIState.SufficientBehavioralData
            && !AIState.SurveyCompleted;
    }

    private string SafeLogValue(string value)
    {
        return string.IsNullOrEmpty(value) ? "<vacio>" : value;
    }
}
