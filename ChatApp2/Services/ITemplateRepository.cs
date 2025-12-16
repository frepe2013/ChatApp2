namespace ChatApp2.Services
{
    public interface ITemplateRepository
    {
        IReadOnlyList<TemplateItem> GetAll();
        IReadOnlyList<TemplateItem> Search(string? question);
        TemplateItem? FindBestAnswer(string? question);
    }

    public sealed class TemplateItem
    {
        public TemplateItem(string key, string text)
        {
            Key = key;
            Text = text;
        }

        public string Key { get; }
        public string Text { get; }
    }
}