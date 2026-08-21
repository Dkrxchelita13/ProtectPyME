using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // Necesario para detectar clics en botones de la interfaz
using TMPro; // Necesario para los textos de la ventana flotante

public class Escenario5Manager : MonoBehaviour
{
    private float tiempoInicio;

    [Header("Paneles")]
    public GameObject panelIntroduccion;
    public GameObject panelCorreo;
    public GameObject panelSospechoso;
    public GameObject panelDecision;
    public GameObject panelMalo;
    public GameObject panelBueno;
    public GameObject panelRetroCorrecto;
    public GameObject panelRetroIncorrecto;

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
    
    [Header("Mecánica de Inspección (Panel 2)")]
    public GameObject panelTooltip;
    public TextMeshProUGUI textoTooltip;
    public string[] textosTeoricos; // Pon aquí los textos desde el inspector (ej. 2 pistas)
    private int totalPistas;
    private int pistasEncontradas = 0;
    private bool[] pistaDescubierta;
    public bool inspeccionCompletada = false;

    [Header("Título Escenario")]
    public GameObject tituloEscenario;
    public float tiempoVisibilidadTitulo = 3.5f;

    private bool yaRespondio = false;
    private bool bloquearClick = false;

    /*
        0 = Introducción
        1 = Correo
        2 = Sospechoso
        3 = Decisión
    */

    int panelActual = 0;

    void Start()
    {
        tiempoInicio = Time.time;
        ActualizarCorazones();

        panelBueno.SetActive(false);
        panelMalo.SetActive(false);
        
        // Inicializamos la mecánica de inspección
        if (panelTooltip != null) panelTooltip.SetActive(false);
        totalPistas = textosTeoricos.Length;
        pistaDescubierta = new bool[totalPistas];
        inspeccionCompletada = false;

        if (tituloEscenario != null)
        {
            tituloEscenario.SetActive(true);
            Invoke(nameof(OcultarTituloEscenario), tiempoVisibilidadTitulo);
        }

        MostrarIntroduccion();
    }

    void Update()
    {
        if (bloquearClick) return;

        if (panelDecision.activeSelf || panelMalo.activeSelf || panelBueno.activeSelf) return;
    }

    // =========================
    // NUEVO: FUNCIONES DE INSPECCIÓN
    // =========================
    public void AlHacerClicEnPista(int indiceDeLaPista)
    {
        // Mostramos el tooltip
        if(panelTooltip != null)
        {
            textoTooltip.text = textosTeoricos[indiceDeLaPista];
            panelTooltip.SetActive(true);
        }
        
        ReproducirSonido(sonidoBoton); // O un sonido específico de "lupa"

        // Registramos si es nueva
        if (!pistaDescubierta[indiceDeLaPista])
        {
            pistaDescubierta[indiceDeLaPista] = true;
            pistasEncontradas++;
            
            if (pistasEncontradas >= totalPistas)
            {
                inspeccionCompletada = true;
                Debug.Log("¡Todas las pistas encontradas! Ya puedes avanzar.");
            }
        }
    }

    public void CerrarTooltip()
    {
        if(panelTooltip != null) panelTooltip.SetActive(false);
        ReproducirSonido(sonidoBoton);
    }

    // =========================
    // PANELES
    // =========================

    void MostrarIntroduccion()
    {
        panelActual = 0;
        panelIntroduccion.SetActive(true);
        panelCorreo.SetActive(false);
        panelSospechoso.SetActive(false);
        panelDecision.SetActive(false);
        panelBueno.SetActive(false);
        panelMalo.SetActive(false);
        panelRetroCorrecto.SetActive(false);
        panelRetroIncorrecto.SetActive(false);
        ReproducirSonido(sonidoCorreo);
    }

    public void MostrarCorreo()
    {
        panelActual = 1;
        panelIntroduccion.SetActive(false);
        panelCorreo.SetActive(true);
        panelSospechoso.SetActive(false);
        panelDecision.SetActive(false);
        panelRetroCorrecto.SetActive(false);
        panelRetroIncorrecto.SetActive(false);
        ReproducirSonido(sonidoCorreo);
    }

    void MostrarSospechoso()
    {
        panelActual = 2;
        panelIntroduccion.SetActive(false);
        panelCorreo.SetActive(false);
        panelSospechoso.SetActive(true);
        panelDecision.SetActive(false);
        panelRetroCorrecto.SetActive(false);
        panelRetroIncorrecto.SetActive(false);
        ReproducirSonido(sonidoCorreo);
    }

    void MostrarDecision()
    {
        panelActual = 3;
        panelIntroduccion.SetActive(false);
        panelCorreo.SetActive(false);
        panelSospechoso.SetActive(false);
        panelDecision.SetActive(true);
        panelRetroCorrecto.SetActive(false);
        panelRetroIncorrecto.SetActive(false);
        ReproducirSonido(sonidoDetalle);
        StartCoroutine(ZoomSuave(3.6f));
    }

    // (Aquí siguen tus funciones MostrarRetroCorrecta y MostrarRetroIncorrecta tal cual las tenías...)
    void MostrarRetroCorrecta()
    {
        panelActual = 4;
        panelIntroduccion.SetActive(false);
        panelCorreo.SetActive(false);
        panelSospechoso.SetActive(false);
        panelDecision.SetActive(false);
        panelRetroCorrecto.SetActive(true);
        panelRetroIncorrecto.SetActive(false);
        ReproducirSonido(sonidoDetalle);
        StartCoroutine(ZoomSuave(3.6f));
    }

    void MostrarRetroIncorrecta()
    {
        panelActual = 5;
        panelIntroduccion.SetActive(false);
        panelCorreo.SetActive(false);
        panelSospechoso.SetActive(false);
        panelDecision.SetActive(false);
        panelRetroCorrecto.SetActive(false);
        panelRetroIncorrecto.SetActive(true);
        panelMalo.SetActive(false);
        ReproducirSonido(sonidoDetalle);
        StartCoroutine(ZoomSuave(3.6f));
    }

    // =========================
    // NAVEGACIÓN MODIFICADA
    // =========================

    public void SiguientePanel()
    {
        if (panelActual == 0)
        {
            MostrarCorreo();
        }
        else if (panelActual == 1)
        {
            MostrarSospechoso();
        }
        else if (panelActual == 2)
        {
            // Nuestro candado
            if (inspeccionCompletada)
            {
                MostrarDecision();
            }
            else
            {
                Debug.Log("Bloqueado: El jugador debe inspeccionar las pistas primero.");
                ReproducirSonido(sonidoError); // Opcional, para que suene si intenta avanzar
            }
        }
    }

    // Añadimos "public" aquí también
    public void PanelAnterior()
    {
        Debug.Log("Botón Regresar presionado. Estábamos en el panel: " + panelActual);

        if (panelActual == 1) MostrarIntroduccion();
        else if (panelActual == 2) MostrarCorreo(); // Ojo: MostrarCorreo activa la variable 'panelCorreo', que en tu Inspector es el Panel_Llamada.
        else if (panelActual == 3) MostrarSospechoso();
    }

    
    // =========================
    // RESPUESTAS
    // =========================

    public void OpcionCorrecta()
    {
        if (yaRespondio) return;
        yaRespondio = true;
        int tiempoRespuesta = Mathf.RoundToInt(Time.time - tiempoInicio);
        StartCoroutine(APIManager.Instance.SendDecision(1, "reportar_phishing", tiempoRespuesta));

        if (PlayerPrefs.GetInt("ProgresoIntermedio", 1) < 3)
        {
            PlayerPrefs.SetInt("ProgresoIntermedio", 3); 
            PlayerPrefs.Save();
        }

        ModificarSeguridadEscenario(3f);
        GanarVida();
        panelDecision.SetActive(false);
        panelBueno.SetActive(true);
        ReproducirSonido(sonidoCorrecto);
        Invoke(nameof(MostrarRetroCorrecta), 2f);
    }

    public void OpcionIncorrecta()
    {
        if (yaRespondio) return;
        yaRespondio = true;
        int tiempoRespuesta = Mathf.RoundToInt(Time.time - tiempoInicio);
        StartCoroutine(APIManager.Instance.SendDecision(1, "abrir_correo", tiempoRespuesta));
        panelDecision.SetActive(false);
        panelMalo.SetActive(true);
        ReproducirSonido(sonidoError);
        Invoke(nameof(MostrarRetroIncorrecta), 2f);
        ModificarSeguridadEscenario(-3f);
        PerderVida();
    }

    public void OtroIntento()
    {
        panelBueno.SetActive(false);
        panelMalo.SetActive(false);
        panelDecision.SetActive(true);
        panelRetroCorrecto.SetActive(false);
        panelRetroIncorrecto.SetActive(false);
        yaRespondio = false;
    }
    void OcultarTituloEscenario()
    {
        if (tituloEscenario != null)
        {
            tituloEscenario.SetActive(false);
        }
    }

    // =========================
    // ESCENAS, VIDAS Y EFECTOS
    // =========================
    // (Pega aquí el resto de tus funciones: IrAEscenario2, PerderVida, GanarVida, ActualizarCorazones, ReproducirSonido, ZoomSuave, DesbloquearClick, ModificarSeguridadEscenario)
    
    public void IrAEscenario2() { SceneManager.LoadScene("MenuNivelIntermedio"); }

    void PerderVida() { if (GameManagerGlobal.instancia != null) { GameManagerGlobal.instancia.PerderVida(); } ActualizarCorazones(); }
    void GanarVida() { if (GameManagerGlobal.instancia != null) { GameManagerGlobal.instancia.GanarVida(); } ActualizarCorazones(); }
    void ActualizarCorazones()
    {
        if (corazones == null || corazones.Length == 0) return;
        int vidasActuales = (GameManagerGlobal.instancia != null) ? GameManagerGlobal.instancia.vidas : 3;
        for (int i = 0; i < corazones.Length; i++) { if (corazones[i] != null) { corazones[i].SetActive(i < vidasActuales); } }
    }
    public void SonidoBoton() { ReproducirSonido(sonidoBoton); }
    void ReproducirSonido(AudioClip clip) { if (audioSource != null && clip != null) { audioSource.Stop(); audioSource.PlayOneShot(clip); } }
    IEnumerator ZoomSuave(float tamaño) { float tiempo = 0f; float duracion = 1f; float tamañoInicial = camara.orthographicSize; while (tiempo < duracion) { camara.orthographicSize = Mathf.Lerp(tamañoInicial, tamaño, tiempo / duracion); tiempo += Time.deltaTime; yield return null; } camara.orthographicSize = tamaño; }
    public void SkipIntroduccion() { bloquearClick = true; MostrarCorreo(); StartCoroutine(DesbloquearClick()); }
    IEnumerator DesbloquearClick() { yield return new WaitForSeconds(0.2f); bloquearClick = false; }
    private void ModificarSeguridadEscenario(float cambio) { string claveSeguridad = (GameManagerGlobal.instancia != null) ? GameManagerGlobal.instancia.ObtenerClaveUsuario("SeguridadPersistente") : "SeguridadPersistente"; float seguridadActual = PlayerPrefs.GetFloat(claveSeguridad, 0f); float nuevaSeguridad = Mathf.Clamp(seguridadActual + cambio, 0f, 100f); if (GameManagerGlobal.instancia != null) { GameManagerGlobal.instancia.nivelSeguridad = nuevaSeguridad; } PlayerPrefs.SetFloat(claveSeguridad, nuevaSeguridad); PlayerPrefs.Save(); }
}