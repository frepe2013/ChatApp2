using ChatApp2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatApp2.Pages
{
    public class TextQAModel : PageModel
    {
        private readonly ITextAnswerService _qa;

        public TextQAModel(ITextAnswerService qa)
        {
            _qa = qa;
        }

        [BindProperty]
        public string? SourceText { get; set; }

        [BindProperty]
        public string? Question { get; set; }

        public string[] Answers { get; private set; } = Array.Empty<string>();

        public void OnGet()
        {
        }

        public void OnPost()
        {
            if (string.IsNullOrWhiteSpace(SourceText) || string.IsNullOrWhiteSpace(Question))
            {
                ModelState.AddModelError(string.Empty, "テキストと質問を入力してください。");
                return;
            }

            Answers = _qa.AnswerFromText(SourceText!, Question!, maxSentences: 3);
        }
    }
}