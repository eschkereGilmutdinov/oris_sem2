using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientWinForm
{
    static class CardImageLoader
    {
        private static readonly Dictionary<string, Image> Cache = new();
        private static readonly Dictionary<string, string> ImageByCardId = new();
        private static bool _catalogLoaded;

        private static void EnsureCatalogLoaded()
        {
            if (_catalogLoaded) return;

            var baseDir = AppContext.BaseDirectory;
            var catalogPath = Path.Combine(baseDir, "Assets", "cards.json");

            var catalog = CardLoader.LoadFromFile(catalogPath);
            foreach (var card in catalog.Cards)
            {
                if (!string.IsNullOrWhiteSpace(card.Id) && !string.IsNullOrWhiteSpace(card.Image))
                    ImageByCardId[card.Id] = card.Image;
            }

            _catalogLoaded = true;
        }

        public static Image Load(string cardId)
        {
            EnsureCatalogLoaded();

            if (Cache.TryGetValue(cardId, out var cached))
                return cached;

            if (!ImageByCardId.TryGetValue(cardId, out var fileName))
                return MakePlaceholder(cardId, "no image in catalog");

            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "Assets", "Cards", fileName);

            if (!File.Exists(path))
                return MakePlaceholder(cardId, $"file not found: {fileName}");

            var img = Image.FromFile(path);
            Cache[cardId] = img;
            return img;
        }

        private static Image MakePlaceholder(string cardId, string reason)
        {
            var bmp = new Bitmap(100, 150);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.LightGray);
            g.DrawRectangle(Pens.Black, 0, 0, 99, 149);
            g.DrawString(cardId, SystemFonts.DefaultFont, Brushes.Black, new PointF(5, 5));
            g.DrawString(reason, SystemFonts.DefaultFont, Brushes.Black, new PointF(5, 25));
            Cache[cardId] = bmp;
            return bmp;
        }
    }
}
