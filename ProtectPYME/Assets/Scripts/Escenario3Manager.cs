using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Escenario3Manager : MonoBehaviour
{
    private float tiempoInicio;
    private bool yaRespondio = false;
    public GameObject panelUSB;
    public GameObject panelDecision;
    public GameObject panelMalo;
    public GameObject panelBueno;

    public GameObject[] corazones;
    public Camera camara;

    public AudioSource audioSource;
    public AudioClip sonidoDetalle;
    public AudioClip sonidoCorreo; 
    public AudioClip sonidoBoton;
    public AudioClip sonidoError;
    public AudioClip sonidoCorrecto;

    int panelActual = 0; 

    void Start()
    {
        tiempoInicio = Time.time;

        if (GameManagerGlobal.instancia == null)
        {
            GameObject gm = new GameObject("GameManagerGlobal");
            gm.AddComponent<GameManagerGlobal>();
        }

        camara.orthographicSize = 3.6f;

        ActualizarCorazones();

        panelActual = 0;

        MostrarUSB(); 
    }


    void Update()
    {
        if (panelDecision.activeSelf || panelMalo.activeSelf || panelBueno.activeSelf)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            float mitad = Screen.width / 2;

            if (Input.mousePosition.x > mitad)
                SiguientePanel();
            else
                PanelAnterior();
        }
    }

    void MostrarUSB()
    {
        panelUSB.SetActive(true);
        panelDecision.SetActive(false);

        if (audioSource != null && sonidoCorreo != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(sonidoCorreo);
        }
    }

    void MostrarDecision()
    {
        panelUSB.SetActive(false);
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
            panelActual = 1;
            MostrarDecision();
        }
    }

    void PanelAnterior()
    {
        if (panelActual == 1)
        {
            panelActual = 0;
            MostrarUSB();
        }
    }

    public void OpcionMala()
    {
        if (yaRespondio) return;

        yaRespondio = true;
        int tiempoRespuesta = Mathf.RoundToInt(Time.time - tiempoInicio);
        StartCoroutine(
            APIManager.Instance.SendDecision(
                2,
                "conectar_usb",
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
        StartCoroutine(ZoomSuave(3.6f));
    }

    public void OpcionBuena()
    {
        if (yaRespondio) return;

        yaRespondio = true;
        int tiempoRespuesta = Mathf.RoundToInt(Time.time - tiempoInicio);
        StartCoroutine(
            APIManager.Instance.SendDecision(
                2,
                "no_conectar",
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

        Invoke("IrAEscenario", 2f);
        StartCoroutine(ZoomSuave(3.6f));
    }

    void IrAEscenario()
    {
        SceneManager.LoadScene("MenuNivelInicial");
    }

    void PerderVida()
    {
        GameManagerGlobal.instancia.PerderVida();
        ActualizarCorazones();
    }

    void ActualizarCorazones()
    {
        if (corazones == null || corazones.Length == 0)
            return;

        int vidas = 3;

        if (GameManagerGlobal.instancia != null)
        {
            vidas = GameManagerGlobal.instancia.vidas;
        }

        for (int i = 0; i < corazones.Length; i++)
        {
            if (corazones[i] != null)
                corazones[i].SetActive(i < vidas);
        }
    }

    public void OtroIntento()
    {
        yaRespondio = false;

        panelMalo.SetActive(false);
        panelDecision.SetActive(true);
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

    public void SonidoBoton()
    {
        if (audioSource != null && sonidoBoton != null)
        {
            audioSource.PlayOneShot(sonidoBoton);
        }
    }
}