using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AssignmentBackend.Models
{
    public static class UserRoles
    {
        public const string Admin   = "Admin";
        public const string Teacher = "Teacher";
        public const string Student = "Student";
    }

    public class User
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = UserRoles.Student;

        public int? ClassId { get; set; }

        [JsonIgnore]
        public Class? StudentClass { get; set; }

        [JsonIgnore]
        public ICollection<Subject>? TaughtSubjects { get; set; }

        [JsonIgnore]
        public ICollection<Assignment>? CreatedAssignments { get; set; }

        [JsonIgnore]
        public ICollection<Submission>? Submissions { get; set; }
    }

    public class Class
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        [JsonIgnore]
        public ICollection<User> Students { get; set; } = new List<User>();

        [JsonIgnore]
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();

        [JsonIgnore]
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }

    public class Subject
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public int ClassId { get; set; }

        [JsonIgnore]
        public Class? Class { get; set; }

        public int? TeacherId { get; set; }

        [JsonIgnore]
        public User? Teacher { get; set; }

        [JsonIgnore]
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }

    public class Assignment
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime Deadline { get; set; }

        public double MaxMarks { get; set; } = 100;

        public bool IsPublished { get; set; } = false;

        public int ClassId { get; set; }
        public Class? Class { get; set; }

        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        public int TeacherId { get; set; }
        public User? Teacher { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    }

    public class Submission
    {
        public int Id { get; set; }

        public int AssignmentId { get; set; }
        public Assignment? Assignment { get; set; }

        public int StudentId { get; set; }
        public User? Student { get; set; }

        public string? Content  { get; set; }
        public string? LinkUrl  { get; set; }
        public string? FilePath { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // "Submitted" | "Graded" | "NeedsRevision"
        public string Status { get; set; } = "Submitted";

        public double? Marks    { get; set; }
        public string? Feedback { get; set; }
    }

    public class Notification
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        // "assignment_published" | "submission_received" | "grade_posted"
        public string Type { get; set; } = "info";

        public int? ReferenceId { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
