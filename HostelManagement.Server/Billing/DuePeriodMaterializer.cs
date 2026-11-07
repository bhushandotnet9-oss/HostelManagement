using HostelManagement.Server.Data;
using HostelManagement.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace HostelManagement.Server.Billing;

/// <summary>
/// Materializes DuePeriod rows lazily (on read, or before logging a payment)
/// rather than via a background job, per design.md. A checked-out resident
/// never gets further periods, even past ones that would otherwise now be due.
/// </summary>
public static class DuePeriodMaterializer
{
    public static async Task MaterializeAsync(AppDbContext db, Resident resident, DateOnly today)
    {
        if (resident.Status == ResidentStatus.CheckedOut || resident.BillingCycle == default)
        {
            return;
        }

        var existingDueDates = await db.DuePeriods
            .Where(d => d.ResidentId == resident.Id)
            .Select(d => d.DueDate)
            .ToListAsync();
        var existing = existingDueDates.ToHashSet();

        var cycleMonths = (int)resident.BillingCycle;
        foreach (var dueDate in DueDateCalculator.DueDatesUpTo(resident.MoveInDate, cycleMonths, today))
        {
            if (existing.Contains(dueDate))
            {
                continue;
            }

            db.DuePeriods.Add(new DuePeriod
            {
                Id = Guid.NewGuid(),
                ResidentId = resident.Id,
                DueDate = dueDate,
                RentAmount = resident.RentAmount,
                AmountPaid = 0,
                IsPaid = false,
            });
        }

        await db.SaveChangesAsync();
    }
}
