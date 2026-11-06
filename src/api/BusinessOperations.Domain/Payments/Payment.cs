using BusinessOperations.Domain.Customers;
using BusinessOperations.Domain.Common;
using BusinessOperations.Domain.Invoices;

namespace BusinessOperations.Domain.Payments;

public class Payment: EntityBase
{
   public Guid InvoiceId{get;set;}
   public Invoice? Invoice{get;set;}
   public Guid CustomerId{get;set;}

   public Customer? Customer{get;set;}
   public decimal Amount{get;set;}
   public DateTime PaidAtUtc{get;set;}
   public string? Reference{get;set;}
   public PaymentMethod Method{get;set;}
   public string? CardBrand{get;set;}
   public string? CardLast4{get;set;}
   public string? AuthorizationCode{get;set;}

    
}