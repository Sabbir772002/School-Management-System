using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AssignmentBackend.Common;
using AssignmentBackend.Data;
using AssignmentBackend.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AssignmentBackend.Features.Submissions
{
    public record SubmitAssignmentCommand(
        int AssignmentId,
        int StudentId,
        string? Content,
        string? LinkUrl,
        IFormFile? File
    ) : IRequest<OperationResult<Submission>>;

    public class SubmitAssignmentCommandHandler : IRequestHandler<SubmitAssignmentCommand, OperationResult<Submission>>
    {
        private readonly AppDbContext _db;
        public SubmitAssignmentCommandHandler(AppDbContext db) => _db = db;

        public async Task<OperationResult<Submission>> Handle(SubmitAssignmentCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _db.Assignments.FindAsync(new object[] { request.AssignmentId }, cancellationToken);
            if (assignment == null)
                return OperationResult<Submission>.NotFound("Assignment not found");

            if (!assignment.IsPublished)
                return OperationResult<Submission>.BadRequest("Cannot submit to an unpublished assignment");

            if (DateTime.UtcNow > assignment.Deadline)
                return OperationResult<Submission>.BadRequest("Deadline has passed. Submission closed.");

            var existing = await _db.Submissions
                .FirstOrDefaultAsync(s => s.AssignmentId == request.AssignmentId && s.StudentId == request.StudentId, cancellationToken);

            string? savedFilePath = existing?.FilePath;

            if (request.File != null && request.File.Length > 0)
            {
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.File.FileName)}";
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                    await request.File.CopyToAsync(stream, cancellationToken);

                savedFilePath = $"/uploads/{fileName}";
            }

            Submission submission;
            if (existing != null)
            {
                existing.Content     = request.Content ?? existing.Content;
                existing.LinkUrl     = request.LinkUrl ?? existing.LinkUrl;
                existing.FilePath    = savedFilePath;
                existing.SubmittedAt = DateTime.UtcNow;
                existing.Status      = "Submitted";
                await _db.SaveChangesAsync(cancellationToken);
                submission = existing;
            }
            else
            {
                submission = new Submission
                {
                    AssignmentId = request.AssignmentId,
                    StudentId    = request.StudentId,
                    Content      = request.Content,
                    LinkUrl      = request.LinkUrl,
                    FilePath     = savedFilePath,
                    SubmittedAt  = DateTime.UtcNow,
                    Status       = "Submitted"
                };
                _db.Submissions.Add(submission);
                await _db.SaveChangesAsync(cancellationToken);
            }

            var student = await _db.Users.FindAsync(new object[] { request.StudentId }, cancellationToken);
            _db.Notifications.Add(new Notification
            {
                UserId      = assignment.TeacherId,
                Title       = "New Submission Received",
                Message     = $"{student?.FullName ?? "A student"} submitted an answer for \"{assignment.Title}\".",
                Type        = "submission_received",
                ReferenceId = assignment.Id,
                CreatedAt   = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);

            return OperationResult<Submission>.Success(submission);
        }
    }

    public record GradeSubmissionCommand(int SubmissionId, GradeSubmissionDto Dto) : IRequest<OperationResult<Submission>>;

    public class GradeSubmissionCommandHandler : IRequestHandler<GradeSubmissionCommand, OperationResult<Submission>>
    {
        private readonly AppDbContext _db;
        public GradeSubmissionCommandHandler(AppDbContext db) => _db = db;

        public async Task<OperationResult<Submission>> Handle(GradeSubmissionCommand request, CancellationToken cancellationToken)
        {
            var submission = await _db.Submissions
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, cancellationToken);

            if (submission == null)
                return OperationResult<Submission>.NotFound("Submission not found");

            if (submission.Assignment != null && request.Dto.Marks > submission.Assignment.MaxMarks)
                return OperationResult<Submission>.BadRequest($"Marks cannot exceed {submission.Assignment.MaxMarks}");

            submission.Marks    = request.Dto.Marks;
            submission.Feedback = request.Dto.Feedback;
            submission.Status   = string.IsNullOrWhiteSpace(request.Dto.Status) ? "Graded" : request.Dto.Status;

            await _db.SaveChangesAsync(cancellationToken);

            _db.Notifications.Add(new Notification
            {
                UserId      = submission.StudentId,
                Title       = "Your Submission Has Been Graded",
                Message     = $"You received {submission.Marks}/{submission.Assignment?.MaxMarks} for \"{submission.Assignment?.Title}\". {submission.Feedback}",
                Type        = "grade_posted",
                ReferenceId = submission.AssignmentId,
                CreatedAt   = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);

            return OperationResult<Submission>.Success(submission);
        }
    }

    public record GetAssignmentSubmissionsQuery(
        int AssignmentId,
        string? StatusFilter,
        int Page,
        int PageSize
    ) : IRequest<OperationResult<object>>;

    public class GetAssignmentSubmissionsQueryHandler : IRequestHandler<GetAssignmentSubmissionsQuery, OperationResult<object>>
    {
        private readonly AppDbContext _db;
        public GetAssignmentSubmissionsQueryHandler(AppDbContext db) => _db = db;

        public async Task<OperationResult<object>> Handle(GetAssignmentSubmissionsQuery request, CancellationToken cancellationToken)
        {
            var query = _db.Submissions
                .Include(s => s.Student)
                .Where(s => s.AssignmentId == request.AssignmentId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.StatusFilter))
                query = query.Where(s => s.Status == request.StatusFilter);

            var total    = await query.CountAsync(cancellationToken);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var page     = Math.Max(1, request.Page);

            var items = await query
                .OrderByDescending(s => s.SubmittedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.Id, s.AssignmentId, s.StudentId,
                    StudentName  = s.Student != null ? s.Student.FullName : "Unknown",
                    StudentEmail = s.Student != null ? s.Student.Email    : "",
                    s.Content, s.LinkUrl, s.FilePath,
                    s.SubmittedAt, s.Status, s.Marks, s.Feedback
                })
                .ToListAsync(cancellationToken);

            return OperationResult<object>.Success(new PagedResult<object>(items.Cast<object>(), page, pageSize, total));
        }
    }
}
