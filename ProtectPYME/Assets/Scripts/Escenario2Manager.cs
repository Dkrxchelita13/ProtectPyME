using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class Escenario2Manager : MonoBehaviour
{
    private float tiempoInicio;
    private float decisionDisponibleDesde;
    private int tiempoRespuestaPendiente = -1;

    [Header("Paneles de la Historia (Lectura)")]
    [Tooltip("Arrastra aquí en orden: Introducción, Alerta 1, Alerta 2, etc.")]
    public GameObject[] panelesHistoria;

    [Header("Paneles de Acción y Retroalimentación")]
    public GameObject panelDecision;
    public GameObject panelMalo;
    public GameObject panelMedio;           // NUEVO: Panel de reacción inmediata (ej. advertencia amarilla)
    public GameObject panelBueno;
    public GameObject panelRetroCorrecto;
    public GameObject panelRetroIncorrecto; 
    public GameObject panelRetroMedio;      

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
    private int panelActual = 0; 

    void Start()
    {
        tiempoInicio = Time.time;
        ActualizarCorazones();

        if (tituloEscenario != null)
        {
            tituloEscenario.SetActive(true);
            Invoke(nameof(OcultarTituloEscenario), tiempoVisibilidadTitulo);
        }
        MostrarPanelHistoria(0); 
    }

    void Update()
    {
        // Si hay algún panel de decisión, reacción o retro activo, no permitir navegar
        if (panelDecision.activeSelf || panelMalo.activeSelf || panelBueno.activeSelf || 
            (panelMedio != null && panelMedio.activeSelf) ||
            panelRetroCorrecto.activeSelf || panelRetroIncorrecto.activeSelf || 
            (panelRetroMedio != null && panelRetroMedio.activeSelf))
            return;
    }

    // =========================
    // CONTROL DE PANELES
    // =========================

    void ApagarTodosLosPaneles()
    {
        foreach (GameObject p in panelesHistoria)
        {
            if (p != null) p.SetActive(false);
        }
        panelDecision.SetActive(false);
        panelBueno.SetActive(false);
        panelMalo.SetActive(false);
        if (panelMedio != null) panelMedio.SetActive(false);
        panelRetroCorrecto.SetActive(false);
        panelRetroIncorrecto.SetActive(false);
        if (panelRetroMedio != null) panelRetroMedio.SetActive(false);
    }

    void MostrarPanelHistoria(int indice)
    {
        ApagarTodosLosPaneles();
        panelActual = indice;
        
        if (panelActual >= 0 && panelActual < panelesHistoria.Length)
        {
            panelesHistoria[panelActual].SetActive(true);
            ReproducirSonido(sonidoCorreo);
        }
    }

    void MostrarDecision()
    {
        ApagarTodosLosPaneles();
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
        if (panelActual < panelesHistoria.Length - 1)
        {
            MostrarPanelHistoria(panelActual + 1);
        }
        else if (panelActual == panelesHistoria.Length - 1)
        {
            MostrarDecision();
        }
    }

    void PanelAnterior()
    {
        if (panelActual > 0)
        {
            MostrarPanelHistoria(panelActual - 1);
        }
    }

    // =========================
    // RETROALIMENTACIONES
    // =========================

    void MostrarRetroCorrecta()
    {
        ApagarTodosLosPaneles();
        panelRetroCorrecto.SetActive(true);
        ReproducirSonido(sonidoDetalle);
        StartCoroutine(ZoomSuave(3.6f));
    }

    void MostrarRetroIncorrecta()
    {
        ApagarTodosLosPaneles();
        panelRetroIncorrecto.SetActive(true);
        ReproducirSonido(sonidoDetalle);
        StartCoroutine(ZoomSuave(3.6f));
    }

    void MostrarRetroMedio()
    {
        ApagarTodosLosPaneles();
        if (panelRetroMedio != null) panelRetroMedio.SetActive(true);
        ReproducirSonido(sonidoDetalle);
        StartCoroutine(ZoomSuave(3.6f));
    }

    // =========================
    // RESPUESTAS
    // =========================

    public void OpcionBuena()
    {
        RegistrarDecision("cambiar_password", ContinuarRespuestaBuena);
    }

    public void OpcionMala() 
    {
        RegistrarDecision("ignorar_alerta", ContinuarRespuestaMala);
    }

    public void OpcionMedia() 
    {
        RegistrarDecision("posponer_cambio", ContinuarRespuestaMedia);
    }

    public void OtroIntento()
    {
        ApagarTodosLosPaneles();
        panelDecision.SetActive(true);
        yaRespondio = false;
        enviandoDecision = false;
        tiempoRespuestaPendiente = -1;
        decisionDisponibleDesde = Time.realtimeSinceStartup;
        LimpiarMensajeDecision();
        SetDecisionButtonsInteractable(true);
    }

    // =========================
    // ESCENAS, VIDAS, AUDIO, EFECTOS...
    // =========================

    public void IrAEscenario3()
    {
        SceneManager.LoadScene("MenuNivelInicial");
    }

    void PerderVida()
    {
        if (GameManagerGlobal.instancia != null)
            GameManagerGlobal.instancia.PerderVida(); 
        ActualizarCorazones();
    }

    void GanarVida()
    {
        if (GameManagerGlobal.instancia != null)
            GameManagerGlobal.instancia.GanarVida();
        ActualizarCorazones();
    }

    void ActualizarCorazones()
    {
        if (corazones == null || corazones.Length == 0) return;
        int vidasActuales = (GameManagerGlobal.instancia != null) ? GameManagerGlobal.instancia.vidas : 3;
        for (int i = 0; i < corazones.Length; i++)
        {
            if (corazones[i] != null) corazones[i].SetActive(i < vidasActuales);
        }
    }

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

    IEnumerator ZoomSuave(float tamaño)
    {
        float tiempo = 0f;
        float duracion = 1f;
        float tamañoInicial = camara.orthographicSize;

        while (tiempo < duracion)
        {
            camara.orthographicSize = Mathf.Lerp(tamañoInicial, tamaño, tiempo / duracion);
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
                2,
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
        if (PlayerPrefs.GetInt("NivelAlcanzado", 1) < 3)
        {
            PlayerPrefs.SetInt("NivelAlcanzado", 3);
            PlayerPrefs.Save();
        }

        ModificarSeguridadEscenario(3f);
        GanarVida();

        panelDecision.SetActive(false);
        panelBueno.SetActive(true);
        ReproducirSonido(sonidoCorrecto);
        Invoke(nameof(MostrarRetroCorrecta), 2f);
    }

    private void ContinuarRespuestaMala()
    {
        panelDecision.SetActive(false);
        panelMalo.SetActive(true);
        ReproducirSonido(sonidoError);

        Invoke(nameof(MostrarRetroIncorrecta), 2f);
        ModificarSeguridadEscenario(-3f);
        PerderVida();
    }

    private void ContinuarRespuestaMedia()
    {
        panelDecision.SetActive(false);

        if (panelMedio != null) panelMedio.SetActive(true);

        ReproducirSonido(sonidoError);

        Invoke(nameof(MostrarRetroMedio), 2f);
        ModificarSeguridadEscenario(-2f);
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
                "Decision no registrada escenario=2 responseCode="
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
