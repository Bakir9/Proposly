using Proposly.Shared.Exceptions;

namespace Proposly.Domain.Tests.Shared;

public class AppExceptionTests
{
    [Fact]
    public void Carries_code_message_and_default_status()
    {
        var ex = new AppException("offer.locked", "Offer items are locked after sending.");

        Assert.Equal("offer.locked", ex.Code);
        Assert.Equal("Offer items are locked after sending.", ex.Message);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public void Factories_set_the_matching_status_codes()
    {
        Assert.Equal(404, AppException.NotFound("project.not_found", "Project not found.").StatusCode);
        Assert.Equal(403, AppException.Forbidden("plan.module_required", "Not on this plan.").StatusCode);
        Assert.Equal(409, AppException.Conflict("email.already_registered", "Already registered.").StatusCode);
    }

    [Fact]
    public void Is_an_InvalidOperationException_so_existing_handlers_and_tests_keep_working()
    {
        // The global exception handler's legacy 400 branch and older catch blocks match on
        // InvalidOperationException; AppException must stay substitutable for it.
        Assert.IsAssignableFrom<InvalidOperationException>(AppException.NotFound("x.not_found", "x"));
    }
}
