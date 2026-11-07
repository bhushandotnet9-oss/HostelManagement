using HostelManagement.Server.Billing;
using HostelManagement.Server.Models;

namespace HostelManagement.Server.Tests;

public class DueDateCalculatorTests
{
    [Fact]
    public void Monthly_cycle_advances_by_one_month()
    {
        var moveIn = new DateOnly(2026, 1, 15);
        var today = new DateOnly(2026, 3, 20);

        var current = DueDateCalculator.CurrentDueDate(moveIn, (int)BillingCycle.Monthly, today);

        Assert.Equal(new DateOnly(2026, 3, 15), current);
    }

    [Fact]
    public void Quarterly_cycle_advances_by_three_months()
    {
        var moveIn = new DateOnly(2026, 1, 10);
        var today = new DateOnly(2026, 8, 1);

        var current = DueDateCalculator.CurrentDueDate(moveIn, (int)BillingCycle.Quarterly, today);

        // Due dates: Jan 10, Apr 10, Jul 10, Oct 10 - the latest one <= Aug 1 is Jul 10.
        Assert.Equal(new DateOnly(2026, 7, 10), current);
    }

    [Fact]
    public void HalfYearly_cycle_advances_by_six_months()
    {
        var moveIn = new DateOnly(2026, 1, 1);
        var today = new DateOnly(2026, 9, 1);

        var current = DueDateCalculator.CurrentDueDate(moveIn, (int)BillingCycle.HalfYearly, today);

        Assert.Equal(new DateOnly(2026, 7, 1), current);
    }

    [Fact]
    public void Yearly_cycle_advances_by_twelve_months()
    {
        var moveIn = new DateOnly(2025, 1, 1);
        var today = new DateOnly(2026, 6, 1);

        var current = DueDateCalculator.CurrentDueDate(moveIn, (int)BillingCycle.Yearly, today);

        Assert.Equal(new DateOnly(2026, 1, 1), current);
    }

    [Fact]
    public void Due_date_on_the_31st_clamps_to_the_last_day_of_a_short_month()
    {
        var moveIn = new DateOnly(2026, 1, 31);

        var next = DueDateCalculator.NextDueDate(moveIn, (int)BillingCycle.Monthly);

        Assert.Equal(new DateOnly(2026, 2, 28), next);
    }

    [Fact]
    public void Due_dates_up_to_today_includes_every_elapsed_period()
    {
        var moveIn = new DateOnly(2026, 1, 15);
        var today = new DateOnly(2026, 3, 15);

        var dueDates = DueDateCalculator.DueDatesUpTo(moveIn, (int)BillingCycle.Monthly, today).ToList();

        Assert.Equal(
            [new DateOnly(2026, 1, 15), new DateOnly(2026, 2, 15), new DateOnly(2026, 3, 15)],
            dueDates);
    }

    [Fact]
    public void Due_dates_up_to_today_is_empty_when_move_in_is_in_the_future()
    {
        var moveIn = new DateOnly(2026, 5, 1);
        var today = new DateOnly(2026, 1, 1);

        var dueDates = DueDateCalculator.DueDatesUpTo(moveIn, (int)BillingCycle.Monthly, today).ToList();

        Assert.Empty(dueDates);
    }
}
