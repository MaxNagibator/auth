using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Data;

public class ApplicationUser : IdentityUser
{
    public string? EmailConfirmCode { get; set; }
}

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.EmailConfirmCode)
            .HasMaxLength(64)
            .IsRequired(false);
    }
}
