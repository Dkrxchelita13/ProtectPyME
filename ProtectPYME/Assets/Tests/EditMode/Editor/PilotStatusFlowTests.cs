using NUnit.Framework;

public class PilotStatusFlowTests
{
    [Test]
    public void ResolveStatusViewState_PreNull_ReturnsPrePending()
    {
        PilotAssessmentStatusResponse status = BaseStatus();
        status.pre = null;

        Assert.AreEqual(
            PilotStatusViewState.PrePending,
            PilotController.ResolveStatusViewState(status)
        );
    }

    [Test]
    public void ResolveStatusViewState_PreStarted_ReturnsPreStarted()
    {
        PilotAssessmentStatusResponse status = BaseStatus();
        status.pre = new PilotAssessmentStatusItem { status = "started" };

        Assert.AreEqual(
            PilotStatusViewState.PreStarted,
            PilotController.ResolveStatusViewState(status)
        );
    }

    [Test]
    public void ResolveStatusViewState_PreCompletedWithoutPost_ReturnsInterventionPending()
    {
        PilotAssessmentStatusResponse status = BaseStatus();
        status.pre = new PilotAssessmentStatusItem { status = "completed" };
        status.post = null;
        status.post_eligible = false;

        Assert.AreEqual(
            PilotStatusViewState.InterventionPending,
            PilotController.ResolveStatusViewState(status)
        );
    }

    [Test]
    public void ResolveStatusViewState_UnknownPreStatus_DoesNotReturnCompletedFlow()
    {
        PilotAssessmentStatusResponse status = BaseStatus();
        status.pre = new PilotAssessmentStatusItem { status = "" };

        PilotStatusViewState viewState =
            PilotController.ResolveStatusViewState(status);

        Assert.AreEqual(PilotStatusViewState.InvalidPreStatus, viewState);
        Assert.AreNotEqual(PilotStatusViewState.InterventionPending, viewState);
        Assert.AreNotEqual(PilotStatusViewState.PostAvailable, viewState);
        Assert.AreNotEqual(PilotStatusViewState.AllCompleted, viewState);
    }

    [Test]
    public void ResolveStatusViewState_PostStarted_ReturnsPostStarted()
    {
        PilotAssessmentStatusResponse status = BaseStatus();
        status.pre = new PilotAssessmentStatusItem { status = "completed" };
        status.post = new PilotAssessmentStatusItem { status = "started" };

        Assert.AreEqual(
            PilotStatusViewState.PostStarted,
            PilotController.ResolveStatusViewState(status)
        );
    }

    [Test]
    public void ResolveStatusViewState_PostCompleted_ReturnsAllCompleted()
    {
        PilotAssessmentStatusResponse status = BaseStatus();
        status.pre = new PilotAssessmentStatusItem { status = "completed" };
        status.post = new PilotAssessmentStatusItem { status = "completed" };

        Assert.AreEqual(
            PilotStatusViewState.AllCompleted,
            PilotController.ResolveStatusViewState(status)
        );
    }

    [Test]
    public void ResolveStatusViewState_UnknownPostStatus_DoesNotReturnCompletedFlow()
    {
        PilotAssessmentStatusResponse status = BaseStatus();
        status.pre = new PilotAssessmentStatusItem { status = "completed" };
        status.post = new PilotAssessmentStatusItem { status = "paused" };

        PilotStatusViewState viewState =
            PilotController.ResolveStatusViewState(status);

        Assert.AreEqual(PilotStatusViewState.InvalidPostStatus, viewState);
        Assert.AreNotEqual(PilotStatusViewState.AllCompleted, viewState);
        Assert.AreNotEqual(PilotStatusViewState.PostStarted, viewState);
        Assert.AreNotEqual(PilotStatusViewState.PostAvailable, viewState);
    }

    private static PilotAssessmentStatusResponse BaseStatus()
    {
        return new PilotAssessmentStatusResponse
        {
            consent_active = true,
            post_eligible = false,
            intervention_progress = new PilotInterventionProgress
            {
                distinct_scenarios_completed = 0,
                required_distinct_scenarios = 3,
                completed_minigame_sessions = 0,
                required_minigame_sessions = 1
            }
        };
    }
}
