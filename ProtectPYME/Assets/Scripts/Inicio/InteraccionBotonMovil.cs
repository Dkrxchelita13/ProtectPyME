using UnityEngine;
using UnityEngine.EventSystems;

public class InteraccionBotonMovil : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Configuración de Escala")]
    [SerializeField] private Vector3 escalaNormal = Vector3.one;
    [SerializeField] private Vector3 escalaGrande = new Vector3(1.15f, 1.15f, 1.15f);
    [SerializeField] private float velocidadTransicion = 10f;

    [Header("Referencia al Manager")]
    [Tooltip("Arrastra aquí el objeto que tiene el script MenuManager.")]
    public MenuManager menuManager;

    private Vector3 escalaObjetivo;

    void Start()
    {
        escalaObjetivo = escalaNormal;
        transform.localScale = escalaNormal;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, escalaObjetivo, Time.deltaTime * velocidadTransicion);
    }

    public void OnPointerEnter(PointerEventData eventData) { ActivarEfecto(true); }
    public void OnPointerExit(PointerEventData eventData) { ActivarEfecto(false); }
    public void OnPointerDown(PointerEventData eventData) { ActivarEfecto(true); }
    public void OnPointerUp(PointerEventData eventData) { ActivarEfecto(false); }

    private void ActivarEfecto(bool activar)
    {
        if (activar)
        {
            escalaObjetivo = escalaGrande;
            if (menuManager != null) menuManager.BotonEncendido();
        }
        else
        {
            escalaObjetivo = escalaNormal;
            if (menuManager != null) menuManager.BotonApagado();
        }
    }
}