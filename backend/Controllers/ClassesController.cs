using System.Threading.Tasks;
using AssignmentBackend.Common;
using AssignmentBackend.Features.Classes;
using AssignmentBackend.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClassesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ClassesController(IMediator mediator)
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

        // GET: api/classes
        [HttpGet]
        public async Task<IActionResult> GetClasses()
        {
            var query = new GetClassesQuery();
            var result = await _mediator.Send(query);
            return ToActionResult(result);
        }

        // POST: api/classes
        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassDto dto)
        {
            var command = new CreateClassCommand(dto);
            var result = await _mediator.Send(command);
            return ToActionResult(result);
        }

        // POST: api/classes/subjects
        [HttpPost("subjects")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectDto dto)
        {
            var command = new CreateSubjectCommand(dto);
            var result = await _mediator.Send(command);
            return ToActionResult(result);
        }

        // POST: api/classes/assign-teacher
        [HttpPost("assign-teacher")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> AssignTeacher([FromBody] AssignTeacherDto dto)
        {
            var command = new AssignTeacherCommand(dto);
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                return Ok(new { message = "Teacher assigned to subject successfully" });
            }
            return ToActionResult(result);
        }
    }
}
