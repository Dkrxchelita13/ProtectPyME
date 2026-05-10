using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ProgresoController : MonoBehaviour
{
    [Header("Referencias de UI")]
    public TextMeshProUGUI txtPuntajeTotal;
    public TextMeshProUGUI txtSeguridadPromedio;
    public TextMeshProUGUI txtPartidasJugadas;
    public TextMeshProUGUI txtVidasMax;

    void Start()
    {
        // Al iniciar la escena, cargamos y mostramos los datos
        CargarYMostrarProgreso();
    }

    public void CargarYMostrarProgreso()
    {
        // 1. Recuperamos los datos acumulados de PlayerPrefs
        // El segundo parámetro (0) es el valor por defecto si no hay nada guardado
        int puntosTotales = PlayerPrefs.GetInt("PuntajeTotal", 0);
        int totalPartidas = PlayerPrefs.GetInt("PartidasJugadas", 0);
        float sumaSeguridades = PlayerPrefs.GetFloat("SumaSeguridades", 0f);
        int recordVidas = PlayerPrefs.GetInt("RecordVidas", 0);

        // 2. Calculamos el promedio de seguridad
        float promedioSeguridad = 0f;
        if (totalPartidas > 0)
        {
            promedioSeguridad = sumaSeguridades / totalPartidas;
        }

        // 3. Mostramos en los textos con formato
        if (txtPuntajeTotal != null)
        {
            // "D6" para mostrar los ceros (ej: 000030)
            txtPuntajeTotal.text = puntosTotales.ToString("D6");
        }

        if (txtSeguridadPromedio != null)
        {
            // "F0" para que no muestre decimales (ej: 85%)
            txtSeguridadPromedio.text = promedioSeguridad.ToString("F0");
        }

        if (txtPartidasJugadas != null)
        {
            txtPartidasJugadas.text = "Partidas Jugadas: " + totalPartidas;
        }

        if (txtVidasMax != null)
        {
            txtVidasMax.text = recordVidas.ToString();
        }
        
    }
}