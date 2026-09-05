using NUnit.Framework;
using System.Reflection;
using TMPro;
using UnityEngine;

public class MinigameFeedbackPresenterMotivationTests
{
    [Test]
    public void WaitingStateKeepsMotivationalTextHidden()
    {
        GameObject finalPanel = CreateFinalPanel();

        try
        {
            MinigameFeedbackPresenter presenter =
                MinigameFeedbackPresenter.AttachOrGet(finalPanel.transform);

            presenter.BeginWaitingForFeedback("session-test");

            TextMeshProUGUI motivationalText =
                FindText(presenter.transform, "MotivationalText");

            Assert.IsNotNull(motivationalText);
            Assert.IsFalse(motivationalText.gameObject.activeSelf);
            Assert.AreEqual("", motivationalText.text);
        }
        finally
        {
            Object.DestroyImmediate(finalPanel);
        }
    }

    [Test]
    public void RealFeedbackShowsMotivationalText()
    {
        GameObject finalPanel = CreateFinalPanel();

        try
        {
            MinigameFeedbackPresenter presenter =
                MinigameFeedbackPresenter.AttachOrGet(finalPanel.transform);

            presenter.BeginWaitingForFeedback("session-test");
            RenderFeedback(presenter, CreateFeedback("session-test", "excelente"));

            TextMeshProUGUI motivationalText =
                FindText(presenter.transform, "MotivationalText");

            Assert.IsNotNull(motivationalText);
            Assert.IsTrue(motivationalText.gameObject.activeSelf);
            Assert.IsFalse(string.IsNullOrEmpty(motivationalText.text));
            Assert.IsTrue(
                MotivationalMessageProvider.ContainsMessage(
                    MotivationContext.PositiveReinforcement,
                    motivationalText.text
                )
            );
        }
        finally
        {
            Object.DestroyImmediate(finalPanel);
        }
    }

    [Test]
    public void TimeoutStateKeepsMotivationalTextHidden()
    {
        GameObject finalPanel = CreateFinalPanel();

        try
        {
            MinigameFeedbackPresenter presenter =
                MinigameFeedbackPresenter.AttachOrGet(finalPanel.transform);

            presenter.BeginWaitingForFeedback("session-test");
            ShowTimeoutState(presenter, "session-test");

            TextMeshProUGUI motivationalText =
                FindText(presenter.transform, "MotivationalText");

            Assert.IsNotNull(motivationalText);
            Assert.IsFalse(motivationalText.gameObject.activeSelf);
            Assert.AreEqual("", motivationalText.text);
        }
        finally
        {
            Object.DestroyImmediate(finalPanel);
        }
    }

    private static GameObject CreateFinalPanel()
    {
        GameObject finalPanel =
            new GameObject("FinalPanel", typeof(RectTransform), typeof(Canvas));
        RectTransform rect = finalPanel.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1200f, 800f);
        return finalPanel;
    }

    private static MinigameFeedbackResponse CreateFeedback(
        string sessionId,
        string performanceLevel
    )
    {
        return new MinigameFeedbackResponse
        {
            session_id = sessionId,
            topic = "passwords",
            risk = "medio",
            minigame = "quiz",
            accuracy = 90f,
            total_attempts = 3,
            correct_attempts = 3,
            incorrect_attempts = 0,
            performance_level = performanceLevel,
            title = "Excelente trabajo",
            message = "Mostraste un desempeño sólido.",
            next_step = "Continúa con la siguiente actividad.",
            strengths = new ConceptFeedbackResponse[0],
            reinforcement = new ConceptFeedbackResponse[0],
            recommended_concept_ids = new string[0],
            recommended_minigame = "wordsearch"
        };
    }

    private static void RenderFeedback(
        MinigameFeedbackPresenter presenter,
        MinigameFeedbackResponse feedback
    )
    {
        typeof(MinigameFeedbackPresenter)
            .GetMethod("RenderFeedback", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(presenter, new object[] { feedback });
    }

    private static void ShowTimeoutState(
        MinigameFeedbackPresenter presenter,
        string sessionId
    )
    {
        typeof(MinigameFeedbackPresenter)
            .GetMethod("ShowTimeoutState", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(presenter, new object[] { sessionId });
    }

    private static TextMeshProUGUI FindText(Transform parent, string name)
    {
        foreach (TextMeshProUGUI text in parent.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.gameObject.name == name)
            {
                return text;
            }
        }

        return null;
    }
}
