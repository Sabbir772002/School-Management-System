using System.Security.Claims;
using System.Threading.Tasks;
using AssignmentBackend.Common;
using AssignmentBackend.Features.Assignments;
using AssignmentBackend.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace AssignmentBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AssignmentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AssignmentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private IActionResult ToActionResult<T>(OperationResult<T> result) => result.Status switch
        {
            OperationStatus.Success    => Ok(result.Data),
            OperationStatus.NotFound   => NotFound(new { message = result.ErrorMessage }),
            OperationStatus.BadRequest => BadRequest(new { message = result.ErrorMessage }),
            OperationStatus.Forbidden  => Forbid(),
            OperationStatus.Unauthorized => Unauthorized(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };

        [HttpGet]
        public async Task<IActionResult> GetAssignments(
            [FromQuery] int?      classId      = null,
            [FromQuery] string?   search       = null,
            [FromQuery] int?      subjectId    = null,
            [FromQuery] bool?     isPublished  = null,
            [FromQuery] DateTime? deadlineFrom = null,
            [FromQuery] DateTime? deadlineTo   = null,
            [FromQuery] string    sortBy       = "createdAt",
            [FromQuery] string    sortDir      = "desc",
            [FromQuery] int       page         = 1,
            [FromQuery] int       pageSize     = 10)
        {
            var userRole  = User.FindFirstValue(ClaimTypes.Role) ?? "";
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = int.TryParse(userIdStr, out int userId);

            var query = new GetAssignmentsQuery(
                classId, userId, userRole,
                search, subjectId, isPublished, deadlineFrom, deadlineTo,
                sortBy, sortDir, page, pageSize);

            var result = await _mediator.Send(query);
            return ToActionResult(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAssignmentById(int id)
        {
            var userRole  = User.FindFirstValue(ClaimTypes.Role) ?? "";
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = int.TryParse(userIdStr, out int userId);

            var result = await _mediator.Send(new GetAssignmentByIdQuery(id, userId, userRole));
            return ToActionResult(result);
        }

        [HttpPost]
        [Authorize(Roles = $"{UserRoles.Teacher},{UserRoles.Admin}")]
        public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentDto dto)
        {
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
            var result = await _mediator.Send(new CreateAssignmentCommand(dto, userId));
            return ToActionResult(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = $"{UserRoles.Teacher},{UserRoles.Admin}")]
        public async Task<IActionResult> UpdateAssignment(int id, [FromBody] CreateAssignmentDto dto)
        {
            var result = await _mediator.Send(new UpdateAssignmentCommand(id, dto));
            return ToActionResult(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = $"{UserRoles.Teacher},{UserRoles.Admin}")]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            var result = await _mediator.Send(new DeleteAssignmentCommand(id));
            if (result.IsSuccess) return Ok(new { message = "Assignment deleted" });
            return ToActionResult(result);
        }
    }
}
