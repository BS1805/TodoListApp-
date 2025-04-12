using Microsoft.EntityFrameworkCore;
using TodoListApp.Models;

namespace TodoListApp.Data
{
    public class TodoContext : DbContext
    {
        public TodoContext(DbContextOptions<TodoContext> options)
            : base(options)
        {
        }

        public DbSet<TodoItem> TodoItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Add indexes for performance since we'll have up to 100k entries
            modelBuilder.Entity<TodoItem>()
                .HasIndex(t => t.IsCompleted);

            modelBuilder.Entity<TodoItem>()
                .HasIndex(t => t.DueDate);

            modelBuilder.Entity<TodoItem>()
                .HasIndex(t => t.Priority);
        }
    }
}