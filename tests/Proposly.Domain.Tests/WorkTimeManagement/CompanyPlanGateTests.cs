using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Enums;

namespace Proposly.Domain.Tests.WorkTimeManagement;

/// <summary>
/// Company.HasWorkTimeModule() gates writes in the WorkTimeManagement module. It must follow the
/// same expired-plan fallback as the existing limit checks.
/// </summary>
public class CompanyPlanGateTests
{
    private static Company WithPlan(PlanTier tier, DateTime? expiresAt = null)
    {
        var company = Company.Create("Test GmbH");
        company.SetPlan(tier, maxUsers: null, maxProjects: null, expiresAt: expiresAt);
        return company;
    }

    [Theory]
    [InlineData(PlanTier.Pro)]
    [InlineData(PlanTier.Business)]
    public void Module_is_available_on_pro_and_business(PlanTier tier)
    {
        Assert.True(WithPlan(tier).HasWorkTimeModule());
    }

    [Theory]
    [InlineData(PlanTier.Free)]
    [InlineData(PlanTier.Starter)]
    public void Module_is_unavailable_on_free_and_starter(PlanTier tier)
    {
        Assert.False(WithPlan(tier).HasWorkTimeModule());
    }

    [Fact]
    public void Expired_pro_plan_falls_back_to_free_and_loses_the_module()
    {
        var company = WithPlan(PlanTier.Pro, expiresAt: DateTime.UtcNow.AddDays(-1));

        Assert.False(company.HasWorkTimeModule());
    }

    [Fact]
    public void Unexpired_pro_plan_keeps_the_module()
    {
        var company = WithPlan(PlanTier.Pro, expiresAt: DateTime.UtcNow.AddDays(30));

        Assert.True(company.HasWorkTimeModule());
    }
}
