using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Results;

[Group("commands")]
public class SampleCommandGroup : CommandGroup
{
    [Command("do-thing")]
    public async Task<IResult> MyCommand()
    {
        return Result.FromSuccess();
    }
}
