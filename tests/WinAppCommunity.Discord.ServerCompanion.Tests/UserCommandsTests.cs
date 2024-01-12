using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using WinAppCommunity.Discord.ServerCompanion.Commands;
using WinAppCommunity.Discord.ServerCompanion.Keystore;

namespace WinAppCommunity.Discord.ServerCompanion.Tests;

[TestClass]
public class UserCommandsTests
{
    [TestMethod]
    public async Task RegisterUser()
    {
        Assert.IsNotNull(TestFixture.Client);
        
        IInteractionContext context = null!;
        IFeedbackService feedback = null!;
        UserKeystore keystore = null!;
        
        var userCommands = new UserCommands(context, feedback, keystore, TestFixture.Client);
        
        
    }
        
    [TestMethod]
    public async Task GetProfile()
    {
    }
}