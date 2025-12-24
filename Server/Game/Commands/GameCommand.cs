using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Commands
{
    public abstract record GameCommand;

    public sealed record DestroyCommand(string SourceCardId, string TargetInstanceId) : GameCommand;
    public sealed record DrawCommand(string PlayerId, string SourceCardId, int Amount) : GameCommand;
    public sealed record MoveCardCommand(string InstanceId, string FromZone, string ToZone) : GameCommand;
}
