using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Escenario1Manager : MonoBehaviour
{
    private float tiempoInicio;
    public GameObject panelIntroduccion;
    public GameObject panelCorreo;
    public GameObject panelSospechoso;
    public GameObject panelDecision;
    public GameObject panelMalo;
    public GameObject panelBueno;

    public GameObject[] corazones;
    public Camera camara;

    private bool yaRespondio = false;
    private bool bloquearClick = false;

    public AudioSource audioSource;
    public AudioClip sonidoDetalle;
    public AudioClip sonidoCorreo;
    public AudioClip sonidoBoton;
    public AudioClip sonidoError;
    public AudioClip sonidoCorrecto;

    int panelActual = -1;

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

            if (Input.mousePosition.x > mitad)
            {
                SiguientePanel();
            }
            else
            {
                PanelAnterior();
            }
        }
    }

    void MostrarIntroduccion()
    {
        panelActual = -1;

        panelIntroduccion.SetActive(true);
        panelCorreo.SetActive(false);
        panelSospechoso.SetActive(false);
        panelDecision.SetActive(false);
        panelBueno.SetActive(false);
        panelMalo.SetActive(false);

        if (audioSource != null && sonidoCorreo != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(sonidoCorreo);
        }
    }

    public void SkipIntroduccion()
    {
        bloquearClick = true;

        panelActual = 0;

        panelIntroduccion.SetActive(false);
        panelCorreo.SetActive(true);
        panelSospechoso.SetActive(false);
        panelDecision.SetActive(false);

        if (audioSource != null && sonidoCorreo != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(sonidoCorreo);
        }

        StartCoroutine(DesbloquearClick());
    }

    public void MostrarCorreo()
    {
        panelActual = 0;

        panelIntroduccion.SetActive(false);
        panelCorreo.SetActive(true);
        panelSospechoso.SetActive(false);
        panelDecision.SetActive(false);

        if (audioSource != null && sonidoCorreo != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(sonidoCorreo);
        }
    }

    void MostrarSospechoso()
    {
        panelActual = 1;

        panelIntroduccion.SetActive(false);
        panelCorreo.SetActive(false);
        panelSospechoso.SetActive(true);
        panelDecision.SetActive(false);

        if (audioSource != null && sonidoCorreo != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(sonidoCorreo);
        }
    }

    void MostrarDecision()
    {
        panelActual = 2;

        panelIntroduccion.SetActive(false);
        panelCorreo.SetActive(false);
        panelSospechoso.SetActive(false);
        panelDecision.SetActive(true);

        if (audioSource != null && sonidoDetalle != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(sonidoDetalle);
        }

        StartCoroutine(ZoomSuave(3.6f));
    }

    void SiguientePanel()
    {
        if (panelActual == 0)
        {
            MostrarSospechoso();
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
            MostrarCorreo();
        }
        else if (panelActual == 2)
        {
            MostrarSospechoso();
        }
    }

    public void OpcionCorrecta()
    {
        if (yaRespondio) return;

        yaRespondio = true;
        int tiempoRespuesta = Mathf.RoundToInt(Time.time - tiempoInicio);
        // 🔥 ENVIAR DECISION AL BACKEND
        StartCoroutine(
            APIManager.Instance.SendDecision(
                1,
                "reportar",
                 tiempoRespuesta

            )
        );

        panelDecision.SetActive(false);
        panelBueno.SetActive(true);

        if (audioSource != null && sonidoCorrecto != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(sonidoCorrecto);
        }

        Invoke(nameof(IrAEscenario2), 2f);
    }

    public void OpcionIncorrecta()
    {
        if (yaRespondio) return;

        yaRespondio = true;
        int tiempoRespuesta = Mathf.RoundToInt(Time.time - tiempoInicio);
        // 🔥 RESPUESTA INCORRECTA
        StartCoroutine(
            APIManager.Instance.SendDecision(
                1,
                "dar_contraseña",
                tiempoRespuesta
            )
        );

        panelDecision.SetActive(false);
        panelMalo.SetActive(true);

        if (audioSource != null && sonidoError != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(sonidoError);
        }

        PerderVida();
    }

    void IrAEscenario2()
    {
        SceneManager.LoadScene("MenuNivelInicial");
    }

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

    public void OtroIntento()
    {
        panelMalo.SetActive(false);
        panelDecision.SetActive(true);
        yaRespondio = false;
    }

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

    public void SonidoBoton()
    {
        if (audioSource != null && sonidoBoton != null)
        {
            audioSource.PlayOneShot(sonidoBoton);
        }
    }
}