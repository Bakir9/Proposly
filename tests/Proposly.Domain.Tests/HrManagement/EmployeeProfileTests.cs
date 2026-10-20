using Proposly.Domain.HrManagement.Entities;
using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.Tests.HrManagement;

public class EmployeeProfileTests
{
    private static EmployeeProfile NewProfile()
        => EmployeeProfile.Create(Guid.NewGuid(), Guid.NewGuid(), "Site engineer");

    // --- Documents ---

    [Fact]
    public void AddDocument_Contract_DemotesPreviousContract()
    {
        var profile = NewProfile();
        var first = profile.AddDocument("Contract", "contract-2021.pdf", "application/pdf", 100, "key-1");

        var second = profile.AddDocument("Contract", "contract-2026.pdf", "application/pdf", 100, "key-2");

        Assert.Equal("Previous contract", first.Category);
        Assert.Equal("Contract", second.Category);
        Assert.Single(profile.Documents, d => d.Category == "Contract");
    }

    [Fact]
    public void AddDocument_UnknownCategory_Throws()
    {
        var profile = NewProfile();
        Assert.Throws<InvalidOperationException>(
            () => profile.AddDocument("Secret", "x.pdf", "application/pdf", 1, "k"));
    }

    [Fact]
    public void RemoveDocument_Unknown_Throws()
        => Assert.Throws<InvalidOperationException>(() => NewProfile().RemoveDocument(Guid.NewGuid()));

    // --- Notes ---

    [Fact]
    public void AddNote_TrimsAndStoresTag()
    {
        var profile = NewProfile();
        var note = profile.AddNote("HR", "  Discuss 4-day week.  ");

        Assert.Equal("Discuss 4-day week.", note.Text);
        Assert.Equal("HR", note.Tag);
        Assert.False(note.Pinned);
    }

    [Fact]
    public void AddNote_EmptyText_Throws()
        => Assert.Throws<InvalidOperationException>(() => NewProfile().AddNote("HR", "   "));

    [Fact]
    public void AddNote_UnknownTag_Throws()
        => Assert.Throws<InvalidOperationException>(() => NewProfile().AddNote("Gossip", "text"));

    [Fact]
    public void SetNotePinned_TogglesFlag()
    {
        var profile = NewProfile();
        var note = profile.AddNote("General", "Prefers Mondays at home.");

        profile.SetNotePinned(note.Id, true);
        Assert.True(note.Pinned);

        profile.SetNotePinned(note.Id, false);
        Assert.False(note.Pinned);
    }

    // --- Allowances ---

    [Fact]
    public void ReplaceAllowances_SwapsTheWholeList()
    {
        var profile = NewProfile();
        profile.ReplaceAllowances([("Meal voucher", new Money(8, "EUR"), "per day")]);

        profile.ReplaceAllowances([
            ("Travel allowance", new Money(0.5m, "EUR"), "per km"),
            ("Site bonus", new Money(12, "EUR"), "per day"),
        ]);

        Assert.Equal(2, profile.Allowances.Count);
        Assert.DoesNotContain(profile.Allowances, a => a.Name == "Meal voucher");
    }

    [Fact]
    public void ReplaceAllowances_UnknownUnit_Throws()
        => Assert.Throws<InvalidOperationException>(() =>
            NewProfile().ReplaceAllowances([("Bonus", new Money(1, "EUR"), "per sprint")]));
}
