using UnityEngine;

public class EfectoFlotarLogo : MonoBehaviour
{
    [Header("Configuración del Movimiento")]
    [Tooltip("Qué tan rápido se mueve de arriba a abajo.")]
    [SerializeField] private float velocidad = 3f;

    [Tooltip("Qué tan lejos (arriba y abajo) se desplazará desde su punto original.")]
    [SerializeField] private float amplitud = 15f;

    private RectTransform rectTransform;
    private Vector3 posicionInicial;

    void Start()
    {
        // Al ser un elemento de Canvas, usamos RectTransform en lugar de Transform normal
        rectTransform = GetComponent<RectTransform>();
        
        if (rectTransform != null)
        {
            // Guardamos la posición exacta en la que acomodaste el logo en el Canvas
            posicionInicial = rectTransform.anchoredPosition;
        }
    }

    void Update()
    {
        if (rectTransform != null)
        {
            // Calculamos el desfase vertical usando la función matemática de Seno
            // Mathf.Sin nos da un valor que oscila suavemente entre -1 y 1
            float nuevoY = posicionInicial.y + Mathf.Sin(Time.time * velocidad) * amplitud;

            // Aplicamos la nueva posición manteniendo fijos los ejes X y Z
            rectTransform.anchoredPosition = new Vector3(posicionInicial.x, nuevoY, posicionInicial.z);
        }
    }
}