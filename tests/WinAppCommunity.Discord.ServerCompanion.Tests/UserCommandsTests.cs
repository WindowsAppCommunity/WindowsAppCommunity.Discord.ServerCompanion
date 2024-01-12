using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using WinAppCommunity.Discord.ServerCompanion.Commands;
using WinAppCommunity.Discord.ServerCompanion.Keystore;
using WinAppCommunity.Discord.ServerCompanion.Tests.Mocks;

namespace WinAppCommunity.Discord.ServerCompanion.Tests;

[TestClass]
public partial class UserCommandsTests
{
    [TestMethod]
    public async Task RegisterUser()
    {
        var workingFolder = await TestFixture.SafeCreateWorkingFolder($"{nameof(UserCommandsTests)}.{nameof(RegisterUser)}");
        
        UserKeystore keystore = new UserKeystore(workingFolder);
        IInteractionContext context = new MockInteractionContext();
        IFeedbackService feedback = new MockFeedbackService();
        
        var userCommands = new UserCommands(context, feedback, keystore, TestFixture.Client);
        
        var result = await userCommands.RegisterUserAsync(name: "userA", contactEmail: "test.ing@example.com");

        Assert.IsTrue(result.IsSuccess);
    }
        
    [TestMethod]
    public async Task GetProfile()
    {
    }
}