using HoneyDo.Domain;
using Microsoft.EntityFrameworkCore;
using TaskStatus = HoneyDo.Domain.TaskStatus;

namespace HoneyDo.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<TodoList> TodoLists => Set<TodoList>();
    public DbSet<ListMember> ListMembers => Set<ListMember>();
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<TaskStatus> TaskStatuses => Set<TaskStatus>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TodoItemTag> TodoItemTags => Set<TodoItemTag>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Friend> Friends => Set<Friend>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global soft-delete filters
        modelBuilder.Entity<TodoList>().HasQueryFilter(l => l.DeletedAt == null);
        modelBuilder.Entity<TodoItem>().HasQueryFilter(i => i.DeletedAt == null);

        // Seed task statuses
        modelBuilder.Entity<TaskStatus>().HasData(
            new TaskStatus { Id = 1, Name = "Not Started" },
            new TaskStatus { Id = 2, Name = "Partial" },
            new TaskStatus { Id = 3, Name = "Complete" },
            new TaskStatus { Id = 4, Name = "Abandoned" }
        );
    }
}
