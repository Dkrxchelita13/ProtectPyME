using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static QuizController;

public class QuizController : MonoBehaviour
{
    private bool usandoBackend = false;

    [System.Serializable]
    public class Pregunta
    {
        public string enunciado;
        public string[] opciones;
        public int indiceCorrecto;
    }

    [Header("Referencias de UI")]
    public TextMeshProUGUI textoPregunta;
    public TextMeshProUGUI[] textosOpciones;
    public TextMeshProUGUI textoTimer;
    public TextMeshProUGUI textoTimer2;
    public Image barraTiempo;
    public Image barraTiempo2;
    public Image[] imagenesBotones;
    public GameObject[] iconosResultado;

    [Header("Sprites de Feedback")]
    public Sprite spritePalomita;
    public Sprite spriteTache;

    [Header("Colores de Feedback")]
    public Color colorCorrecto = Color.green;
    public Color colorIncorrecto = Color.red;
    private Color colorOriginalBotones;

    [Header("Configuración")]
    public Pregunta[] bancoDePreguntas;
    public float tiempoPorPregunta = 15f;

    [Header("Sistema de Puntos")]
    public TextMeshProUGUI textoPuntaje;
    private int puntajeActual = 0;

    [Header("Sistema de Vidas")]
    public GameObject[] iconosVidas;
    private int vidasRestantes;

    [Header("Barra de Seguridad")]
    public Image barraSeguridad;
    private float nivelSeguridad = 1.0f;

    [Header("Pantalla de Resultados")]
    public GameObject canvasGanador;
    public TextMeshProUGUI textoPuntosFinal;
    public TextMeshProUGUI textoVidasFinal;
    public TextMeshProUGUI textoSeguridadFinal;

    [Header("Pantalla de Game Over")]
    public GameObject canvasGameOver;

    [Header("Sonidos")]
    public AudioSource fuenteAudio;
    public AudioClip sonidoCorrecto;
    public AudioClip sonidoIncorrecto;
    public AudioClip sonidoGameWin;
    public AudioClip sonidoGameOver;

    private int preguntaActual = 0;
    private float tiempoRestante;
    private bool juegoActivo = true;
    private int erroresAcumulados = 0;

    void Start()
    {
        Time.timeScale = 1f;
        vidasRestantes = iconosVidas.Length;

        if (textoPuntaje != null)
        {
            textoPuntaje.text = puntajeActual.ToString();
        }

        if (imagenesBotones.Length > 0)
            colorOriginalBotones = imagenesBotones[0].color;

        // intentar backend
        StartCoroutine(APIManager.Instance.GetQuiz(OnQuizLoaded));
    }

    void OnQuizLoaded(string json)
    {
        if (string.IsNullOrEmpty(json) || json == "ERROR")
        {
            Debug.Log("⚠️ OFFLINE → usando banco local");
            usandoBackend = false;
            CargarPregunta();
            return;
        }

        Debug.Log("✅ QUIZ DESDE BACKEND");

        // 🔥 FIX para array
        string fixedJson = "{\"items\":" + json + "}";

        QuizBackendList data = JsonUtility.FromJson<QuizBackendList>(fixedJson);

        if (data == null || data.items == null || data.items.Length == 0)
        {
            Debug.Log("⚠️ Backend vacío → usando banco local");
            usandoBackend = false;
            CargarPregunta();
            return;
        }

        usandoBackend = true;

        bancoDePreguntas = new Pregunta[data.items.Length];

        for (int i = 0; i < data.items.Length; i++)
        {
            bancoDePreguntas[i] = new Pregunta
            {
                enunciado = data.items[i].question ?? "Pregunta vacía",
                opciones = data.items[i].options ?? new string[] { "N/A" },
                indiceCorrecto = data.items[i].answer
            };
        }

        CargarPregunta();
    }
    [System.Serializable]
    public class QuizBackend
    {
        public string question;
        public string[] options;
        public int answer;
    }

    [System.Serializable]
    public class QuizBackendList
    {
        public QuizBackend[] items;
    }
    void Update()
    {
        if (juegoActivo)
        {
            ActualizarTemporizador();
        }
    }

    void CargarPregunta() {
        if (preguntaActual < bancoDePreguntas.Length) {
            // Restablecer feedback visual de la pregunta anterior
            RestablecerBotones();

            // Enviar datos a las etiquetas
            textoPregunta.text = bancoDePreguntas[preguntaActual].enunciado;
            for (int i = 0; i < textosOpciones.Length; i++)
            {
                if (i < bancoDePreguntas[preguntaActual].opciones.Length)
                    textosOpciones[i].text = bancoDePreguntas[preguntaActual].opciones[i];
                else
                    textosOpciones[i].text = "";
            }

            // Reiniciar tiempo
            tiempoRestante = tiempoPorPregunta;
            juegoActivo = true;
        } else {
            TerminarJuego();
        }
    }

    void RestablecerBotones() {
        for (int i = 0; i < imagenesBotones.Length; i++) {
            imagenesBotones[i].color = colorOriginalBotones; // Vuelven a su color base
            iconosResultado[i].SetActive(false); // Ocultamos los iconos
        }
    }

    void ActualizarTemporizador() {
    tiempoRestante -= Time.deltaTime;
    
    // ACTUALIZACIÓN PARA EL CÍRCULO 360
    if (barraTiempo != null && barraTiempo2 != null) {
        // Dividimos el tiempo restante entre el tiempo total para obtener un valor entre 0 y 1
        barraTiempo.fillAmount = tiempoRestante / tiempoPorPregunta;
        barraTiempo2.fillAmount = tiempoRestante / tiempoPorPregunta;
    }

    // El resto de tu código de texto
    if (textoTimer != null && textoTimer2 != null) {
        textoTimer.text = Mathf.Ceil(tiempoRestante).ToString();
        textoTimer2.text = Mathf.Ceil(tiempoRestante).ToString();
    }

    if (tiempoRestante <= 0) {
        juegoActivo = false;
        SiguientePregunta();
    }
}

public void Responder(int indiceSeleccionado) {
        if (!juegoActivo) return;
        juegoActivo = false; // Detener cronómetro inmediatamente

        int indiceCorrecto = bancoDePreguntas[preguntaActual].indiceCorrecto;
        Image imagenIcono = iconosResultado[indiceSeleccionado].GetComponent<Image>();

        if (indiceSeleccionado == indiceCorrecto) {
            SumarPuntos(30);
            Debug.Log("¡Correcto!");
            // Feedback Visual Acierto
            imagenesBotones[indiceSeleccionado].color = colorCorrecto;
            imagenIcono.sprite = spritePalomita;
            fuenteAudio.PlayOneShot(sonidoCorrecto);
        } else {

            BajarSeguridad(0.05f);
            erroresAcumulados++; 

        if (erroresAcumulados >= 2) {
            RestarVida();          
            erroresAcumulados = 0; 
        }

            Debug.Log("Incorrecto");
            // Feedback Visual Fallo
            imagenesBotones[indiceSeleccionado].color = colorIncorrecto;
            imagenIcono.sprite = spriteTache;
            fuenteAudio.PlayOneShot(sonidoIncorrecto);
        }

        // Activamos el icono del botón presionado (ya sea check o tache)
        iconosResultado[indiceSeleccionado].SetActive(true);

        if (vidasRestantes > 0) {
        Invoke("SiguientePregunta", 2f);
    }
    }

    void SumarPuntos(int cantidad) {
    puntajeActual += cantidad; 
    if (textoPuntaje != null) {
        // "D6" rellena con ceros a la izquierda hasta completar 6 dígitos
        textoPuntaje.text = puntajeActual.ToString(); 
    }
    }

    void RestarVida() {
    if (vidasRestantes > 0) {
        vidasRestantes--;
        // Desactivamos la imagen de la vida correspondiente
        // Si tienes 3 vidas, el índice es 2, luego 1, luego 0
        iconosVidas[vidasRestantes].SetActive(false); 
    }

    if (vidasRestantes <= 0) {
        Debug.Log("Game Over por falta de vidas");
        TerminarJuego();
    }
}

void BajarSeguridad(float porcentaje) {
    nivelSeguridad -= porcentaje;
    
    nivelSeguridad = Mathf.Clamp(nivelSeguridad, 0f, 1f);

    if (barraSeguridad != null) {
        barraSeguridad.fillAmount = nivelSeguridad;
    }

    if (nivelSeguridad <= 0) {
        Debug.Log("Seguridad agotada");
        TerminarJuego();
    }
}

    void SiguientePregunta() {
        preguntaActual++;
        CargarPregunta();
    }

    void TerminarJuego() {

        juegoActivo = false;
        StartCoroutine(APIManager.Instance.SendScore(puntajeActual));
        GuardarProgresoAcumulado();

        if (vidasRestantes > 0 && nivelSeguridad > 0 && puntajeActual >= 40) {
            MostrarPantallaVictoria();
            fuenteAudio.PlayOneShot(sonidoGameWin);
        } 
        else {
            MostrarPantallaGameOver();
            fuenteAudio.PlayOneShot(sonidoGameOver);
        }
    }


    void MostrarPantallaVictoria() {
    // Activamos el panel
    if (canvasGanador != null) {
        canvasGanador.SetActive(true);

        // Llenamos los datos finales
        if (textoPuntosFinal != null) 
            textoPuntosFinal.text = puntajeActual.ToString();

        if (textoVidasFinal != null) 
            textoVidasFinal.text = vidasRestantes.ToString();

        if (textoSeguridadFinal != null) {
            // Multiplicamos por 100 para que se vea como "95%" en lugar de "0.95"
            float porcentajeSeguridad = nivelSeguridad * 100f;
            textoSeguridadFinal.text = porcentajeSeguridad.ToString();
        }
    }
}

    void MostrarPantallaGameOver() {
        if (canvasGameOver != null) {
            canvasGameOver.SetActive(true);
            Debug.Log("Derrota: No se cumplieron los requisitos.");
        }
    }

    void GuardarProgresoAcumulado() {
    // 1. Obtener datos viejos
    int puntosAnteriores = PlayerPrefs.GetInt("PuntajeTotal", 0);
    int totalPartidas = PlayerPrefs.GetInt("PartidasJugadas", 0);
    float sumaSeguridades = PlayerPrefs.GetFloat("SumaSeguridades", 0f);int recordVidasAnterior = PlayerPrefs.GetInt("RecordVidas", 0);

    // 2. Actualizar valores
    int nuevoTotalPuntos = puntosAnteriores + puntajeActual;
    totalPartidas += 1; // Sumamos esta partida
    sumaSeguridades += (nivelSeguridad * 100f); // Sumamos el porcentaje de esta partida

    // 3. Guardar
    PlayerPrefs.SetInt("PuntajeTotal", nuevoTotalPuntos);
    PlayerPrefs.SetInt("PartidasJugadas", totalPartidas);
    PlayerPrefs.SetFloat("SumaSeguridades", sumaSeguridades);

    if (vidasRestantes > recordVidasAnterior) {
        PlayerPrefs.SetInt("RecordVidas", vidasRestantes);
    }
    
    PlayerPrefs.Save();
}

[System.Serializable]
public class PreguntaBackend
{
    public int id;
    public string question;
    public string[] options;
    public int correct_index;
}

[System.Serializable]
public class PreguntaBackendList
{
    public PreguntaBackend[] items;
}
}