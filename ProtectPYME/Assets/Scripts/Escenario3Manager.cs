using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Escenario3Manager : MonoBehaviour
{
    private float tiempoInicio;

    [Header("Paneles")]
    public GameObject panelIntroduccion;
    public GameObject panelUSB;
    public GameObject panelDecision;

    public GameObject panelMalo;
    public GameObject panelBueno;

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

    private bool yaRespondio = false;

    /*
        0 = Introducción
        1 = USB
        2 = Decisión
        3 = Retro Correcta
        4 = Retro Incorrecta
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

            // CLICK DERECHA
            if (Input.mousePosition.x > mitad)
            {
                SiguientePanel();
            }
            // CLICK IZQUIERDA
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
        panelUSB.SetActive(false);
        panelDecision.SetActive(false);

        panelBueno.SetActive(false);
        panelMalo.SetActive(false);

        retroCorrecta.SetActive(false);
        retroIncorrecta.SetActive(false);

        ReproducirSonido(sonidoCorreo);
    }

    void MostrarUSB()
    {
        panelActual = 1;

        panelIntroduccion.SetActive(false);
        panelUSB.SetActive(true);
        panelDecision.SetActive(false);

        panelBueno.SetActive(false);
        panelMalo.SetActive(false);

        retroCorrecta.SetActive(false);
        retroIncorrecta.SetActive(false);

        ReproducirSonido(sonidoCorreo);
    }

    void MostrarDecision()
    {
        panelActual = 2;

        panelIntroduccion.SetActive(false);
        panelUSB.SetActive(false);
        panelDecision.SetActive(true);

        panelBueno.SetActive(false);
        panelMalo.SetActive(false);

        retroCorrecta.SetActive(false);
        retroIncorrecta.SetActive(false);

        ReproducirSonido(sonidoDetalle);

        StartCoroutine(ZoomSuave(3.6f));
    }

    void MostrarRetroCorrecta()
    {
        panelActual = 3;

        panelIntroduccion.SetActive(false);
        panelUSB.SetActive(false);
        panelDecision.SetActive(false);

        panelBueno.SetActive(false);
        panelMalo.SetActive(false);

        retroCorrecta.SetActive(true);
        retroIncorrecta.SetActive(false);

        ReproducirSonido(sonidoDetalle);

        StartCoroutine(ZoomSuave(3.6f));
    }

    void MostrarRetroIncorrecta()
    {
        panelActual = 4;

        panelIntroduccion.SetActive(false);
        panelUSB.SetActive(false);
        panelDecision.SetActive(false);

        panelBueno.SetActive(false);
        panelMalo.SetActive(false);

        retroCorrecta.SetActive(false);
        retroIncorrecta.SetActive(true);

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
            MostrarUSB();
        }
        else if (panelActual == 1)
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
            MostrarUSB();
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

        panelDecision.SetActive(false);
        panelBueno.SetActive(true);

        ReproducirSonido(sonidoCorrecto);

        //StartCoroutine(
            //APIManager.Instance.SendDecision(
            //    2,
             //   "no_conectar",
             //   tiempoRespuesta
            //)
        //);

        Invoke(nameof(MostrarRetroCorrecta), 2f);
    }

    public void OpcionMala()
    {
        if (yaRespondio) return;

        yaRespondio = true;

        int tiempoRespuesta = Mathf.RoundToInt(Time.time - tiempoInicio);

        panelDecision.SetActive(false);
        panelMalo.SetActive(true);

        ReproducirSonido(sonidoError);

       // StartCoroutine(
            //APIManager.Instance.SendDecision(
            //    2,
              //  "conectar_usb",
              //  tiempoRespuesta
           // )
       // );

        Invoke(nameof(MostrarRetroIncorrecta), 2f);

        PerderVida();
    }

    public void OtroIntento()
    {
        panelBueno.SetActive(false);
        panelMalo.SetActive(false);

        retroCorrecta.SetActive(false);
        retroIncorrecta.SetActive(false);

        panelDecision.SetActive(true);

        yaRespondio = false;
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