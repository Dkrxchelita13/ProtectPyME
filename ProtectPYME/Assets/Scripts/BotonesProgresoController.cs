using UnityEngine;
using UnityEngine.UI;

public class BotonesProgresoController : MonoBehaviour
{
    [Header("Botones")]
    public Button botonEscenario1;
    public Button botonEscenario2;
    public Button botonEscenario3;
    public Button botonMinijuegos;

    [Header("Botones Nivel Intermedio")]
    public Button botonEscenario4;
    public Button botonEscenario5;
    public Button botonEscenario6;

    [Header("Candados Visuales")]
    public GameObject candadoEscenario2;
    public GameObject candadoEscenario3;
    public GameObject candadoMinijuegos;

    public GameObject candadoEscenario5;
    public GameObject candadoEscenario6;

    void Start()
    {
        ActualizarProgreso();
    }

    void OnEnable()
    {
        // Se vuelve a evaluar cada vez que el menú se activa o vuelve a aparecer
        ActualizarProgreso();
    }

    public void ActualizarProgreso()
    {
        // 1 es el nivel inicial (solo Escenario 1 desbloqueado)
        int nivelAlcanzado = PlayerPrefs.GetInt("NivelAlcanzado", 1);

        int progresoIntermedio = PlayerPrefs.GetInt("ProgresoIntermedio", 1);

        // --- ESCENARIO 1 ---
        // Siempre desbloqueado
        if (botonEscenario1 != null) botonEscenario1.interactable = true;

        // --- ESCENARIO 2 ---
        bool desblEscenario2 = nivelAlcanzado >= 2;
        if (botonEscenario2 != null) botonEscenario2.interactable = desblEscenario2;
        if (candadoEscenario2 != null) candadoEscenario2.SetActive(!desblEscenario2);

        // --- ESCENARIO 3 ---
        bool desblEscenario3 = nivelAlcanzado >= 3;
        if (botonEscenario3 != null) botonEscenario3.interactable = desblEscenario3;
        if (candadoEscenario3 != null) candadoEscenario3.SetActive(!desblEscenario3);

        // --- MINIJUEGOS ---
        bool desblMinijuegos = nivelAlcanzado >= 4;
        if (botonMinijuegos != null) botonMinijuegos.interactable = desblMinijuegos;
        if (candadoMinijuegos != null) candadoMinijuegos.SetActive(!desblMinijuegos);



        if (botonEscenario4 != null) botonEscenario4.interactable = true;

        // Escenario 5: Se desbloquea cuando progresoIntermedio sea 2
        bool desblEscenario5 = progresoIntermedio >= 2;
        if (botonEscenario5 != null) botonEscenario5.interactable = desblEscenario5;
        if (candadoEscenario5 != null) candadoEscenario5.SetActive(!desblEscenario5);

        // Escenario 6: Se desbloquea cuando progresoIntermedio sea 3
        bool desblEscenario6 = progresoIntermedio >= 3;
        if (botonEscenario6 != null) botonEscenario6.interactable = desblEscenario6;
        if (candadoEscenario6 != null) candadoEscenario6.SetActive(!desblEscenario6);

        bool desblMinijuegos2 = progresoIntermedio >= 4;
        if (botonMinijuegos != null) botonMinijuegos.interactable = desblMinijuegos2;
        if (candadoMinijuegos != null) candadoMinijuegos.SetActive(!desblMinijuegos2);


    }

    // Método de prueba/desarrollo para restablecer todo al inicio
    [ContextMenu("Reiniciar Progreso")]
    public void ReiniciarProgreso()
    {
        PlayerPrefs.DeleteKey("NivelAlcanzado");
        PlayerPrefs.DeleteKey("ProgresoIntermedio");
        PlayerPrefs.Save();
        ActualizarProgreso();
    }
}
