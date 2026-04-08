using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;

namespace PayItOff.Infrastructure.Persistence
{
    public class PayItOffDbContext : DbContext
    {
        public PayItOffDbContext(DbContextOptions<PayItOffDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<ExpenseItem> ExpenseItems { get; set; }
        public DbSet<ExpenseGroup> ExpenseGroups { get; set; }
        public DbSet<ExpenseSplit> ExpenseSplits { get; set; }
        public DbSet<GroupDebt> GroupDebts { get; set; }
        public DbSet<Settlement> Settlements { get; set; }
        public DbSet<Friend> Friends { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

                foreach (var property in properties)
                {
                    property.SetPrecision(18);
                    property.SetScale(2);
                }
            }

            modelBuilder.Entity<User>(builder =>
            {
                builder.OwnsOne(u => u.NotificationsSettings).ToJson();

                builder.HasIndex(u => u.Email).IsUnique();
                builder.HasIndex(u => u.Nickname).IsUnique();

                builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
                builder.Property(u => u.Nickname).HasMaxLength(50).IsRequired();
            });

            modelBuilder.Entity<Expense>(builder =>
            {
                builder.Metadata.FindNavigation(nameof(Expense.Items))
                    ?.SetPropertyAccessMode(PropertyAccessMode.Field);

                builder.Metadata.FindNavigation(nameof(Expense.Groups))
                    ?.SetPropertyAccessMode(PropertyAccessMode.Field);

                builder.HasMany(e => e.Items)
                       .WithOne(i => i.Expense)
                       .HasForeignKey(i => i.ExpenseId)
                       .OnDelete(DeleteBehavior.Restrict);

                builder.HasMany(e => e.Groups)
                       .WithOne(g => g.Expense)
                       .HasForeignKey(g => g.ExpenseId)
                       .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ExpenseItem>(builder =>
            {
                builder.Metadata.FindNavigation(nameof(ExpenseItem.Splits))
                    ?.SetPropertyAccessMode(PropertyAccessMode.Field);
            });

            modelBuilder.Entity<ExpenseGroup>(builder =>
            {
                builder.Metadata.FindNavigation(nameof(ExpenseGroup.Splits))
                    ?.SetPropertyAccessMode(PropertyAccessMode.Field);
            });

            modelBuilder.Entity<ExpenseSplit>(builder =>
            {
                builder.HasOne(s => s.ExpenseItem)
                       .WithMany(i => i.Splits)
                       .HasForeignKey(s => s.ExpenseItemId)
                       .OnDelete(DeleteBehavior.Restrict);

                builder.HasOne(s => s.ExpenseGroup)
                       .WithMany(g => g.Splits)
                       .HasForeignKey(s => s.ExpenseGroupId)
                       .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<GroupDebt>(builder =>
            {
                builder.HasIndex(gd => new { gd.GroupId, gd.DebtorId, gd.CreditorId }).IsUnique();

                builder.HasOne(gd => gd.Debtor).WithMany().HasForeignKey(gd => gd.DebtorId).OnDelete(DeleteBehavior.Restrict);
                builder.HasOne(gd => gd.Creditor).WithMany().HasForeignKey(gd => gd.CreditorId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Friend>(builder =>
            {
                builder.HasIndex(f => new { f.InviterId, f.ReceiverId }).IsUnique();

                builder.HasOne(f => f.Inviter).WithMany().HasForeignKey(f => f.InviterId).OnDelete(DeleteBehavior.Restrict);
                builder.HasOne(f => f.Receiver).WithMany().HasForeignKey(f => f.ReceiverId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Settlement>(builder =>
            {
                builder.Property(s => s.TransferReference).HasMaxLength(50).IsRequired();

                builder.HasOne(s => s.Sender).WithMany().HasForeignKey(s => s.SenderId).OnDelete(DeleteBehavior.Restrict);
                builder.HasOne(s => s.Receiver).WithMany().HasForeignKey(s => s.ReceiverId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Notification>(builder =>
            {
                builder.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Restrict);
                builder.HasOne(n => n.Actor).WithMany().HasForeignKey(n => n.ActorId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AuditLog>(builder =>
            {
                builder.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<GroupMember>(builder =>
            {
                builder.HasIndex(gm => new { gm.UserId, gm.GroupId }).IsUnique();
            });
        }
    }
}