using BusinessOperations.Application.Erp;
using BusinessOperations.Infrastructure.Erp;
using FluentAssertions;

namespace BusinessOperations.UnitTests.Erp;

public class SimulatedErpClientTests
{
    [Fact]
    public async Task Retry_with_the_same_idempotency_key_keeps_one_external_record()
    {
        var client = new SimulatedErpClient { FailFirstAttempts = 1 };
        var submission = new ErpInvoiceSubmission("key-1", "INV-1", "Harbor Supply", 80m);

        var failed = () => client.SubmitInvoiceAsync(submission);
        await failed.Should().ThrowAsync<ErpUnavailableException>();

        var first = await client.SubmitInvoiceAsync(submission);
        var second = await client.SubmitInvoiceAsync(submission);

        second.Should().Be(first);
        client.AcceptedCount.Should().Be(1);
    }
}