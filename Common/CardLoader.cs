using System.Text.Json;

namespace Common;

public static class CardLoader
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static CardCatalog LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var catalog = JsonSerializer.Deserialize<CardCatalog>(json, Opts)
                     ?? throw new Exception("Failed to parse cards.json");
        return catalog;
    }
}
