using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Application.Services
{
    internal class DbConflictHelper
    {
        public static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
            ex.InnerException?.Message.Contains("2601") == true
            || ex.InnerException?.Message.Contains("2627") == true
            || ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true;
    }
}
