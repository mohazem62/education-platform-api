using EducationPlatform.Domain;

namespace EducationPlatform.UnitTests;

public sealed class FinancialRulesTests
{
    [Fact] public void Gross_earnings_are_sum_of_verified_rate_snapshots() => Assert.Equal(425.50m, FinancialCalculator.GrossTeacherEarnings([100m, 125.50m, 200m]));
    [Fact] public void Net_profit_subtracts_teacher_costs_and_expenses() => Assert.Equal(6_500m, FinancialCalculator.NetProfit(10_000m, 2_500m, 1_000m));
    [Fact] public void Partner_distribution_uses_snapshot_percentages() => Assert.Equal([6_000m, 4_000m], FinancialCalculator.Distribute(10_000m, [60m, 40m]));
    [Fact] public void Invalid_partner_percentages_are_rejected() { var ex = Assert.Throws<DomainException>(() => FinancialCalculator.Distribute(10_000m, [60m, 30m])); Assert.Equal("INVALID_PARTNER_PERCENTAGES", ex.Code); }
    [Fact] public void Credit_deduction_returns_traceable_before_and_after() => Assert.Equal((5, 3), CreditRules.Deduct(5, 2));
    [Fact] public void Credit_deduction_never_allows_negative_balance() { var ex = Assert.Throws<DomainException>(() => CreditRules.Deduct(1, 2)); Assert.Equal("INSUFFICIENT_SESSION_BALANCE", ex.Code); }
    [Theory] [InlineData(0, 1, false)] [InlineData(1, 1, true)] [InlineData(2, 1, true)] [InlineData(2, 3, false)] public void Session_visibility_requires_enough_credit(int balance, int cost, bool expected) => Assert.Equal(expected, CreditRules.CanAttend(balance, cost));
    [Theory] [InlineData(PayoutStatus.Draft, PayoutStatus.PendingReview, true)] [InlineData(PayoutStatus.PendingReview, PayoutStatus.Approved, true)] [InlineData(PayoutStatus.Approved, PayoutStatus.Paid, true)] [InlineData(PayoutStatus.Paid, PayoutStatus.Draft, false)] public void Payout_transitions_are_explicit(PayoutStatus from, PayoutStatus to, bool expected) => Assert.Equal(expected, PayoutRules.CanTransition(from, to));
}
