using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models.DTOs;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
builder.Services.AddEndpointsApiExplorer();

// ✅ Configure Swagger with JWT Authentication
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hardware Store API",
        Version = "v1",
        Description = "Hardware Store Management System API with JWT Authentication",
        Contact = new OpenApiContact
        {
            Name = "DevInfantary",
            Email = "devinfantary@example.com",
            Url = new Uri("https://devinfantary.com")
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();

// ✅ Build connection string from Railway MySQL variables, or fall back to appsettings
var connectionString = BuildConnectionString(builder.Configuration);
DatabaseHelper.Initialize(connectionString);

// Register all services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IQuotationService, QuotationService>();
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IReturnService, ReturnService>();
builder.Services.AddScoped<IPurchaseBatchService, PurchaseBatchService>();
builder.Services.AddScoped<ICustomerBillService, CustomerBillService>();
builder.Services.AddScoped<ISupplierBillService, SupplierBillService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDailyExpenseService, DailyExpenseService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IStockRequirementService, StockRequirementService>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ✅ CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.SetIsOriginAllowed(origin => 
            new Uri(origin).Host == "localhost" || 
            new Uri(origin).Host.Contains("railway.app") ||
            new Uri(origin).Host.Contains("netlify.app") ||
            new Uri(origin).Host.Contains("devinfantary.com"))
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hardware Store API v1");
    c.RoutePrefix = string.Empty;
});

app.UseStaticFiles();
app.UseCors("AllowAngularApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var billsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "bills");
var quotationsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "quotations");
Directory.CreateDirectory(billsDirectory);
Directory.CreateDirectory(quotationsDirectory);

Console.WriteLine("🚀 Hardware Store API is running...");
Console.WriteLine($"📄 Swagger UI available at root URL");

app.Run();

static string BuildConnectionString(IConfiguration config)
{
    var host = Environment.GetEnvironmentVariable("MYSQLHOST");
    var port = Environment.GetEnvironmentVariable("MYSQLPORT") ?? "3306";
    var user = Environment.GetEnvironmentVariable("MYSQLUSER");
    var password = Environment.GetEnvironmentVariable("MYSQLPASSWORD");
    var database = Environment.GetEnvironmentVariable("MYSQLDATABASE");

    if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(database))
        return $"Server={host};Port={port};Database={database};User={user};Password={password};";

    var connectionUrl = Environment.GetEnvironmentVariable("MYSQL_URL");
    if (!string.IsNullOrEmpty(connectionUrl))
        return connectionUrl;

    return config.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string not found");
}