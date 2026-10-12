using System;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class UserProgressCacheTests
{
    private static readonly int[] TestUsers = { 8, 9, 10, 12 };

    [SetUp]
    public void SetUp()
    {
        Cleanup();
    }

    [TearDown]
    public void TearDown()
    {
        Cleanup();
    }

    [Test]
    public void UserBCacheDoesNotReadUserAProgress()
    {
        UserProgressCache.SetActiveUserId(8);
        UserProgressCache.SetInitialStage(4);

        UserProgressCache.SetActiveUserId(9);
        UserProgressCache.SetInitialStage(1);

        Assert.AreEqual(1, UserProgressCache.GetInitialStage());
    }

    [Test]
    public void SwitchingAtoBtoAShowsEachUsersBackendProgressInUi()
    {
        SyncBackendProgress(8, 4, 1, true);

        using (ProgressControllerHarness harness = new ProgressControllerHarness())
        {
            harness.Controller.ActualizarProgreso();
            Assert.IsTrue(harness.Escenario2.interactable);
            Assert.IsTrue(harness.Escenario3.interactable);
            Assert.IsTrue(harness.Minijuegos.interactable);

            UserProgressCache.ClearActiveUserState();
            SyncBackendProgress(9, 1, 1, false);
            harness.Controller.ActualizarProgreso();
            Assert.IsFalse(harness.Escenario2.interactable);
            Assert.IsFalse(harness.Escenario3.interactable);
            Assert.IsFalse(harness.Minijuegos.interactable);

            UserProgressCache.ClearActiveUserState();
            SyncBackendProgress(8, 4, 1, true);
            harness.Controller.ActualizarProgreso();
            Assert.IsTrue(harness.Escenario2.interactable);
            Assert.IsTrue(harness.Escenario3.interactable);
            Assert.IsTrue(harness.Minijuegos.interactable);
        }
    }

    [Test]
    public void BackendSuccessCanLowerCorruptLocalCache()
    {
        UserProgressCache.SetActiveUserId(8);
        UserProgressCache.SetInitialStage(4);

        SyncBackendProgress(8, 2, 1, false);

        Assert.AreEqual(2, UserProgressCache.GetInitialStage());
    }

    [Test]
    public void BackendFailureUsesOnlySameUserCache()
    {
        UserProgressCache.SetActiveUserId(8);
        UserProgressCache.SetInitialStage(2);

        UserProgressCache.SetActiveUserId(8);

        Assert.AreEqual(2, UserProgressCache.GetInitialStage());
    }

    [Test]
    public void BackendFailureForUserWithoutCacheDoesNotInheritOtherUser()
    {
        UserProgressCache.SetActiveUserId(8);
        UserProgressCache.SetInitialStage(4);

        UserProgressCache.SetActiveUserId(9);

        Assert.AreEqual(1, UserProgressCache.GetInitialStage());
    }

    [Test]
    public void NewUserBackendDefaultsCreateOnlyThatUsersCache()
    {
        UserProgressCache.SetActiveUserId(8);
        UserProgressCache.SetInitialStage(4);
        UserProgressCache.SetActiveUserId(9);
        UserProgressCache.SetInitialStage(2);

        SyncBackendProgress(10, 1, 1, false);

        Assert.AreEqual(1, UserProgressCache.GetInitialStage());

        UserProgressCache.SetActiveUserId(8);
        Assert.AreEqual(4, UserProgressCache.GetInitialStage());

        UserProgressCache.SetActiveUserId(9);
        Assert.AreEqual(2, UserProgressCache.GetInitialStage());
    }

    [Test]
    public void DecisionRefreshUpdatesOnlyAuthenticatedUsersCache()
    {
        SyncBackendProgress(8, 1, 1, false);
        SyncBackendProgress(9, 1, 1, false);

        SyncBackendProgress(8, 2, 1, false);

        Assert.AreEqual(2, UserProgressCache.GetInitialStage());

        UserProgressCache.SetActiveUserId(9);
        Assert.AreEqual(1, UserProgressCache.GetInitialStage());
    }

    [Test]
    public void LogoutClearsActiveIdentityButKeepsUserCache()
    {
        UserProgressCache.SetActiveUserId(8);
        UserProgressCache.SetInitialStage(4);

        UserProgressCache.ClearActiveUserState();

        Assert.IsFalse(UserProgressCache.HasActiveUser);
        Assert.AreEqual(1, UserProgressCache.GetInitialStage());

        UserProgressCache.SetActiveUserId(8);
        Assert.AreEqual(4, UserProgressCache.GetInitialStage());
    }

    [Test]
    public void LegacyGlobalProgressKeysAreIgnoredForNewUser()
    {
        PlayerPrefs.SetInt("NivelAlcanzado", 4);
        PlayerPrefs.SetInt("ProgresoIntermedio", 4);
        PlayerPrefs.Save();

        SyncBackendProgress(12, 1, 1, false);

        Assert.AreEqual(1, UserProgressCache.GetInitialStage());
        Assert.AreEqual(1, UserProgressCache.GetIntermediateStage());
    }

    [Test]
    public void JwtSubSelectsStableUserId()
    {
        string token = CreateJwtWithSub(8);

        Assert.IsTrue(UserProgressCache.SetActiveUserFromToken(token));
        Assert.IsTrue(UserProgressCache.HasActiveUser);
        Assert.AreEqual(8, UserProgressCache.ActiveUserId);
        Assert.AreEqual(
            "user_8_NivelAlcanzado",
            UserProgressCache.GetKey(UserProgressCache.InitialStageKey)
        );
    }

    private static void SyncBackendProgress(
        int userId,
        int initialStage,
        int intermediateStage,
        bool minigamesUnlocked
    )
    {
        UserProgressCache.SyncFromBackend(
            new UserProgressResponse
            {
                user_id = userId,
                highest_initial_stage_unlocked = initialStage,
                highest_intermediate_stage_unlocked = intermediateStage,
                minigames_unlocked = minigamesUnlocked,
                updated_at = "test"
            }
        );
    }

    private static string CreateJwtWithSub(int userId)
    {
        string header = Base64UrlEncode("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
        string payload = Base64UrlEncode("{\"sub\":\"" + userId + "\"}");
        return header + "." + payload + ".signature";
    }

    private static string Base64UrlEncode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static void Cleanup()
    {
        foreach (int userId in TestUsers)
        {
            PlayerPrefs.DeleteKey(
                UserProgressCache.GetKeyForUser(
                    userId,
                    UserProgressCache.InitialStageKey
                )
            );
            PlayerPrefs.DeleteKey(
                UserProgressCache.GetKeyForUser(
                    userId,
                    UserProgressCache.IntermediateStageKey
                )
            );
            PlayerPrefs.DeleteKey(
                UserProgressCache.GetKeyForUser(
                    userId,
                    UserProgressCache.MinigamesUnlockedKey
                )
            );
        }

        PlayerPrefs.DeleteKey("NivelAlcanzado");
        PlayerPrefs.DeleteKey("ProgresoIntermedio");
        PlayerPrefs.DeleteKey("MinigamesUnlocked");
        UserProgressCache.ClearActiveUserState();
    }

    private sealed class ProgressControllerHarness : IDisposable
    {
        public readonly GameObject Root;
        public readonly BotonesProgresoController Controller;
        public readonly Button Escenario2;
        public readonly Button Escenario3;
        public readonly Button Minijuegos;

        public ProgressControllerHarness()
        {
            Root = new GameObject("ProgressHarness");
            Controller = Root.AddComponent<BotonesProgresoController>();
            Controller.botonEscenario1 = CreateButton("Escenario1");
            Escenario2 = CreateButton("Escenario2");
            Escenario3 = CreateButton("Escenario3");
            Minijuegos = CreateButton("Minijuegos");
            Controller.botonEscenario2 = Escenario2;
            Controller.botonEscenario3 = Escenario3;
            Controller.botonMinijuegos = Minijuegos;
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Root);
        }

        private Button CreateButton(string name)
        {
            GameObject buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );
            buttonObject.transform.SetParent(Root.transform);
            return buttonObject.GetComponent<Button>();
        }
    }
}
