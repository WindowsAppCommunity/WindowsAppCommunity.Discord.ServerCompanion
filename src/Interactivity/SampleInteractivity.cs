﻿using Remora.Commands.Attributes;
using Remora.Discord.Interactivity;
using Remora.Results;

namespace WindowsAppCommunity.Discord.ServerCompanion.Interactivity
{
    public class MyInteractions : InteractionGroup
    {
        [Button("my-button")]
        public async Task<Result> OnButtonPressedAsync()
        {
            return Result.FromSuccess();
        }
    }
}
