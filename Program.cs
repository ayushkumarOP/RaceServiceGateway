var builder = WebApplication.CreateBuilder(args);

// Cloud Run environment-variable names cannot contain the hyphens used by the
// YARP cluster IDs. Map deploy-time service URLs into their YARP paths before
// the proxy reads configuration.
var downstreamServiceUrls = new Dictionary<string, string?>
{
    ["ReverseProxy:Clusters:raceservice-cluster:Destinations:raceservice-api:Address"] =
        builder.Configuration["RACE_SERVICE_URL"],
    ["ReverseProxy:Clusters:forumservice-cluster:Destinations:forumservice-api:Address"] =
        builder.Configuration["FORUM_SERVICE_URL"],
    ["ReverseProxy:Clusters:userservice-cluster:Destinations:userservice-api:Address"] =
        builder.Configuration["USER_SERVICE_URL"]
};

var configuredDownstreamServiceUrls = downstreamServiceUrls
    .Where(entry => !string.IsNullOrWhiteSpace(entry.Value))
    .ToDictionary(entry => entry.Key, entry => entry.Value);

if (configuredDownstreamServiceUrls.Count > 0)
    builder.Configuration.AddInMemoryCollection(configuredDownstreamServiceUrls);

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
