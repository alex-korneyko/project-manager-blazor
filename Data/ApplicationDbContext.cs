using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data.Models;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Project -> Owner (один пользователь может владеть многими проектами)
        modelBuilder.Entity<Project>()
            .HasOne(p => p.Owner)
            .WithMany() // без нав. колл. у пользователя
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict); // не удаляем юзера каскадом

        // Уникальность (ProjectId, UserId)
        modelBuilder.Entity<ProjectMember>()
            .HasIndex(pm => new { pm.ProjectId, pm.UserId })
            .IsUnique();

        // TaskItem -> Project каскадом: удаление проекта удаляет его задачи
        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // TaskAttachment -> Task каскадом
        modelBuilder.Entity<TaskAttachment>()
            .HasOne(a => a.Task)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // TaskComment -> Task каскадом
        modelBuilder.Entity<TaskComment>()
            .HasOne(c => c.Task)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Самоссылка комментариев: удаление родителя удаляет всю ветку
        modelBuilder.Entity<TaskComment>()
            .HasOne(c => c.Parent)
            .WithMany(p => p.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Авторские связи (Restrict, чтобы не удалить юзера из Identity)
        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Author)
            .WithMany()
            .HasForeignKey(t => t.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaskAttachment>()
            .HasOne(a => a.Uploader)
            .WithMany()
            .HasForeignKey(a => a.UploaderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaskComment>()
            .HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
