public static class AIState
{
    public static string RecommendedTraining = "phishing";
    public static int RecommendedScenario = 1;
    public static string RawRecommendedTraining = "";
    public static bool RecommendationLoaded = false;

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

    public static string NormalizePlayableTopic(string topic)
    {
        if (string.IsNullOrEmpty(topic))
        {
            return "";
        }

        string normalized = topic.Trim().ToLowerInvariant();

        switch (normalized)
        {
            case "phishing":
            case "passwords":
            case "malware":
            case "wifi":
                return normalized;

            case "password":
            case "contraseñas":
            case "contrasenas":
                return "passwords";

            case "malicious_software":
                return "malware";

            case "network":
            case "redes_wifi":
                return "wifi";

            default:
                return "";
        }
    }

    public static string ResolvePlayableTopic(
        string topic,
        int recommendedScenario,
        string surveyPrimaryWeakness = ""
    )
    {
        string playableTopic = NormalizePlayableTopic(topic);

        if (!string.IsNullOrEmpty(playableTopic))
        {
            return playableTopic;
        }

        string scenarioTopic = GetTopicForScenario(recommendedScenario);

        if (!string.IsNullOrEmpty(scenarioTopic))
        {
            return scenarioTopic;
        }

        string surveyTopic = NormalizePlayableTopic(surveyPrimaryWeakness);

        if (!string.IsNullOrEmpty(surveyTopic))
        {
            return surveyTopic;
        }

        return "phishing";
    }

    public static string GetTopicForScenario(int scenario)
    {
        switch (scenario)
        {
            case 1:
                return "phishing";

            case 2:
                return "passwords";

            case 3:
                return "malware";

            case 5:
                return "phishing";

            case 6:
                return "passwords";

            case 7:
                return "wifi";

            default:
                return "";
        }
    }

    public static bool IsPlayableTopic(string topic)
    {
        return !string.IsNullOrEmpty(NormalizePlayableTopic(topic));
    }

    public static bool IsValidPracticeScenario(int scenario)
    {
        return scenario == 1
            || scenario == 2
            || scenario == 3
            || scenario == 5
            || scenario == 6
            || scenario == 7;
    }
}
