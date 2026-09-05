using NUnit.Framework;
using System.IO;
using UnityEngine;

public class MotivationalMessageProviderTests
{
    [Test]
    public void PositiveReinforcementReturnsPositiveCatalogMessage()
    {
        string message =
            MotivationalMessageProvider.GetMessage(
                MotivationContext.PositiveReinforcement
            );

        Assert.IsFalse(string.IsNullOrEmpty(message));
        Assert.IsTrue(
            MotivationalMessageProvider.ContainsMessage(
                MotivationContext.PositiveReinforcement,
                message
            )
        );
    }

    [Test]
    public void NeedsReinforcementReturnsNeedsCatalogMessage()
    {
        string message =
            MotivationalMessageProvider.GetMessage(
                MotivationContext.NeedsReinforcement
            );

        Assert.IsFalse(string.IsNullOrEmpty(message));
        Assert.IsTrue(
            MotivationalMessageProvider.ContainsMessage(
                MotivationContext.NeedsReinforcement,
                message
            )
        );
    }

    [Test]
    public void ConsecutiveCallsDoNotRepeatWhenContextHasMultipleMessages()
    {
        if (MotivationalMessageProvider.GetMessageCount(
                MotivationContext.PositiveReinforcement
            ) <= 1)
        {
            Assert.Pass();
        }

        string first =
            MotivationalMessageProvider.GetMessage(
                MotivationContext.PositiveReinforcement
            );
        string second =
            MotivationalMessageProvider.GetMessage(
                MotivationContext.PositiveReinforcement
            );

        Assert.AreNotEqual(first, second);
    }

    [Test]
    public void PositiveMessageNeverComesFromNeedsCatalog()
    {
        string message =
            MotivationalMessageProvider.GetMessage(
                MotivationContext.PositiveReinforcement
            );

        Assert.IsFalse(
            MotivationalMessageProvider.ContainsMessage(
                MotivationContext.NeedsReinforcement,
                message
            )
        );
    }

    [Test]
    public void NeedsMessageNeverComesFromPositiveCatalog()
    {
        string message =
            MotivationalMessageProvider.GetMessage(
                MotivationContext.NeedsReinforcement
            );

        Assert.IsFalse(
            MotivationalMessageProvider.ContainsMessage(
                MotivationContext.PositiveReinforcement,
                message
            )
        );
    }

    [Test]
    public void ProviderIsNotMonoBehaviour()
    {
        Assert.IsFalse(
            typeof(MotivationalMessageProvider).IsSubclassOf(
                typeof(MonoBehaviour)
            )
        );
    }

    [Test]
    public void ProviderDoesNotUsePlayerPrefsOrBackend()
    {
        string providerPath = Path.Combine(
            Application.dataPath,
            "Scripts",
            "UI",
            "MotivationalMessageProvider.cs"
        );
        string source = File.ReadAllText(providerPath);

        Assert.IsFalse(source.Contains("PlayerPrefs"));
        Assert.IsFalse(source.Contains("UnityWebRequest"));
        Assert.IsFalse(source.Contains("APIManager"));
        Assert.IsFalse(source.Contains("http"));
    }
}
