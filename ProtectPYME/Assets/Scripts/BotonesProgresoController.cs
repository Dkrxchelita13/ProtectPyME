using UnityEngine;
using UnityEngine.UI;

public enum TipoMenuProgreso
{
    Auto,
    Inicial,
    Intermedio
}

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

    [Header("Configuracion")]
    [SerializeField] private TipoMenuProgreso tipoMenu = TipoMenuProgreso.Auto;

    private string ultimoLogProgreso = "";

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
        int nivelAlcanzado = UserProgressCache.GetInitialStage();
        int progresoIntermedio = UserProgressCache.GetIntermediateStage();

        TipoMenuProgreso menuResuelto = ResolverTipoMenu();
        bool minijuegosDesbloqueados = menuResuelto == TipoMenuProgreso.Intermedio
            ? ActualizarNivelIntermedio(nivelAlcanzado, progresoIntermedio)
            : ActualizarNivelInicial(nivelAlcanzado);

        LogEstadoProgreso(
            menuResuelto,
            nivelAlcanzado,
            progresoIntermedio,
            minijuegosDesbloqueados
        );
    }

    private TipoMenuProgreso ResolverTipoMenu()
    {
        if (tipoMenu != TipoMenuProgreso.Auto)
        {
            return tipoMenu;
        }

        bool tieneBotonesIntermedios =
            botonEscenario4 != null ||
            botonEscenario5 != null ||
            botonEscenario6 != null;

        return tieneBotonesIntermedios
            ? TipoMenuProgreso.Intermedio
            : TipoMenuProgreso.Inicial;
    }

    private bool ActualizarNivelInicial(int nivelAlcanzado)
    {
        SetButtonInteractable(botonEscenario1, true);

        bool desblEscenario2 = nivelAlcanzado >= 2;
        SetButtonInteractable(botonEscenario2, desblEscenario2);
        SetLockActive(candadoEscenario2, !desblEscenario2);

        bool desblEscenario3 = nivelAlcanzado >= 3;
        SetButtonInteractable(botonEscenario3, desblEscenario3);
        SetLockActive(candadoEscenario3, !desblEscenario3);

        bool desblMinijuegos = nivelAlcanzado >= 4;
        SetMinigamesUnlocked(desblMinijuegos);
        return desblMinijuegos;
    }

    private bool ActualizarNivelIntermedio(
        int nivelAlcanzado,
        int progresoIntermedio
    )
    {
        SetButtonInteractable(botonEscenario4, true);

        bool desblEscenario5 = progresoIntermedio >= 2;
        SetButtonInteractable(botonEscenario5, desblEscenario5);
        SetLockActive(candadoEscenario5, !desblEscenario5);

        bool desblEscenario6 = progresoIntermedio >= 3;
        SetButtonInteractable(botonEscenario6, desblEscenario6);
        SetLockActive(candadoEscenario6, !desblEscenario6);

        bool nivelInicialCompleto = nivelAlcanzado >= 4;
        bool nivelIntermedioCompleto = progresoIntermedio >= 4;
        bool desblMinijuegos =
            nivelInicialCompleto || nivelIntermedioCompleto;

        SetMinigamesUnlocked(desblMinijuegos);
        return desblMinijuegos;
    }

    private void SetMinigamesUnlocked(bool unlocked)
    {
        SetButtonInteractable(botonMinijuegos, unlocked);
        SetLockActive(candadoMinijuegos, !unlocked);
    }

    private void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private void SetLockActive(GameObject lockObject, bool active)
    {
        if (lockObject != null)
        {
            lockObject.SetActive(active);
        }
    }

    private void LogEstadoProgreso(
        TipoMenuProgreso menu,
        int nivelAlcanzado,
        int progresoIntermedio,
        bool minijuegosDesbloqueados
    )
    {
        string snapshot =
            "[Progress] Menu=" +
            menu +
            " NivelAlcanzado=" +
            nivelAlcanzado +
            " ProgresoIntermedio=" +
            progresoIntermedio +
            " Minijuegos=" +
            minijuegosDesbloqueados;

        if (snapshot == ultimoLogProgreso)
        {
            return;
        }

        ultimoLogProgreso = snapshot;
        Debug.Log(snapshot);
    }

    // Método de prueba/desarrollo para restablecer todo al inicio
    [ContextMenu("Reiniciar Progreso")]
    public void ReiniciarProgreso()
    {
        UserProgressCache.ClearActiveUserProgress();
        ActualizarProgreso();
    }
}
