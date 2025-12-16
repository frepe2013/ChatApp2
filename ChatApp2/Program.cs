using ChatApp2.Services;
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
        .Where(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (files.Length > 0)
    {
        var sb = new StringBuilder();
        foreach (var f in files)
        {
            var content = File.ReadAllText(f).Trim();
            if (content.Length == 0)
                continue;
            sb.AppendLine($"[BEGIN {Path.GetFileName(f)}]");
            sb.AppendLine(content);
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
        opt.SystemPrompt = string.IsNullOrWhiteSpace(opt.SystemPrompt)
            ? combinedContext
            : $"{opt.SystemPrompt}\n\n{combinedContext}";
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
