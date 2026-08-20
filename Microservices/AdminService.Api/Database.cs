using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NexusEnroll.AdminService
{
    public class AdminDbContext : DbContext
    {
        public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options)
        {
        }

        public DbSet<Admin> Admins { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<StudentProfile> StudentProfiles { get; set; }
        public DbSet<FacultyProfile> FacultyProfiles { get; set; }
        public DbSet<DegreeProgram> DegreePrograms { get; set; }
        public DbSet<CourseChangeRequest> CourseChangeRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Admin>().HasKey(a => a.UserId);
            modelBuilder.Entity<Course>().HasKey(c => c.CourseId);
            modelBuilder.Entity<StudentProfile>().HasKey(s => s.UserId);
            modelBuilder.Entity<FacultyProfile>().HasKey(f => f.UserId);
            modelBuilder.Entity<DegreeProgram>().HasKey(dp => dp.ProgramId);
            modelBuilder.Entity<CourseChangeRequest>().HasKey(ccr => ccr.RequestId);

            var listConverter = new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null) ?? new List<string>()
            );

            var listComparer = new ValueComparer<List<string>>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
                c => c.ToList()
            );

            modelBuilder.Entity<Course>()
                .Property(c => c.PrerequisiteCourseIds)
                .HasConversion(listConverter)
                .Metadata.SetValueComparer(listComparer);

            modelBuilder.Entity<StudentProfile>()
                .Property(s => s.EnrolledCourseIds)
                .HasConversion(listConverter)
                .Metadata.SetValueComparer(listComparer);

            modelBuilder.Entity<StudentProfile>()
                .Property(s => s.WaitlistedCourseIds)
                .HasConversion(listConverter)
                .Metadata.SetValueComparer(listComparer);

            modelBuilder.Entity<FacultyProfile>()
                .Property(f => f.TeachingCourseIds)
                .HasConversion(listConverter)
                .Metadata.SetValueComparer(listComparer);

            modelBuilder.Entity<DegreeProgram>()
                .Property(dp => dp.RequiredCourseIds)
                .HasConversion(listConverter)
                .Metadata.SetValueComparer(listComparer);

            modelBuilder.Entity<DegreeProgram>()
                .Property(dp => dp.ElectiveCourseIds)
                .HasConversion(listConverter)
                .Metadata.SetValueComparer(listComparer);
        }
    }
}
