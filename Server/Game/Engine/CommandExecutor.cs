using Server.Game.Commands;
using Server.Game.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Engine
{
    public sealed class CommandExecutor
    {
        private readonly Room _room;
        private readonly EffectEngine _effects;

        public CommandExecutor(Room room, EffectEngine effects)
        {
            _room = room;
            _effects = effects;
        }

        public void Execute(GameCommand cmd)
        {
            var before = ToBeforeEvent(cmd);
            var decision = _effects.ProcessBefore(before, cmd);
            if (decision.Cancel) return;
            cmd = decision.ReplacedWith ?? cmd;

            Apply(cmd);

            var after = ToAfterEvent(cmd);
            _effects.ProcessAfter(after);
        }

        private GameEvent ToBeforeEvent(GameCommand cmd) => cmd switch
        {
            DestroyCommand d => new DestroyRequestedEvent(d.SourceCardId, d.TargetInstanceId),
            _ => throw new NotSupportedException(cmd.GetType().Name)
        };

        private GameEvent ToAfterEvent(GameCommand cmd) => cmd switch
        {
            DestroyCommand d => new DestroyedEvent(d.SourceCardId, d.TargetInstanceId),
            _ => throw new NotSupportedException(cmd.GetType().Name)
        };

        private void Apply(GameCommand cmd)
        {
            switch (cmd)
            {
                case DestroyCommand d:
                    _room.State.DestroyInstance(d.TargetInstanceId);
                    break;

                case DrawCommand dr:
                    _room.State.Draw(dr.PlayerId, dr.Amount);
                    break;

                case MoveCardCommand mv:
                    _room.State.Move(mv.InstanceId, mv.FromZone, mv.ToZone);
                    break;

                default: throw new NotSupportedException(cmd.GetType().Name);
            }
        }
    }
}
