using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Services;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Razor Pages
builder.Services.AddRazorPages();

// Conexión con la base de datos
builder.Services.AddDbContext<EscuelaInventarioContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("EscuelaInventarioConnection")));

builder.Services.AddScoped<InventarioService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
