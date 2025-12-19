using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game
{
    public sealed record CardInstance(string InstanceId, string CardId);

    public static class DeckGenerator
    {
        public static List<CardInstance> GenerateFullDeck(Common.CardCatalog catalog, int? seed = null)
        {
            if (catalog is null) throw new ArgumentNullException(nameof(catalog));
            if (catalog.Cards is null) throw new ArgumentException("Catalog.Cards is null");

            var deck = new List<CardInstance>(capacity: Math.Max(0, catalog.Cards.Sum(c => Math.Max(0, c.CopiesInDeck))));

            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var card in catalog.Cards)
            {
                if (string.IsNullOrWhiteSpace(card.Id))
                    throw new InvalidOperationException($"Card has empty id (name='{card.Name}')");

                if (!seenIds.Add(card.Id))
                    throw new InvalidOperationException($"Duplicate card id in catalog: {card.Id}");

                if (card.CopiesInDeck < 0)
                    throw new InvalidOperationException($"Negative copiesInDeck for card {card.Id}");

                for (int i = 0; i < card.CopiesInDeck; i++)
                {
                    deck.Add(new CardInstance(
                        InstanceId: $"{card.Id}#{i + 1}",
                        CardId: card.Id
                    ));
                }
            }

            ShuffleInPlace(deck, seed);

            return deck;
        }

        private static void ShuffleInPlace<T>(IList<T> list, int? seed)
        {
            if (list.Count <= 1) return;

            if (seed.HasValue)
            {
                var rng = new Random(seed.Value);
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (list[i], list[j]) = (list[j], list[i]);
                }
                return;
            }

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
