var builder = WebApplication.CreateBuilder(args);

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

// TODO: Uncomment when JWT auth is configured:
// app.UseAuthentication();
// app.UseAuthorization();

app.MapReverseProxy();

app.Run();
