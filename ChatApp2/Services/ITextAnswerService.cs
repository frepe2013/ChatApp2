using System.Text.RegularExpressions;

namespace ChatApp2.Services
{
    public interface ITextAnswerService
    {
        string[] AnswerFromText(string sourceText, string question, int maxSentences = 3);
    }
}