using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using quanlyfilesBE.Data;
using quanlyfilesBE.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "QuanLyFiles API",
        Version = "v1",
        Description = "API for QuanLyFiles application"
    });
});

// Add Entity Framework - SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add CORS - Allow Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200") // Angular default port
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Note: Using Swashbuckle (AddSwaggerGen) instead of AddOpenApi to avoid conflicts

// JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = signingKey,
        ClockSkew = TimeSpan.FromMinutes(1),
        // Map role claims correctly
        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        NameClaimType = System.Security.Claims.ClaimTypes.Name
    };
    
    // Add event handlers for debugging
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception?.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var claims = context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}");
            Console.WriteLine($"Token validated. Claims: {string.Join(", ", claims ?? Array.Empty<string>())}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"Challenge: {context.Error}, {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Register Firebase Service
builder.Services.AddScoped<quanlyfilesBE.Services.IFirebaseService, quanlyfilesBE.Services.FirebaseService>();

// Configure FileStorage options
builder.Services.Configure<FileStorageOptions>(
    builder.Configuration.GetSection(FileStorageOptions.SectionName));

var app = builder.Build();

// Configure the HTTP request pipeline.
// Swagger should be enabled early in the pipeline, before authentication
// Enable Swagger in Development environment
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuanLyFiles API V1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
});

// Enable CORS - must be before UseHttpsRedirection
app.UseCors("AllowAngularApp");

// HTTPS Redirection
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Add root route
app.MapGet("/", () => {
    if (app.Environment.IsDevelopment())
    {
        return Results.Redirect("/swagger");
    }
    return Results.Json(new { 
        message = "QuanLyFiles API", 
        version = "1.0",
        endpoints = new[] { "/swagger", "/api" }
    });
});

// Test database connection endpoint
app.MapGet("/api/test-db", async (ApplicationDbContext db) => {
    try
    {
        var connectionString = db.Database.GetConnectionString();
        var canConnect = await db.Database.CanConnectAsync();
        var providerName = db.Database.ProviderName;
        
        if (canConnect)
        {
            // Get basic connection info - must open connection first
            var connection = db.Database.GetDbConnection();
            string? databaseName = null;
            string? serverVersion = null;
            int tableCount = 0;
            List<string> tableNames = new();
            
            try
            {
                // Open connection to get connection info
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await db.Database.OpenConnectionAsync();
                }
                
                // Now we can safely access connection properties
                databaseName = connection.Database;
                serverVersion = connection.ServerVersion?.ToString();
                
                // Get table count and names - SQL Server syntax
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = 'dbo' 
                    AND TABLE_TYPE = 'BASE TABLE'
                    ORDER BY TABLE_NAME";
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var tableName = reader.GetString(0);
                    tableNames.Add(tableName);
                }
                tableCount = tableNames.Count;
            }
            catch (Exception queryEx)
            {
                // If query fails, just return basic info
                Console.WriteLine($"Query error: {queryEx.Message}");
            }
            finally
            {
                // Close connection if we opened it
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    await db.Database.CloseConnectionAsync();
                }
            }
            
            // Test a simple query to verify database is working
            bool canQuery = false;
            try
            {
                var userCount = await db.Users.CountAsync();
                canQuery = true;
            }
            catch { }
            
            return Results.Ok(new
            {
                success = true,
                message = "Database connection successful",
                provider = providerName,
                databaseName = databaseName ?? "Unknown",
                serverVersion = serverVersion ?? "Unknown",
                tableCount = tableCount,
                tables = tableNames,
                canQuery = canQuery,
                connectionString = connectionString?.Replace("Password=Ab!123456", "Password=***") // Hide password
            });
        }
        else
        {
            return Results.Problem(
                title: "Database Connection Failed",
                detail: "Cannot connect to database",
                statusCode: 500
            );
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Database Connection Error",
            detail: $"{ex.Message}\n\nStack Trace: {ex.StackTrace}",
            statusCode: 500
        );
    }
});

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.Run();
