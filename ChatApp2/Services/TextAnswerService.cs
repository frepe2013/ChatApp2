using System.Text.RegularExpressions;

namespace ChatApp2.Services
{
    public sealed class TextAnswerService : ITextAnswerService
    {
        public string[] AnswerFromText(string sourceText, string question, int maxSentences = 3)
        {
            sourceText = sourceText ?? string.Empty;
            question = question ?? string.Empty;
            maxSentences = Math.Max(1, Math.Min(10, maxSentences));

            var sentences = SplitSentences(sourceText);
            if (sentences.Length == 0)
            {
                return Array.Empty<string>();
            }

            var keywords = ExtractKeywords(question);
            if (keywords.Count == 0)
            {
                // 質問が空に近い場合は先頭文を返す
                return sentences.Take(maxSentences).ToArray();
            }

            var ranked = sentences
                .Select(s => new
                {
                    Sentence = s,
                    Score = ScoreSentence(s, keywords)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Sentence.Length)
                .Take(maxSentences)
                .Select(x => x.Sentence.Trim())
                .ToArray();

            return ranked.Length > 0 ? ranked : sentences.Take(maxSentences).ToArray();
        }

        private static string[] SplitSentences(string text)
        {
            // 句点や改行で分割（日本語/英語混在を想定）
            var parts = Regex.Split(text, @"(?<=[。\.\!\?])\s+|\r?\n+")
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToArray();
            return parts;
        }

        private static HashSet<string> ExtractKeywords(string question)
        {
            // 記号除去・小文字化・簡易ストップワード除去
            var cleaned = Regex.Replace(question.ToLowerInvariant(), @"[^\p{L}\p{N}\s]", " ");
            var tokens = cleaned.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var stop = new HashSet<string>(new[]
            {
                "the","a","an","of","and","or","to","in","on","for","with","is","are","was","were",
                "が","の","は","に","を","と","で","や","も","から","まで","より","へ","です","ます"
            });

            var keywords = tokens
                .Where(t => t.Length > 1 && !stop.Contains(t))
                .Select(t => Stem(t))
                .ToHashSet();

            return keywords;
        }

        private static int ScoreSentence(string sentence, HashSet<string> keywords)
        {
            var cleaned = Regex.Replace(sentence.ToLowerInvariant(), @"[^\p{L}\p{N}\s]", " ");
            var tokens = cleaned.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(Stem)
                                .ToArray();

            int hits = tokens.Count(t => keywords.Contains(t));
            if (hits == 0)
                return 0;

            // 簡易スコア: ヒット数 + 密度補正
            double density = (double)hits / Math.Max(5, tokens.Length);
            return (int)Math.Round(hits * (1.0 + density * 2.0));
        }

        // ごく簡単なステミング（英語のみ軽く対応）
        private static string Stem(string token)
        {
            if (token.Length > 4)
            {
                if (token.EndsWith("ing"))
                    return token[..^3];
                if (token.EndsWith("ed"))
                    return token[..^2];
                if (token.EndsWith("s"))
                    return token[..^1];
            }
            return token;
        }
    }
}
