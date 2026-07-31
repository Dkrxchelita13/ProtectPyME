using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class PreguntaEncuesta
{
    public string idPregunta;

    [TextArea(2, 4)]
    public string enunciado;

    public string opcionA;
    public string opcionB;
    public string opcionC;
}

[System.Serializable]
public class RespuestaItem
{
    public string id_pregunta;
    public string respuesta_seleccionada;
}

[System.Serializable]
public class EnvioRespuestasEncuesta
{
    public List<RespuestaItem> respuestas = new List<RespuestaItem>();
}

public class EncuestaManager : MonoBehaviour
{
    private const string SurveyVersion = "diagnostic_v1";

    private static readonly string[] QuestionIds =
    {
        "P1_PHISH_HABITO",
        "P2_PHISH_CONOCIMIENTO",
        "P3_PASS_HABITO",
        "P4_PASS_CONOCIMIENTO",
        "P5_USB_HABITO",
        "P6_USB_CONOCIMIENTO"
    };

    private static readonly string[] QuestionCategories =
    {
        "phishing",
        "phishing",
        "passwords",
        "passwords",
        "malware",
        "malware"
    };

    [Header("Referencias de UI en Pantalla")]
    public TextMeshProUGUI txtPregunta;
    public TextMeshProUGUI txtOpcionA;
    public TextMeshProUGUI txtOpcionB;
    public TextMeshProUGUI txtOpcionC;
    public TextMeshProUGUI txtContador;

    [Header("Botones de Respuesta")]
    public Button btnOpcionA;
    public Button btnOpcionB;
    public Button btnOpcionC;

    [Header("Configuracion de Navegacion")]
    [Tooltip("Escribe el nombre EXACTO de la escena a la que ira al terminar")]
    public string nombreEscenaSiguiente = "MenuPrincipal";

    [Header("Preguntas Fijas")]
    public List<PreguntaEncuesta> preguntas = new List<PreguntaEncuesta>();

    private int indiceActual = 0;
    private bool envioEnProgreso;
    private List<RespuestaItem> listaRespuestas = new List<RespuestaItem>();

    private void Start()
    {
        if (btnOpcionA != null)
        {
            btnOpcionA.onClick.AddListener(() => SeleccionarOpcion("A"));
        }

        if (btnOpcionB != null)
        {
            btnOpcionB.onClick.AddListener(() => SeleccionarOpcion("B"));
        }

        if (btnOpcionC != null)
        {
            btnOpcionC.onClick.AddListener(() => SeleccionarOpcion("C"));
        }

        MostrarPreguntaActual();
    }

    private void MostrarPreguntaActual()
    {
        if (preguntas == null || preguntas.Count == 0)
        {
            MostrarErrorEncuesta("No hay preguntas configuradas en el Inspector.");
            SetBotonesRespuestasInteractables(false);
            return;
        }

        if (indiceActual < 0 || indiceActual >= preguntas.Count)
        {
            MostrarErrorEncuesta("Indice de pregunta fuera de rango.");
            SetBotonesRespuestasInteractables(false);
            return;
        }

        PreguntaEncuesta pregunta = preguntas[indiceActual];

        if (txtPregunta != null)
        {
            txtPregunta.text = pregunta.enunciado;
        }

        if (txtOpcionA != null)
        {
            txtOpcionA.text = pregunta.opcionA;
        }

        if (txtOpcionB != null)
        {
            txtOpcionB.text = pregunta.opcionB;
        }

        if (txtOpcionC != null)
        {
            txtOpcionC.text = pregunta.opcionC;
        }

        if (txtContador != null)
        {
            txtContador.text = (indiceActual + 1) + "/" + preguntas.Count;
        }
    }

    public void SeleccionarOpcion(string letraOpcion)
    {
        if (envioEnProgreso)
        {
            return;
        }

        if (preguntas == null || preguntas.Count == 0)
        {
            MostrarErrorEncuesta("No hay preguntas para responder.");
            return;
        }

        if (listaRespuestas.Count >= preguntas.Count)
        {
            FinalizarEncuesta();
            return;
        }

        string opcionNormalizada = NormalizarOpcion(letraOpcion);

        if (string.IsNullOrEmpty(opcionNormalizada))
        {
            MostrarErrorEncuesta("Opcion de respuesta invalida.");
            return;
        }

        RespuestaItem respuesta = new RespuestaItem
        {
            id_pregunta = ObtenerQuestionId(indiceActual),
            respuesta_seleccionada = opcionNormalizada
        };

        listaRespuestas.Add(respuesta);
        Debug.Log(
            "Pregunta " + (indiceActual + 1) + ": respondio " + opcionNormalizada
        );

        if (indiceActual < preguntas.Count - 1)
        {
            indiceActual++;
            MostrarPreguntaActual();
        }
        else
        {
            FinalizarEncuesta();
        }
    }

    private void FinalizarEncuesta()
    {
        if (envioEnProgreso)
        {
            return;
        }

        Debug.Log("Encuesta completada. Enviando diagnostico...");
        StartCoroutine(EnviarDatosYCambiarEscena());
    }

    private IEnumerator EnviarDatosYCambiarEscena()
    {
        envioEnProgreso = true;
        SetBotonesRespuestasInteractables(false);

        string validationError = ValidarRespuestas();

        if (!string.IsNullOrEmpty(validationError))
        {
            RehabilitarEncuesta(validationError);
            yield break;
        }

        if (APIManager.Instance == null)
        {
            RehabilitarEncuesta("APIManager no esta disponible.");
            yield break;
        }

        SurveySubmitRequest request = ConstruirSolicitudEncuesta();
        SurveySubmitResponse response = null;
        string error = "";

        yield return StartCoroutine(APIManager.Instance.SubmitSurvey(
            request,
            submitResponse => response = submitResponse,
            requestError => error = requestError
        ));

        if (response != null && response.submitted)
        {
            AplicarDiagnostico(response);
            SceneManager.LoadScene(nombreEscenaSiguiente);
            yield break;
        }

        if (EsConflicto(error))
        {
            yield return StartCoroutine(ContinuarSiEncuestaYaExiste());
            yield break;
        }

        if (string.IsNullOrEmpty(error))
        {
            error = "Respuesta invalida del servidor.";
        }

        RehabilitarEncuesta("No se pudo guardar la encuesta: " + error);
    }

    private IEnumerator ContinuarSiEncuestaYaExiste()
    {
        AIState.SurveyResultPending = false;

        SurveyStatusResponse status = null;
        string statusError = "";

        yield return StartCoroutine(APIManager.Instance.GetSurveyStatus(
            response => status = response,
            requestError => statusError = requestError
        ));

        if (status != null && status.has_submitted)
        {
            AplicarDiagnostico(status);
            AIState.SurveyResultPending = false;
            SceneManager.LoadScene("MenuPrincipal");
            yield break;
        }

        if (string.IsNullOrEmpty(statusError))
        {
            statusError = "No se pudo confirmar la encuesta existente.";
        }

        RehabilitarEncuesta(
            "La encuesta ya existe, pero no se pudo continuar: " + statusError
        );
    }

    private SurveySubmitRequest ConstruirSolicitudEncuesta()
    {
        SurveySubmitRequest request = new SurveySubmitRequest
        {
            survey_version = SurveyVersion,
            answers = new List<SurveyAnswerSubmit>()
        };

        for (int i = 0; i < listaRespuestas.Count; i++)
        {
            string questionId = listaRespuestas[i].id_pregunta;

            request.answers.Add(new SurveyAnswerSubmit
            {
                question_id = questionId,
                category = ObtenerCategoriaPorQuestionId(questionId),
                selected_option = listaRespuestas[i].respuesta_seleccionada
            });
        }

        return request;
    }

    private string ValidarRespuestas()
    {
        if (listaRespuestas.Count != QuestionIds.Length)
        {
            return "La encuesta requiere exactamente 6 respuestas.";
        }

        HashSet<string> questionIds = new HashSet<string>();

        for (int i = 0; i < listaRespuestas.Count; i++)
        {
            RespuestaItem respuesta = listaRespuestas[i];

            if (respuesta == null)
            {
                return "Respuesta vacia en la posicion " + (i + 1) + ".";
            }

            if (string.IsNullOrEmpty(respuesta.id_pregunta))
            {
                return "Pregunta sin identificador en la posicion " + (i + 1) + ".";
            }

            if (!questionIds.Add(respuesta.id_pregunta))
            {
                return "Pregunta duplicada: " + respuesta.id_pregunta + ".";
            }

            if (!EsQuestionIdValido(respuesta.id_pregunta))
            {
                return "Identificador de pregunta invalido: "
                    + respuesta.id_pregunta
                    + ".";
            }

            if (string.IsNullOrEmpty(NormalizarOpcion(respuesta.respuesta_seleccionada)))
            {
                return "Respuesta invalida para " + respuesta.id_pregunta + ".";
            }
        }

        return "";
    }

    private string ObtenerQuestionId(int index)
    {
        if (index >= 0 && index < preguntas.Count)
        {
            string idInspector = preguntas[index].idPregunta;

            if (!string.IsNullOrEmpty(idInspector) && EsQuestionIdValido(idInspector))
            {
                return idInspector;
            }
        }

        if (index >= 0 && index < QuestionIds.Length)
        {
            return QuestionIds[index];
        }

        return "";
    }

    private string ObtenerCategoriaPorQuestionId(string questionId)
    {
        for (int i = 0; i < QuestionIds.Length; i++)
        {
            if (QuestionIds[i] == questionId)
            {
                return QuestionCategories[i];
            }
        }

        return "";
    }

    private bool EsQuestionIdValido(string questionId)
    {
        for (int i = 0; i < QuestionIds.Length; i++)
        {
            if (QuestionIds[i] == questionId)
            {
                return true;
            }
        }

        return false;
    }

    private string NormalizarOpcion(string opcion)
    {
        if (string.IsNullOrEmpty(opcion))
        {
            return "";
        }

        string normalizada = opcion.Trim().ToUpper();

        if (normalizada == "A" || normalizada == "B" || normalizada == "C")
        {
            return normalizada;
        }

        return "";
    }

    private bool EsConflicto(string error)
    {
        return !string.IsNullOrEmpty(error) && error.Contains("HTTP_409");
    }

    private void AplicarDiagnostico(SurveySubmitResponse response)
    {
        AIState.SurveyCompleted = true;
        AIState.SurveyResultPending = true;
        AIState.SurveyInitialRisk = response.initial_risk;
        AIState.SurveyPrimaryWeakness = response.primary_weakness;
        AIState.SurveyTotalRiskScore = response.total_risk_score;
    }

    private void AplicarDiagnostico(SurveyStatusResponse status)
    {
        AIState.SurveyCompleted = true;
        AIState.SurveyResultPending = false;
        AIState.SurveyInitialRisk = status.initial_risk;
        AIState.SurveyPrimaryWeakness = status.primary_weakness;
        AIState.SurveyTotalRiskScore = 0;
    }

    private void RehabilitarEncuesta(string message)
    {
        envioEnProgreso = false;
        SetBotonesRespuestasInteractables(true);
        MostrarErrorEncuesta(message);
    }

    private void SetBotonesRespuestasInteractables(bool interactable)
    {
        if (btnOpcionA != null)
        {
            btnOpcionA.interactable = interactable;
        }

        if (btnOpcionB != null)
        {
            btnOpcionB.interactable = interactable;
        }

        if (btnOpcionC != null)
        {
            btnOpcionC.interactable = interactable;
        }
    }

    private void MostrarErrorEncuesta(string message)
    {
        Debug.LogError(message);

        if (txtPregunta != null)
        {
            txtPregunta.text = message;
        }
    }
}
