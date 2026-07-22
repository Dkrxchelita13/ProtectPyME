using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GamificacionController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI txtPuntaje;
    public GameObject[] iconosVidas;

    [Header("Ajustes")]
    public float tiempoLimite = 15f;
    public float tiempoActual;

    public int puntajeObtenido = 0;
    public float progresoSeguridad = 0f; // Cambiado a float

    private int erroresAcumulados = 0;
    private PreguntasController pController;
    public GameObject canvasGameOver;

    void Awake()
    {
        progresoSeguridad = ObtenerSeguridadActual();
    }

    void Start()
    {
        pController = GetComponent<PreguntasController>();
        tiempoActual = tiempoLimite;

        puntajeObtenido = 0;
        erroresAcumulados = 0;

        progresoSeguridad = ObtenerSeguridadActual(); 

        if (txtPuntaje != null)
            txtPuntaje.text = puntajeObtenido.ToString();

        ActualizarUIVidas(); 
        
        Debug.Log("🎮 Minijuego iniciado. Vidas: " + ObtenerVidasActuales() + " | Seguridad inicial: " + progresoSeguridad.ToString("F1") + "%");
    }

    public float ObtenerSeguridadActual()
    {
        string claveSeguridad = (GameManagerGlobal.instancia != null) 
            ? GameManagerGlobal.instancia.ObtenerClaveUsuario("SeguridadPersistente") 
            : "SeguridadPersistente";

        float seguridadGuardada = PlayerPrefs.GetFloat(claveSeguridad, 0f);

        if (GameManagerGlobal.instancia != null)
        {
            if (GameManagerGlobal.instancia.nivelSeguridad > seguridadGuardada)
            {
                seguridadGuardada = GameManagerGlobal.instancia.nivelSeguridad;
            }
            else
            {
                GameManagerGlobal.instancia.nivelSeguridad = seguridadGuardada;
            }
        }
        
        return seguridadGuardada;
    }

    public void SumarPuntos(int cantidad) {
        puntajeObtenido += cantidad;
        txtPuntaje.text = puntajeObtenido.ToString();
    }

    public void RestarPuntos(int cantidad) {
        puntajeObtenido = Mathf.Max(0, puntajeObtenido - cantidad);
        txtPuntaje.text = puntajeObtenido.ToString();
    }

    public void RegistrarError()
    {
        erroresAcumulados++;
        Debug.Log($"❌ Error acumulado: {erroresAcumulados}/2");

        if (erroresAcumulados >= 2)
        {
            QuitarVida();
            erroresAcumulados = 0; 
        }
    }

    public int ObtenerVidasActuales()
    {
        if (GameManagerGlobal.instancia != null)
            return GameManagerGlobal.instancia.vidas;
        
        string claveVidas = (GameManagerGlobal.instancia != null) 
            ? GameManagerGlobal.instancia.ObtenerClaveUsuario("Vidas") 
            : "Vidas";

        return PlayerPrefs.GetInt(claveVidas, 3);
    }

    public void QuitarVida() {
        if (GameManagerGlobal.instancia != null)
        {
            GameManagerGlobal.instancia.PerderVida();
        }
        else
        {
            int vidas = PlayerPrefs.GetInt("Vidas", 3);
            vidas--;
            if (vidas < 0) vidas = 0;
            PlayerPrefs.SetInt("Vidas", vidas);
            PlayerPrefs.Save();
        }

        ActualizarUIVidas();

        if (ObtenerVidasActuales() <= 0) 
        {
            FinalizarJuego();
        }
    }

    public void ReiniciarCronometro() { }

    public void ActualizarUI() 
    {
        txtPuntaje.text = puntajeObtenido.ToString();
    }

    void ActualizarUIVidas()
    {
        int vidasActuales = ObtenerVidasActuales();

        for (int i = 0; i < iconosVidas.Length; i++)
        {
            iconosVidas[i].SetActive(i < vidasActuales);
        }
    }

    void FinalizarJuego()
    {
        Debug.Log("GAME OVER");
        if (canvasGameOver != null)
        {
            canvasGameOver.SetActive(true);
        }
        if (pController != null)
        {
            pController.DetenerJuegoPorGameOver();
        }
    }

    public void ModificarSeguridad(float cantidad)
    {
        progresoSeguridad += cantidad;
        progresoSeguridad = Mathf.Clamp(progresoSeguridad, 0f, 100f);

        if (GameManagerGlobal.instancia != null)
        {
            GameManagerGlobal.instancia.nivelSeguridad = progresoSeguridad;
            
            string claveSeguridad = GameManagerGlobal.instancia.ObtenerClaveUsuario("SeguridadPersistente");
            PlayerPrefs.SetFloat(claveSeguridad, progresoSeguridad);
            PlayerPrefs.Save();
        }
        else
        {
            PlayerPrefs.SetFloat("SeguridadPersistente", progresoSeguridad);
            PlayerPrefs.Save();
        }

        Debug.Log($"🛡️ Seguridad Minijuego modificada ({cantidad}%). Nueva seguridad: {progresoSeguridad}%");
    }

    public int ObtenerPuntos()
    {
        return puntajeObtenido;
    }

    public int ObtenerVidas()
    {
        return ObtenerVidasActuales();
    }
}


