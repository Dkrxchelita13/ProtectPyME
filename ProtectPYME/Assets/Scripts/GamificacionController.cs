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
    public static int puntaje = 0;
    public static int vidas = 3;

    private int erroresAcumulados = 0;
    private PreguntasController pController;
    public GameObject canvasGameOver;

    void Start()
    {
        pController = GetComponent<PreguntasController>();

        tiempoActual = tiempoLimite;

        puntaje = 0;
        vidas = 3;
        erroresAcumulados = 0;

        txtPuntaje.text = puntaje.ToString();

        ActualizarUI();
    }



    public void SumarPuntos(int cantidad) {
        puntaje += cantidad;
        ActualizarUI();
    }

    public void RestarPuntos(int cantidad) {
        puntaje = Mathf.Max(0, puntaje - cantidad);
        ActualizarUI();
    }

    public void RegistrarError()
    {
        erroresAcumulados++;
        Debug.Log($"❌ Error acumulado: {erroresAcumulados}/2");

        if (erroresAcumulados >= 2)
        {
            QuitarVida();
            erroresAcumulados = 0; // Reiniciar contador tras perder la vida
        }
    }

    public void QuitarVida() {
        vidas--;
        
        // Apagamos el icono de vida correspondiente (3 vidas -> índices 2, 1, 0)
        if (vidas >= 0 && vidas < iconosVidas.Length) 
        {
            if (iconosVidas[vidas] != null)
                iconosVidas[vidas].SetActive(false);
        }

        if (vidas <= 0) 
        {
            FinalizarJuego();
        }
    }

    public void ReiniciarCronometro()

    {

    }

    void ActualizarUI() => txtPuntaje.text = puntaje.ToString();

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
}

