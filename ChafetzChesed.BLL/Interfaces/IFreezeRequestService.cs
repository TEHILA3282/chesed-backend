using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChafetzChesed.BLL.Interfaces
{
    public interface IFreezeRequestService
    {
        Task<FreezeRequest> CreateAsync(CreateFreezeRequestDto dto, Registration user, int institutionId);
    }
}
