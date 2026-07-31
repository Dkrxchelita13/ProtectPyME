public static class AIState
{
    public static string RecommendedTraining = "phishing";
    public static int RecommendedScenario = 1;

    // ALTO = usuario vulnerable -> contenido basico
    // MEDIO = contenido intermedio
    // BAJO = usuario solido -> contenido avanzado
    public static string RiskLevel = "alto";

    public static string RiskSource = "";
    public static int BehavioralDecisions = 0;
    public static int MinBehavioralDecisions = 3;
    public static bool SufficientBehavioralData = false;

    public static bool SurveyCompleted = false;
    public static bool SurveyResultPending = false;
    public static string SurveyInitialRisk = "";
    public static string SurveyPrimaryWeakness = "";
    public static int SurveyTotalRiskScore = 0;
}
