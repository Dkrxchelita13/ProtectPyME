using System;

public static class MinigameLessonState
{
    public static bool IsPending = false;
    public static string MinigameKey = "";
    public static string TargetScene = "";
    public static string Topic = "";
    public static string Risk = "";
    public static MinigameSessionResponse Session;

    public static string SessionId
    {
        get
        {
            return Session != null ? Session.session_id : "";
        }
    }

    public static MinigameLessonResponse Lesson
    {
        get
        {
            return GetLesson();
        }
    }

    public static MinigameSessionItem[] Items
    {
        get
        {
            return GetItems();
        }
    }

    public static bool HasValidSession
    {
        get
        {
            return IsPending
                && Session != null
                && !string.IsNullOrEmpty(Session.session_id)
                && Session.lesson != null
                && Session.items != null
                && Session.items.Length > 0
                && ValuesMatch(Session.topic, Topic)
                && ValuesMatch(Session.risk, Risk)
                && ValuesMatch(Session.minigame, MinigameKey);
        }
    }

    public static void Prepare(
        string minigameKey,
        string targetScene,
        string topic,
        string risk
    )
    {
        if (string.IsNullOrEmpty(minigameKey))
        {
            throw new ArgumentException("minigameKey no puede estar vacio.");
        }

        if (string.IsNullOrEmpty(targetScene))
        {
            throw new ArgumentException("targetScene no puede estar vacio.");
        }

        if (string.IsNullOrEmpty(topic))
        {
            throw new ArgumentException("topic no puede estar vacio.");
        }

        if (string.IsNullOrEmpty(risk))
        {
            throw new ArgumentException("risk no puede estar vacio.");
        }

        string normalizedMinigameKey = minigameKey.Trim().ToLower();

        if (!IsValidMinigameKey(normalizedMinigameKey))
        {
            throw new ArgumentException(
                "minigameKey debe ser quiz, wordsearch o crossword."
            );
        }

        MinigameKey = normalizedMinigameKey;
        TargetScene = targetScene.Trim();
        Topic = topic.Trim();
        Risk = risk.Trim();
        IsPending = true;
    }

    public static void PrepareWithSession(
        string minigameKey,
        string targetScene,
        string topic,
        string risk,
        MinigameSessionResponse session
    )
    {
        Prepare(minigameKey, targetScene, topic, risk);
        Session = session;

        if (!HasValidSession)
        {
            ClearSession();
            throw new ArgumentException("La sesion de minijuego no es valida.");
        }
    }

    public static void Clear()
    {
        ClearSession();
    }

    public static MinigameLessonResponse GetLesson()
    {
        return HasValidSession ? Session.lesson : null;
    }

    public static MinigameSessionItem[] GetItems()
    {
        return HasValidSession ? Session.items : null;
    }

    public static void ClearSession()
    {
        IsPending = false;
        MinigameKey = "";
        TargetScene = "";
        Topic = "";
        Risk = "";
        Session = null;
    }

    private static bool IsValidMinigameKey(string minigameKey)
    {
        return minigameKey == "quiz"
            || minigameKey == "wordsearch"
            || minigameKey == "crossword";
    }

    private static bool ValuesMatch(string left, string right)
    {
        return string.Equals(
            (left ?? "").Trim(),
            (right ?? "").Trim(),
            StringComparison.OrdinalIgnoreCase
        );
    }
}
