using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Entities;

namespace ChafetzChesed.BLL.Interfaces
{
    public interface IRegistrationService
    {
        Task<IEnumerable<Registration>> GetAllAsync();

        Task<Registration?> GetByIdAsync(string id, int institutionId);

        Task<Registration> AddAsync(Registration registration);

        Task<bool> DeleteAsync(string id, int institutionId);

        Task<bool> UpdateStatusAsync(string registrationId, int institutionId, string newStatus);

        Task<List<Registration>> GetPendingAsync(int institutionId);

        Task<List<Registration>> GetByStatusAsync(int institutionId, string status);

        Task<bool> ExistsAsync(string email, string id, int institutionId);

        Task<bool> UpdateAsync(Registration updated);

        Task<bool> UpdatePartialAsync(string userId, RegistrationUpdateDto dto, int institutionId, string actorId);
    }
}
