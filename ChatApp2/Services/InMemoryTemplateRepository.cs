namespace ChatApp2.Services
{
    public sealed class InMemoryTemplateRepository : ITemplateRepository
    {
        private static readonly List<TemplateItem> Items =
        [
            new("greeting", "お問い合わせありがとうございます。順次対応いたします。"),
            new("delay", "回答に少々お時間をいただいております。今しばらくお待ちください。"),
            new("closing", "ご不明点があればお気軽にお知らせください。よろしくお願いいたします。"),
            new("refund", "返金手続きについてはサポート窓口までご連絡ください。"),
            new("password_reset", "パスワードリセットの手順をメールでお送りしました。ご確認ください。")
        ];

        public IReadOnlyList<TemplateItem> GetAll()
        {
            return Items;
        }

        public IReadOnlyList<TemplateItem> Search(string? question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return Items;
            }

            var q = question.Trim();
            return Items
                .Where(x =>
                    x.Key.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    x.Text.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public TemplateItem? FindBestAnswer(string? question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                // 未入力時は汎用の挨拶を返す
                return Items.FirstOrDefault(x => x.Key == "greeting") ?? Items.FirstOrDefault();
            }

            var q = question.Trim();

            // 1) キー完全一致
            var exactKey = Items.FirstOrDefault(x => x.Key.Equals(q, StringComparison.OrdinalIgnoreCase));
            if (exactKey != null)
            {
                return exactKey;
            }

            // 2) テキスト部分一致で最短マッチ（より特化した文を優先）
            var containsMatches = Items
                .Where(x => x.Text.Contains(q, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Text.Length)
                .ToList();

            if (containsMatches.Count > 0)
            {
                return containsMatches[0];
            }

            // 3) キー部分一致
            var keyContains = Items
                .Where(x => x.Key.Contains(q, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Key.Length)
                .FirstOrDefault();

            // 4) 何もなければ汎用の挨拶
            return keyContains ?? Items.FirstOrDefault(x => x.Key == "greeting") ?? Items.FirstOrDefault();
        }
    }
}