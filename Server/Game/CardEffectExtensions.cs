using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Server.Game
{
    public static class CardEffectExtensions
    {
        public static JsonElement? GetCriteria(this CardEffect effect)
        {
            if (effect.Extra != null &&
                effect.Extra.TryGetValue("criteria", out var el) &&
                el.ValueKind == JsonValueKind.Object)
            {
                return el;
            }

            return null;
        }

        public static string? GetString(this JsonElement el, string prop)
            => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
               ? v.GetString()
               : null;

        public static int? GetInt(this JsonElement el, string prop)
            => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number
               ? v.GetInt32()
               : (int?)null;
    }
}
