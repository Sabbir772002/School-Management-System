using System;
using System.Threading.Tasks;
using AssignmentBackend.Common;
using AssignmentBackend.Data;
using AssignmentBackend.Features.Assignments;
using AssignmentBackend.Features.Submissions;
using AssignmentBackend.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssignmentSystem.Tests
{
    public class BusinessRulesTests
    {
        private AppDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task Test_CQRS_SubmitAssignmentCommandHandler_Rejects_Past_Deadline()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            
            var pastAssignment = new Assignment
            {
                Id = 10,
                Title = "Late Physics Test",
                Deadline = DateTime.UtcNow.AddHours(-2),
                MaxMarks = 100,
                IsPublished = true,
                ClassId = 1,
                SubjectId = 1,
                TeacherId = 2
            };
            db.Assignments.Add(pastAssignment);
            await db.SaveChangesAsync();

            var handler = new SubmitAssignmentCommandHandler(db);
            var command = new SubmitAssignmentCommand(10, 4, "Direct handler late submission", null, null);
            var result = await handler.Handle(command, default);

            Assert.False(result.IsSuccess);
            Assert.Equal(OperationStatus.BadRequest, result.Status);
            Assert.Contains("Deadline has already passed", result.ErrorMessage);
        }

        [Fact]
        public async Task Test_CQRS_GetAssignmentsQueryHandler_Hides_Drafts_From_Students()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());

            db.Classes.Add(new Class { Id = 1, Name = "Class 10", Code = "10" });
            db.Users.Add(new User { Id = 2, FullName = "Teacher Rahim", Email = "t@test.com", PasswordHash = "123", Role = UserRoles.Teacher });
            db.Subjects.Add(new Subject { Id = 1, Name = "Math", ClassId = 1, TeacherId = 2 });
            db.Users.Add(new User { Id = 4, FullName = "Rafiq", Email = "rafiq@test.com", PasswordHash = "123", Role = UserRoles.Student, ClassId = 1 });

            db.Assignments.Add(new Assignment { Id = 1, Title = "Published Math", IsPublished = true, ClassId = 1, SubjectId = 1, TeacherId = 2 });
            db.Assignments.Add(new Assignment { Id = 2, Title = "Draft Math", IsPublished = false, ClassId = 1, SubjectId = 1, TeacherId = 2 });
            await db.SaveChangesAsync();

            var handler = new GetAssignmentsQueryHandler(db);
            var query = new GetAssignmentsQuery(null, 4, UserRoles.Student);
            var result = await handler.Handle(query, default);

            Assert.True(result.IsSuccess);
            var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(result.Data);
            int count = 0;
            foreach (var item in list) count++;
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Test_CQRS_GradeSubmissionCommandHandler_Validates_MaxMarks()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());

            var assignment = new Assignment { Id = 5, Title = "Chemistry Test", MaxMarks = 50, IsPublished = true, ClassId = 1, SubjectId = 1, TeacherId = 2 };
            var submission = new Submission { Id = 1, AssignmentId = 5, StudentId = 4, Content = "My chemistry answer", Status = "Submitted" };
            
            db.Assignments.Add(assignment);
            db.Submissions.Add(submission);
            await db.SaveChangesAsync();

            var handler = new GradeSubmissionCommandHandler(db);
            var gradeDto = new GradeSubmissionDto { Marks = 75, Feedback = "Over max marks!", Status = "Graded" };
            var command = new GradeSubmissionCommand(1, gradeDto);
            var result = await handler.Handle(command, default);

            Assert.False(result.IsSuccess);
            Assert.Equal(OperationStatus.BadRequest, result.Status);
            Assert.Contains("Marks cannot exceed max marks", result.ErrorMessage);
        }
    }
}
