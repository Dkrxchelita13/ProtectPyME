using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class GamificacionController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI txtPuntaje;
    public Image fillCronometro;
    public GameObject[] iconosVidas;

    [Header("Ajustes")]
    public float tiempoLimite = 15f;
    private float tiempoActual;
    private int puntaje = 0;
    private int vidas = 3;
    private int fallosPorTiempoSeguidos = 0;
    private bool juegoActivo = true;

    private PreguntasController pController;

    void Start() {
        pController = GetComponent<PreguntasController>();
        tiempoActual = tiempoLimite;
        ActualizarUI();
    }

    void Update() {
        if (!juegoActivo) return;

        if (tiempoActual > 0) {
            tiempoActual -= Time.deltaTime;
            fillCronometro.fillAmount = tiempoActual / tiempoLimite;
        } else {
            ProcesarFalloTiempo();
        }
    }

    public void SumarPuntos(int cantidad) {
        puntaje += cantidad;
        fallosPorTiempoSeguidos = 0; // Si responde, se limpia el contador de fallos de tiempo
        ActualizarUI();
    }

    public void RestarPuntos(int cantidad) {
        puntaje = Mathf.Max(0, puntaje - cantidad);
        ActualizarUI();
    }

    public void ProcesarFalloTiempo() {
        RestarPuntos(25);
        fallosPorTiempoSeguidos++;

        if (fallosPorTiempoSeguidos >= 2) {
            QuitarVida();
            fallosPorTiempoSeguidos = 0;
        }

        ReiniciarCronometro();
        pController.PasarSiguientePregunta(); // Saltamos de pregunta porque se acabó el tiempo
    }

    public void QuitarVida() {
        vidas--;
        if (vidas >= 0) iconosVidas[vidas].SetActive(false);
        if (vidas <= 0) FinalizarJuego();
    }

    public void ReiniciarCronometro() => tiempoActual = tiempoLimite;

    void ActualizarUI() => txtPuntaje.text = puntaje.ToString();

    void FinalizarJuego() {
        juegoActivo = false;
        Debug.Log("GAME OVER");
        // Aquí activarías tu panel de perder
    }
}

