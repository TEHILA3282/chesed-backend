using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChafetzChesed.DAL.Data;
using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Entities;
using ChafetzChesed.Common.Utilities;
using System.Text.RegularExpressions;

namespace ChafetzChesed.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        private Registration? GetCurrentUser() => HttpContext.Items["User"] as Registration;

    
        private static readonly Regex RxLoanBalance = new(
            @"יתרת\s*הלו(?:ו)?אתך",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static readonly Regex RxDepositsBalance = new(
            @"יתרת\s*כל\s*הפקדות",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static readonly Regex RxRepaymentStrict = new(
            @"(^|\s)נפרע(\s|$)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static readonly Regex RxDonation = new(
            @"תרומ",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private enum ActionKind
        {
            Unknown = 0,
            Loan = 1,
            Repayment = 2,
            Deposit = 3,
            Donation = 4
        }

        private static ActionKind ClassifyByText(AccountAction a)
        {
            var perut = (a.Perut ?? string.Empty).Trim();

            if (RxLoanBalance.IsMatch(perut)) return ActionKind.Loan;
            if (RxDepositsBalance.IsMatch(perut)) return ActionKind.Deposit;
            if (RxRepaymentStrict.IsMatch(perut)) return ActionKind.Repayment;
            if (RxDonation.IsMatch(perut)) return ActionKind.Donation;

            return ActionKind.Unknown;
        }

   
        [HttpGet("account-actions")]
        public async Task<IActionResult> GetAccountActionsForUser()
        {
            var user = GetCurrentUser();
            if (user == null)
                return Unauthorized("משתמש לא מאומת");

            var actions = await _context.AccountActions
                .AsNoTracking()
                .Where(a => a.InstitutionId == user.InstitutionId && a.Zeout == user.ID)
                .OrderBy(a => a.Seder)
                .Select(a => new AccountActionDto
                {
                    Seder = a.Seder,
                    Perut = a.Perut,
                    Important = a.Important
                })
                .ToListAsync();

            return Ok(actions);
        }

        [HttpGet("account-actions/summary/{userId}")]
        public async Task<IActionResult> GetAccountSummaryByUserId(string userId)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null || currentUser.Role != "Admin")
                return Unauthorized("גישה נדחתה");

            var actions = await _context.AccountActions
                .AsNoTracking()
                .Where(a => a.InstitutionId == currentUser.InstitutionId && a.Zeout == userId)
                .ToListAsync();

            var (totalLoans, totalRepayments, totalDeposits, totalDonations) = SumByKinds(actions);

            return Ok(new { totalLoans, totalRepayments, totalDeposits, totalDonations });
        }

        [HttpGet("messages")]
        public IActionResult GetMessages()
        {
            var user = GetCurrentUser();
            if (user == null)
                return Unauthorized("משתמש לא מחובר");

            var messages = _context.Messages
                .AsNoTracking()
                .Where(m =>
                    m.InstitutionId == user.InstitutionId &&
                    (
                        (m.Seder == 1 && (m.Zeout == null || m.Zeout == "" || m.Zeout == "0")) ||
                        (m.Seder == 7 && m.Zeout == user.ID)
                    )
                )
                .OrderByDescending(m => m.CreatedAt)
                .ToList();

            return Ok(messages);
        }

        [HttpGet("account-summary")]
        public async Task<IActionResult> GetAccountSummary()
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            var actions = await _context.AccountActions
                .AsNoTracking()
                .Where(a => a.Zeout == user.ID && a.InstitutionId == user.InstitutionId)
                .ToListAsync();

            var (totalLoans, totalRepayments, totalDeposits, totalDonations) = SumByKinds(actions);

            return Ok(new { totalLoans, totalRepayments, totalDeposits, totalDonations });
        }

        private static (decimal totalLoans, decimal totalRepayments, decimal totalDeposits, decimal totalDonations)
            SumByKinds(List<AccountAction> actions)
        {
            decimal totalLoans = 0m;
            decimal totalRepayments = 0m;
            decimal totalDeposits = 0m;
            decimal totalDonations = 0m;

            foreach (var a in actions)
            {
                var perut = a.Perut?.Trim() ?? string.Empty;
                var amount = TextParsingHelper.ExtractAmount(perut);
                if (amount <= 0m) continue;

                var kind = ClassifyByText(a);
                if (kind == ActionKind.Unknown) continue;

                switch (kind)
                {
                    case ActionKind.Loan:
                        totalLoans += amount;
                        break;
                    case ActionKind.Deposit:
                        totalDeposits += amount;
                        break;
                    case ActionKind.Repayment:
                        totalRepayments += amount;
                        break;
                    case ActionKind.Donation:
                        totalDonations += amount;
                        break;
                }
            }

            return (totalLoans, totalRepayments, totalDeposits, totalDonations);
        }
    }
}
