using System;
using System.Collections.Generic;

[Serializable]
public class SurveyStatusResponse
{
    public bool has_submitted;
    public string survey_version;
    public string submitted_at;
    public string primary_weakness;
    public string initial_risk;
}

[Serializable]
public class SurveyAnswerSubmit
{
    public string question_id;
    public string category;
    public string selected_option;
}

[Serializable]
public class SurveySubmitRequest
{
    public string survey_version;
    public List<SurveyAnswerSubmit> answers;
}

[Serializable]
public class SurveyCategoryScore
{
    public int safe_score;
    public int max_score;
    public int risk_score;
}

[Serializable]
public class SurveyCategoryScores
{
    public SurveyCategoryScore phishing;
    public SurveyCategoryScore passwords;
    public SurveyCategoryScore malware;
}

[Serializable]
public class SurveySubmitResponse
{
    public bool submitted;
    public string survey_version;
    public string primary_weakness;
    public string initial_risk;
    public int total_risk_score;
    public SurveyCategoryScores category_scores;
}
