using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Authorization;
using ProjectManager.Components;
using ProjectManager.Components.Account;
using ProjectManager.Components.Common;
using ProjectManager.Data;
using ProjectManager.Data.Models;
using ProjectManager.Services;
using ProjectManager.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddApplicationIdentity();

builder.Services.AddControllers();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));

builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<CommentsService>();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContextFactory = services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    var db = dbContextFactory.CreateDbContext();
    await db.Database.MigrateAsync();

    var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = services.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = ["Admin", "User"];
    foreach (var r in roles)
        if (!await roleMgr.RoleExistsAsync(r))
            await roleMgr.CreateAsync(new IdentityRole(r));

    var adminEmail = "admin@pm.local";
    var admin = await userMgr.FindByEmailAsync(adminEmail);
    if (admin is null)
    {
        admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var identityResult = await userMgr.CreateAsync(admin, "qwe321");
        await userMgr.AddToRolesAsync(admin, roles);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
