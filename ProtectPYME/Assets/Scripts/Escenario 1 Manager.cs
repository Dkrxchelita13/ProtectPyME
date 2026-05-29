using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Escenario1Manager : MonoBehaviour
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

        MostrarIntroduccion();
    }

    void Update()
    {
        if (bloquearClick)
            return;

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
        panelCorreo.SetActive(false);
        panelSospechoso.SetActive(false);
        panelDecision.SetActive(false);
        panelBueno.SetActive(false);
        panelMalo.SetActive(false);
        panelRetroCorrecto.SetActive(false);
        panelRetroIncorrecto.SetActive(false);

        ReproducirSonido(sonidoCorreo);
    }

    public void SkipIntroduccion()
    {
        bloquearClick = true;

        MostrarCorreo();

        StartCoroutine(DesbloquearClick());
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

        //SceneManager.LoadScene("MenuNivelInicial");
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

        //SceneManager.LoadScene("MenuNivelInicial");
    }

    // =========================
    // NAVEGACIÓN
    // =========================

    void SiguientePanel()
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
            MostrarCorreo();
        }
        else if (panelActual == 3)
        {
            MostrarSospechoso();
        }
    }

    // =========================
    // RESPUESTAS
    // =========================

    public void OpcionCorrecta()
    {
        if (yaRespondio) return;

        yaRespondio = true;

        int tiempoRespuesta = Mathf.RoundToInt(Time.time - tiempoInicio);

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

    public void IrAEscenario2()
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

    IEnumerator DesbloquearClick()
    {
        yield return new WaitForSeconds(0.2f);

        bloquearClick = false;
    }
}