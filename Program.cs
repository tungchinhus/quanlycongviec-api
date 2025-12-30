using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using quanlyfilesBE.Data;
using quanlyfilesBE.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure file logging directory
var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
if (!Directory.Exists(logDirectory))
{
    Directory.CreateDirectory(logDirectory);
}

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Cấu hình JSON serializer để nhận camelCase từ frontend
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

// Configure multipart form data limits for file uploads
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50MB
    options.ValueLengthLimit = 52428800; // 50MB
    options.MultipartHeadersLengthLimit = 52428800; // 50MB
});
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

// Add Entity Framework - SQL Server with retry on failure for transient errors
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), 
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

// Add CORS - Allow Angular frontend
// Cấu hình CORS theo hướng dẫn: https://docs.microsoft.com/en-us/aspnet/core/security/cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localsite.thibidi.com",  // Production frontend
                "http://localhost:4200"           // Development Angular default port
            )
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS", "PATCH", "CONNECT")  // Explicit methods (CONNECT for SignalR)
            .WithHeaders(
                "Content-Type",
                "Authorization",
                "X-Requested-With",
                "Accept",
                "Origin"
            )  // Explicit headers
            .AllowCredentials()  // Cho phép gửi cookies/credentials
            .WithExposedHeaders("Authorization")  // Expose Authorization header cho frontend
            .SetPreflightMaxAge(TimeSpan.FromHours(24));  // Cache preflight requests trong 24 giờ
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

builder.Services.AddAuthorization(options =>
{
    // Policy cho phép admin bypass tất cả - Admin có full permissions
    options.AddPolicy("AdminFullAccess", policy => 
        policy.RequireRole(RoleHelper.AuthorizeRoles.Administrator, RoleHelper.AuthorizeRoles.Admin));
});

// Register Firebase Service
builder.Services.AddScoped<quanlyfilesBE.Services.IFirebaseService, quanlyfilesBE.Services.FirebaseService>();

// Register File Logger Service
builder.Services.AddSingleton<quanlyfilesBE.Services.IFileLoggerService, quanlyfilesBE.Services.FileLoggerService>();

// Register Power Automate Service
builder.Services.AddHttpClient();
builder.Services.AddScoped<quanlyfilesBE.Services.IPowerAutomateService, quanlyfilesBE.Services.PowerAutomateService>();

// Add SignalR
builder.Services.AddSignalR();

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

// Enable CORS - MUST be before UseRouting, UseAuthentication, UseAuthorization
// Order: CORS → Routing → Authentication → Authorization
app.UseCors("AllowFrontend");

// Optional: Log CORS-related requests for debugging (remove in production)
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var origin = context.Request.Headers["Origin"].ToString();
        var method = context.Request.Method;
        var path = context.Request.Path;
        var query = context.Request.QueryString;
        Console.WriteLine($"[CORS Debug] Method: {method}, Origin: {origin}, Path: {path}{query}");
        await next();
    });
}

// HTTPS Redirection - Disable for HTTP-only environments
// Uncomment if you need HTTPS redirection
// app.UseHttpsRedirection();

// Routing must be explicitly called before Authentication/Authorization
app.UseRouting();

// Logging middleware để debug routing
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path;
        var method = context.Request.Method;
        Console.WriteLine($"[Routing Debug] {method} {path}");
        await next();
    });
}

app.UseAuthentication();
app.UseAuthorization();

// Log all incoming requests for debugging
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var method = context.Request.Method;
    if (path.Value?.Contains("page-permissions") == true)
    {
        Console.WriteLine($"[PagePermissions Debug] {method} {path}");
    }
    await next();
});

app.MapControllers();

// Map SignalR Hub
app.MapHub<quanlyfilesBE.Hubs.NotificationHub>("/notificationHub");

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
