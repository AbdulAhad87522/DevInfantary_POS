//using HardwareStoreAPI.Data;
//using HardwareStoreAPI.Models.DTOs;
//using HardwareStoreAPI.Services;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.IdentityModel.Tokens;
//using Microsoft.OpenApi.Models;
//using System.Text;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container
//builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();

//// ✅ Configure Swagger with JWT Authentication
//builder.Services.AddSwaggerGen(options =>
//{
//    options.SwaggerDoc("v1", new OpenApiInfo
//    {
//        Title = "Hardware Store API",
//        Version = "v1",
//        Description = "Hardware Store Management System API with JWT Authentication",
//        Contact = new OpenApiContact
//        {
//            Name = "DevInfantary",
//            Email = "devinfantary@example.com",
//            Url = new Uri("https://devinfantary.com")
//        }
//    });

//    // ✅ Add JWT Authentication to Swagger
//    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Name = "Authorization",
//        Type = SecuritySchemeType.Http,
//        Scheme = "Bearer",
//        BearerFormat = "JWT",
//        In = ParameterLocation.Header,
//        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
//    });

//    options.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            Array.Empty<string>()
//        }
//    });
//});

//builder.Services.AddHttpContextAccessor();

//// Get connection string and initialize DatabaseHelper
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
//    ?? throw new InvalidOperationException("Connection string not found");

//DatabaseHelper.Initialize(connectionString);

//// Register all services
//builder.Services.AddScoped<IProductService, ProductService>();
//builder.Services.AddScoped<ICustomerService, CustomerService>();
//builder.Services.AddScoped<ISupplierService, SupplierService>();
//builder.Services.AddScoped<IQuotationService, QuotationService>();
//builder.Services.AddScoped<IBillService, BillService>();
//builder.Services.AddScoped<IReturnService, ReturnService>();
//builder.Services.AddScoped<IPurchaseBatchService, PurchaseBatchService>();
//builder.Services.AddScoped<ICustomerBillService, CustomerBillService>();
//builder.Services.AddScoped<ISupplierBillService, SupplierBillService>();
//builder.Services.AddScoped<IDashboardService, DashboardService>();
//builder.Services.AddScoped<IAuthService, AuthService>();
//builder.Services.AddScoped<IPdfService, PdfService>();
//builder.Services.AddScoped<ICategoryService, CategoryService>();
//builder.Services.AddScoped<IStaffService, StaffService>();

//// Configure JWT Authentication
//var jwtSettings = builder.Configuration.GetSection("JwtSettings");
//var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = jwtSettings["Issuer"],
//        ValidAudience = jwtSettings["Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
//    };
//});

//builder.Services.AddAuthorization();

//// Logging
//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
//builder.Logging.AddDebug();

//// ✅ CORS Configuration - Allow Angular dev server
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAngularApp", policy =>
//    {
//        policy.WithOrigins("http://localhost:4200")  // Angular dev server
//              .AllowAnyMethod()                      // GET, POST, PUT, DELETE, etc.
//              .AllowAnyHeader()                      // Authorization, Content-Type, etc.
//              .AllowCredentials();                   // Important for JWT tokens
//    });
//});

//var app = builder.Build();

//// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI(c =>
//    {
//        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hardware Store API v1");
//        c.RoutePrefix = string.Empty; // Swagger available at root URL
//        c.DocumentTitle = "Hardware Store API - Swagger UI";
//    });
//}

//// ✅ Static files for serving PDFs
//app.UseStaticFiles();

//// ❌ HTTPS Redirection commented out for development (CORS preflight issues)
//// app.UseHttpsRedirection();

//// ✅ CORS - Must come before Authentication
//app.UseCors("AllowAngularApp");

//// Authentication & Authorization
//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllers();

//// ✅ Auto-create directories for bills and quotations on startup
//var billsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "bills");
//var quotationsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "quotations");

//if (!Directory.Exists(billsDirectory))
//{
//    Directory.CreateDirectory(billsDirectory);
//    Console.WriteLine($"✅ Created bills directory: {billsDirectory}");
//}
//else
//{
//    Console.WriteLine($"✅ Bills directory exists: {billsDirectory}");
//}

//if (!Directory.Exists(quotationsDirectory))
//{
//    Directory.CreateDirectory(quotationsDirectory);
//    Console.WriteLine($"✅ Created quotations directory: {quotationsDirectory}");
//}
//else
//{
//    Console.WriteLine($"✅ Quotations directory exists: {quotationsDirectory}");
//}

//Console.WriteLine("🚀 Hardware Store API is running...");
//Console.WriteLine($"📄 Swagger UI: https://localhost:{builder.Configuration["Kestrel:Endpoints:Https:Url"]?.Split(':').Last() ?? "7073"}");
//Console.WriteLine("🔐 JWT Authentication enabled");
//Console.WriteLine("🌐 CORS enabled for http://localhost:4200");

//app.Run();

//deployment
using HardwareStoreAPI.Data;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ✅ Connection string from appsettings.json
var connectionString =
    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found");

DatabaseHelper.Initialize(connectionString);

// ✅ Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// ✅ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://hardwarestore-production.up.railway.app",
                "https://*.vercel.app"
              )
              .SetIsOriginAllowedToAllowWildcardSubdomains()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ✅ Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hardware Store API",
        Version = "v1",
        Description = "Hardware Store Management System API with JWT Authentication"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token only (without 'Bearer' prefix)"
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

// ✅ Register all services
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
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IStaffService, StaffService>();

// ✅ JWT Authentication
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"]
    ?? throw new InvalidOperationException("JWT Secret Key not found");

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
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };
});

var app = builder.Build();

// ✅ Create directories
Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "bills"));
Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "quotations"));

// ✅ Swagger always on
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

app.Run();
