using BusinessOperations.Application.Common;
using BusinessOperations.Application.Erp;
using BusinessOperations.Domain.Customers;
using BusinessOperations.Domain.Erp;
using BusinessOperations.Domain.Invoices;
using BusinessOperations.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BusinessOperations.Infrastructure.Erp;

public sealed class ErpSyncService : IErpSyncService
{
    private readonly AppDbContext _db;
    private readonly IErpClient _erp;

    public ErpSyncService(AppDbContext db, IErpClient erp)
    {
        _db = db;
        _erp = erp;
    }

    public async Task<PagedResult<ErpSyncDto>> SearchAsync(
        Guid? invoiceId,
        ErpSyncStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.ErpSyncs.AsNoTracking().AsQueryable();
        if (invoiceId.HasValue && invoiceId.Value != Guid.Empty)
        {
            query = query.Where(s => s.InvoiceId == invoiceId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.LastAttemptAtUtc ?? s.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ErpSyncDto(
                s.Id,
                s.InvoiceId,
                s.Invoice != null ? s.Invoice.InvoiceNumber : string.Empty,
                s.Status,
                s.IdempotencyKey,
                s.ExternalReference,
                s.AttemptCount,
                s.LastError,
                s.LastAttemptAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<ErpSyncDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ErpSyncDto?> SyncAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Void)
        {
            throw new ValidationException("Only issued or paid invoices can be synced to the ERP.");
        }

        var sync = await _db.ErpSyncs.FirstOrDefaultAsync(s => s.InvoiceId == invoice.Id, cancellationToken);
        if (sync is null)
        {
            sync = new ErpSync
            {
                InvoiceId = invoice.Id,
                IdempotencyKey = Guid.NewGuid().ToString("N"),
                Status = ErpSyncStatus.Pending
            };
            _db.ErpSyncs.Add(sync);
            await _db.SaveChangesAsync(cancellationToken);
        }

        string externalReference;
        try
        {
            externalReference = await _erp.SubmitInvoiceAsync(
                new ErpInvoiceSubmission(
                    sync.IdempotencyKey,
                    invoice.InvoiceNumber,
                    invoice.Customer?.Name ?? string.Empty,
                    invoice.TotalAmount),
                cancellationToken);
        }
        catch (ErpUnavailableException ex)
        {
            sync.Status = ErpSyncStatus.Failed;
            sync.LastError = ex.Message;
            sync.AttemptCount += 1;
            sync.LastAttemptAtUtc = DateTime.UtcNow;
            sync.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Map(sync, invoice.InvoiceNumber);
        }

        var firstSuccess = sync.Status != ErpSyncStatus.Succeeded;
        sync.Status = ErpSyncStatus.Succeeded;
        sync.ExternalReference = externalReference;
        sync.LastError = null;
        sync.AttemptCount += 1;
        sync.LastAttemptAtUtc = DateTime.UtcNow;
        sync.UpdatedAtUtc = DateTime.UtcNow;

        if (firstSuccess)
        {
            _db.CustomerActivities.Add(new CustomerActivity
            {
                CustomerId = invoice.CustomerId,
                EventType = "ErpSynced",
                Description = $"Invoice '{invoice.InvoiceNumber}' synced to ERP as {externalReference}.",
                RelatedRecordId = sync.Id,
                OccurredAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Map(sync, invoice.InvoiceNumber);
    }

    private static ErpSyncDto Map(ErpSync sync, string invoiceNumber) =>
        new(
            sync.Id,
            sync.InvoiceId,
            invoiceNumber,
            sync.Status,
            sync.IdempotencyKey,
            sync.ExternalReference,
            sync.AttemptCount,
            sync.LastError,
            sync.LastAttemptAtUtc);
}