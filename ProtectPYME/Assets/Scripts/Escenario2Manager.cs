using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Escenario2Manager : MonoBehaviour
{
    private float tiempoInicio;

    [Header("Paneles")]
    public GameObject panelIntroduccion;
    public GameObject panelAlerta;
    public GameObject panelDetalle;
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

    private bool yaRespondio = false;

    /*
        0 = Introducción
        1 = Alerta
        2 = Detalle
        3 = Decisión
        4 = Retro Correcta
        5 = Retro Incorrecta
    */

    int panelActual = 0;

    void Start()
    {
        tiempoInicio = Time.time;

        ActualizarCorazones();

        panelBueno.SetActive(false);
        panelMalo.SetActive(false);

        MostrarIntroduccion();
    }

    void Update()
    {
        if (panelDecision.activeSelf || panelMalo.activeSelf || panelBueno.activeSelf)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            float mitad = Screen.width / 2;

            // CLICK DERECHA = SIGUIENTE
            if (Input.mousePosition.x > mitad)
            {
                SiguientePanel();
            }
            // CLICK IZQUIERDA = ANTERIOR
            else
            {
                PanelAnterior();
            }
        }
    }

    // =========================
    // PANELES
    // =========================

    void MostrarIntroduccion()
    {
        panelActual = 0;

        panelIntroduccion.SetActive(true);
        panelAlerta.SetActive(false);
        panelDetalle.SetActive(false);
        panelDecision.SetActive(false);
        panelBueno.SetActive(false);
        panelMalo.SetActive(false);
        panelRetroCorrecto.SetActive(false);
        panelRetroIncorrecto.SetActive(false);

        ReproducirSonido(sonidoCorreo);
    }

    void MostrarAlerta()
    {
        panelActual = 1;

        panelIntroduccion.SetActive(false);
        panelAlerta.SetActive(true);
        panelDetalle.SetActive(false);
        panelDecision.SetActive(false);
        panelBueno.SetActive(false);
        panelMalo.SetActive(false);
        panelRetroCorrecto.SetActive(false);
        panelRetroIncorrecto.SetActive(false);

        ReproducirSonido(sonidoCorreo);
    }

    void MostrarDetalle()
    {
        panelActual = 2;

        panelIntroduccion.SetActive(false);
        panelAlerta.SetActive(false);
        panelDetalle.SetActive(true);
        panelDecision.SetActive(false);
        panelBueno.SetActive(false);
        panelMalo.SetActive(false);
        panelRetroCorrecto.SetActive(false);
        panelRetroIncorrecto.SetActive(false);

        ReproducirSonido(sonidoCorreo);
    }

    void MostrarDecision()
    {
        panelActual = 3;

        panelIntroduccion.SetActive(false);
        panelAlerta.SetActive(false);
        panelDetalle.SetActive(false);
        panelDecision.SetActive(true);
        panelBueno.SetActive(false);
        panelMalo.SetActive(false);
        panelRetroCorrecto.SetActive(false);
        panelRetroIncorrecto.SetActive(false);

        ReproducirSonido(sonidoDetalle);

        StartCoroutine(ZoomSuave(3.6f));
    }

    void MostrarRetroCorrecta()
    {
        panelActual = 4;

        panelIntroduccion.SetActive(false);
        panelAlerta.SetActive(false);
        panelDetalle.SetActive(false);
        panelDecision.SetActive(false);
        panelBueno.SetActive(false);
        panelMalo.SetActive(false);
        panelRetroCorrecto.SetActive(true);
        panelRetroIncorrecto.SetActive(false);

        ReproducirSonido(sonidoDetalle);

        StartCoroutine(ZoomSuave(3.6f));
    }

    void MostrarRetroIncorrecta()
    {
        panelActual = 5;

        panelIntroduccion.SetActive(false);
        panelAlerta.SetActive(false);
        panelDetalle.SetActive(false);
        panelDecision.SetActive(false);
        panelBueno.SetActive(false);
        panelMalo.SetActive(false);
        panelRetroCorrecto.SetActive(false);
        panelRetroIncorrecto.SetActive(true);

        ReproducirSonido(sonidoDetalle);

        StartCoroutine(ZoomSuave(3.6f));
    }

    // =========================
    // NAVEGACIÓN
    // =========================

    void SiguientePanel()
    {
        if (panelActual == 0)
        {
            MostrarAlerta();
        }
        else if (panelActual == 1)
        {
            MostrarDetalle();
        }
        else if (panelActual == 2)
        {
            MostrarDecision();
        }
    }

    void PanelAnterior()
    {
        if (panelActual == 1)
        {
            MostrarIntroduccion();
        }
        else if (panelActual == 2)
        {
            MostrarAlerta();
        }
        else if (panelActual == 3)
        {
            MostrarDetalle();
        }
    }

    // =========================
    // RESPUESTAS
    // =========================

    public void OpcionBuena()
    {
        if (yaRespondio) return;

        yaRespondio = true;

        int tiempoRespuesta = Mathf.RoundToInt(Time.time - tiempoInicio);

        StartCoroutine(
            APIManager.Instance.SendDecision(
                2,
                "cambiar_password",
                tiempoRespuesta
            )
        );
        panelDecision.SetActive(false);
        panelBueno.SetActive(true);

        ReproducirSonido(sonidoCorrecto);

        Invoke(nameof(MostrarRetroCorrecta), 2f);
    }

    public void OpcionMala()
    {
        if (yaRespondio) return;

        yaRespondio = true;

        int tiempoRespuesta = Mathf.RoundToInt(Time.time - tiempoInicio);

        StartCoroutine(
            APIManager.Instance.SendDecision(
                2,
                "ignorar_alerta",
                tiempoRespuesta
            )
        );

        panelDecision.SetActive(false);
        panelMalo.SetActive(true);

        ReproducirSonido(sonidoError);

        Invoke(nameof(MostrarRetroIncorrecta), 2f);

        PerderVida();
    }

    public void OpcionMedia()
    {
        if (yaRespondio) return;

        yaRespondio = true;

        int tiempoRespuesta = Mathf.RoundToInt(Time.time - tiempoInicio);

        StartCoroutine(
            APIManager.Instance.SendDecision(
                2,
                "posponer_cambio",
                tiempoRespuesta
            )
        );

        panelDecision.SetActive(false);
        panelMalo.SetActive(true);

        ReproducirSonido(sonidoError);

        Invoke(nameof(MostrarRetroIncorrecta), 2f);

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

    // =========================
    // ESCENAS
    // =========================

    public void IrAEscenario3()
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

    void ActualizarCorazones()
    {
        int vidas = 3;

        if (GameManagerGlobal.instancia != null)
        {
            vidas = GameManagerGlobal.instancia.vidas;
        }

        for (int i = 0; i < corazones.Length; i++)
        {
            corazones[i].SetActive(i < vidas);
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
}