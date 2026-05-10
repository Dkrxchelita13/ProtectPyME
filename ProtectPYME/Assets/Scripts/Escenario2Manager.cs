using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Escenario2Manager : MonoBehaviour
{
    public GameObject panelAlerta;
    public GameObject panelDetalle;
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
        camara.orthographicSize = 3.6f;

        ActualizarCorazones();

        panelActual = 0;

        MostrarAlerta(); 
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

 
    void MostrarAlerta()
    {
        panelAlerta.SetActive(true);
        panelDetalle.SetActive(false);
        panelDecision.SetActive(false);

        if (audioSource != null && sonidoCorreo != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(sonidoCorreo);
        }
    }

    void MostrarDetalle()
    {
        panelAlerta.SetActive(false);
        panelDetalle.SetActive(true);
        panelDecision.SetActive(false);

        if (audioSource != null && sonidoCorreo != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(sonidoCorreo);
        }
    }


    void MostrarDecision()
    {
        panelAlerta.SetActive(false);
        panelDetalle.SetActive(false);
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
            MostrarDetalle();
        }
        else if (panelActual == 1)
        {
            panelActual = 2;
            MostrarDecision();
        }
    }


    void PanelAnterior()
    {
        if (panelActual == 1)
        {
            panelActual = 0;
            MostrarAlerta();
        }
        else if (panelActual == 2)
        {
            panelActual = 1;
            MostrarDetalle();
        }
    }

    public void OpcionMala()
    {
        panelDecision.SetActive(false);
        panelMalo.SetActive(true);

        if (audioSource != null && sonidoError != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(sonidoError);
        }

        PerderVida();
    }

    public void OpcionMedia()
    {
        panelDecision.SetActive(false);
        panelMalo.SetActive(true);

        if (audioSource != null && sonidoError != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(sonidoError);
        }

        PerderVida();
    }

    public void OpcionBuena()
    {
        panelDecision.SetActive(false);
        panelBueno.SetActive(true);

        if (audioSource != null && sonidoCorrecto != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(sonidoCorrecto);
        }

        Invoke("IrAEscenario3", 2f);
    }

    void IrAEscenario3()
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