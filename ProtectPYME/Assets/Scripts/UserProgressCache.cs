using System;
using System.Text;
using UnityEngine;

public static class UserProgressCache
{
    public const string InitialStageKey = "NivelAlcanzado";
    public const string IntermediateStageKey = "ProgresoIntermedio";
    public const string MinigamesUnlockedKey = "MinigamesUnlocked";

    private const string ActiveUserIdKey = "ProgressCacheActiveUserId";
    private const string ScopedPrefix = "user_";

    private static bool loadedActiveUser;
    private static bool hasActiveUser;
    private static int activeUserId;

    public static bool HasActiveUser
    {
        get
        {
            EnsureActiveUserLoaded();
            return hasActiveUser;
        }
    }

    public static int ActiveUserId
    {
        get
        {
            EnsureActiveUserLoaded();
            return activeUserId;
        }
    }

    public static void SetActiveUserId(int userId)
    {
        if (userId <= 0)
        {
            ClearActiveUserState();
            return;
        }

        activeUserId = userId;
        hasActiveUser = true;
        loadedActiveUser = true;
        PlayerPrefs.SetInt(ActiveUserIdKey, userId);
        PlayerPrefs.Save();
    }

    public static bool SetActiveUserFromToken(string jwt)
    {
        if (TryExtractUserIdFromJwt(jwt, out int userId))
        {
            SetActiveUserId(userId);
            return true;
        }

        ClearActiveUserState();
        return false;
    }

    public static void ClearActiveUserState()
    {
        activeUserId = 0;
        hasActiveUser = false;
        loadedActiveUser = true;
        PlayerPrefs.DeleteKey(ActiveUserIdKey);
        PlayerPrefs.Save();
    }

    public static string GetKey(string keyName)
    {
        EnsureActiveUserLoaded();

        if (!hasActiveUser)
        {
            return "";
        }

        return GetKeyForUser(activeUserId, keyName);
    }

    public static string GetKeyForUser(int userId, string keyName)
    {
        return ScopedPrefix + userId + "_" + keyName;
    }

    public static int GetInitialStage()
    {
        return GetStage(InitialStageKey, 1);
    }

    public static int GetIntermediateStage()
    {
        return GetStage(IntermediateStageKey, 1);
    }

    public static bool GetMinigamesUnlocked()
    {
        string key = GetKey(MinigamesUnlockedKey);
        bool derivedFromStages =
            GetInitialStage() >= 4 || GetIntermediateStage() >= 4;

        if (string.IsNullOrEmpty(key))
        {
            return derivedFromStages;
        }

        return PlayerPrefs.GetInt(key, derivedFromStages ? 1 : 0) == 1;
    }

    public static bool SetInitialStage(int stage)
    {
        return SetStage(InitialStageKey, stage);
    }

    public static bool SetIntermediateStage(int stage)
    {
        return SetStage(IntermediateStageKey, stage);
    }

    public static bool SetInitialStageIfHigher(int stage)
    {
        return SetStageIfHigher(InitialStageKey, stage);
    }

    public static bool SetIntermediateStageIfHigher(int stage)
    {
        return SetStageIfHigher(IntermediateStageKey, stage);
    }

    public static bool SetMinigamesUnlocked(bool unlocked)
    {
        string key = GetKey(MinigamesUnlockedKey);

        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        PlayerPrefs.SetInt(key, unlocked ? 1 : 0);
        PlayerPrefs.Save();
        return true;
    }

    public static void SyncFromBackend(UserProgressResponse progress)
    {
        if (progress == null || progress.user_id <= 0)
        {
            return;
        }

        SetActiveUserId(progress.user_id);

        PlayerPrefs.SetInt(
            GetKey(InitialStageKey),
            ClampStage(progress.highest_initial_stage_unlocked)
        );
        PlayerPrefs.SetInt(
            GetKey(IntermediateStageKey),
            ClampStage(progress.highest_intermediate_stage_unlocked)
        );
        PlayerPrefs.SetInt(
            GetKey(MinigamesUnlockedKey),
            progress.minigames_unlocked ? 1 : 0
        );
        PlayerPrefs.Save();
    }

    public static void InitializeActiveUserProgress()
    {
        if (!HasActiveUser)
        {
            return;
        }

        PlayerPrefs.SetInt(GetKey(InitialStageKey), 1);
        PlayerPrefs.SetInt(GetKey(IntermediateStageKey), 1);
        PlayerPrefs.SetInt(GetKey(MinigamesUnlockedKey), 0);
        PlayerPrefs.Save();
    }

    public static void ClearActiveUserProgress()
    {
        if (!HasActiveUser)
        {
            return;
        }

        PlayerPrefs.DeleteKey(GetKey(InitialStageKey));
        PlayerPrefs.DeleteKey(GetKey(IntermediateStageKey));
        PlayerPrefs.DeleteKey(GetKey(MinigamesUnlockedKey));
        PlayerPrefs.Save();
    }

    public static bool TryExtractUserIdFromJwt(string jwt, out int userId)
    {
        userId = 0;

        if (string.IsNullOrEmpty(jwt))
        {
            return false;
        }

        string[] parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return false;
        }

        try
        {
            string payload = parts[1].Replace('-', '+').Replace('_', '/');
            int padding = payload.Length % 4;

            if (padding > 0)
            {
                payload = payload.PadRight(payload.Length + (4 - padding), '=');
            }

            string json = Encoding.UTF8.GetString(
                Convert.FromBase64String(payload)
            );
            return TryExtractJsonUserId(json, out userId);
        }
        catch (Exception)
        {
            userId = 0;
            return false;
        }
    }

    private static int GetStage(string keyName, int defaultValue)
    {
        string key = GetKey(keyName);

        if (string.IsNullOrEmpty(key))
        {
            return defaultValue;
        }

        return ClampStage(PlayerPrefs.GetInt(key, defaultValue));
    }

    private static bool SetStage(string keyName, int stage)
    {
        string key = GetKey(keyName);

        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        PlayerPrefs.SetInt(key, ClampStage(stage));
        PlayerPrefs.Save();
        return true;
    }

    private static bool SetStageIfHigher(string keyName, int stage)
    {
        string key = GetKey(keyName);

        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        int targetStage = ClampStage(stage);
        int currentStage = ClampStage(PlayerPrefs.GetInt(key, 1));

        if (targetStage <= currentStage)
        {
            return false;
        }

        PlayerPrefs.SetInt(key, targetStage);
        PlayerPrefs.Save();
        return true;
    }

    private static int ClampStage(int stage)
    {
        return Mathf.Clamp(stage, 1, 4);
    }

    private static void EnsureActiveUserLoaded()
    {
        if (loadedActiveUser)
        {
            return;
        }

        if (PlayerPrefs.HasKey(ActiveUserIdKey))
        {
            activeUserId = PlayerPrefs.GetInt(ActiveUserIdKey, 0);
            hasActiveUser = activeUserId > 0;
        }
        else
        {
            activeUserId = 0;
            hasActiveUser = false;
        }

        loadedActiveUser = true;
    }

    private static bool TryExtractJsonUserId(string json, out int userId)
    {
        userId = 0;

        if (string.IsNullOrEmpty(json))
        {
            return false;
        }

        const string fieldName = "\"sub\"";
        int fieldIndex = json.IndexOf(fieldName, StringComparison.Ordinal);

        if (fieldIndex < 0)
        {
            return false;
        }

        int colonIndex = json.IndexOf(':', fieldIndex + fieldName.Length);
        if (colonIndex < 0)
        {
            return false;
        }

        int valueIndex = colonIndex + 1;
        while (valueIndex < json.Length && char.IsWhiteSpace(json[valueIndex]))
        {
            valueIndex++;
        }

        if (valueIndex >= json.Length)
        {
            return false;
        }

        if (json[valueIndex] == '"')
        {
            int endQuoteIndex = json.IndexOf('"', valueIndex + 1);
            if (endQuoteIndex < 0)
            {
                return false;
            }

            string value = json.Substring(
                valueIndex + 1,
                endQuoteIndex - valueIndex - 1
            );
            return int.TryParse(value, out userId) && userId > 0;
        }

        int endIndex = valueIndex;
        while (endIndex < json.Length && char.IsDigit(json[endIndex]))
        {
            endIndex++;
        }

        if (endIndex == valueIndex)
        {
            return false;
        }

        string numericValue = json.Substring(valueIndex, endIndex - valueIndex);
        return int.TryParse(numericValue, out userId) && userId > 0;
    }
}
