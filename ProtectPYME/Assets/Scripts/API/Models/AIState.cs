public static class AIState
{
    public static string RecommendedTraining = "phishing";

    // ALTO = usuario vulnerable -> contenido basico
    // MEDIO = contenido intermedio
    // BAJO = usuario solido -> contenido avanzado
    public static string RiskLevel = "alto";

    public static bool SurveyCompleted = false;
    public static bool SurveyResultPending = false;
    public static string SurveyInitialRisk = "";
    public static string SurveyPrimaryWeakness = "";
    public static int SurveyTotalRiskScore = 0;
}
