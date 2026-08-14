using System;
using System.Security.Cryptography;
using System.Text;
using AssignmentBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AssignmentBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User>         Users         { get; set; } = null!;
        public DbSet<Class>        Classes       { get; set; } = null!;
        public DbSet<Subject>      Subjects      { get; set; } = null!;
        public DbSet<Assignment>   Assignments   { get; set; } = null!;
        public DbSet<Submission>   Submissions   { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasOne(u => u.StudentClass)
                .WithMany(c => c.Students)
                .HasForeignKey(u => u.ClassId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Subject>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Subjects)
                .HasForeignKey(s => s.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Subject>()
                .HasOne(s => s.Teacher)
                .WithMany(u => u.TaughtSubjects)
                .HasForeignKey(s => s.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Class)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Subject)
                .WithMany(s => s.Assignments)
                .HasForeignKey(a => a.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Teacher)
                .WithMany(u => u.CreatedAssignments)
                .HasForeignKey(a => a.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Submission>()
                .HasOne(s => s.Assignment)
                .WithMany(a => a.Submissions)
                .HasForeignKey(s => s.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Submission>()
                .HasOne(s => s.Student)
                .WithMany(u => u.Submissions)
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            string HashPassword(string password)
            {
                using var sha256 = SHA256.Create();
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }

            modelBuilder.Entity<Class>().HasData(
                new Class { Id = 1, Name = "Class 10 - Science", Code = "10-SCI" },
                new Class { Id = 2, Name = "Class 11 - Science", Code = "11-SCI" }
            );

            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, FullName = "System Admin",    Email = "admin@school.com",                PasswordHash = HashPassword("Admin123!"),   Role = UserRoles.Admin,   ClassId = null },
                new User { Id = 2, FullName = "Teacher Rahim",   Email = "teacher.rahim@school.com",        PasswordHash = HashPassword("Teacher123!"), Role = UserRoles.Teacher, ClassId = null },
                new User { Id = 3, FullName = "Teacher Karim",   Email = "teacher.karim@school.com",        PasswordHash = HashPassword("Teacher123!"), Role = UserRoles.Teacher, ClassId = null },
                new User { Id = 4, FullName = "Student Rafiq",   Email = "student.rafiq@school.com",        PasswordHash = HashPassword("Student123!"), Role = UserRoles.Student, ClassId = 1 },
                new User { Id = 5, FullName = "Student Salma",   Email = "student.salma@school.com",        PasswordHash = HashPassword("Student123!"), Role = UserRoles.Student, ClassId = 1 },
                new User { Id = 6, FullName = "Student Tariq",   Email = "student.tariq@school.com",        PasswordHash = HashPassword("Student123!"), Role = UserRoles.Student, ClassId = 2 }
            );

            modelBuilder.Entity<Subject>().HasData(
                new Subject { Id = 1, Name = "Physics",     ClassId = 1, TeacherId = 2 },
                new Subject { Id = 2, Name = "Chemistry",   ClassId = 1, TeacherId = 3 },
                new Subject { Id = 3, Name = "Higher Math", ClassId = 2, TeacherId = 2 }
            );

            modelBuilder.Entity<Assignment>().HasData(
                new Assignment
                {
                    Id = 1, Title = "Physics Motion & Dynamics Assignment",
                    Description = "Solve exercise questions from chapter 2 and submit PDF or text answers.",
                    Deadline = DateTime.UtcNow.AddDays(7), MaxMarks = 100, IsPublished = true,
                    ClassId = 1, SubjectId = 1, TeacherId = 2, CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Assignment
                {
                    Id = 2, Title = "Chemistry Periodic Table Reaction Report",
                    Description = "Write summary about alkali metals reaction with water. Attach link or document.",
                    Deadline = DateTime.UtcNow.AddDays(5), MaxMarks = 50, IsPublished = true,
                    ClassId = 1, SubjectId = 2, TeacherId = 3, CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Assignment
                {
                    Id = 3, Title = "Draft Assignment - Physics Lab Report",
                    Description = "This is a draft assignment not visible to students.",
                    Deadline = DateTime.UtcNow.AddDays(10), MaxMarks = 20, IsPublished = false,
                    ClassId = 1, SubjectId = 1, TeacherId = 2, CreatedAt = DateTime.UtcNow
                }
            );

            modelBuilder.Entity<Submission>().HasData(
                new Submission
                {
                    Id = 1, AssignmentId = 1, StudentId = 4,
                    Content = "Sir here is my answer for question 1 to 5. Velocity v = u + at, Acceleration a = (v-u)/t.",
                    LinkUrl = "https://github.com/example/physics-homework",
                    FilePath = null, SubmittedAt = DateTime.UtcNow.AddHours(-5),
                    Status = "Graded", Marks = 92, Feedback = "Good work Rafiq, step 3 calculation is very clear!"
                }
            );
        }
    }
}
