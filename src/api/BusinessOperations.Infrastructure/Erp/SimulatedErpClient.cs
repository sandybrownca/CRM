using System.Collections.Concurrent;
using BusinessOperations.Application.Erp;

namespace BusinessOperations.Infrastructure.Erp;

public sealed class SimulatedErpClient : IErpClient
{
    private readonly ConcurrentDictionary<string, string> _accepted = new();
    private readonly ConcurrentDictionary<string, int> _attempts = new();

    public int FailFirstAttempts { get; set; }

    public int AcceptedCount => _accepted.Count;

    public Task<string> SubmitInvoiceAsync(ErpInvoiceSubmission submission, CancellationToken cancellationToken = default)
    {
        var attempt = _attempts.AddOrUpdate(submission.IdempotencyKey, 1, (_, count) => count + 1);
        if (attempt <= FailFirstAttempts && !_accepted.ContainsKey(submission.IdempotencyKey))
        {
            throw new ErpUnavailableException("Simulated ERP rejected the attempt. Retry with the same idempotency key.");
        }

        var externalReference = _accepted.GetOrAdd(
            submission.IdempotencyKey,
            key => $"ERP-{(key.Length <= 12 ? key : key[..12]).ToUpperInvariant()}");

        return Task.FromResult(externalReference);
    }
}