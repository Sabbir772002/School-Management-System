using System.Security.Claims;
using System.Threading.Tasks;
using AssignmentBackend.Common;
using AssignmentBackend.Features.Auth;
using AssignmentBackend.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private IActionResult ToActionResult<T>(OperationResult<T> result)
        {
            return result.Status switch
            {
                OperationStatus.Success => Ok(result.Data),
                OperationStatus.NotFound => NotFound(new { message = result.ErrorMessage }),
                OperationStatus.BadRequest => BadRequest(new { message = result.ErrorMessage }),
                OperationStatus.Forbidden => Forbid(),
                OperationStatus.Unauthorized => Unauthorized(new { message = result.ErrorMessage }),
                _ => BadRequest(new { message = result.ErrorMessage })
            };
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var command = new LoginCommand(dto);
            var result = await _mediator.Send(command);
            return ToActionResult(result);
        }

        // GET: api/auth/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            var query = new GetCurrentUserQuery(userId);
            var result = await _mediator.Send(query);
            return ToActionResult(result);
        }
    }
}
