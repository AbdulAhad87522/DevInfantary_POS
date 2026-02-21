using HardwareStoreAPI.Data;
using HardwareStoreAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Get connection string and initialize DatabaseHelper
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found");

// Initialize the DatabaseHelper singleton
DatabaseHelper.Initialize(connectionString);

// Register services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
 builder.Services.AddScoped<IQuotationService, QuotationService>();
 builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IReturnService, ReturnService>();
builder.Services.AddScoped<IPurchaseBatchService, PurchaseBatchService>();
builder.Services.AddScoped<ICustomerBillService, CustomerBillService>();


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