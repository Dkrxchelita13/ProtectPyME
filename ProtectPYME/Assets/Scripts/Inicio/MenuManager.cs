using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Componente de Sombra")]
    public Image shadowBackground;
    
    [Tooltip("La opacidad en el estado oscuro inicial (0 = Invisible, 1 = Totalmente oscuro).")]
    [Range(0f, 1f)] public float opacidadOscuraInicial = 0.7f;
    
    [Tooltip("La opacidad cuando se ilumina al pulsar/pasar el cursor (0 significa que se limpia la sombra por completo).")]
    [Range(0f, 1f)] public float opacidadAlAclarar = 0f;

    public float velocidadTransicion = 10f;

    private float opacidadObjetivo;
    private int botonesActivos = 0;

    void Start()
    {
        // Al iniciar el juego, la meta es el estado oscuro por defecto
        opacidadObjetivo = opacidadOscuraInicial;
        
        if (shadowBackground != null)
        {
            Color c = shadowBackground.color;
            c.a = opacidadOscuraInicial;
            shadowBackground.color = c;
        }
    }

    void Update()
    {
        // LÓGICA INVERTIDA: 
        // Si hay botones activos, bajamos la opacidad para que se ACLARE la pantalla.
        // Si no hay ninguno, regresa a la opacidad OSCURA inicial.
        opacidadObjetivo = (botonesActivos > 0) ? opacidadAlAclarar : opacidadOscuraInicial;

        // Modifica suavemente la opacidad frame a frame
        if (shadowBackground != null)
        {
            Color c = shadowBackground.color;
            c.a = Mathf.Lerp(c.a, opacidadObjetivo, Time.deltaTime * velocidadTransicion);
            shadowBackground.color = c;
        }
    }

    public void BotonEncendido()
    {
        botonesActivos++;
    }

    public void BotonApagado()
    {
        botonesActivos = Mathf.Max(0, botonesActivos - 1);
    }
}