using dotenv.net;
using SimpleWebAppReact.Services;
using Scalar.AspNetCore; // Added for the modern API UI

// Load .env into environment variables before configuration is built,
// so entries like ConnectionStrings__DbConnection override appsettings.json
DotEnv.Load(new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 6));

const string CorsPolicy = "AllowAll";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Built-in .NET OpenAPI document generation package
builder.Services.AddOpenApi();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// The Expo web and native clients call the API from other origins
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddHttpClient<BuildingOutlineService>();
builder.Services.AddHttpClient<GoogleMapsService>();
builder.Services.AddSingleton<PasswordHasherService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Exposes the OpenAPI v1 spec at: /openapi/v1.json
    app.MapOpenApi();

    // Renders the modern, interactive document UI at: /scalar
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("My API v1")
               .WithTheme(ScalarTheme.DeepSpace); // Optional: Customize your theme
    });
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors(CorsPolicy);
app.MapControllers();

app.Run();
