using Server.Game.Commands;
using Server.Game.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Engine
{
    public sealed record BeforeDecision(bool Cancel, GameCommand? ReplacedWith = null);

    public sealed class EffectEngine
    {
        private readonly Room _room;

        public EffectEngine(Room room) => _room = room;

        public BeforeDecision ProcessBefore(GameEvent ev, GameCommand original)
        {
            return new BeforeDecision(Cancel: false);
        }

        public void ProcessAfter(GameEvent ev)
        {

        }
    }
}
