using System.Text;

namespace ChatApp2.Services
{
    public static class AIClientExtensions
    {
        // 質問に追加文脈を埋め込み、既存AskAsyncをそのまま利用
        public static Task<string?> AskWithContextAsync(
            this IAIClient client,
            string? question,
            string? context,
            CancellationToken cancellationToken = default)
        {
            var q = (question ?? string.Empty).Trim();
            var c = (context ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(c))
            {
                return client.AskAsync(q, cancellationToken);
            }

            // 過剰な長文対策（必要に応じ調整）
            const int maxContextChars = 8000;
            if (c.Length > maxContextChars)
            {
                c = c[..maxContextChars];
            }

            var sb = new StringBuilder();
            sb.AppendLine("次のコンテキストを参照して厳密に回答してください。");
            sb.AppendLine("=== コンテキスト開始 ===");
            sb.AppendLine(c);
            sb.AppendLine("=== コンテキスト終了 ===");
            sb.AppendLine();
            sb.Append("質問: ");
            sb.Append(q);

            return client.AskAsync(sb.ToString(), cancellationToken);
        }
    }
}