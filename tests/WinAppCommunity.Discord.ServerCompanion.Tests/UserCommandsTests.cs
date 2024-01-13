using OwlCore.Storage.SystemIO;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using WinAppCommunity.Discord.ServerCompanion.Commands;
using WinAppCommunity.Discord.ServerCompanion.Commands.Errors;
using WinAppCommunity.Discord.ServerCompanion.Keystore;
using WinAppCommunity.Discord.ServerCompanion.Tests.Mocks;

namespace WinAppCommunity.Discord.ServerCompanion.Tests;

[TestClass]
public partial class UserCommandsTests
{
    private static UserCommands? _userCommands;

    [ClassInitialize]
    public static async Task Setup(TestContext context)
    {
        Assert.IsNotNull(context.DeploymentDirectory);
        var workingFolder = await TestFixture.SafeCreateWorkingFolder(new SystemFolder(context.DeploymentDirectory), $"{nameof(UserCommandsTests)}.{nameof(RegisterUser)}");

        UserKeystore keystore = new UserKeystore(workingFolder);

        var user = new MockUser { ID = Snowflake.CreateTimestampSnowflake(DateTime.UtcNow) };
        var bot = new MockUser { ID = Snowflake.CreateTimestampSnowflake(DateTime.UtcNow + TimeSpan.FromSeconds(1)) };

        var interaction = new MockInteraction { User = user, Member = new MockGuildMember { User = user } };
        IInteractionContext interactionContext = new MockInteractionContext { Interaction = interaction };

        IFeedbackService feedback = new MockFeedbackService(messageAuthor: user);

        _userCommands = new UserCommands(interactionContext, feedback, keystore, TestFixture.Client);
    }

    [DataRow("userA", "test.ing@example.com")]
    [TestMethod]
    public async Task RegisterUser(string name, string contactEmail)
    {
        Assert.IsNotNull(_userCommands);

        var result = await _userCommands.RegisterUserAsync(name, contactEmail);
        Assert.IsTrue(result.IsSuccess, result.Error?.Message);
    }

    [TestMethod]
    public async Task GetProfileWithoutRegistration()
    {
        Assert.IsNotNull(_userCommands);

        var result = await _userCommands.GetProfileAsync();

        Assert.IsFalse(result.IsSuccess);
        Assert.IsInstanceOfType(result.Error, typeof(UserProfileNotFoundError));
    }

    [DataRow("userA", "test.ing@example.com")]
    [TestMethod]
    public async Task RegisterUserAndGetProfile(string name, string contactEmail)
    {
        Assert.IsNotNull(_userCommands);
        await RegisterUser(name, contactEmail);

        var result = await _userCommands.GetProfileAsync();

        Assert.IsTrue(result.IsSuccess, result.Error?.Message);

        // Result should contain a sent Discord message.
        var message = result switch
        {
            Result<IMessage> msg => msg.Entity,
            Result<IReadOnlyList<IMessage>> msgList => msgList.Entity.First(),
            null => throw new NullReferenceException("Returned result value was unexpectedly null"),
            _ => throw new NotSupportedException($"Type of {result.GetType()} wasn't handled here."),
        };

        // User information should be provided in via embed
        Assert.IsTrue(message.Embeds.Any(), "Expected content in message embeds, but no embeds were found.");

        // Each embed provided...
        foreach (var embed in message.Embeds)
        {
            // Should have an author
            Assert.IsNotNull(embed.Author.OrDefault());

            // Which matches the registered user.
            Assert.AreEqual(embed.Author.Value.Name, name);
        }
    }
}