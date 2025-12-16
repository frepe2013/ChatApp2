using ChatApp2.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// JSONファイルのパスを決定（コンテンツとして配置）
var jsonPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "templates.json");

// Json ベースのレポジトリを DI に登録
builder.Services.AddSingleton<ITemplateRepository>(sp => new JsonTemplateRepository(jsonPath));

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
