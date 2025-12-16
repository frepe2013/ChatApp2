using ChatApp2.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// 任意: 専用設定ファイル（存在すれば読み込み）
builder.Configuration.AddJsonFile("appsettings.Ollama.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddRazorPages();

// Ollama 設定
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));

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
