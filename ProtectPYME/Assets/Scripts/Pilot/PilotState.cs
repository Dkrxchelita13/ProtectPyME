using System.Collections.Generic;

public class PilotState
{
    public string CurrentAssessmentId { get; private set; }
    public string Phase { get; private set; }
    public PilotAssessmentQuestion[] Questions { get; private set; }
    public HashSet<string> AnsweredQuestionIds { get; private set; }
    public int CurrentQuestionIndex { get; private set; }
    public string CurrentSelection { get; set; }

    public int TotalQuestions
    {
        get { return Questions == null ? 0 : Questions.Length; }
    }

    public int AnsweredCount
    {
        get { return AnsweredQuestionIds == null ? 0 : AnsweredQuestionIds.Count; }
    }

    public void Load(PilotAssessmentStartResponse response)
    {
        if (response == null)
        {
            CurrentAssessmentId = "";
            Phase = "";
            Questions = new PilotAssessmentQuestion[0];
            AnsweredQuestionIds = new HashSet<string>();
            CurrentSelection = "";
            CurrentQuestionIndex = -1;
            return;
        }

        CurrentAssessmentId = response.assessment_id;
        Phase = response.phase;
        Questions = response.questions ?? new PilotAssessmentQuestion[0];
        AnsweredQuestionIds = BuildAnsweredSet(response.answered_question_ids);
        CurrentSelection = "";
        CurrentQuestionIndex = FindFirstPendingQuestionIndex();
    }

    public PilotAssessmentQuestion GetCurrentQuestion()
    {
        if (Questions == null ||
            CurrentQuestionIndex < 0 ||
            CurrentQuestionIndex >= Questions.Length)
        {
            return null;
        }

        return Questions[CurrentQuestionIndex];
    }

    public void MarkAnswered(string questionId)
    {
        if (string.IsNullOrEmpty(questionId))
        {
            return;
        }

        AnsweredQuestionIds.Add(questionId);
        CurrentSelection = "";
        CurrentQuestionIndex = FindFirstPendingQuestionIndex();
    }

    public bool IsComplete()
    {
        return Questions != null && FindFirstPendingQuestionIndex() < 0;
    }

    private HashSet<string> BuildAnsweredSet(string[] answeredQuestionIds)
    {
        HashSet<string> answered = new HashSet<string>();

        if (answeredQuestionIds == null)
        {
            return answered;
        }

        for (int i = 0; i < answeredQuestionIds.Length; i++)
        {
            if (!string.IsNullOrEmpty(answeredQuestionIds[i]))
            {
                answered.Add(answeredQuestionIds[i]);
            }
        }

        return answered;
    }

    private int FindFirstPendingQuestionIndex()
    {
        if (Questions == null)
        {
            return -1;
        }

        for (int i = 0; i < Questions.Length; i++)
        {
            PilotAssessmentQuestion question = Questions[i];

            if (question != null &&
                !string.IsNullOrEmpty(question.question_id) &&
                !AnsweredQuestionIds.Contains(question.question_id))
            {
                return i;
            }
        }

        return -1;
    }
}
