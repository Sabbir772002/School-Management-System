using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AssignmentBackend.Common;
using AssignmentBackend.Data;
using AssignmentBackend.Models;
using AssignmentBackend.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssignmentBackend.Features.Users
{
    public record GetAllUsersQuery() : IRequest<OperationResult<object>>;

    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, OperationResult<object>>
    {
        private readonly AppDbContext _db;

        public GetAllUsersQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<OperationResult<object>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _db.Users
                .Include(u => u.StudentClass)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.ClassId,
                    ClassName = u.StudentClass != null ? u.StudentClass.Name : null
                })
                .ToListAsync(cancellationToken);

            return OperationResult<object>.Success(users);
        }
    }

    public record CreateUserCommand(CreateUserDto Dto) : IRequest<OperationResult<int>>;

    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, OperationResult<int>>
    {
        private readonly AppDbContext _db;
        private readonly IAuthService _authService;

        public CreateUserCommandHandler(AppDbContext db, IAuthService authService)
        {
            _db = db;
            _authService = authService;
        }

        public async Task<OperationResult<int>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var exist = await _db.Users.AnyAsync(u => u.Email.ToLower() == request.Dto.Email.ToLower(), cancellationToken);
            if (exist)
            {
                return OperationResult<int>.BadRequest("Email already in use");
            }

            var newUser = new User
            {
                FullName = request.Dto.FullName,
                Email = request.Dto.Email,
                PasswordHash = _authService.HashPassword(request.Dto.Password),
                Role = request.Dto.Role,
                ClassId = request.Dto.Role == UserRoles.Student ? request.Dto.ClassId : null
            };

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync(cancellationToken);

            return OperationResult<int>.Success(newUser.Id);
        }
    }

    public record DeleteUserCommand(int Id) : IRequest<OperationResult<bool>>;

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, OperationResult<bool>>
    {
        private readonly AppDbContext _db;

        public DeleteUserCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<OperationResult<bool>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _db.Users.FindAsync(new object[] { request.Id }, cancellationToken);
            if (user == null)
            {
                return OperationResult<bool>.NotFound("User not found");
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}
