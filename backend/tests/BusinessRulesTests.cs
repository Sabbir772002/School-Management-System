using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AssignmentBackend.Controllers;
using AssignmentBackend.Data;
using AssignmentBackend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        private void SetupUserClaims(ControllerBase controller, int userId, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task Test_Student_Cannot_Submit_After_Deadline()
        {
            // test if student submit answer after deadline pass
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            
            // add assignment with past deadline
            var pastAssignment = new Assignment
            {
                Id = 10,
                Title = "Late Physics Test",
                Deadline = DateTime.UtcNow.AddHours(-2), // deadline passed 2 hours ago
                MaxMarks = 100,
                IsPublished = true,
                ClassId = 1,
                SubjectId = 1,
                TeacherId = 2
            };
            db.Assignments.Add(pastAssignment);
            await db.SaveChangesAsync();

            var controller = new SubmissionsController(db);
            SetupUserClaims(controller, 4, UserRoles.Student);

            // try to submit
            var result = await controller.SubmitAssignment(10, "My late answer", null, null);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task Test_Student_Only_Sees_Published_Assignments()
        {
            // test draft assignment hide from student
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());

            // student user
            db.Users.Add(new User { Id = 4, FullName = "Rafiq", Email = "rafiq@test.com", PasswordHash = "123", Role = UserRoles.Student, ClassId = 1 });

            // published and draft assignment
            db.Assignments.Add(new Assignment { Id = 1, Title = "Published Math", IsPublished = true, ClassId = 1, SubjectId = 1, TeacherId = 2 });
            db.Assignments.Add(new Assignment { Id = 2, Title = "Draft Math", IsPublished = false, ClassId = 1, SubjectId = 1, TeacherId = 2 });
            await db.SaveChangesAsync();

            var controller = new AssignmentsController(db);
            SetupUserClaims(controller, 4, UserRoles.Student);

            var actionResult = await controller.GetAssignments(null);
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            
            dynamic list = okResult.Value!;
            Assert.Equal(1, list.Count);
        }

        [Fact]
        public async Task Test_Grading_Exceeding_Max_Marks_Fails()
        {
            // test teacher give marks more than max marks
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());

            var assignment = new Assignment { Id = 5, Title = "Chemistry Test", MaxMarks = 50, IsPublished = true, ClassId = 1, SubjectId = 1, TeacherId = 2 };
            var submission = new Submission { Id = 1, AssignmentId = 5, StudentId = 4, Content = "My chemistry answer", Status = "Submitted" };
            
            db.Assignments.Add(assignment);
            db.Submissions.Add(submission);
            await db.SaveChangesAsync();

            var controller = new SubmissionsController(db);
            SetupUserClaims(controller, 2, UserRoles.Teacher);

            // try give 75 marks when max is 50
            var gradeDto = new GradeSubmissionDto { Marks = 75, Feedback = "Over max marks!", Status = "Graded" };
            var result = await controller.GradeSubmission(1, gradeDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }
    }
}
