
using BusinessOperations.Domain.Customers;
using BusinessOperations.Domain.Erp;
using BusinessOperations.Domain.Invoices;
using BusinessOperations.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessOperations.Infrastructure.Persistence.Seeding;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        // Prevent duplicate demo data on subsequent application starts.
        if (await db.Customers.AnyAsync(
            x => x.Email.EndsWith("@demo.local")))
        {
            return;
        }

        var today = DateTime.UtcNow.Date;

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var customers = CreateCustomers();

        db.Customers.AddRange(customers);

        var invoices = new List<Invoice>();
        var payments = new List<Payment>();
        var activities = new List<CustomerActivity>();
        var interactions = new List<CustomerInteraction>();
        var erpSyncs = new List<ErpSync>();

        var channels = Enum.GetValues<InteractionChannel>();

        for (var i = 0; i < 20; i++)
        {
            var customer = customers[i % customers.Count];

            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-2026-{1001 + i}",
                CustomerId = customer.Id,
                Customer = customer
            };

            var issueDate = today.AddDays(-(i * 4 + 10));

            invoice.SetSchedule(issueDate, issueDate.AddDays(30));

            invoice.AddLineItem(
                "Professional Services",
                1 + (i % 3),
                500m + (i * 75m));

            invoice.AddLineItem(
                "Software Subscription",
                1,
                199m + (i * 10m));

            invoice.ApplyTaxRate(0.10m);

            // Four invoice scenarios:
            // Paid, overdue, issued and draft.
            switch (i % 4)
            {
                case 0:
                    invoice.Issue();
                    invoice.RecordPayment(invoice.TotalAmount);

                    payments.Add(CreatePayment(
                        invoice,
                        customer,
                        invoice.TotalAmount,
                        today.AddDays(-5),
                        PaymentMethod.SimulatedCard,
                        $"PAY-{10001 + i}"));

                    break;

                case 1:
                    // Deliberately overdue.
                    invoice.SetSchedule(
                        today.AddDays(-60),
                        today.AddDays(-30));

                    invoice.Issue();
                    invoice.RecordPayment(
                        invoice.TotalAmount * 0.25m);

                    payments.Add(CreatePayment(
                        invoice,
                        customer,
                        invoice.AmountPaid,
                        today.AddDays(-20),
                        PaymentMethod.Manual,
                        $"PAY-{10001 + i}"));

                    break;

                case 2:
                    invoice.Issue();
                    break;

                case 3:
                    // Draft invoice remains editable.
                    break;
            }

            invoices.Add(invoice);

            activities.Add(new CustomerActivity
            {
                Customer = customer,
                CustomerId = customer.Id,
                EventType = "Invoice",
                Description =
                    $"Invoice {invoice.InvoiceNumber} created.",
                RelatedRecordId = invoice.Id,
                OccurredAtUtc = issueDate.AddHours(10)
            });

            if (i % 3 == 0)
            {
                erpSyncs.Add(new ErpSync
                {
                    InvoiceId = invoice.Id,
                    IdempotencyKey =
                        $"DEMO-ERP-{invoice.InvoiceNumber}",
                    Status = i % 2 == 0
                        ? ErpSyncStatus.Succeeded
                        : ErpSyncStatus.Pending,
                    ExternalReference = i % 2 == 0
                        ? $"ERP-{5000 + i}"
                        : null,
                    AttemptCount = i % 2 == 0 ? 1 : 0,
                    LastAttemptAtUtc = i % 2 == 0
                        ? today.AddDays(-2)
                        : null
                });
            }
        }

        for (var i = 0; i < customers.Count; i++)
        {
            var customer = customers[i];

            activities.Add(new CustomerActivity
            {
                Customer = customer,
                CustomerId = customer.Id,
                EventType = "CustomerCreated",
                Description = $"Customer {customer.Name} added to CRM.",
                OccurredAtUtc = today.AddDays(-i - 20)
            });

            interactions.Add(new CustomerInteraction
            {
                Customer = customer,
                CustomerId = customer.Id,
                Channel = channels[i % channels.Length],
                Subject = "Account follow-up",
                Body = $"Follow-up discussion with {customer.Name} regarding current services and upcoming requirements.",
                RecordedByName = "Demo Administrator",
                OccurredAtUtc = today.AddDays(-i - 2)
            });
        }

        db.Invoices.AddRange(invoices);
        db.Payments.AddRange(payments);
        db.CustomerActivities.AddRange(activities);
        db.CustomerInteractions.AddRange(interactions);
        db.ErpSyncs.AddRange(erpSyncs);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static List<Customer> CreateCustomers()
    {
        var names = new[]
        {
            ("Acme Technologies", "contact@acme.demo.local", CustomerKind.Company),
            ("Global Solutions Inc", "info@global.demo.local", CustomerKind.Company),
            ("Bright Future Ltd", "hello@bright.demo.local", CustomerKind.Company),
            ("John Smith", "john@demo.local", CustomerKind.Individual),
            ("Sarah Johnson", "sarah@demo.local", CustomerKind.Individual),
            ("Northstar Consulting", "contact@northstar.demo.local", CustomerKind.Company),
            ("Bluewave Systems", "info@bluewave.demo.local", CustomerKind.Company),
            ("Michael Brown", "michael@demo.local", CustomerKind.Individual),
            ("Evergreen Enterprises", "info@evergreen.demo.local", CustomerKind.Company),
            ("Emily Davis", "emily@demo.local", CustomerKind.Individual),
            ("Vertex Digital", "hello@vertex.demo.local", CustomerKind.Company),
            ("Pinnacle Group", "contact@pinnacle.demo.local", CustomerKind.Company),
            ("David Wilson", "david@demo.local", CustomerKind.Individual),
            ("Silverline Partners", "info@silverline.demo.local", CustomerKind.Company),
            ("Horizon Industries", "contact@horizon.demo.local", CustomerKind.Company)
        };

        return names.Select((item, index) => new Customer
        {
            Name = item.Item1,
            Email = item.Item2,
            Kind = item.Item3,
            Status = index == 14
                ? CustomerStatus.Inactive
                : CustomerStatus.Active,
            Phone = $"+1-416-555-{1000 + index}",
            BillingAddressLine1 = $"{100 + index} King Street",
            BillingCity = "Toronto",
            BillingState = "Ontario",
            BillingPostalCode = $"M5H {index + 1}A1",
            BillingCountry = "Canada",
            ShippingAddressLine1 = $"{100 + index} King Street",
            ShippingCity = "Toronto",
            ShippingState = "Ontario",
            ShippingPostalCode = $"M5H {index + 1}A1",
            ShippingCountry = "Canada",
            JobTitle = item.Item3 == CustomerKind.Individual
                ? "Business Owner"
                : null
        }).ToList();
    }

    private static Payment CreatePayment(
        Invoice invoice,
        Customer customer,
        decimal amount,
        DateTime paidAt,
        PaymentMethod method,
        string reference)
    {
        return new Payment
        {
            Invoice = invoice,
            InvoiceId = invoice.Id,
            Customer = customer,
            CustomerId = customer.Id,
            Amount = amount,
            PaidAtUtc = paidAt,
            Method = method,
            Reference = reference,
            CardBrand = method == PaymentMethod.SimulatedCard
                ? "Visa"
                : null,
            CardLast4 = method == PaymentMethod.SimulatedCard
                ? "4242"
                : null,
            AuthorizationCode = method == PaymentMethod.SimulatedCard
                ? $"AUTH{reference[^4..]}"
                : null
        };
    }
}
