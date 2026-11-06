using BusinessOperations.Application.Common;
using BusinessOperations.Application.Customers;
using BusinessOperations.Domain.Customers;
using BusinessOperations.Domain.Invoices;
using BusinessOperations.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BusinessOperations.Infrastructure.Customers;

public sealed class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;
    private readonly IValidator<CreateCustomerRequest> _createValidator;
    private readonly IValidator<UpdateCustomerRequest> _updateValidator;

    public CustomerService(
        AppDbContext db,
        IValidator<CreateCustomerRequest> createValidator,
        IValidator<UpdateCustomerRequest> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<CustomerListItemDto>> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                (c.JobTitle != null && c.JobTitle.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerListItemDto(
                c.Id,
                c.Name,
                c.Email,
                c.Status,
                c.Invoices.Where(i => i.Status == InvoiceStatus.Issued).Sum(i => (decimal?)(i.TotalAmount - i.AmountPaid)) ?? 0m,
                c.Invoices.Count(i => i.Status == InvoiceStatus.Issued),
                c.Kind,
                c.JobTitle,
                c.BillingCity))

            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CustomerDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return customer is null ? null : MapDetails(customer);
    }

    public async Task<CustomerDetailsDto> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = request.Email.Trim().ToLowerInvariant();
        var emailTaken = await _db.Customers.AnyAsync(
            c => c.Email == email && c.Status == CustomerStatus.Active,
            cancellationToken);

        if (emailTaken)
        {
            throw new ValidationException("Email must be unique among active customers.");
        }

        var customer = new Customer();
        customer.UpdateContact(
            request.Name,
            request.Email,
            request.Phone,
            request.BillingAddressLine1,
            request.BillingAddressLine2,
            request.BillingCity,
            request.BillingState,
            request.BillingPostalCode,
            request.BillingCountry,
            request.Kind,
            request.JobTitle,
            request.ShippingAddressLine1,
            request.ShippingAddressLine2,
            request.ShippingCity,
            request.ShippingState,
            request.ShippingPostalCode,
            request.ShippingCountry);

        _db.Customers.Add(customer);
        _db.CustomerActivities.Add(new CustomerActivity
        {
            CustomerId = customer.Id,
            EventType = "CustomerCreated",
            Description = $"Customer '{customer.Name}' was created.",
            RelatedRecordId = customer.Id,
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return MapDetails(customer);
    }

    public async Task<CustomerDetailsDto?> UpdateAsync(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var emailTaken = await _db.Customers.AnyAsync(
            c => c.Id != id && c.Email == email && c.Status == CustomerStatus.Active,
            cancellationToken);

        if (emailTaken)
        {
            throw new ValidationException("Email must be unique among active customers.");
        }

        customer.UpdateContact(
            request.Name,
            request.Email,
            request.Phone,
            request.BillingAddressLine1,
            request.BillingAddressLine2,
            request.BillingCity,
            request.BillingState,
            request.BillingPostalCode,
            request.BillingCountry,
            request.Kind,
            request.JobTitle,
            request.ShippingAddressLine1,
            request.ShippingAddressLine2,
            request.ShippingCity,
            request.ShippingState,
            request.ShippingPostalCode,
            request.ShippingCountry);

        _db.CustomerActivities.Add(new CustomerActivity
        {
            CustomerId = customer.Id,
            EventType = "CustomerUpdated",
            Description = $"Customer '{customer.Name}' was updated.",
            RelatedRecordId = customer.Id,
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return MapDetails(customer);
    }

    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (customer is null)
        {
            return false;
        }

        if (customer.Status == CustomerStatus.Inactive)
        {
            return true;
        }

        customer.Deactivate();
        _db.CustomerActivities.Add(new CustomerActivity
        {
            CustomerId = customer.Id,
            EventType = "CustomerDeactivated",
            Description = $"Customer '{customer.Name}' was deactivated.",
            RelatedRecordId = customer.Id,
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PagedResult<CustomerActivityDto>?> SearchActivitiesAsync(
        Guid customerId,
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

        var query = _db.CustomerActivities.AsNoTracking().Where(a => a.CustomerId == customerId);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new CustomerActivityDto(
                a.Id,
                a.CustomerId,
                a.Customer != null ? a.Customer.Name : string.Empty,
                a.EventType,
                a.Description,
                a.RelatedRecordId,
                a.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerActivityDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static CustomerDetailsDto MapDetails(Customer customer) =>
        new(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.Phone,
            customer.BillingAddressLine1,
            customer.BillingAddressLine2,
            customer.BillingCity,
            customer.BillingState,
            customer.BillingPostalCode,
            customer.BillingCountry,
            customer.Status,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc,
            customer.Kind,
            customer.JobTitle,
            customer.ShippingAddressLine1,
            customer.ShippingAddressLine2,
            customer.ShippingCity,
            customer.ShippingState,
            customer.ShippingPostalCode,
            customer.ShippingCountry);
}