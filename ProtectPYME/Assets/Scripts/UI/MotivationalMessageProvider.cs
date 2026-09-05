using System;

public enum MotivationContext
{
    PositiveReinforcement,
    NeedsReinforcement
}

public static class MotivationalMessageProvider
{
    private static readonly Random Random = new Random();

    private static readonly string[] PositiveReinforcementMessages =
    {
        "Buen trabajo. Sigue reforzando ese criterio.",
        "Sigue aplicando ese análisis en tus próximas decisiones.",
        "La práctica constante ayuda a consolidar lo aprendido.",
        "Vas fortaleciendo tu forma de analizar estas situaciones.",
        "Buen avance. Mantén ese criterio al seguir practicando."
    };

    private static readonly string[] NeedsReinforcementMessages =
    {
        "Cada intento ayuda a reconocer mejor estas situaciones.",
        "Revisar los errores también fortalece tu criterio.",
        "La práctica ayuda a identificar señales con mayor claridad.",
        "Este aspecto puede fortalecerse con un poco más de práctica.",
        "Tómalo como una oportunidad para reforzar lo aprendido."
    };

    private static int lastPositiveIndex = -1;
    private static int lastNeedsIndex = -1;

    public static string GetMessage(MotivationContext context)
    {
        string[] messages = GetMessages(context);
        int lastIndex = GetLastIndex(context);

        int index = GetNextIndex(messages.Length, lastIndex);
        SetLastIndex(context, index);

        return messages[index];
    }

    public static bool ContainsMessage(MotivationContext context, string message)
    {
        string[] messages = GetMessages(context);

        for (int i = 0; i < messages.Length; i++)
        {
            if (messages[i] == message)
            {
                return true;
            }
        }

        return false;
    }

    public static int GetMessageCount(MotivationContext context)
    {
        return GetMessages(context).Length;
    }

    private static int GetNextIndex(int messageCount, int lastIndex)
    {
        if (messageCount <= 1)
        {
            return 0;
        }

        int index = Random.Next(messageCount);

        if (index == lastIndex)
        {
            index = (index + 1) % messageCount;
        }

        return index;
    }

    private static string[] GetMessages(MotivationContext context)
    {
        return context == MotivationContext.PositiveReinforcement
            ? PositiveReinforcementMessages
            : NeedsReinforcementMessages;
    }

    private static int GetLastIndex(MotivationContext context)
    {
        return context == MotivationContext.PositiveReinforcement
            ? lastPositiveIndex
            : lastNeedsIndex;
    }

    private static void SetLastIndex(MotivationContext context, int index)
    {
        if (context == MotivationContext.PositiveReinforcement)
        {
            lastPositiveIndex = index;
            return;
        }

        lastNeedsIndex = index;
    }
}
