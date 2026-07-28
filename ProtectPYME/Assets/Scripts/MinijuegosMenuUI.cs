using UnityEngine;
using TMPro;

public class MinijuegosMenuUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI txtTituloReforzamiento;

    void Start()
    {
        ActualizarTituloMinijuegos();
    }

    void ActualizarTituloMinijuegos()
    {
        if (txtTituloReforzamiento == null) return;

        // Leemos la sugerencia almacenada en la memoria global de la IA
        string temaReforzamiento = AIState.RecommendedTraining;

        if (!string.IsNullOrEmpty(temaReforzamiento))
        {
            // Traducimos el tema que viene de la IA a un título legible para el jugador
            txtTituloReforzamiento.text = FormatearTitulo(temaReforzamiento);
        }
        else
        {
            // Si por alguna razón la IA no tiene datos aún, hacemos una consulta rápida al API
            CargarRiesgoDesdeAPI();
        }
    }

    void CargarRiesgoDesdeAPI()
    {
        if (APIManager.Instance == null) return;

        StartCoroutine(APIManager.Instance.GetAIRisk(
            onSuccess: (data) =>
            {
                if (txtTituloReforzamiento != null)
                {
                    txtTituloReforzamiento.text = FormatearTitulo(data.recommended_training);
                }
            },
            onError: (error) =>
            {
                // Título por defecto en caso de no tener internet/error
                txtTituloReforzamiento.text = "REFORZAMIENTO GENERAL";
            }
        ));
    }

    private string FormatearTitulo(string tema)
    {
        if (string.IsNullOrEmpty(tema)) return "REFORZAMIENTO GENERAL";

        string temaLiso = tema.ToLower().Trim();

        // Mapeo adaptativo según las temáticas de tus 3 escenarios
        if (temaLiso.Contains("phishing") || temaLiso.Contains("correo") || temaLiso.Contains("1"))
        {
            return "REFORZAMIENTO: DETECCIÓN DE PHISHING";
        }
        else if (temaLiso.Contains("contraseña") || temaLiso.Contains("password") || temaLiso.Contains("acceso") || temaLiso.Contains("2"))
        {
            return "REFORZAMIENTO: GESTIÓN DE CONTRASEÑAS";
        }
        else if (temaLiso.Contains("usb") || temaLiso.Contains("baiting") || temaLiso.Contains("extraible") || temaLiso.Contains("3"))
        {
            return "REFORZAMIENTO: SEGURIDAD EN MEDIOS EXTRAÍBLES";
        }

        // Si la IA devuelve la categoría directamente (ej. "Phishing"), la ponemos en mayúsculas
        return "REFORZAMIENTO: " + tema.ToUpper();
    }
}