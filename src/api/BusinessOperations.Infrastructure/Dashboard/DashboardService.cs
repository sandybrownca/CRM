using BusinessOperations.Application.Customers;
using BusinessOperations.Application.Dashboard;
using BusinessOperations.Application.Invoices;
using BusinessOperations.Domain.Erp;
using BusinessOperations.Domain.Customers;
using BusinessOperations.Domain.Invoices;
using BusinessOperations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BusinessOperations.Infrastructure.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var activeCustomers = await _db.Customers
            .CountAsync(c => c.Status == CustomerStatus.Active, cancellationToken);

        var issued = _db.Invoices.AsNoTracking().Where(i => i.Status == InvoiceStatus.Issued);

        var outstanding = await issued
            .SumAsync(i => (decimal?)(i.TotalAmount - i.AmountPaid), cancellationToken) ?? 0m;

        var openCount = await issued.CountAsync(cancellationToken);

        var overdueCount = await issued
            .CountAsync(i => i.DueDate < today, cancellationToken);

        var openInvoices = await issued
            .OrderBy(i => i.DueDate)
            .ThenBy(i => i.InvoiceNumber)
            .Take(8)
            .Select(i => new InvoiceListItemDto(
                i.Id,
                i.InvoiceNumber,
                i.CustomerId,
                i.Customer != null ? i.Customer.Name : string.Empty,
                i.IssueDate,
                i.DueDate,
                i.Status,
                i.TotalAmount,
                i.AmountPaid,
                i.TotalAmount - i.AmountPaid,
                i.CreatedAtUtc,
                (ErpSyncStatus?)null,
                null))
            .ToListAsync(cancellationToken);

        var recentActivity = await _db.CustomerActivities.AsNoTracking()
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(8)
            .Select(a => new CustomerActivityDto(
                a.Id,
                a.CustomerId,
                a.Customer != null ? a.Customer.Name : string.Empty,
                a.EventType,
                a.Description,
                a.RelatedRecordId,
                a.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return new DashboardDto(
            activeCustomers,
            outstanding,
            openCount,
            overdueCount,
            openInvoices,
            recentActivity);
    }
}