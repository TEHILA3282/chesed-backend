using ChafetzChesed.Common.Models;


namespace ChafetzChesed.BLL.Interfaces
{
    public interface IExternalFormService
    {
        Task<List<ExternalForm>> GetFormsAsync();
    }
}
