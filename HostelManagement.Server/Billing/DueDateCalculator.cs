namespace HostelManagement.Server.Billing;

/// <summary>
/// Pure due-date math: first due date is the move-in date, each subsequent one
/// advances by the billing cycle's month count via DateOnly.AddMonths, which
/// clamps to the last valid day of the resulting month (matching
/// DateTime.AddMonths). No proration - the day-of-month anchor can drift after
/// a clamp (e.g. Jan 31 -> Feb 28 -> Mar 28, not Mar 31); this is accepted
/// per design.md rather than worked around with custom calendar math.
/// </summary>
public static class DueDateCalculator
{
    public static DateOnly NextDueDate(DateOnly previousDueDate, int cycleMonths) =>
        previousDueDate.AddMonths(cycleMonths);

    /// <summary>
    /// The most recent due date that has occurred by <paramref name="today"/>
    /// (i.e. the current billing period's due date). If the move-in date is
    /// in the future relative to <paramref name="today"/>, returns the
    /// move-in date itself, since that is the resident's first due date.
    /// </summary>
    public static DateOnly CurrentDueDate(DateOnly moveInDate, int cycleMonths, DateOnly today)
    {
        var due = moveInDate;
        var next = NextDueDate(due, cycleMonths);
        while (next <= today)
        {
            due = next;
            next = NextDueDate(due, cycleMonths);
        }

        return due;
    }

    /// <summary>
    /// All due dates from the move-in date up to and including <paramref name="today"/>.
    /// </summary>
    public static IEnumerable<DateOnly> DueDatesUpTo(DateOnly moveInDate, int cycleMonths, DateOnly today)
    {
        if (moveInDate > today)
        {
            yield break;
        }

        var due = moveInDate;
        yield return due;

        var next = NextDueDate(due, cycleMonths);
        while (next <= today)
        {
            due = next;
            yield return due;
            next = NextDueDate(due, cycleMonths);
        }
    }
}
