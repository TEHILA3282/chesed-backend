using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace ChafetzChesed.BLL.Services
{
    public class SearchService : ISearchService
    {
        private readonly AppDbContext _db;
        public SearchService(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<SearchResultDto>> SuggestAsync(string q, int institutionId, int take)
        {
            var allResults = await QueryAll(q, institutionId);
            return allResults
                .OrderByDescending(r => r.Score)
                .ThenBy(r => r.Title)
                .Take(take)
                .ToList();
        }

        public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(string q, int institutionId, int take)
        {
            var allResults = await QueryAll(q, institutionId);
            return allResults
                .OrderByDescending(r => r.Score)
                .ThenBy(r => r.Title)
                .Take(take)
                .ToList();
        }

        private async Task<List<SearchResultDto>> QueryAll(string q, int institutionId)
        {
            q = q.Trim();
            if (string.IsNullOrEmpty(q)) return new List<SearchResultDto>();
            var like = $"%{q}%";

            var loanTypes = await _db.LoanTypes
                .Where(t => EF.Functions.Like(t.Name, like))
                .Select(t => new SearchResultDto(
                    "loanType",
                    t.ID,
                    t.Name,
                    "סוג הלוואה",
                    t.Description,
                    $"/loan/{t.ID}",          
                    100
                ))
                .ToListAsync();

            var depositTypes = await _db.DepositTypes
                .Where(t => EF.Functions.Like(t.Name, like))
                .Select(t => new SearchResultDto(
                    "depositType",
                    t.ID,
                    t.Name,
                    "סוג הפקדה",
                    t.Description,
                    $"/deposit/{t.ID}",       
                    90
                ))
                .ToListAsync();

            var pages = await _db.SearchIndexItem
                .Where(s =>
                    (s.InstitutionId == null || s.InstitutionId == institutionId) &&
                    (EF.Functions.Like(s.Title, like) || EF.Functions.Like(s.Keywords, like)))
                .Select(s => new SearchResultDto(
                    "route",
                    s.Id,
                    s.Title,
                    s.Category,
                    s.Description ?? s.Keywords,
                    s.Route,
                    60
                ))
                .ToListAsync();

            return loanTypes
                .Concat(depositTypes)
                .Concat(pages)
                .ToList();
        }
    }
}
