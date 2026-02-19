using Google.Protobuf.WellKnownTypes;
using HardwareStoreAPI.Data;
using HardwareStoreAPI.Services;
using Mysqlx.Crud;
using Org.BouncyCastle.Utilities.Collections;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found");
DatabaseHelper.Initialize(connectionString);

// Register services
<<<<<<< HEAD
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IUserService, UserService>();
=======
builder.Services.AddSingleton<DatabaseHelper>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
>>>>>>> 374a1943cc0e402b6963feed412c5c1ce11aad0b
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISupplierService, SupplierService>(); // ✅ ADD THIS

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
//```

//## API Endpoints Summary
//```
//GET / api / suppliers - Get all suppliers
//GET    /api/suppliers/paginated            - Get paginated suppliers
//GET    /api/suppliers/{id}                 -Get supplier by ID    
//POST   /api/suppliers                      - Create new supplier
//PUT / api / suppliers /{ id}
//-Update supplier
//DELETE /api/suppliers/{id}                 -Delete(soft delete) supplier
//POST   /api/suppliers/{id}/ restore - Restore deleted supplier
//GET    /api/suppliers/search?term=xxx      - Search suppliers
//GET    /api/suppliers/{id}/ balance - Get supplier balance
//PATCH  /api/suppliers/{id}/ balance - Update supplier balance
//GET    /api/suppliers/with-balance         - Get suppliers with outstanding balance