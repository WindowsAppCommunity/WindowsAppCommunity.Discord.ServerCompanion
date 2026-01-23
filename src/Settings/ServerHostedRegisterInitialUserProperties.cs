namespace WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// A record of the initial properties that the end user can set through the optional parameters on Discord slash commands. 
/// </summary>
public record struct ServerHostedRegisterInitialUserProperties(string DisplayName);