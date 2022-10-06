using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

var options = new DbContextOptionsBuilder()
    .UseSqlServer("Data Source = .\\SQLEXPRESS; Database = EfCoreGroupByJsonColumnProperty; Integrated Security = True; TrustServerCertificate=True");

var dbContext = new ApplicationDbContext(options.Options);
await dbContext.Database.EnsureDeletedAsync();
await dbContext.Database.EnsureCreatedAsync();

dbContext.AddRange(new[]
{
  new Customer("test@mail.com")
  {
    Verification = new(CustomerVerificationStatus.Verified, "Verified", DateTime.MaxValue)
  },
  new Customer("test2@mail.com")
  {
    Verification = new(null, "Pending", DateTime.MaxValue)
  }
});
await dbContext.SaveChangesAsync();

//
// SqlException:
//
// The multi-part identifier "t.Id" could not be bound.
//
var query = dbContext.Customers
    .GroupBy(x => x.Verification!.CreationDateTime.Date)
    .Select(x => x.First());

var sql = query.ToQueryString();

var result = await query.ToListAsync();

Console.Read();




public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseLoggerFactory(LoggerFactory.Create(x => x
                .AddConsole()
                .AddFilter(y => y >= LogLevel.Debug)))
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>()
            .OwnsOne(x => x.Verification)
            .ToJson();

        base.OnModelCreating(modelBuilder);
    }
}

public class Customer
{
    public Customer(string email)
    {
        Email = email;
    }

    public int Id { get; private set; }
    public string Email { get; private set; }
    public CustomerVerification? Verification { get; set; }
}

public class CustomerVerification
{
    public CustomerVerification(CustomerVerificationStatus? status, string text, DateTime creationDateTime)
    {
        Status = status;
        Text = text;
        CreationDateTime = creationDateTime;
    }

    public CustomerVerificationStatus? Status { get; private set; }
    public string Text { get; private set; }
    public DateTime CreationDateTime { get; private set; }
}

public enum CustomerVerificationStatus
{
    Pending,
    Verified,
}