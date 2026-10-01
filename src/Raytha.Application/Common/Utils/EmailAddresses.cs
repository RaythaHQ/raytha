using Raytha.Application.Common.Interfaces;

namespace Raytha.Application.Common.Utils;

public static class EmailAddresses
{
    public static bool IsTaken(IRaythaDbContext db, string emailAddress, Guid? exceptUserId = null)
    {
        var lowered = emailAddress.Trim().ToLower();
        return db.Users.Any(user =>
            user.EmailAddress.ToLower() == lowered
            && (exceptUserId == null || user.Id != exceptUserId.Value)
        );
    }
}
