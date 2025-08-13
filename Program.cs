using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Authorization;
using ProjectManager.Authorization.Handlers;
using ProjectManager.Components;
using ProjectManager.Components.Account;
using ProjectManager.Data;
using ProjectManager.Data.Models;
using ProjectManager.Services.Security;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add services to the container.

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsProjectMember", policy => policy.Requirements.Add(new ProjectMemberRequirement()));
    options.AddPolicy("IsProjectOwner", policy => policy.Requirements.Add(new ProjectOwnerRequirement()));
    options.AddPolicy("IsCommentAuthor", policy => policy.Requirements.Add(new CommentAuthorRequirement()));
    options.AddPolicy("CanTaskModify", policy => policy.Requirements.Add(new TaskModifyRequirement()));
});

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddScoped<IProjectAccessService, ProjectAccessService>();
builder.Services.AddScoped<IAuthorizationHandler, ProjectMemberForProjectHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ProjectMemberForTaskHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ProjectOwnerForProjectHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ProjectOwnerForTaskHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CommentAuthorHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TaskModifyHandler>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
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
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapPost("/api/projects/{projectId:guid}/invite", async (
        Guid projectId,
        string userToInviteId,
        ApplicationDbContext db,
        IAuthorizationService auth,
        ClaimsPrincipal user,
        CancellationToken ct) =>
    {
        var project = await db.Projects.FindAsync([projectId], ct);
        if (project is null) return Results.NotFound();

        var result = await auth.AuthorizeAsync(user, project, "IsProjectOwner");
        if (!result.Succeeded) return Results.Forbid();

        // ... добавить участника ...
        return Results.Ok();
    })
    .RequireAuthorization();

app.Run();
