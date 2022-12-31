using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Raytha.Domain.Entities;

public class User : BaseAuditableEntity, IPassivable
{   
    //meta
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoggedInTime { get; set; }
    public byte[] Salt { get; set; } = null!;
    public byte[] PasswordHash { get; set; } = null!;
    public string? SsoId { get; set; }
    public Guid? AuthenticationSchemeId { get; set; }
    public virtual AuthenticationScheme? AuthenticationScheme { get; set; }

    //base profile
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string EmailAddress { get; set; } = null!;
    public bool IsEmailAddressConfirmed { get; set; }
    public string FullName
    {
        get { return $"{FirstName} {LastName}"; }
    }

    public string? _RecentlyAccessedViews { get; set; }
    public virtual ICollection<Role> Roles { get; set; }
    public virtual ICollection<View> FavoriteViews { get; set; }
    public virtual ICollection<UserGroup> UserGroups { get; set; }

    [NotMapped]
    public IEnumerable<RecentlyAccessedView> RecentlyAccessedViews
    {
        get { return JsonSerializer.Deserialize<IEnumerable<RecentlyAccessedView>>(_RecentlyAccessedViews ?? "[]"); }
        set { _RecentlyAccessedViews = JsonSerializer.Serialize(value); }
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(new
        {
            Id, 
            FullName, 
            EmailAddress
        });
    }
}