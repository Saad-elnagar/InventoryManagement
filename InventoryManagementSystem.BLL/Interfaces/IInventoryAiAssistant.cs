using InventoryManagementSystem.BLL.DTOs;

namespace InventoryManagementSystem.BLL.Interfaces;

public interface IInventoryAiAssistant
{
    Task<string> ChatAsync(
        IReadOnlyCollection<InventoryAiMessageDTO> messages,
        CancellationToken cancellationToken = default);
}
