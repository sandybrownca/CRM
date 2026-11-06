using BusinessOperations.Application.Common;
using BusinessOperations.Application.Customers;
using BusinessOperations.Domain.Customers;
using BusinessOperations.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BusinessOperations.Infrastructure.Customers;

public sealed class CustomerInteractionService : ICustomerInteractionService
{
    private readonly AppDbContext _db;
    private readonly IValidator<LogInteractionRequest> _validator;

    public CustomerInteractionService(AppDbContext db, IValidator<LogInteractionRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<PagedResult<CustomerInteractionDto>?> SearchAsync(
        Guid customerId,
        InteractionChannel? channel,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Customers.AnyAsync(c => c.Id == customerId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.CustomerInteractions.AsNoTracking().Where(i => i.CustomerId == customerId);
        if (channel is not null)
        {
            query = query.Where(i => i.Channel == channel);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(i => i.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new CustomerInteractionDto(
                i.Id,
                i.CustomerId,
                i.Channel,
                i.Subject,
                i.Body,
                i.OccurredAtUtc,
                i.RecordedByName))
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerInteractionDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CustomerInteractionDto?> LogAsync(
        Guid customerId,
        LogInteractionRequest request,
        string recordedByName,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        var occurred = request.OccurredAtUtc ?? DateTime.UtcNow;
        occurred = occurred.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(occurred, DateTimeKind.Utc)
            : occurred.ToUniversalTime();

        var interaction = new CustomerInteraction
        {
            CustomerId = customer.Id,
            Channel = request.Channel,
            Subject = request.Subject.Trim(),
            Body = request.Body.Trim(),
            OccurredAtUtc = occurred,
            RecordedByName = string.IsNullOrWhiteSpace(recordedByName) ? "Staff" : recordedByName.Trim()
        };

        var summary = $"{interaction.Channel}: {interaction.Subject}";
        if (summary.Length > 1000)
        {
            summary = summary[..1000];
        }

        _db.CustomerInteractions.Add(interaction);
        _db.CustomerActivities.Add(new CustomerActivity
        {
            CustomerId = customer.Id,
            EventType = "InteractionLogged",
            Description = summary,
            RelatedRecordId = interaction.Id,
            OccurredAtUtc = occurred
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new CustomerInteractionDto(
            interaction.Id,
            interaction.CustomerId,
            interaction.Channel,
            interaction.Subject,
            interaction.Body,
            interaction.OccurredAtUtc,
            interaction.RecordedByName);
    }
}