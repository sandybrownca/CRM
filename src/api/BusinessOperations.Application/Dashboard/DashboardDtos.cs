

using BusinessOperations.Application.Customers;
using BusinessOperations.Application.Invoices;

namespace BusinessOperations.Application.Dashboard;
public sealed record DashboardDto(
    int ActiveCustomerCount,
    decimal OutstandingBalance,
    int OpenInvoiceCount,
    int OverdueInvoiceCount,
    IReadOnlyList<InvoiceListItemDto> OpenInvoices,
    IReadOnlyList<CustomerActivityDto> RecentActivity
    
);