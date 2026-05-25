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
    public Image fillCronometro;
    public GameObject[] iconosVidas;

    [Header("Ajustes")]
    public float tiempoLimite = 15f;
    public float tiempoActual;
    public static int puntaje = 0;
    public static int vidas = 3;

    private PreguntasController pController;

    void Start()
    {
        pController = GetComponent<PreguntasController>();

        tiempoActual = tiempoLimite;

        puntaje = 0;
        vidas = 3;

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



    public void QuitarVida() {
        vidas--;
        if (vidas >= 0) iconosVidas[vidas].SetActive(false);
        if (vidas <= 0) FinalizarJuego();
    }

    public void ReiniciarCronometro()
    {
    }
    void ActualizarUI() => txtPuntaje.text = puntaje.ToString();

    void FinalizarJuego()
    {
        Debug.Log("GAME OVER");
    }
}

