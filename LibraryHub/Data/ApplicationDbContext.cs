using LibraryHub.Extensions;
using LibraryHub.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibraryHub.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Loan> Loans { get; set; }
    public DbSet<Test1> Test1 { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply global query filters for soft delete
        builder.Entity<Author>()
            .HasQueryFilter(author => !author.IsDeleted);

        builder.Entity<Book>()
            .HasQueryFilter(book => !book.IsDeleted);

        builder.Entity<Category>()
            .HasQueryFilter(category => !category.IsDeleted);

        builder.Entity<Member>()
            .HasQueryFilter(member => !member.IsDeleted);

        builder.Entity<Loan>()
            .HasQueryFilter(loan => !loan.IsDeleted);

        // Configure the Version property as a concurrency token
        //builder.Entity<Book>()
        //.Property(b => b.Version)
        //.IsRowVersion();

        builder.Entity<Book>()   // Book Author -  (1-M)
           .HasOne(x => x.Author)
           .WithMany(x => x.Books)
           .HasForeignKey(x => x.AuthorId)
           .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Book>()   //  Book Category - (1-M)
            .HasOne(c => c.Category)
            .WithMany(c => c.Books)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Loan>() // Loan Member - (1-M)
           .HasOne(l => l.Member)
           .WithMany(m => m.Loans)
           .HasForeignKey(l => l.MemberId)
           .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Loan>()   // Loan Book - (1-M)
            .HasOne(l => l.Book)
            .WithMany(b => b.Loans)
            .HasForeignKey(l => l.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RefreshToken>()  // RefreshToken User - (1-M)
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Member>()
           .HasOne(m => m.ApplicationUser)
           .WithOne()
           .HasForeignKey<Member>(m => m.ApplicationUserId)
           .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Member>()
            .HasIndex(m => m.MembershipNumber)
            .IsUnique();

        builder.Entity<Member>()
            .HasIndex(m => m.ApplicationUserId)
            .IsUnique();

        builder.Entity<Book>()  // ISBN unique
            .HasIndex(b => b.ISBN)
            .IsUnique();

        builder.Entity<RefreshToken>()    // RefreshToke unique
            .HasIndex(x => x.Token)
            .IsUnique();
    }

    public override int SaveChanges()
    {
        ChangeTracker.ApplySoftDelete();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.ApplySoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }
}
