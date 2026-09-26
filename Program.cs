var builder = WebApplication.CreateBuilder(args);

const string corsPolicyName = "TrustedFrontend";
var corsAllowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.Ordinal)
    .ToArray() ?? [];

builder.Services.AddCors(options => options.AddPolicy(corsPolicyName, policy =>
{
    policy.WithOrigins(corsAllowedOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader();
}));

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// TODO: Add JWT authentication here when an identity provider is chosen.
// Example:
// builder.Services.AddAuthentication().AddJwtBearer(options =>
// {
//     options.Authority = builder.Configuration["Authentication:Authority"];
//     options.Audience  = builder.Configuration["Authentication:Audience"];
// });
// builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors(corsPolicyName);

// The browser negotiates CORS with the public gateway. Do not forward Origin
// downstream, otherwise ForumService could add a second CORS response header.
app.Use(async (context, next) =>
{
    context.Request.Headers.Remove("Origin");
    await next();
});

// TODO: Uncomment when JWT auth is configured:
// app.UseAuthentication();
// app.UseAuthorization();

app.MapReverseProxy();

app.Run();
