using Azure.Identity;
using CelexWebApp.Models;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;


var builder = WebApplication.CreateBuilder(args);

// Agregar configuración de Key Vault
string keyVaultUri = builder.Configuration["AzureKeyVault:VaultUri"];

// Registrar KeyVaultService y Conexion
builder.Services.AddSingleton(new KeyVaultService(keyVaultUri));
builder.Services.AddTransient<Conexion>();

// Configurar la autenticación con Azure AD sin duplicar la configuración de cookies
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    // Aquí se configuran los Downstream APIs
    .AddDownstreamApi("DownstreamApi", builder.Configuration.GetSection("DownstreamApi"))
    // Opcionalmente, si llamas a Microsoft Graph
    .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
    .AddInMemoryTokenCaches();


// Configurar servicios adicionales (Controllers, RazorPages, etc.)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configurar la sesión de manera temporal por 30 minutos de inactividad
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(60); 
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();
app.UseSession();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
