using PriorityGear.Contracts;

namespace PriorityGear.App.Tests;

public sealed class ServicePipeContractTests
{
    [Fact]
    public void StatusAndAdminPipes_AreSeparate()
    {
        Assert.Equal("PriorityGear.Service.Status.v0", ServiceContractConstants.StatusPipeName);
        Assert.Equal("PriorityGear.Service.Admin.v0", ServiceContractConstants.AdminPipeName);
        Assert.NotEqual(ServiceContractConstants.StatusPipeName, ServiceContractConstants.AdminPipeName);
    }

    [Fact]
    public void TestApplyPriority_IsDiagnosticAdminCommand()
    {
        Assert.Equal(2, (int)ServiceCommandKind.TestApplyPriority);
    }
}
