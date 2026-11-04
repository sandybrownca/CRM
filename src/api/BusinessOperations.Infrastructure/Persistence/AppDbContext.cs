using BusinessOperations.Domain.Customers;
using BusinessOperations.Domain.Invoices;
using BusinessOperations.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BusinessOperations.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerActivity> CustomerActivities => Set<CustomerActivity>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(50);
            entity.Property(x => x.BillingAddressLine1).HasMaxLength(200);
            entity.Property(x => x.BillingAddressLine2).HasMaxLength(200);
            entity.Property(x => x.BillingCity).HasMaxLength(100);
            entity.Property(x => x.BillingState).HasMaxLength(100);
            entity.Property(x => x.BillingPostalCode).HasMaxLength(20);
            entity.Property(x => x.BillingCountry).HasMaxLength(100);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

            entity.HasIndex(x => x.Email);
            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => new { x.Email, x.Status });
        });

        builder.Entity<CustomerActivity>(entity =>
        {
            entity.ToTable("CustomerActivities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            entity.HasIndex(x => new { x.CustomerId, x.OccurredAtUtc });
            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.AmountPaid).HasColumnType("decimal(18,2)");

            entity.HasIndex(x => x.InvoiceNumber).IsUnique();
            entity.HasIndex(x => x.CustomerId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => new { x.CustomerId, x.Status });

            entity.HasOne(x => x.Customer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.LineItems)
                .WithOne(li => li.Invoice)
                .HasForeignKey(li => li.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InvoiceLineItem>(entity =>
        {
            entity.ToTable("InvoiceLineItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).HasMaxLength(200).IsRequired();
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.HasIndex(x => x.InvoiceId);
        });
    }
}
