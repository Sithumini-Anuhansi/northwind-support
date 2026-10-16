using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Domain.Aggregates.Tickets;

public class SlaEvaluatorTests
{
    private static readonly DateTime Now = FixedClock.DefaultNow;

    [Fact]
    public void Evaluate_WithoutADueDate_IsNotApplicable() =>
        Assert.Equal(SlaStatus.NotApplicable, SlaEvaluator.Evaluate(Now, null, null, Now, 25));

    [Fact]
    public void Evaluate_ResolvedOnTime_IsMet() =>
        Assert.Equal(SlaStatus.Met, SlaEvaluator.Evaluate(Now, Now.AddHours(4), Now.AddHours(4), Now.AddDays(1), 25));

    [Fact]
    public void Evaluate_ResolvedLate_IsBreached() =>
        Assert.Equal(SlaStatus.Breached, SlaEvaluator.Evaluate(Now, Now, Now.AddMinutes(1), Now, 25));

    [Fact]
    public void Evaluate_OpenAndPastDue_IsBreached() =>
        Assert.Equal(SlaStatus.Breached, SlaEvaluator.Evaluate(Now.AddHours(-1), Now.AddMinutes(-1), null, Now, 25));

    [Fact]
    public void Evaluate_OpenWithMoreThanThresholdRemaining_IsWithinSla() =>
        Assert.Equal(SlaStatus.WithinSla, SlaEvaluator.Evaluate(Now, Now.AddHours(4), null, Now.AddHours(2), 25));

    [Fact]
    public void Evaluate_OpenAtAtRiskThreshold_IsAtRisk() =>
        Assert.Equal(SlaStatus.AtRisk, SlaEvaluator.Evaluate(Now, Now.AddHours(4), null, Now.AddHours(3), 25));

    [Fact]
    public void Evaluate_OpenExactlyAtDue_IsAtRiskAndOneTickLaterIsBreached()
    {
        var due = Now.AddHours(4);

        Assert.Equal(SlaStatus.AtRisk, SlaEvaluator.Evaluate(Now, due, null, due, 25));
        Assert.Equal(SlaStatus.Breached, SlaEvaluator.Evaluate(Now, due, null, due.AddTicks(1), 25));
    }
}
