using System;
using System.ComponentModel.DataAnnotations;

namespace AssignmentBackend.Models
{
    // login request payload
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    // response after login success
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? ClassId { get; set; }
        public string? ClassName { get; set; }
    }

    // create new user by admin
    public class CreateUserDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = UserRoles.Student;

        public int? ClassId { get; set; }
    }

    // class create payload
    public class CreateClassDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }

    // subject create payload
    public class CreateSubjectDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public int ClassId { get; set; }
        public int? TeacherId { get; set; }
    }

    // assign teacher payload
    public class AssignTeacherDto
    {
        public int SubjectId { get; set; }
        public int TeacherId { get; set; }
    }

    // create or edit assignment payload
    public class CreateAssignmentDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime Deadline { get; set; }

        public double MaxMarks { get; set; } = 100;

        public bool IsPublished { get; set; } = false;

        public int ClassId { get; set; }
        public int SubjectId { get; set; }
    }

    // student submission dto
    public class CreateSubmissionDto
    {
        public int AssignmentId { get; set; }
        public string? Content { get; set; }
        public string? LinkUrl { get; set; }
    }

    // teacher grade submission dto
    public class GradeSubmissionDto
    {
        public double Marks { get; set; }
        public string? Feedback { get; set; }
        public string Status { get; set; } = "Graded"; // Graded or NeedsRevision
    }
}
