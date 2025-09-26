using ChafetzChesed.Common.DTOs;

namespace ChafetzChesed.BLL.Interfaces
{
    public interface ISearchService
    {
        Task<IReadOnlyList<SearchResultDto>> SuggestAsync(string q, int institutionId, int take);
        Task<IReadOnlyList<SearchResultDto>> SearchAsync(string q, int institutionId, int take);
    }
}
