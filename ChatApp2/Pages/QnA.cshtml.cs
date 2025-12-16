using ChatApp2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatApp2.Pages
{
    public sealed class QnAModel : PageModel
    {
        private readonly IAIClient _ai;

        public QnAModel(IAIClient ai)
        {
            _ai = ai;
        }

        [BindProperty(SupportsGet = true)]
        public string? Question { get; set; }

        public string? Answer { get; private set; }

        public async Task OnGetAsync()
        {
            if (!string.IsNullOrWhiteSpace(Question))
            {
                Answer = await _ai.AskAsync(Question, HttpContext.RequestAborted);
            }
        }

        public async Task<IActionResult> OnPostAskAsync()
        {
            Answer = await _ai.AskAsync(Question, HttpContext.RequestAborted);
            return Page();
        }
    }
}