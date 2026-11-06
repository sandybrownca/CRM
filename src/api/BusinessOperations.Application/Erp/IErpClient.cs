namespace BusinessOperations.Application.Erp;

public interface IErpClient
{
    Task<string> SubmitInvoiceAsync(ErpInvoiceSubmission submission, CancellationToken cancellationToken = default);
}