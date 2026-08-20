using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NexusEnroll.StudentService
{
    public class StudentDbContext : DbContext
    {
        public StudentDbContext(DbContextOptions<StudentDbContext> options) : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<CourseRecord> CourseRecords { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<DegreeProgram> DegreePrograms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Student>().HasKey(s => s.UserId);
            modelBuilder.Entity<Enrollment>().HasKey(e => e.EnrollmentId);
            modelBuilder.Entity<CourseRecord>().HasKey(cr => cr.Id);
            modelBuilder.Entity<Course>().HasKey(c => c.CourseId);
            modelBuilder.Entity<DegreeProgram>().HasKey(dp => dp.ProgramId);

            modelBuilder.Entity<Student>()
                .HasMany(s => s.AcademicHistory)
                .WithOne()
                .HasForeignKey(cr => cr.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            var listConverter = new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null) ?? new List<string>()
            );

            var listComparer = new ValueComparer<List<string>>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
                c => c.ToList()
            );

            modelBuilder.Entity<Student>()
                .Property(s => s.EnrolledCourseIds)
                .HasConversion(listConverter)
                .Metadata.SetValueComparer(listComparer);

            modelBuilder.Entity<Student>()
                .Property(s => s.WaitlistedCourseIds)
                .HasConversion(listConverter)
                .Metadata.SetValueComparer(listComparer);

            modelBuilder.Entity<Course>()
                .Property(c => c.PrerequisiteCourseIds)
                .HasConversion(listConverter)
                .Metadata.SetValueComparer(listComparer);

            modelBuilder.Entity<Course>()
                .Property(c => c.Waitlist)
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
