using ChatApp2.Services;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 任意: 専用設定ファイル（存在すれば読み込み）
builder.Configuration.AddJsonFile("appsettings.Ollama.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddRazorPages();

// Ollama 設定
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));

// 恒常的な文脈ファイルを読み込み、SystemPrompt に結合
var contextDir = Path.Combine(builder.Environment.ContentRootPath, "Context");
string? combinedContext = null;
if (Directory.Exists(contextDir))
{
    var files = Directory.EnumerateFiles(contextDir, "*.*", SearchOption.TopDirectoryOnly)
        .Where(f =>
            f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
            || f.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            || f.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (files.Length > 0)
    {
        var sb = new StringBuilder();
        foreach (var f in files)
        {
            string content = f.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)
                ? ReadDocxBodyText(f).Trim()
                : File.ReadAllText(f).Trim();

            if (content.Length == 0) continue;

            // 軽量要約（見出し行＋先頭数百文字）
            var summary = SummarizeLightweight(content, maxCharsPerFile: 1500);

            sb.AppendLine($"[BEGIN {Path.GetFileName(f)}]");
            sb.AppendLine(summary);
            sb.AppendLine($"[END {Path.GetFileName(f)}]");
            sb.AppendLine();
        }
        combinedContext = sb.ToString().Trim();
    }
}

// SystemPrompt へ後置で反映（設定と併用可）
builder.Services.PostConfigure<OllamaOptions>(opt =>
{
    if (!string.IsNullOrWhiteSpace(combinedContext))
    {
        var merged = string.IsNullOrWhiteSpace(opt.SystemPrompt)
            ? combinedContext
            : $"{opt.SystemPrompt}\n\n{combinedContext}";

        // 設定上限（既定 10000 文字）で安全に切り詰め
        var max = opt.MaxSystemPromptChars <= 0 ? 10000 : opt.MaxSystemPromptChars;
        opt.SystemPrompt = merged.Length > max ? merged[..max] : merged;
    }
});

// Typed HttpClient 登録（BaseUrl/Timeout は設定から）
builder.Services.AddHttpClient<IAIClient, OllamaClient>((sp, http) =>
{
    var opt = sp.GetRequiredService<IOptionsMonitor<OllamaOptions>>().CurrentValue;
    var baseUrl = string.IsNullOrWhiteSpace(opt.BaseUrl) ? "http://localhost:11434" : opt.BaseUrl!;
    http.BaseAddress = new Uri(baseUrl);
    http.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds <= 0 ? 60 : opt.TimeoutSeconds);
});

// テキストQAサービス
builder.Services.AddSingleton<ITextAnswerService, TextAnswerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();

// --- docx本文抽出（Open XML SDK） ---
static string ReadDocxBodyText(string filePath)
{
    var sb = new StringBuilder();

    using var doc = WordprocessingDocument.Open(filePath, false);
    var body = doc.MainDocumentPart?.Document?.Body;
    if (body is null)
        return string.Empty;

    foreach (var element in body.Elements())
    {
        switch (element)
        {
            case Paragraph p:
                var line = string.Concat(p.Descendants<Text>().Select(t => t.Text));
                sb.AppendLine(line);
                break;

            case Table table:
                foreach (var row in table.Descendants<TableRow>())
                {
                    var cells = row.Descendants<TableCell>()
                        .Select(c => string.Join(" ", c.Descendants<Paragraph>()
                            .Select(pg => string.Concat(pg.Descendants<Text>().Select(t => t.Text)).Trim())))
                        .ToArray();
                    if (cells.Length > 0)
                    {
                        sb.AppendLine(string.Join("\t", cells));
                    }
                }
                sb.AppendLine();
                break;

            default:
                break;
        }
    }

    return sb.ToString().Trim();
}

// --- 簡易要約：見出し抽出＋先頭抜粋 ---
static string SummarizeLightweight(string content, int maxCharsPerFile)
{
    // 見出し（Markdownの#）と最初の数段落を優先
    var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
    var sb = new StringBuilder();
    foreach (var line in lines)
    {
        if (line.TrimStart().StartsWith("#"))
            sb.AppendLine(line.Trim());
        if (sb.Length > 2000)
            break;
    }

    if (sb.Length < 500)
    {
        // 先頭抜粋（段落単位）
        var paragraphs = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in paragraphs.Take(5))
        {
            sb.AppendLine(p.Trim());
            if (sb.Length > 2000)
                break;
        }
    }

    var summary = sb.Length > 0 ? sb.ToString().Trim() : content;
    return summary.Length > maxCharsPerFile ? summary[..maxCharsPerFile] : summary;
}