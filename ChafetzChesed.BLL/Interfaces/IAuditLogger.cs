using System.Threading.Tasks;
using ChafetzChesed.DAL.Entities;

public interface IAuditLogger
{
    Task LogAsync(AuditLog entry);
    string BuildChanges(object? before, object? after, params string[] fieldsWhitelist);
}
