using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientWinForm
{
    static class CardRuleLoader
    {
        private static readonly Dictionary<string, string> RuleByCardId = new();
        private static bool _loaded;

        private static void EnsureLoaded()
        {
            if (_loaded) return;

            var baseDir = AppContext.BaseDirectory;
            var catalogPath = Path.Combine(baseDir, "Assets", "cards.json");

            var catalog = CardLoader.LoadFromFile(catalogPath);

            foreach (var card in catalog.Cards)
            {
                if (!string.IsNullOrWhiteSpace(card.Id))
                    RuleByCardId[card.Id] = card.Rule ?? "";
            }

            _loaded = true;
        }

        public static string GetRule(string cardId)
        {
            EnsureLoaded();
            return RuleByCardId.TryGetValue(cardId, out var rule) && !string.IsNullOrWhiteSpace(rule)
                ? rule
                : "(правило не найдено)";
        }
    }
}
