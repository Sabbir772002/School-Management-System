using System.Threading.Tasks;
using AssignmentBackend.Common;
using AssignmentBackend.Features.Users;
using AssignmentBackend.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoles.Admin)]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private IActionResult ToActionResult<T>(OperationResult<T> result) => result.Status switch
        {
            OperationStatus.Success      => Ok(result.Data),
            OperationStatus.NotFound     => NotFound(new { message = result.ErrorMessage }),
            OperationStatus.BadRequest   => BadRequest(new { message = result.ErrorMessage }),
            OperationStatus.Forbidden    => Forbid(),
            OperationStatus.Unauthorized => Unauthorized(new { message = result.ErrorMessage }),
            _                            => BadRequest(new { message = result.ErrorMessage })
        };

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _mediator.Send(new GetAllUsersQuery());
            return ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            var result = await _mediator.Send(new CreateUserCommand(dto));
            if (result.IsSuccess)
            {
                return Ok(new { message = "User created successfully", userId = result.Data });
            }
            return ToActionResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _mediator.Send(new DeleteUserCommand(id));
            if (result.IsSuccess)
            {
                return Ok(new { message = "User deleted successfully" });
            }
            return ToActionResult(result);
        }
    }
}
