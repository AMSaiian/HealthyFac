using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HealthyFac.Core.Application.Companies;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;

namespace HealthyFac.Core.Infrastructure.Companies.Persistence;

/// <summary>
/// Configures entity framework mapping for the company entity.
/// </summary>
public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();

        builder.Property(company => company.Name)
            .HasMaxLength(CompanyValidationConstants.NameMaxLength)
            .IsRequired();

        builder.HasNotBlankCheck(company => company.Name);
    }
}
