using HardwareStoreAPI.Data;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
// Get connection string and initialize DatabaseHelper
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found");

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
builder.Services.AddScoped<IPdfService, PdfService>();


// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ✅ FIXED CORS Configuration - Use specific origin instead of AllowAnyOrigin
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")  // Explicitly allow Angular dev server
              .AllowAnyMethod()                      // Allow all HTTP methods (GET, POST, PUT, DELETE, etc.)
              .AllowAnyHeader()                    // Allow all headers
              .AllowCredentials();                     // Important for authentication cookies/tokens
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseStaticFiles();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hardware Store API");
        c.RoutePrefix = string.Empty; // This makes swagger available at root URL
    });
}

// ❌ COMMENTED OUT HTTPS Redirection - This was causing CORS issues with preflight requests
 //app.UseHttpsRedirection();

// ✅ Use the CORS policy - Order matters! This should come before Authentication
app.UseCors("AllowAngularApp");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ✅ Auto-create bills directory on startup
var billsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "bills");
if (!Directory.Exists(billsDirectory))
{
    Directory.CreateDirectory(billsDirectory);
    Console.WriteLine($"Created bills directory: {billsDirectory}");
}
else
{
    Console.WriteLine($"Bills directory exists: {billsDirectory}");
}

app.Run();