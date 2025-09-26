using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ClosedXML.Excel;
using ChafetzChesed.DAL.Data;
using ChafetzChesed.DAL.Entities;
using ChafetzChesed.BLL.Interfaces;

namespace ChafetzChesed.BLL.Services
{
    public class AccountActionsService : IAccountActionsService
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountActionsService(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task FetchAndParseExcelFromExternalAsync(int institutionId)
        {
            var client = _httpClientFactory.CreateClient();
            // אפשר לשקול להגדיר BaseAddress/Timeout ב-Program.cs עבור client בשם

            var response = await client.GetAsync("https://external-system.com/api/excel/file");
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // דילוג על כותרת

            foreach (var row in rows)
            {
             
                var zeout = (row.Cell(2).GetString() ?? string.Empty).Trim().PadLeft(9, '0');

                int seder = 0;
                int.TryParse(row.Cell(3).GetString(), out seder);

                var perut = row.Cell(4).GetString() ?? string.Empty;

                int important = 0;
                int.TryParse(row.Cell(5).GetString(), out important);

                var action = new AccountAction
                {
                    InstitutionId = institutionId,
                    Zeout = zeout,
                    Seder = seder,
                    Perut = perut,
                    Important = important
                };

                _context.AccountActions.Add(action);
            }

            await _context.SaveChangesAsync();
        }
    }
}
