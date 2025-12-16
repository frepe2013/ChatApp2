using ChatApp2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace ChatApp2.Pages
{
    public class AskModel : PageModel
    {
        private readonly IAIClient _ai;

        public AskModel(IAIClient ai)
        {
            _ai = ai;
        }

        [BindProperty]
        public string? Question { get; set; }

        [BindProperty]
        public string? ContextText { get; set; }

        [BindProperty]
        public List<IFormFile> UploadFiles { get; set; } = new();

        public string? Answer { get; private set; }

        public void OnGet()
        {
        }

        public async Task OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Question) && string.IsNullOrWhiteSpace(ContextText) && UploadFiles.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "質問または文脈テキスト/ファイルを入力してください。");
                return;
            }

            var context = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(ContextText))
            {
                context.AppendLine(ContextText!.Trim());
            }

            foreach (var file in UploadFiles)
            {
                if (file.Length == 0)
                    continue;

                // 簡易対応: .txt/.mdのみ読み込み（必要に応じ拡張）
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext is ".txt" or ".md" or ".csv" or ".log")
                {
                    using var reader = new StreamReader(file.OpenReadStream(), detectEncodingFromByteOrderMarks: true);
                    var content = await reader.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        context.AppendLine();
                        context.AppendLine($"[FILE:{file.FileName}]");
                        context.AppendLine(content.Trim());
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, $"{file.FileName}: 未対応の拡張子です（.txt/.md/.csv/.log を使用してください）。");
                }
            }

            Answer = await _ai.AskWithContextAsync(Question, context.ToString(), HttpContext.RequestAborted);
        }
    }
}