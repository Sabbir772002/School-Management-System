using System.Threading;
using System.Threading.Tasks;
using AssignmentBackend.Common;
using AssignmentBackend.Data;
using AssignmentBackend.Models;
using AssignmentBackend.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssignmentBackend.Features.Auth
{
    // --- COMMANDS ---

    public record LoginCommand(LoginDto Dto) : IRequest<OperationResult<LoginResponseDto>>;

    public class LoginCommandHandler : IRequestHandler<LoginCommand, OperationResult<LoginResponseDto>>
    {
        private readonly AppDbContext _db;
        private readonly IAuthService _authService;

        public LoginCommandHandler(AppDbContext db, IAuthService authService)
        {
            _db = db;
            _authService = authService;
        }

        public async Task<OperationResult<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _db.Users
                .Include(u => u.StudentClass)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Dto.Email.ToLower(), cancellationToken);

            if (user == null)
            {
                return OperationResult<LoginResponseDto>.BadRequest("Invalid email or password");
            }

            bool isValid = _authService.VerifyPassword(request.Dto.Password, user.PasswordHash);
            if (!isValid)
            {
                return OperationResult<LoginResponseDto>.BadRequest("Invalid email or password");
            }

            var token = _authService.GenerateJwtToken(user);

            var response = new LoginResponseDto
            {
                Token = token,
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                ClassId = user.ClassId,
                ClassName = user.StudentClass?.Name
            };

            return OperationResult<LoginResponseDto>.Success(response);
        }
    }

    // --- QUERIES ---

    public record GetCurrentUserQuery(int UserId) : IRequest<OperationResult<object>>;

    public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, OperationResult<object>>
    {
        private readonly AppDbContext _db;

        public GetCurrentUserQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<OperationResult<object>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var user = await _db.Users
                .Include(u => u.StudentClass)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                return OperationResult<object>.NotFound("User not found");
            }

            var result = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                user.ClassId,
                ClassName = user.StudentClass?.Name
            };

            return OperationResult<object>.Success(result);
        }
    }
}
