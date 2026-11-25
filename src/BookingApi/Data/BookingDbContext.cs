using BookingApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Data
{
    public class BookingDbContext : DbContext
    {
        public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options)
        {
        }

        public DbSet<Slot> Slots => Set<Slot>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<BookingStateChange> BookingStateChanges => Set<BookingStateChange>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Booking>(b =>
            {
                b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            modelBuilder.Entity<BookingStateChange>(c =>
            {
                c.HasOne<Booking>().WithMany().HasForeignKey(x => x.BookingId);
                c.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(20);
                c.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(20);
                c.Property(x => x.Actor).HasConversion(
                    v => v.ToString().ToLowerInvariant(),
                    v => Enum.Parse<Actor>(v, true));
            });
        }
    }
}
