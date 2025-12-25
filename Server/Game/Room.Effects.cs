using Common;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace Server.Game
{
    public partial class Room
    {
        private readonly Random _rng = new Random();

        private T ChooseRandomOrDefault<T>(IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
                return default!;

            var index = _rng.Next(items.Count);
            return items[index];
        }

        private async Task ApplyOnPlayEffectsAsync(string playerId, CardInstance instance)
        {
            if (!_cardById.TryGetValue(instance.CardId, out var def))
                return;

            foreach (var effect in def.Effects)
            {
                var kind = (effect.Kind ?? string.Empty).ToUpperInvariant();
                var timing = effect.Timing?.ToUpperInvariant() ?? "ON_PLAY";
                if (timing != "ON_PLAY")
                    continue;

                switch (kind)
                {
                    case "DRAW_SEARCH":
                        await ApplyDrawSearchAsync(playerId, def, effect);
                        break;

                    case "STEAL_BOARD":
                        await ApplyStealBoardAsync(playerId, def, effect);
                        break;

                    case "STEAL_HAND":
                        await ApplyStealHandAsync(playerId, def, effect);
                        break;

                    case "DESTROY_SACRIFICE":
                        await ApplyDestroySacrificeAsync(playerId, def, effect);
                        break;

                    case "DISCARD":
                        await ApplyDiscardEffectAsync(playerId, def, effect);
                        break;

                    case "GLOBAL_RESET":
                        await ApplyGlobalResetAsync(playerId, def, effect);
                        break;

                    case "RULE_MOD":
                        break;

                    case "COUNTER":
                        await ApplyCounterAsync(playerId, def, effect);
                        break;

                    case "PROTECT":
                        break;

                    default:
                        break;
                }
            }
        }

        private Task ApplyDrawSearchAsync(string playerId, CardDefinition sourceCard, CardEffect effect)
        {
            var criteria = effect.GetCriteria();
            if (criteria is null)
            {
                int count = effect.Amount ?? 1;
                DrawFromMainDeck(playerId, count);
                return Task.CompletedTask;
            }

            var c = criteria.Value;
            var fromZone = c.GetString("fromZone") ?? "DECK";
            var cardsMode = c.GetString("cards");
            var cardName = c.GetString("cardName");
            var cardTypeStr = c.GetString("cardType");

            if (fromZone.Equals("DISCARD", StringComparison.OrdinalIgnoreCase))
            {
                var candidates = _discardPile.ToList();

                if (!string.IsNullOrWhiteSpace(cardName))
                {
                    candidates = candidates
                        .Where(ci => _cardById.TryGetValue(ci.CardId, out var cd) &&
                                     string.Equals(cd.Name, cardName, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(cardTypeStr))
                {
                    candidates = candidates
                        .Where(ci => _cardById.TryGetValue(ci.CardId, out var cd) &&
                                     CardMatchesType(cd, cardTypeStr))
                        .ToList();
                }

                var picked = ChooseRandomOrDefault(candidates);
                if (picked != null)
                {
                    _discardPile.Remove(picked);
                    GiveCardToHand(playerId, picked);
                }
            }
            else
            {
                int count = effect.Amount ?? 1;
                DrawFromMainDeck(playerId, count);
            }

            return Task.CompletedTask;
        }

        private async Task ApplyStealBoardAsync(string playerId, CardDefinition sourceCard, CardEffect effect)
        {
            var criteria = effect.GetCriteria();
            if (criteria is null)
                return;

            var c = criteria.Value;
            var fromZoneRaw = c.GetString("fromZone") ?? "OTHER_PLAYER_STABLE";
            var fromZone = fromZoneRaw.ToUpperInvariant();
            var cardTypeStr = c.GetString("cardType");

            if (fromZone == "NURSERY")
            {
                var babyCandidates = _babyDeck
                    .Where(ci =>
                        cardTypeStr == null ||
                        (_cardById.TryGetValue(ci.CardId, out var cd) && CardMatchesType(cd, cardTypeStr)))
                    .ToList();

                var picked = ChooseRandomOrDefault(babyCandidates);
                if (picked == null)
                    return;

                _babyDeck.Remove(picked);

                if (!_stalls.TryGetValue(playerId, out var myStallNursery))
                    _stalls[playerId] = myStallNursery = new List<CardInstance>();

                myStallNursery.Add(picked);

                await BroadcastStallAsync(playerId);
                await BroadcastStateAsync();
                return;
            }

            var candidates = new List<(string ownerId, CardInstance card)>();

            foreach (var (ownerId, stall) in _stalls)
            {
                if (fromZone == "OTHER_PLAYER_STABLE" && ownerId == playerId)
                    continue;

                if (fromZone == "YOUR_STABLE" && ownerId != playerId)
                    continue;

                foreach (var ci in stall)
                {
                    if (cardTypeStr == null ||
                        (_cardById.TryGetValue(ci.CardId, out var cd) && CardMatchesType(cd, cardTypeStr)))
                    {
                        candidates.Add((ownerId, ci));
                    }
                }
            }

            var target = ChooseRandomOrDefault(candidates);
            if (target.card == null)
                return;

            var (fromOwner, cardFromStall) = target;

            if (_stalls.TryGetValue(fromOwner, out var fromStall))
                fromStall.Remove(cardFromStall);

            if (!_stalls.TryGetValue(playerId, out var myStall))
                _stalls[playerId] = myStall = new List<CardInstance>();

            myStall.Add(cardFromStall);

            await BroadcastStallAsync(fromOwner);
            await BroadcastStallAsync(playerId);
            await BroadcastStateAsync();
        }

        private async Task ApplyStealHandAsync(string playerId, CardDefinition sourceCard, CardEffect effect)
        {
            var criteria = effect.GetCriteria();
            var c = criteria ?? default;
            var cardTypeStr = criteria?.GetString("cardType");
            var cardName = criteria?.GetString("cardName");
            var fromZone = (criteria?.GetString("fromZone") ?? "OTHER_PLAYER_HAND").ToUpperInvariant();

            var candidates = new List<(string ownerId, CardInstance card)>();

            foreach (var (ownerId, hand) in _hands)
            {
                if (fromZone == "OTHER_PLAYER_HAND" && ownerId == playerId)
                    continue;

                foreach (var ci in hand)
                {
                    if (!string.IsNullOrWhiteSpace(cardName))
                    {
                        if (!_cardById.TryGetValue(ci.CardId, out var cd) ||
                            !string.Equals(cd.Name, cardName, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    if (!string.IsNullOrWhiteSpace(cardTypeStr))
                    {
                        if (!_cardById.TryGetValue(ci.CardId, out var cd) || !CardMatchesType(cd, cardTypeStr))
                            continue;
                    }

                    candidates.Add((ownerId, ci));
                }
            }

            var target = ChooseRandomOrDefault(candidates);
            if (target.card == null)
                return;

            var (fromOwner, card) = target;

            if (_hands.TryGetValue(fromOwner, out var fromHand))
                fromHand.Remove(card);

            GiveCardToHand(playerId, card);

            await BroadcastHandAsync(fromOwner);
            await BroadcastHandAsync(playerId);
        }

        private Task ApplyDestroySacrificeAsync(string playerId, CardDefinition sourceCard, CardEffect effect)
        {
            var criteria = effect.GetCriteria();
            if (criteria is null)
                return Task.CompletedTask;

            var c = criteria.Value;
            var fromZone = (c.GetString("fromZone") ?? "STABLE").ToUpperInvariant();
            var cardTypeStr = c.GetString("cardType");

            var candidates = new List<(string ownerId, CardInstance card)>();

            foreach (var (ownerId, stall) in _stalls)
            {
                if (fromZone == "OTHER_PLAYER_STABLE" && ownerId == playerId)
                    continue;

                foreach (var ci in stall)
                {
                    if (cardTypeStr == null ||
                        (_cardById.TryGetValue(ci.CardId, out var cd) && CardMatchesType(cd, cardTypeStr)))
                    {
                        candidates.Add((ownerId, ci));
                    }
                }
            }

            var target = ChooseRandomOrDefault(candidates);
            if (target.card == null)
                return Task.CompletedTask;

            var (owner, card) = target;
            DestroyCard(playerId, owner, card);

            return Task.CompletedTask;
        }

        private async Task ApplyDiscardEffectAsync(string playerId, CardDefinition sourceCard, CardEffect effect)
        {
            var criteria = effect.GetCriteria();
            var fromZone = (criteria?.GetString("fromZone") ?? "OTHER_PLAYER_HAND").ToUpperInvariant();

            var cardTypeStr = criteria?.GetString("cardType");
            var amount =
                effect.Amount
                ?? criteria?.GetInt("amount")
                ?? 1;

            for (int n = 0; n < amount; n++)
            {
                var candidates = new List<(string ownerId, CardInstance card)>();

                foreach (var (ownerId, hand) in _hands)
                {
                    if (hand.Count == 0)
                        continue;

                    if (fromZone == "OTHER_PLAYER_HAND" && ownerId == playerId)
                        continue;

                    if (fromZone == "YOUR_HAND" && ownerId != playerId)
                        continue;

                    foreach (var ci in hand)
                    {
                        if (cardTypeStr == null ||
                            (_cardById.TryGetValue(ci.CardId, out var cd) && CardMatchesType(cd, cardTypeStr)))
                        {
                            candidates.Add((ownerId, ci));
                        }
                    }
                }

                var target = ChooseRandomOrDefault(candidates);
                if (target.card == null)
                    break;

                var (owner, card) = target;

                if (_hands.TryGetValue(owner, out var targetHand))
                    targetHand.Remove(card);

                _discardPile.Add(card);

                await BroadcastHandAsync(owner);
            }
        }

        private async Task ApplyGlobalResetAsync(string playerId, CardDefinition sourceCard, CardEffect effect)
        {
            foreach (var (ownerId, stall) in _stalls.ToList())
            {
                if (stall.Count == 0)
                    continue;

                var idx = _rng.Next(stall.Count);
                var cardToSacrifice = stall[idx];

                DestroyCard(sourcePlayerId: playerId, targetPlayerId: ownerId, target: cardToSacrifice);
            }

            foreach (var (ownerId, hand) in _hands.ToList())
            {
                if (hand.Count == 0)
                    continue;

                var cardsToDiscard = hand.ToList();
                hand.Clear();

                foreach (var card in cardsToDiscard)
                {
                    _discardPile.Add(card);
                }
            }

            if (_discardPile.Count > 0)
            {
                _mainDeck.AddRange(_discardPile);
                _discardPile.Clear();

                for (int i = _mainDeck.Count - 1; i > 0; i--)
                {
                    int j = _rng.Next(i + 1);
                    (_mainDeck[i], _mainDeck[j]) = (_mainDeck[j], _mainDeck[i]);
                }
            }

            var allPlayerIds = new HashSet<string>(
                _hands.Keys.Concat(_stalls.Keys),
                StringComparer.Ordinal
            );

            foreach (var pid in allPlayerIds)
            {
                DrawFromMainDeck(pid, 5);
            }

            foreach (var pid in allPlayerIds)
            {
                await BroadcastHandAsync(pid);
                await BroadcastStallAsync(pid);
            }

            await BroadcastStateAsync();
        }

        private async Task ApplyCounterAsync(string playerId, CardDefinition sourceCard, CardEffect effect)
        {
            var criteria = effect.GetCriteria();
            string? cardTypeStr = null;

            if (criteria is not null)
            {
                var c = criteria.Value;
                cardTypeStr = c.GetString("byCardType") ?? c.GetString("cardType");
            }

            var candidates = new List<(string ownerId, CardInstance card)>();

            foreach (var (ownerId, stall) in _stalls)
            {
                if (ownerId == playerId)
                    continue;

                foreach (var ci in stall)
                {
                    if (cardTypeStr == null ||
                        (_cardById.TryGetValue(ci.CardId, out var cd) && CardMatchesType(cd, cardTypeStr)))
                    {
                        candidates.Add((ownerId, ci));
                    }
                }
            }

            var target = ChooseRandomOrDefault(candidates);
            if (target.card == null)
                return;

            var (owner, card) = target;

            DestroyCard(sourcePlayerId: playerId, targetPlayerId: owner, target: card);

            await BroadcastStallAsync(owner);
            await BroadcastStateAsync();
        }

        private void DestroyCard(string sourcePlayerId, string targetPlayerId, CardInstance target)
        {
            if (TryApplyProtection(sourcePlayerId, targetPlayerId, target))
                return;

            RemoveFromAllZones(target);
            _discardPile.Add(target);
        }

        private bool TryApplyProtection(string sourcePlayerId, string targetPlayerId, CardInstance target)
        {
            if (!_cardById.TryGetValue(target.CardId, out var targetDef))
                return false;

            if (targetDef.Type == CardType.BabyUnicornCard)
            {
                RemoveFromAllZones(target);
                _babyDeck.Add(target);
                return true;
            }

            if (_stalls.TryGetValue(targetPlayerId, out var stall))
            {
                var protector = stall.FirstOrDefault(ci =>
                    ci != target &&
                    _cardById.TryGetValue(ci.CardId, out var def) &&
                    def.Effects.Any(e =>
                        (e.Kind ?? string.Empty)
                            .Equals("PROTECT", StringComparison.OrdinalIgnoreCase)));

                if (protector != null)
                {
                    RemoveFromAllZones(protector);
                    _discardPile.Add(protector);
                    return true;
                }
            }

            return false;
        }

        private void RemoveFromAllZones(CardInstance target)
        {
            _mainDeck.Remove(target);
            _babyDeck.Remove(target);
            _discardPile.Remove(target);

            foreach (var kv in _hands.ToList())
            {
                kv.Value.Remove(target);
            }

            foreach (var kv in _stalls.ToList())
            {
                kv.Value.Remove(target);
            }
        }

        private void DrawFromMainDeck(string playerId, int count)
        {
            if (count <= 0) return;

            if (!_hands.TryGetValue(playerId, out var hand))
            {
                hand = new List<CardInstance>();
                _hands[playerId] = hand;
            }

            for (int i = 0; i < count && _mainDeck.Count > 0; i++)
            {
                var top = _mainDeck[^1];
                _mainDeck.RemoveAt(_mainDeck.Count - 1);
                hand.Add(top);
            }
        }

        private void GiveCardToHand(string playerId, CardInstance card)
        {
            if (!_hands.TryGetValue(playerId, out var hand))
            {
                hand = new List<CardInstance>();
                _hands[playerId] = hand;
            }

            if (!hand.Contains(card))
                hand.Add(card);
        }

        private static bool CardMatchesType(CardDefinition def, string cardTypeStr)
        {
            if (string.IsNullOrWhiteSpace(cardTypeStr))
                return true;

            if (Enum.TryParse<CardType>(cardTypeStr, ignoreCase: true, out var enumType))
                return def.Type == enumType;

            return def.Tags != null &&
                   def.Tags.Any(t => string.Equals(t, cardTypeStr, StringComparison.OrdinalIgnoreCase));
        }

        private int GetHandLimitForPlayer(string playerId)
        {
            int limit = 7;

            if (_stalls.TryGetValue(playerId, out var stall))
            {
                foreach (var ci in stall)
                {
                    if (!_cardById.TryGetValue(ci.CardId, out var def))
                        continue;

                    foreach (var eff in def.Effects)
                    {
                        if (!string.Equals(eff.Kind, "RULE_MOD", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var crit = eff.GetCriteria();
                        if (crit is null) continue;

                        var c = crit.Value;
                        var appliesTo = (c.GetString("appliesTo") ?? "YOU").ToUpperInvariant();
                        if (appliesTo != "YOU")
                            continue;

                        var delta = c.GetInt("amount");
                        if (delta.HasValue)
                            limit += delta.Value;
                    }
                }
            }

            if (limit < 0) limit = 0;
            return limit;
        }

        private bool CanPlayCard(string playerId, CardDefinition cardDef)
        {
            if (!_stalls.TryGetValue(playerId, out var stall))
                return true;

            foreach (var ci in stall)
            {
                if (!_cardById.TryGetValue(ci.CardId, out var def))
                    continue;

                foreach (var eff in def.Effects)
                {
                    if (!string.Equals(eff.Kind, "RULE_MOD", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var crit = eff.GetCriteria();
                    if (crit is null) continue;

                    var c = crit.Value;
                    var appliesTo = (c.GetString("appliesTo") ?? "YOU").ToUpperInvariant();
                    if (appliesTo != "YOU")
                        continue;

                    if (!c.TryGetProperty("forbiddenTypes", out var ft) || ft.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var item in ft.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.String)
                            continue;

                        var typeStr = item.GetString();
                        if (string.IsNullOrWhiteSpace(typeStr))
                            continue;

                        if (Enum.TryParse<CardType>(typeStr, true, out var forbiddenType))
                        {
                            if (cardDef.Type == forbiddenType)
                                return false;
                        }
                        else
                        {
                            if (cardDef.Tags.Any(t => string.Equals(t, typeStr, StringComparison.OrdinalIgnoreCase)))
                                return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
