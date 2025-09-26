using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Data;
using ChafetzChesed.DAL.Entities;
using ChafetzChesed.Common.Constants;


namespace ChafetzChesed.BLL.Services
{
    public class FreezeRequestService : IFreezeRequestService
    {
        private readonly AppDbContext _ctx;
        public FreezeRequestService(AppDbContext ctx) => _ctx = ctx;

        public async Task<FreezeRequest> CreateAsync(CreateFreezeRequestDto dto, Registration user, int institutionId)
        {
            if (user == null) throw new UnauthorizedAccessException("משתמש לא מאומת");
            if (!FreezeRequestTypeValues.All.Contains(dto.RequestType?.Trim().ToLowerInvariant()))
                throw new ArgumentOutOfRangeException(nameof(dto.RequestType), "RequestType חייב להיות 'loan' או 'deposit'");
            if (!dto.Acknowledged)
                throw new InvalidOperationException("חובה לאשר ידוע לי.");

            var entity = new FreezeRequest
            {
                ClientID = user.ID,
                InstitutionId = institutionId,
                RequestType = dto.RequestType.Trim().ToLowerInvariant(), 
                Reason = (dto.Reason ?? string.Empty).Trim(),
                Acknowledged = dto.Acknowledged,
                CreatedAt = DateTime.UtcNow
            };

            _ctx.FreezeRequests.Add(entity);
            await _ctx.SaveChangesAsync();
            return entity;
        }
    }
}
