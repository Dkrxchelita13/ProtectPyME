using System;

[Serializable]
public class PilotConsentAcceptRequest
{
    public bool accepted = true;
}

[Serializable]
public class PilotConsentResponse
{
    public string consent_version;
    public bool accepted;
    public string participant_code;
    public string accepted_at;
    public string revoked_at;
}

[Serializable]
public class PilotAssessmentStartRequest
{
    public string phase;

    public PilotAssessmentStartRequest(string phase)
    {
        this.phase = phase;
    }
}

[Serializable]
public class PilotAssessmentQuestion
{
    public string question_id;
    public string prompt;
    public string[] options;
}

[Serializable]
public class PilotAssessmentStartResponse
{
    public string assessment_id;
    public string phase;
    public string instrument_version;
    public string status;
    public string[] answered_question_ids;
    public PilotAssessmentQuestion[] questions;
}

[Serializable]
public class PilotAssessmentAnswerRequest
{
    public string question_id;
    public string selected_option;
    public int response_time_ms;

    public PilotAssessmentAnswerRequest(
        string questionId,
        string selectedOption,
        int responseTimeMs
    )
    {
        question_id = questionId;
        selected_option = selectedOption;
        response_time_ms = responseTimeMs;
    }
}

[Serializable]
public class PilotAssessmentAnswerResponse
{
    public string assessment_id;
    public string question_id;
    public bool recorded;
    public int answered_count;
    public int total_questions;
}

[Serializable]
public class PilotAssessmentStatusItem
{
    public string assessment_id;
    public string phase;
    public string status;
    public string started_at;
    public string completed_at;
    public int answered_count;
    public string[] answered_question_ids;
}

[Serializable]
public class PilotInterventionProgress
{
    public int distinct_scenarios_completed;
    public int required_distinct_scenarios;
    public int completed_minigame_sessions;
    public int required_minigame_sessions;
}

[Serializable]
public class PilotAssessmentStatusResponse
{
    public string instrument_version;
    public bool consent_active;
    public PilotAssessmentStatusItem pre;
    public PilotAssessmentStatusItem post;
    public string next_phase;
    public bool post_eligible;
    public PilotInterventionProgress intervention_progress;
}

[Serializable]
public class PilotAssessmentTopicScores
{
    public float phishing;
    public float passwords;
    public float malware;
    public float wifi;
}

[Serializable]
public class PilotAssessmentResultItem
{
    public string assessment_id;
    public string phase;
    public string instrument_version;
    public string status;
    public string completed_at;
    public float total_score;
    public PilotAssessmentTopicScores topic_scores;
}

[Serializable]
public class PilotAssessmentGainResponse
{
    public float total;
    public float phishing;
    public float passwords;
    public float malware;
    public float wifi;
}

[Serializable]
public class PilotAssessmentResultsResponse
{
    public string instrument_version;
    public PilotAssessmentResultItem pre;
    public PilotAssessmentResultItem post;
    public PilotAssessmentGainResponse gain;
}
