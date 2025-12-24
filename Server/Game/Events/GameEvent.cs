using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Events
{
    public enum GameEventType
    {
        CardPlayed,
        CardMoved,
        DestroyRequested,
        Destroyed,
        SacrificeRequested,
        Sacrificed,
        ReturnToHandRequested,
        ReturnedToHand,
        DrawRequested,
        Drew
    }

    public abstract record GameEvent(GameEventType Type);

    public sealed record DestroyRequestedEvent(string SourceCardId, string TargetInstanceId)
        : GameEvent(GameEventType.DestroyRequested);

    public sealed record DestroyedEvent(string SourceCardId, string TargetInstanceId)
        : GameEvent(GameEventType.Destroyed);
}
