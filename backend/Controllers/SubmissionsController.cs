using System.Security.Claims;
using System.Threading.Tasks;
using AssignmentBackend.Common;
using AssignmentBackend.Features.Submissions;
using AssignmentBackend.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubmissionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SubmissionsController(IMediator mediator)
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

        [HttpPost]
        [Authorize(Roles = UserRoles.Student)]
        public async Task<IActionResult> SubmitAssignment([FromForm] int assignmentId, [FromForm] string? content, [FromForm] string? linkUrl, IFormFile? file)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = int.TryParse(userIdStr, out int studentId);

            var result = await _mediator.Send(new SubmitAssignmentCommand(assignmentId, studentId, content, linkUrl, file));
            return ToActionResult(result);
        }

        [HttpGet("assignment/{assignmentId}")]
        [Authorize(Roles = $"{UserRoles.Teacher},{UserRoles.Admin}")]
        public async Task<IActionResult> GetAssignmentSubmissions(
            int assignmentId,
            [FromQuery] string? statusFilter = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetAssignmentSubmissionsQuery(assignmentId, statusFilter, page, pageSize));
            return ToActionResult(result);
        }

        [HttpPut("{id}/grade")]
        [Authorize(Roles = $"{UserRoles.Teacher},{UserRoles.Admin}")]
        public async Task<IActionResult> GradeSubmission(int id, [FromBody] GradeSubmissionDto dto)
        {
            var result = await _mediator.Send(new GradeSubmissionCommand(id, dto));
            return ToActionResult(result);
        }
    }
}
