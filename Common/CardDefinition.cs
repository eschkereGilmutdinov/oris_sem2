using System.Text.Json.Serialization;
using System.Text.Json;
using System.Runtime.Serialization;

namespace Common;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardType
{
    BabyUnicornCard,
    BasicUnicornCard,
    MagicalUnicornCard,
    MagicCard,
    InstantCard,
    UpgradeCard,
    DowngradeCard
}

public sealed class CardEffect
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "";

    [JsonPropertyName("timing")]
    public string Timing { get; set; } = "ON_PLAY";

    [JsonPropertyName("amount")]
    public int? Amount { get; set; }

    [JsonPropertyName("target")]
    public string? Target { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}

public sealed class CardDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public CardType Type { get; set; }

    [JsonPropertyName("rule")]
    public string Rule { get; set; } = "";

    [JsonPropertyName("image")]
    public string Image { get; set; } = "";

    [JsonPropertyName("copiesInDeck")]
    public int CopiesInDeck { get; set; } = 1;

    [JsonPropertyName("effects")]
    public List<CardEffect> Effects { get; set; } = new();

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}

public sealed class CardCatalog
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("cards")]
    public List<CardDefinition> Cards { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}