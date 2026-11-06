using System.Text;
using BusinessOperations.Application.Common;
using BusinessOperations.Domain.Erp;
namespace BusinessOperations.Application.Erp;

public interface IErpSyncService
{
    Task<PagedResult<ErpSyncDto>> SearchAsync(
        Guid? invoiceId,
        ErpSyncStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

        Task<ErpSyncDto?> SyncAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}