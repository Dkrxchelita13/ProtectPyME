using System;

public static class MinigameLessonState
{
    public static bool IsPending = false;
    public static string MinigameKey = "";
    public static string TargetScene = "";
    public static string Topic = "";
    public static string Risk = "";

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

    public static void Clear()
    {
        IsPending = false;
        MinigameKey = "";
        TargetScene = "";
        Topic = "";
        Risk = "";
    }

    private static bool IsValidMinigameKey(string minigameKey)
    {
        return minigameKey == "quiz"
            || minigameKey == "wordsearch"
            || minigameKey == "crossword";
    }
}
