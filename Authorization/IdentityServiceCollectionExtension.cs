using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using ProjectManager.Authorization.Handlers;
using ProjectManager.Components.Account;
using ProjectManager.Data;
using ProjectManager.Data.Models;
using ProjectManager.Services.Security;

namespace ProjectManager.Authorization;

public static class IdentityServiceCollectionExtension
{
    public static IServiceCollection AddApplicationIdentity(this IServiceCollection services)
    {
        services.AddCascadingAuthenticationState();
        services.AddScoped<IdentityUserAccessor>();
        services.AddScoped<IdentityRedirectManager>();
        services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("IsProjectMember", policy => policy.Requirements.Add(new ProjectMemberRequirement()));
            options.AddPolicy("IsProjectOwner", policy => policy.Requirements.Add(new ProjectOwnerRequirement()));
            options.AddPolicy("IsCommentAuthor", policy => policy.Requirements.Add(new CommentAuthorRequirement()));
            options.AddPolicy("CanTaskModify", policy => policy.Requirements.Add(new TaskModifyRequirement()));
            options.AddPolicy("CommentAuthor", policy => policy.Requirements.Add(new CommentAuthorRequirement()));
        });

        services.AddScoped<IProjectAccessService, ProjectAccessService>();
        services.AddScoped<IAuthorizationHandler, ProjectMemberForProjectHandler>();
        services.AddScoped<IAuthorizationHandler, ProjectMemberForTaskHandler>();
        services.AddScoped<IAuthorizationHandler, ProjectOwnerForProjectHandler>();
        services.AddScoped<IAuthorizationHandler, ProjectOwnerForTaskHandler>();
        services.AddScoped<IAuthorizationHandler, CommentAuthorHandler>();
        services.AddScoped<IAuthorizationHandler, TaskModifyHandler>();
        services.AddScoped<IAuthorizationHandler, ProjectMemberForAttachmentHandler>();
        services.AddScoped<IAuthorizationHandler, TaskModifyForAttachmentHandler>();

        services.AddIdentityCore<ApplicationUser>(options =>
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

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        return services;
    }
}
