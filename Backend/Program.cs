using HardwareStoreAPI.Data;
using HardwareStoreAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Get connection string and initialize DatabaseHelper
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found");

// Initialize the DatabaseHelper singleton (NOT as a service)
DatabaseHelper.Initialize(connectionString);

<<<<<<< HEAD
// Register services
<<<<<<< HEAD
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IUserService, UserService>();
=======
builder.Services.AddSingleton<DatabaseHelper>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
>>>>>>> 374a1943cc0e402b6963feed412c5c1ce11aad0b
=======
// Register services (NOT DatabaseHelper - it's a static singleton)
// builder.Services.AddScoped<ICompanyService, CompanyService>();
>>>>>>> 880fd7dd7513b81357876404551d4fa6a3fc4a1e
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();