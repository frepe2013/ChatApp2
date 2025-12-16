using ChatApp2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatApp2.Pages
{
    public sealed class QnAModel : PageModel
    {
        private readonly ITemplateRepository _repo;

        public QnAModel(ITemplateRepository repo)
        {
            _repo = repo;
        }

        [BindProperty(SupportsGet = true)]
        public string? Question { get; set; }

        public string? Answer { get; private set; }

        public void OnGet()
        {
            Answer = _repo.FindBestAnswer(Question)?.Text;
        }

        public IActionResult OnPostAsk()
        {
            Answer = _repo.FindBestAnswer(Question)?.Text;
            return Page();
        }
    }
}