using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AssignmentBackend.Common;
using AssignmentBackend.Data;
using AssignmentBackend.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssignmentBackend.Features.Assignments
{
    public record GetAssignmentsQuery(
        int? ClassId,
        int UserId,
        string UserRole,
        string? Search,
        int? SubjectId,
        bool? IsPublished,
        DateTime? DeadlineFrom,
        DateTime? DeadlineTo,
        string SortBy,
        string SortDir,
        int Page,
        int PageSize
    ) : IRequest<OperationResult<object>>;

    public class GetAssignmentsQueryHandler : IRequestHandler<GetAssignmentsQuery, OperationResult<object>>
    {
        private readonly AppDbContext _db;
        public GetAssignmentsQueryHandler(AppDbContext db) => _db = db;

        public async Task<OperationResult<object>> Handle(GetAssignmentsQuery request, CancellationToken cancellationToken)
        {
            var query = _db.Assignments
                .Include(a => a.Class)
                .Include(a => a.Subject)
                .Include(a => a.Teacher)
                .Include(a => a.Submissions)
                .AsQueryable();

            if (request.UserRole == UserRoles.Student)
            {
                var user = await _db.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
                if (user?.ClassId == null)
                    return OperationResult<object>.Success(new PagedResult<object>(new List<object>(), request.Page, request.PageSize, 0));

                query = query.Where(a => a.ClassId == user.ClassId && a.IsPublished);
            }
            else if (request.UserRole == UserRoles.Teacher)
            {
                query = request.ClassId.HasValue
                    ? query.Where(a => a.ClassId == request.ClassId.Value)
                    : query.Where(a => a.TeacherId == request.UserId);
            }
            else if (request.ClassId.HasValue)
            {
                query = query.Where(a => a.ClassId == request.ClassId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim().ToLower();
                query = query.Where(a =>
                    EF.Functions.ILike(a.Title, $"%{term}%") ||
                    EF.Functions.ILike(a.Description, $"%{term}%"));
            }

            if (request.SubjectId.HasValue)
                query = query.Where(a => a.SubjectId == request.SubjectId.Value);

            if (request.IsPublished.HasValue)
                query = query.Where(a => a.IsPublished == request.IsPublished.Value);

            if (request.DeadlineFrom.HasValue)
                query = query.Where(a => a.Deadline >= request.DeadlineFrom.Value);

            if (request.DeadlineTo.HasValue)
                query = query.Where(a => a.Deadline <= request.DeadlineTo.Value);

            var desc = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
            query = request.SortBy?.ToLower() switch
            {
                "deadline" => desc ? query.OrderByDescending(a => a.Deadline)   : query.OrderBy(a => a.Deadline),
                "title"    => desc ? query.OrderByDescending(a => a.Title)      : query.OrderBy(a => a.Title),
                _          => desc ? query.OrderByDescending(a => a.CreatedAt)  : query.OrderBy(a => a.CreatedAt),
            };

            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var page     = Math.Max(1, request.Page);
            var total    = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id, a.Title, a.Description, a.Deadline,
                    a.MaxMarks, a.IsPublished, a.ClassId,
                    ClassName   = a.Class   != null ? a.Class.Name    : "",
                    a.SubjectId,
                    SubjectName = a.Subject != null ? a.Subject.Name  : "",
                    a.TeacherId,
                    TeacherName = a.Teacher != null ? a.Teacher.FullName : "",
                    a.CreatedAt,
                    SubmissionsCount = a.Submissions.Count,
                    MySubmission = a.Submissions
                        .Where(s => s.StudentId == request.UserId)
                        .Select(s => new { s.Id, s.SubmittedAt, s.Status, s.Marks, s.Feedback })
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            return OperationResult<object>.Success(new PagedResult<object>(items.Cast<object>(), page, pageSize, total));
        }
    }

    public record GetAssignmentByIdQuery(int Id, int UserId, string UserRole) : IRequest<OperationResult<object>>;

    public class GetAssignmentByIdQueryHandler : IRequestHandler<GetAssignmentByIdQuery, OperationResult<object>>
    {
        private readonly AppDbContext _db;
        public GetAssignmentByIdQueryHandler(AppDbContext db) => _db = db;

        public async Task<OperationResult<object>> Handle(GetAssignmentByIdQuery request, CancellationToken cancellationToken)
        {
            var assignment = await _db.Assignments
                .Include(a => a.Class)
                .Include(a => a.Subject)
                .Include(a => a.Teacher)
                .Include(a => a.Submissions).ThenInclude(s => s.Student)
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

            if (assignment == null)
                return OperationResult<object>.NotFound("Assignment not found");

            if (request.UserRole == UserRoles.Student && !assignment.IsPublished)
                return OperationResult<object>.Forbidden("Students cannot view unpublished assignments");

            var result = new
            {
                assignment.Id, assignment.Title, assignment.Description, assignment.Deadline,
                assignment.MaxMarks, assignment.IsPublished, assignment.ClassId,
                ClassName   = assignment.Class?.Name,
                assignment.SubjectId,
                SubjectName = assignment.Subject?.Name,
                assignment.TeacherId,
                TeacherName = assignment.Teacher?.FullName,
                assignment.CreatedAt,
                Submissions = request.UserRole != UserRoles.Student
                    ? assignment.Submissions.Select(s => new
                    {
                        s.Id, s.StudentId,
                        StudentName  = s.Student?.FullName,
                        StudentEmail = s.Student?.Email,
                        s.Content, s.LinkUrl, s.FilePath,
                        s.SubmittedAt, s.Status, s.Marks, s.Feedback
                    })
                    : null
            };

            return OperationResult<object>.Success(result);
        }
    }

    public record CreateAssignmentCommand(CreateAssignmentDto Dto, int TeacherId) : IRequest<OperationResult<Assignment>>;

    public class CreateAssignmentCommandHandler : IRequestHandler<CreateAssignmentCommand, OperationResult<Assignment>>
    {
        private readonly AppDbContext _db;
        public CreateAssignmentCommandHandler(AppDbContext db) => _db = db;

        public async Task<OperationResult<Assignment>> Handle(CreateAssignmentCommand request, CancellationToken cancellationToken)
        {
            var assignment = new Assignment
            {
                Title       = request.Dto.Title,
                Description = request.Dto.Description,
                Deadline    = request.Dto.Deadline.ToUniversalTime(),
                MaxMarks    = request.Dto.MaxMarks,
                IsPublished = request.Dto.IsPublished,
                ClassId     = request.Dto.ClassId,
                SubjectId   = request.Dto.SubjectId,
                TeacherId   = request.TeacherId,
                CreatedAt   = DateTime.UtcNow
            };

            _db.Assignments.Add(assignment);
            await _db.SaveChangesAsync(cancellationToken);

            if (assignment.IsPublished)
                await NotifyClassStudents(assignment, cancellationToken);

            return OperationResult<Assignment>.Success(assignment);
        }

        private async Task NotifyClassStudents(Assignment assignment, CancellationToken ct)
        {
            var studentIds = await _db.Users
                .Where(u => u.Role == UserRoles.Student && u.ClassId == assignment.ClassId)
                .Select(u => u.Id)
                .ToListAsync(ct);

            _db.Notifications.AddRange(studentIds.Select(sid => new Notification
            {
                UserId      = sid,
                Title       = "New Assignment Published",
                Message     = $"\"{assignment.Title}\" has been published for your class.",
                Type        = "assignment_published",
                ReferenceId = assignment.Id,
                CreatedAt   = DateTime.UtcNow
            }));

            await _db.SaveChangesAsync(ct);
        }
    }

    public record UpdateAssignmentCommand(int Id, CreateAssignmentDto Dto) : IRequest<OperationResult<Assignment>>;

    public class UpdateAssignmentCommandHandler : IRequestHandler<UpdateAssignmentCommand, OperationResult<Assignment>>
    {
        private readonly AppDbContext _db;
        public UpdateAssignmentCommandHandler(AppDbContext db) => _db = db;

        public async Task<OperationResult<Assignment>> Handle(UpdateAssignmentCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _db.Assignments.FindAsync(new object[] { request.Id }, cancellationToken);
            if (assignment == null)
                return OperationResult<Assignment>.NotFound("Assignment not found");

            var wasUnpublished = !assignment.IsPublished;

            assignment.Title       = request.Dto.Title;
            assignment.Description = request.Dto.Description;
            assignment.Deadline    = request.Dto.Deadline.ToUniversalTime();
            assignment.MaxMarks    = request.Dto.MaxMarks;
            assignment.IsPublished = request.Dto.IsPublished;
            assignment.ClassId     = request.Dto.ClassId;
            assignment.SubjectId   = request.Dto.SubjectId;

            await _db.SaveChangesAsync(cancellationToken);

            if (wasUnpublished && assignment.IsPublished)
            {
                var studentIds = await _db.Users
                    .Where(u => u.Role == UserRoles.Student && u.ClassId == assignment.ClassId)
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);

                _db.Notifications.AddRange(studentIds.Select(sid => new Notification
                {
                    UserId      = sid,
                    Title       = "Assignment Published",
                    Message     = $"\"{assignment.Title}\" is now available for submission.",
                    Type        = "assignment_published",
                    ReferenceId = assignment.Id,
                    CreatedAt   = DateTime.UtcNow
                }));

                await _db.SaveChangesAsync(cancellationToken);
            }

            return OperationResult<Assignment>.Success(assignment);
        }
    }

    public record DeleteAssignmentCommand(int Id) : IRequest<OperationResult<bool>>;

    public class DeleteAssignmentCommandHandler : IRequestHandler<DeleteAssignmentCommand, OperationResult<bool>>
    {
        private readonly AppDbContext _db;
        public DeleteAssignmentCommandHandler(AppDbContext db) => _db = db;

        public async Task<OperationResult<bool>> Handle(DeleteAssignmentCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _db.Assignments.FindAsync(new object[] { request.Id }, cancellationToken);
            if (assignment == null)
                return OperationResult<bool>.NotFound("Assignment not found");

            _db.Assignments.Remove(assignment);
            await _db.SaveChangesAsync(cancellationToken);
            return OperationResult<bool>.Success(true);
        }
    }
}
