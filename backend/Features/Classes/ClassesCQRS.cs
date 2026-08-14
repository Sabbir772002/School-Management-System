using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AssignmentBackend.Common;
using AssignmentBackend.Data;
using AssignmentBackend.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssignmentBackend.Features.Classes
{
    // --- QUERIES ---

    public record GetClassesQuery() : IRequest<OperationResult<object>>;

    public class GetClassesQueryHandler : IRequestHandler<GetClassesQuery, OperationResult<object>>
    {
        private readonly AppDbContext _db;

        public GetClassesQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<OperationResult<object>> Handle(GetClassesQuery request, CancellationToken cancellationToken)
        {
            var classes = await _db.Classes
                .Include(c => c.Subjects)
                .ThenInclude(s => s.Teacher)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Code,
                    Subjects = c.Subjects.Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.TeacherId,
                        TeacherName = s.Teacher != null ? s.Teacher.FullName : "Not Assigned"
                    })
                })
                .ToListAsync(cancellationToken);

            return OperationResult<object>.Success(classes);
        }
    }

    // --- COMMANDS ---

    public record CreateClassCommand(CreateClassDto Dto) : IRequest<OperationResult<Class>>;

    public class CreateClassCommandHandler : IRequestHandler<CreateClassCommand, OperationResult<Class>>
    {
        private readonly AppDbContext _db;

        public CreateClassCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<OperationResult<Class>> Handle(CreateClassCommand request, CancellationToken cancellationToken)
        {
            var newClass = new Class
            {
                Name = request.Dto.Name,
                Code = request.Dto.Code
            };

            _db.Classes.Add(newClass);
            await _db.SaveChangesAsync(cancellationToken);

            return OperationResult<Class>.Success(newClass);
        }
    }

    public record CreateSubjectCommand(CreateSubjectDto Dto) : IRequest<OperationResult<Subject>>;

    public class CreateSubjectCommandHandler : IRequestHandler<CreateSubjectCommand, OperationResult<Subject>>
    {
        private readonly AppDbContext _db;

        public CreateSubjectCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<OperationResult<Subject>> Handle(CreateSubjectCommand request, CancellationToken cancellationToken)
        {
            var cls = await _db.Classes.FindAsync(new object[] { request.Dto.ClassId }, cancellationToken);
            if (cls == null)
            {
                return OperationResult<Subject>.NotFound("Class not found");
            }

            var subject = new Subject
            {
                Name = request.Dto.Name,
                ClassId = request.Dto.ClassId,
                TeacherId = request.Dto.TeacherId
            };

            _db.Subjects.Add(subject);
            await _db.SaveChangesAsync(cancellationToken);

            return OperationResult<Subject>.Success(subject);
        }
    }

    public record AssignTeacherCommand(AssignTeacherDto Dto) : IRequest<OperationResult<bool>>;

    public class AssignTeacherCommandHandler : IRequestHandler<AssignTeacherCommand, OperationResult<bool>>
    {
        private readonly AppDbContext _db;

        public AssignTeacherCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<OperationResult<bool>> Handle(AssignTeacherCommand request, CancellationToken cancellationToken)
        {
            var subject = await _db.Subjects.FindAsync(new object[] { request.Dto.SubjectId }, cancellationToken);
            if (subject == null)
            {
                return OperationResult<bool>.NotFound("Subject not found");
            }

            var teacher = await _db.Users.FindAsync(new object[] { request.Dto.TeacherId }, cancellationToken);
            if (teacher == null || teacher.Role != UserRoles.Teacher)
            {
                return OperationResult<bool>.BadRequest("Invalid teacher user id");
            }

            subject.TeacherId = request.Dto.TeacherId;
            await _db.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}
