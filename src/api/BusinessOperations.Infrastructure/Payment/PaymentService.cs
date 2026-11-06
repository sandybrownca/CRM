using System.Data;
using System.Security.Cryptography;
using BusinessOperations.Application.Common;
using BusinessOperations.Application.Payments;
using BusinessOperations.Domain.Customers;
using BusinessOperations.Domain.Payments;
using BusinessOperations.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BusinessOperations.Infrastructure.Payments;

public sealed class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IValidator<RecordPaymentRequest> _validator;

    public PaymentService(AppDbContext db, IValidator<RecordPaymentRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<PagedResult<PaymentListItemDto>> SearchAsync(
        Guid? invoiceId,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Payments.AsNoTracking().AsQueryable();

        if (invoiceId.HasValue && invoiceId.Value != Guid.Empty)
        {
            query = query.Where(p => p.InvoiceId == invoiceId.Value);
        }

        if (customerId.HasValue && customerId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CustomerId == customerId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.PaidAtUtc)
            .ThenByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PaymentListItemDto(
                p.Id,
                p.InvoiceId,
                p.Invoice != null ? p.Invoice.InvoiceNumber : string.Empty,
                p.CustomerId,
                p.Customer != null ? p.Customer.Name : string.Empty,
                p.Amount,
                p.PaidAtUtc,
                p.Reference,
                p.CreatedAtUtc,
                p.Method,
                p.CardBrand,
                p.CardLast4,
                p.AuthorizationCode))
            .ToListAsync(cancellationToken);

        return new PagedResult<PaymentListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PaymentDto?> RecordAsync(
        Guid invoiceId,
        RecordPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        // Serializable so two payments cannot both read the same balance and over-apply it.
        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var invoice = await _db.Invoices
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        try
        {
            invoice.RecordPayment(request.Amount);
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(ex.Message);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new ValidationException(ex.Message);
        }

        var isCard = request.Method == PaymentMethod.SimulatedCard;
        var payment = new Payment
        {
            InvoiceId = invoice.Id,
            CustomerId = invoice.CustomerId,
            Amount = request.Amount,
            PaidAtUtc = request.PaidAtUtc ?? DateTime.UtcNow,
            Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
            Method = request.Method,
            CardBrand = isCard ? request.CardBrand!.Trim() : null,
            CardLast4 = isCard ? request.CardLast4 : null,
            AuthorizationCode = isCard ? "SIM-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(4)) : null
        };

        _db.Payments.Add(payment);
        var cardNote = isCard
            ? $" Simulated card {payment.CardBrand} ending {payment.CardLast4}, authorization {payment.AuthorizationCode}. The card number was not stored."
            : string.Empty;
        _db.CustomerActivities.Add(new CustomerActivity
        {
            CustomerId = invoice.CustomerId,
            EventType = "PaymentRecorded",
            Description = $"Payment of {payment.Amount:C} recorded against invoice '{invoice.InvoiceNumber}'. Balance due {invoice.BalanceDue:C}.{cardNote}",
            RelatedRecordId = payment.Id,
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PaymentDto(
            payment.Id,
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerId,
            invoice.Customer?.Name ?? string.Empty,
            payment.Amount,
            payment.PaidAtUtc,
            payment.Reference,
            invoice.AmountPaid,
            invoice.BalanceDue,
            invoice.Status,
            payment.CreatedAtUtc,
            payment.Method,
            payment.CardBrand,
            payment.CardLast4,
            payment.AuthorizationCode);
    }
}