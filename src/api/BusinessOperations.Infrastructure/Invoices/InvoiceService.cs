using BusinessOperations.Application.Common;
using BusinessOperations.Application.Erp;
using BusinessOperations.Application.Invoices;
using BusinessOperations.Domain.Customers;
using BusinessOperations.Domain.Erp;
using BusinessOperations.Domain.Invoices;
using BusinessOperations.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BusinessOperations.Infrastructure.Invoices;

public sealed class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _db;
    private readonly IErpSyncService _erp;
    private readonly IValidator<CreateInvoiceRequest> _createValidator;
    private readonly IValidator<UpdateInvoiceRequest> _updateValidator;
    private readonly decimal _taxRate;

    public InvoiceService(
        AppDbContext db,
        IErpSyncService erp,
        IValidator<CreateInvoiceRequest> createValidator,
        IValidator<UpdateInvoiceRequest> updateValidator,
        IConfiguration configuration)
    {
        _db = db;
        _erp = erp;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _taxRate = configuration.GetValue("Tax:Rate", 0m);
    }

    public async Task<PagedResult<InvoiceListItemDto>> SearchAsync(
        Guid? customerId,
        InvoiceStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Invoices.AsNoTracking().Include(i => i.Customer).AsQueryable();

        if (customerId.HasValue && customerId.Value != Guid.Empty)
        {
            query = query.Where(i => i.CustomerId == customerId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
                i.BalanceDue,
                i.CreatedAtUtc,
                (ErpSyncStatus?)null,
                null))
            .ToListAsync(cancellationToken);

        items = await AttachErpAsync(items, cancellationToken);

        return new PagedResult<InvoiceListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<InvoiceDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _db.Invoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        return invoice is null ? null : MapDetails(invoice);
    }

    public async Task<InvoiceDetailsDto> CreateAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new ValidationException("Customer does not exist.");
        }

        if (customer.Status != CustomerStatus.Active)
        {
            throw new ValidationException("Cannot create an invoice for an inactive customer.");
        }

        var prefix = $"INV-{DateTime.UtcNow:yyyyMM}-";
        var latestNumber = await _db.Invoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextSeq = 1;
        if (latestNumber is not null && int.TryParse(latestNumber.AsSpan(prefix.Length), out var seq))
        {
            nextSeq = seq + 1;
        }

        var invoice = new Invoice
        {
            InvoiceNumber = $"{prefix}{nextSeq:D4}",
            CustomerId = customer.Id
        };

        invoice.SetSchedule(request.IssueDate, request.DueDate);
        foreach (var item in request.LineItems)
        {
            invoice.AddLineItem(item.Description, item.Quantity, item.UnitPrice);
        }

        invoice.ApplyTaxRate(_taxRate);
        _db.Invoices.Add(invoice);
        _db.CustomerActivities.Add(new CustomerActivity
        {
            CustomerId = customer.Id,
            EventType = "InvoiceCreated",
            Description = $"Draft invoice '{invoice.InvoiceNumber}' created for {invoice.TotalAmount:C}.",
            RelatedRecordId = invoice.Id,
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return MapDetails(invoice, customer.Name, customer.Email);
    }

    public async Task<InvoiceDetailsDto?> UpdateAsync(
        Guid id,
        UpdateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var invoice = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new ValidationException("Only draft invoices can be modified.");
        }

        invoice.SetSchedule(request.IssueDate, request.DueDate);
        invoice.ClearLineItems();
        foreach (var item in request.LineItems)
        {
            invoice.AddLineItem(item.Description, item.Quantity, item.UnitPrice);
        }

        invoice.ApplyTaxRate(_taxRate);
        _db.CustomerActivities.Add(new CustomerActivity
        {
            CustomerId = invoice.CustomerId,
            EventType = "InvoiceUpdated",
            Description = $"Draft invoice '{invoice.InvoiceNumber}' updated. New total: {invoice.TotalAmount:C}.",
            RelatedRecordId = invoice.Id,
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return MapDetails(invoice);
    }

    public async Task<InvoiceDetailsDto?> IssueAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        try
        {
            invoice.Issue();
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(ex.Message);
        }

        _db.CustomerActivities.Add(new CustomerActivity
        {
            CustomerId = invoice.CustomerId,
            EventType = "InvoiceIssued",
            Description = $"Invoice '{invoice.InvoiceNumber}' issued for {invoice.TotalAmount:C}. Due on {invoice.DueDate:yyyy-MM-dd}.",
            RelatedRecordId = invoice.Id,
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        await _erp.SyncAsync(invoice.Id, cancellationToken);
        return MapDetails(invoice);
    }

    public async Task<InvoiceDetailsDto?> VoidAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        try
        {
            invoice.Void();
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(ex.Message);
        }

        _db.CustomerActivities.Add(new CustomerActivity
        {
            CustomerId = invoice.CustomerId,
            EventType = "InvoiceVoided",
            Description = $"Invoice '{invoice.InvoiceNumber}' voided.",
            RelatedRecordId = invoice.Id,
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return MapDetails(invoice);
    }

    private async Task<List<InvoiceListItemDto>> AttachErpAsync(
        List<InvoiceListItemDto> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return items;
        }

        var ids = items.Select(i => i.Id).ToList();
        var syncs = await _db.ErpSyncs.AsNoTracking()
            .Where(s => ids.Contains(s.InvoiceId))
            .Select(s => new { s.InvoiceId, s.Status, s.ExternalReference })
            .ToListAsync(cancellationToken);
        var byInvoice = syncs.ToDictionary(s => s.InvoiceId);

        return items
            .Select(item => byInvoice.TryGetValue(item.Id, out var sync)
                ? item with { ErpStatus = sync.Status, ErpExternalReference = sync.ExternalReference }
                : item)
            .ToList();
    }

    private static InvoiceDetailsDto MapDetails(Invoice invoice, string? customerName = null, string? customerEmail = null)
    {
        return new InvoiceDetailsDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerId,
            customerName ?? invoice.Customer?.Name ?? string.Empty,
            customerEmail ?? invoice.Customer?.Email ?? string.Empty,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.Status,
            invoice.Subtotal,
            invoice.TaxRate,
            invoice.TaxAmount,
            invoice.TotalAmount,
            invoice.AmountPaid,
            invoice.BalanceDue,
            invoice.LineItems.Select(li => new InvoiceLineItemDto(
                li.Id,
                li.Description,
                li.Quantity,
                li.UnitPrice,
                li.LineTotal)).ToList(),
            invoice.CreatedAtUtc,
            invoice.UpdatedAtUtc);
    }
}