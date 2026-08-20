using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NexusEnroll.FacultyService
{
    public class FacultyDbContext : DbContext
    {
        public FacultyDbContext(DbContextOptions<FacultyDbContext> options) : base(options)
        {
        }

        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<CourseChangeRequest> CourseChangeRequests { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<RosterEntry> RosterEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Faculty>().HasKey(f => f.UserId);
            modelBuilder.Entity<CourseChangeRequest>().HasKey(ccr => ccr.RequestId);
            modelBuilder.Entity<Course>().HasKey(c => c.CourseId);
            modelBuilder.Entity<RosterEntry>().HasKey(re => re.Id);

            var listConverter = new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null) ?? new List<string>()
            );

            var listComparer = new ValueComparer<List<string>>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
                c => c.ToList()
            );

            modelBuilder.Entity<Faculty>()
                .Property(f => f.TeachingCourseIds)
                .HasConversion(listConverter)
                .Metadata.SetValueComparer(listComparer);
        }
    }
}
