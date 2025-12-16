using System.Text.Json;

namespace ChatApp2.Services
{
    public sealed class JsonTemplateRepository : ITemplateRepository
    {
        private readonly List<TemplateItem> _items;

        public JsonTemplateRepository(string jsonFilePath)
        {
            _items = Load(jsonFilePath);
        }

        public IReadOnlyList<TemplateItem> GetAll() => _items;

        public IReadOnlyList<TemplateItem> Search(string? question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return _items;
            }

            var q = question.Trim();
            return _items
                .Where(x =>
                    x.Key.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    x.Text.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public TemplateItem? FindBestAnswer(string? question)
        {
            if (_items.Count == 0)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(question))
            {
                return _items.FirstOrDefault(x => x.Key == "greeting") ?? _items.FirstOrDefault();
            }

            var q = question.Trim();

            var exactKey = _items.FirstOrDefault(x => x.Key.Equals(q, StringComparison.OrdinalIgnoreCase));
            if (exactKey != null)
            {
                return exactKey;
            }

            var containsMatches = _items
                .Where(x => x.Text.Contains(q, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Text.Length)
                .ToList();

            if (containsMatches.Count > 0)
            {
                return containsMatches[0];
            }

            var keyContains = _items
                .Where(x => x.Key.Contains(q, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Key.Length)
                .FirstOrDefault();

            return keyContains ?? _items.FirstOrDefault(x => x.Key == "greeting") ?? _items.FirstOrDefault();
        }

        private static List<TemplateItem> Load(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return new List<TemplateItem>();
                }

                using var stream = File.OpenRead(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var items = JsonSerializer.Deserialize<List<JsonTemplateItem>>(stream, options) ?? new List<JsonTemplateItem>();
                return items
                    .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Text))
                    .Select(x => new TemplateItem(x.Key!, x.Text!))
                    .ToList();
            }
            catch
            {
                // 読み込み/解析失敗時は空で継続
                return new List<TemplateItem>();
            }
        }

        private sealed class JsonTemplateItem
        {
            public string? Key { get; set; }
            public string? Text { get; set; }
        }
    }
}