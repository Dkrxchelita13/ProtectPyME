using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class Escenario3Manager : MonoBehaviour
{
    private float tiempoInicio;
    private float decisionDisponibleDesde;
    private int tiempoRespuestaPendiente = -1;

    [Header("Historia (Arreglo de Paneles)")]
    [Tooltip("Arrastra aquí en orden: Panel 1 (Baiting), Panel 2 (Glosario Malware/Ransomware/Botnet), Panel 3 (Antivirus y Riesgo)")]
    public GameObject[] panelesHistoria;

    [Header("Decisión y Reacciones Inmediatas")]
    public GameObject panelDecision;
    public GameObject panelBueno;
    public GameObject panelMalo;

    [Header("Retroalimentaciones Finales")]
    public GameObject retroCorrecta;
    public GameObject retroIncorrecta;

    [Header("Corazones")]
    public GameObject[] corazones;

    [Header("Cámara")]
    public Camera camara;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sonidoDetalle;
    public AudioClip sonidoCorreo;
    public AudioClip sonidoBoton;
    public AudioClip sonidoError;
    public AudioClip sonidoCorrecto;

    [Header("Título Escenario")]
    public GameObject tituloEscenario;
    public float tiempoVisibilidadTitulo = 3.5f;

    private bool yaRespondio = false;
    private bool enviandoDecision = false;
    private TextMeshProUGUI mensajeDecision;
    private const string MensajeErrorDecision =
        "No fue posible registrar tu decision.\nRevisa tu conexion e intenta nuevamente.";
    private int indiceHistoriaActual = 0;

    void Start()
    {
        tiempoInicio = Time.time;

        ActualizarCorazones();

        // Ocultar paneles de reacción
        if (panelBueno != null) panelBueno.SetActive(false);
        if (panelMalo != null) panelMalo.SetActive(false);

        // Ocultar retroalimentaciones
        if (retroCorrecta != null) retroCorrecta.SetActive(false);
        if (retroIncorrecta != null) retroIncorrecta.SetActive(false);

        // Mostrar el título al inicio y programar su desactivación
        if (tituloEscenario != null)
        {
            tituloEscenario.SetActive(true);
            Invoke(nameof(OcultarTituloEscenario), tiempoVisibilidadTitulo);
        }

        MostrarHistoria(0);
    }

    void Update()
    {
        // Si hay un panel de reacción, retroalimentación o decisión activo, deshabilitar clics de pantalla
        if (panelDecision.activeSelf || 
            (panelBueno != null && panelBueno.activeSelf) || 
            (panelMalo != null && panelMalo.activeSelf) ||
            (retroCorrecta != null && retroCorrecta.activeSelf) ||
            (retroIncorrecta != null && retroIncorrecta.activeSelf))
            return;
    }

    // =========================
    // PANELES DE HISTORIA
    // =========================

    void MostrarHistoria(int indice)
    {
        indiceHistoriaActual = indice;

        // Apagar todos los paneles de historia
        for (int i = 0; i < panelesHistoria.Length; i++)
        {
            if (panelesHistoria[i] != null)
                panelesHistoria[i].SetActive(i == indiceHistoriaActual);
        }

        panelDecision.SetActive(false);

        ReproducirSonido(sonidoCorreo);
    }

    void MostrarDecision()
    {
        // Apagar historia
        for (int i = 0; i < panelesHistoria.Length; i++)
        {
            if (panelesHistoria[i] != null)
                panelesHistoria[i].SetActive(false);
        }

        panelDecision.SetActive(true);
        decisionDisponibleDesde = Time.realtimeSinceStartup;
        tiempoRespuestaPendiente = -1;
        LimpiarMensajeDecision();
        SetDecisionButtonsInteractable(true);

        ReproducirSonido(sonidoDetalle);
        StartCoroutine(ZoomSuave(3.6f));
    }

    // =========================
    // NAVEGACIÓN
    // =========================

    void SiguientePanel()
    {
        if (indiceHistoriaActual < panelesHistoria.Length - 1)
        {
            MostrarHistoria(indiceHistoriaActual + 1);
        }
        else
        {
            MostrarDecision();
        }
    }

    void PanelAnterior()
    {
        if (indiceHistoriaActual > 0)
        {
            MostrarHistoria(indiceHistoriaActual - 1);
        }
    }

    // =========================
    // RETROALIMENTACIONES
    // =========================

    void MostrarRetroCorrecta()
    {
        OcultarTodo();
        if (retroCorrecta != null) retroCorrecta.SetActive(true);
        ReproducirSonido(sonidoDetalle);
    }

    void MostrarRetroIncorrecta()
    {
        OcultarTodo();
        if (retroIncorrecta != null) retroIncorrecta.SetActive(true);
        ReproducirSonido(sonidoDetalle);
    }

    void OcultarTodo()
    {
        panelDecision.SetActive(false);
        if (panelBueno != null) panelBueno.SetActive(false);
        if (panelMalo != null) panelMalo.SetActive(false);
    }

    // =========================
    // RESPUESTAS
    // =========================

    // OPCIÓN BUENA: Entregar a Soporte Técnico / TI (O no conectar)
    public void OpcionBuena()
    {
        RegistrarDecision("no_conectar", ContinuarRespuestaBuena);
    }

    // OPCIÓN MALA: Conectar / Abrir el USB
    public void OpcionMala()
    {
        RegistrarDecision("conectar_usb", ContinuarRespuestaMala);
    }

    public void OtroIntento()
    {
        if (panelBueno != null) panelBueno.SetActive(false);
        if (panelMalo != null) panelMalo.SetActive(false);

        if (retroCorrecta != null) retroCorrecta.SetActive(false);
        if (retroIncorrecta != null) retroIncorrecta.SetActive(false);

        panelDecision.SetActive(true);
        yaRespondio = false;
        enviandoDecision = false;
        tiempoRespuestaPendiente = -1;
        decisionDisponibleDesde = Time.realtimeSinceStartup;
        LimpiarMensajeDecision();
        SetDecisionButtonsInteractable(true);
    }

    // =========================
    // ESCENAS
    // =========================

    public void IrAEscenario()
    {
        SceneManager.LoadScene("MenuNivelInicial");
    }

    // =========================
    // VIDAS
    // =========================

    void PerderVida()
    {
        if (GameManagerGlobal.instancia != null)
        {
            GameManagerGlobal.instancia.PerderVida(); 
        }
        ActualizarCorazones();
    }

    void GanarVida()
    {
        if (GameManagerGlobal.instancia != null)
        {
            GameManagerGlobal.instancia.GanarVida();
        }
        ActualizarCorazones();
    }   

    void ActualizarCorazones()
    {
        if (corazones == null || corazones.Length == 0) return;

        int vidasActuales = (GameManagerGlobal.instancia != null) ? GameManagerGlobal.instancia.vidas : 3;

        for (int i = 0; i < corazones.Length; i++)
        {
            if (corazones[i] != null)
            {
                corazones[i].SetActive(i < vidasActuales);
            }
        }
    }

    // =========================
    // AUDIO
    // =========================

    public void SonidoBoton()
    {
        ReproducirSonido(sonidoBoton);
    }

    void ReproducirSonido(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(clip);
        }
    }

    // =========================
    // EFECTOS
    // =========================

    IEnumerator ZoomSuave(float tamaño)
    {
        float tiempo = 0f;
        float duracion = 1f;

        float tamañoInicial = camara.orthographicSize;

        while (tiempo < duracion)
        {
            camara.orthographicSize = Mathf.Lerp(
                tamañoInicial,
                tamaño,
                tiempo / duracion
            );

            tiempo += Time.deltaTime;

            yield return null;
        }

        camara.orthographicSize = tamaño;
    }

    private void ModificarSeguridadEscenario(float cambio)
    {
        string claveSeguridad = (GameManagerGlobal.instancia != null) 
            ? GameManagerGlobal.instancia.ObtenerClaveUsuario("SeguridadPersistente") 
            : "SeguridadPersistente";

        float seguridadActual = PlayerPrefs.GetFloat(claveSeguridad, 0f);
        float nuevaSeguridad = Mathf.Clamp(seguridadActual + cambio, 0f, 100f);

        if (GameManagerGlobal.instancia != null)
        {
            GameManagerGlobal.instancia.nivelSeguridad = nuevaSeguridad;
        }

        PlayerPrefs.SetFloat(claveSeguridad, nuevaSeguridad);
        PlayerPrefs.Save();

        Debug.Log($"🛡️ Seguridad Escenario 3: Tenía {seguridadActual}%, cambió ({cambio}%), Ahora es: {nuevaSeguridad}%");
    }

    void OcultarTituloEscenario()
    {
        if (tituloEscenario != null)
        {
            tituloEscenario.SetActive(false);
        }
    }

    private void RegistrarDecision(string choice, System.Action onSuccess)
    {
        if (yaRespondio || enviandoDecision) return;

        if (APIManager.Instance == null)
        {
            ManejarFalloDecision(null);
            return;
        }

        yaRespondio = true;
        enviandoDecision = true;
        LimpiarMensajeDecision();
        SetDecisionButtonsInteractable(false);

        int tiempoRespuesta = ObtenerTiempoRespuestaDecision();

        StartCoroutine(
            APIManager.Instance.SendDecision(
                3,
                choice,
                tiempoRespuesta,
                result =>
                {
                    if (result != null && result.success)
                    {
                        onSuccess?.Invoke();
                    }
                    else
                    {
                        ManejarFalloDecision(result);
                    }
                }
            )
        );
    }

    private int ObtenerTiempoRespuestaDecision()
    {
        if (tiempoRespuestaPendiente >= 0)
        {
            return tiempoRespuestaPendiente;
        }

        tiempoRespuestaPendiente = Mathf.Max(
            0,
            Mathf.RoundToInt(Time.realtimeSinceStartup - decisionDisponibleDesde)
        );
        return tiempoRespuestaPendiente;
    }

    private void ContinuarRespuestaBuena()
    {
        panelDecision.SetActive(false);
        if (panelBueno != null) panelBueno.SetActive(true);

        ReproducirSonido(sonidoCorrecto);

        if (PlayerPrefs.GetInt("NivelAlcanzado", 1) < 4)
        {
            PlayerPrefs.SetInt("NivelAlcanzado", 4);
            PlayerPrefs.Save();
        }

        Invoke(nameof(MostrarRetroCorrecta), 2f);
        ModificarSeguridadEscenario(3f);
        GanarVida();
    }

    private void ContinuarRespuestaMala()
    {
        panelDecision.SetActive(false);
        if (panelMalo != null) panelMalo.SetActive(true);

        ReproducirSonido(sonidoError);

        Invoke(nameof(MostrarRetroIncorrecta), 2f);
        ModificarSeguridadEscenario(-3f);
        PerderVida();
    }

    private void ManejarFalloDecision(DecisionRequestResult result)
    {
        yaRespondio = false;
        enviandoDecision = false;
        SetDecisionButtonsInteractable(true);
        MostrarMensajeDecision();
        ReproducirSonido(sonidoError);

        if (result != null)
        {
            Debug.LogWarning(
                "Decision no registrada escenario=3 responseCode="
                + result.response_code
            );
        }
    }

    private void SetDecisionButtonsInteractable(bool interactable)
    {
        if (panelDecision == null) return;

        Button[] buttons = panelDecision.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            button.interactable = interactable;
        }
    }

    private void MostrarMensajeDecision()
    {
        TextMeshProUGUI label = ObtenerMensajeDecision();
        if (label != null)
        {
            label.text = MensajeErrorDecision;
        }
    }

    private void LimpiarMensajeDecision()
    {
        if (mensajeDecision != null)
        {
            mensajeDecision.text = "";
        }
    }

    private TextMeshProUGUI ObtenerMensajeDecision()
    {
        if (mensajeDecision != null)
        {
            return mensajeDecision;
        }

        if (panelDecision == null)
        {
            return null;
        }

        GameObject messageObject = new GameObject("DecisionErrorMessage");
        messageObject.transform.SetParent(panelDecision.transform, false);
        mensajeDecision = messageObject.AddComponent<TextMeshProUGUI>();
        mensajeDecision.fontSize = 26;
        mensajeDecision.alignment = TextAlignmentOptions.Center;
        mensajeDecision.color = Color.white;

        RectTransform rect = mensajeDecision.rectTransform;
        rect.anchorMin = new Vector2(0.08f, 0.02f);
        rect.anchorMax = new Vector2(0.92f, 0.18f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return mensajeDecision;
    }
}
