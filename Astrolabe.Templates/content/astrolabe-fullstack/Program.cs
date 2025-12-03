using System.Reflection;
using Astrolabe.JSON.Extensions;
using Astrolabe.Web.Common;
using AstrolabeApp.Data.EF;
using AstrolabeApp.Exceptions;
//#if (IncludeDemoData || IncludeLocalUsers)
using AstrolabeApp.Models;
//#endif
using AstrolabeApp.Services;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
//#if (IncludeOrleans)
using Orleans.Configuration;
//#endif
//#if (IncludeLocalUsers)
using System.Text;
using Astrolabe.LocalUsers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
//#endif

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<FormService>();
//#if (IncludeDemoData)
builder.Services.AddScoped<TeaService>();
//#endif

//#if (IncludeLocalUsers)
// Configure Local User Authentication
var passwordSalt = builder.Configuration["Auth:PasswordSalt"]
    ?? throw new InvalidOperationException("Auth:PasswordSalt is not configured. Run the setup script or add it to appsettings.json.");
builder.Services.AddSingleton<IPasswordHasher>(new SaltedSha256PasswordHasher(passwordSalt));
builder.Services.AddScoped<ILocalUserService<NewUser, Guid>, LocalUserService>();
builder.Services.AddScoped<LocalUserService>();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "DefaultDevKeyThatShouldBeChanged123!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AstrolabeApp";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AstrolabeApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});
//#endif

//#if (IncludeOrleans)
// Configure Orleans Silo
builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("teaRoomStore");
    siloBuilder.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "dev";
        options.ServiceId = "AstrolabeApp";
    });
});
//#endif

// Add exception handling
builder.Services.AddExceptionHandler<ExceptionHandler>();
builder.Services.AddProblemDetails();

// Add services to the container.
builder
    .Services.AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.AddStandardOptions();
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SupportNonNullableReferenceTypes();
    c.AddServer(new OpenApiServer() { Url = "https://localhost:__HttpsPort__" });
    c.CustomOperationIds(apiDesc =>
        apiDesc.TryGetMethodInfo(out var methodInfo)
            ? $"{((ControllerActionDescriptor)apiDesc.ActionDescriptor).ControllerName}_{methodInfo.Name}"
            : null
    );
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AstrolabeApp API", Version = "v1" });
    c.UseAllOfForInheritance();
    c.UseAllOfToExtendReferenceSchemas();
});

builder.Services.AddDbContext<AppDbContext>(op =>
    op.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sqlServerOptions =>
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            )
    )
);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseRouting();
//#if (IncludeLocalUsers)
app.UseAuthentication();
//#endif
app.UseAuthorization();
#pragma warning disable ASP0014 // Suggest using top level route registrations instead of UseEndpoints
app.UseEndpoints(e => e.MapControllers());
#pragma warning restore ASP0014

if (app.Environment.IsDevelopment())
{
    app.UseSpa(b => b.UseProxyToSpaDevelopmentServer("http://localhost:__SpaPort__"));
}
else
{
    app.UseDomainSpa(app.Environment, "__SiteName__", fallback: true);
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var allowDropDb = args.Contains("--allow-drop-db");

    try
    {
        Console.WriteLine("Ensuring database exists and applying migrations...");

        // MigrateAsync will create the database if it doesn't exist and apply pending migrations
        await db.Database.MigrateAsync();
        Console.WriteLine("Database migrations applied successfully.");

        //#if (IncludeDemoData)
        // Seed database with initial data
        await AstrolabeApp.Data.DbSeeder.SeedAsync(db);
        Console.WriteLine("Database seeding completed.");
        //#endif
    }
    catch (Exception e)
    {
        Console.WriteLine($"Failed to migrate DB automatically: {e.Message}");
        Console.WriteLine($"Stack trace: {e.StackTrace}");
        if (e.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {e.InnerException.Message}");
        }
    }
}

app.Run();
