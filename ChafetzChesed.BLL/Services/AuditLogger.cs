using System.Text.Json;
using System.Reflection;
using System.Transactions;
using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.DAL.Data;
using ChafetzChesed.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace ChafetzChesed.BLL.Services
{
    public class AuditLogger : IAuditLogger
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ILogger<AuditLogger> _log;
        public static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public AuditLogger(IDbContextFactory<AppDbContext> factory, ILogger<AuditLogger> log)
        {
            _factory = factory;
            _log = log;
        }

        public async Task LogAsync(AuditLog entry)
        {
            if (entry.InstitutionId <= 0) throw new ArgumentException("InstitutionId is required", nameof(entry));
            if (string.IsNullOrWhiteSpace(entry.Entity)) throw new ArgumentException("Entity is required", nameof(entry));
            if (string.IsNullOrWhiteSpace(entry.EntityId)) throw new ArgumentException("EntityId is required", nameof(entry));
            if (string.IsNullOrWhiteSpace(entry.ChangedBy)) throw new ArgumentException("ChangedBy is required", nameof(entry));

            entry.ChangedAt = DateTime.UtcNow;

            using var scope = new TransactionScope(
                TransactionScopeOption.Suppress,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                await using var db = await _factory.CreateDbContextAsync();
                db.AuditLogs.Add(entry);
                await db.SaveChangesAsync();
                scope.Complete();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to write audit log {@Entry}", entry);
                throw;
            }
        }

        public string BuildChanges(object? before, object? after, params string[] fieldsWhitelist)
        {
            var allow = fieldsWhitelist?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new();
            var beforeDict = ToDict(before, allow);
            var afterDict = ToDict(after, allow);

            var changes = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in afterDict.Keys.Union(beforeDict.Keys, StringComparer.OrdinalIgnoreCase))
            {
                beforeDict.TryGetValue(key, out var b);
                afterDict.TryGetValue(key, out var a);
                if (!EqualsNormalized(b, a))
                    changes[key] = new { before = b, after = a };
            }
            return JsonSerializer.Serialize(changes, JsonOpts); // 👈 משתמש באופציות עם Encoder
        }

        private static Dictionary<string, object?> ToDict(object? o, HashSet<string> allow)
        {
            var d = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (o == null) return d;
            foreach (var p in o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                var name = p.Name;
                if (allow.Count > 0 && !allow.Contains(name)) continue;
                d[name] = p.GetValue(o);
            }
            return d;
        }

        private static bool EqualsNormalized(object? a, object? b)
        {
            if (a is string sa) a = sa.Trim();
            if (b is string sb) b = sb.Trim();
            return Equals(a, b);
        }
    }
}
