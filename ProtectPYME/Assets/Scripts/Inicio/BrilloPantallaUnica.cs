using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BrilloPantallaUnica : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Image imagenPantalla;

    [Header("Configuración de Colores")]
    [SerializeField] private Color colorNormal = new Color(0.6f, 0.6f, 0.6f, 1f); 
    
    [SerializeField] private Color colorHover = Color.white; 
    
    [SerializeField] private Color colorClick = new Color(1.2f, 1.2f, 1.2f, 1f); 

    void Start()
    {
        // Buscamos automáticamente el componente Image del objeto
        imagenPantalla = GetComponent<Image>();
        
        // Iniciamos el juego con la pantalla en su estado normal (apagado/opaco)
        if (imagenPantalla != null)
        {
            imagenPantalla.color = colorNormal;
        }
    }

    // Se ejecuta automáticamente cuando el cursor entra en la pantalla
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (imagenPantalla != null) imagenPantalla.color = colorHover;
    }

    // Se ejecuta automáticamente cuando el cursor sale de la pantalla
    public void OnPointerExit(PointerEventData eventData)
    {
        if (imagenPantalla != null) imagenPantalla.color = colorNormal;
    }

    // Se ejecuta automáticamente cuando haces clic en la pantalla
    public void OnPointerClick(PointerEventData eventData)
    {
        if (imagenPantalla != null)
        {
            imagenPantalla.color = colorClick;
            // Espera 0.1 segundos y regresa al color de Hover
            Invoke("RegresarAHover", 0.1f);
        }
    }

    void RegresarAHover()
    {
        if (imagenPantalla != null) imagenPantalla.color = colorHover;
    }
}