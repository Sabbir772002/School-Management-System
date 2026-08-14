using System.Security.Claims;
using System.Threading.Tasks;
using AssignmentBackend.Common;
using AssignmentBackend.Features.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public NotificationsController(IMediator mediator) => _mediator = mediator;

        private int CurrentUserId()
        {
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id);
            return id;
        }

        private IActionResult ToActionResult<T>(OperationResult<T> result) => result.Status switch
        {
            OperationStatus.Success   => Ok(result.Data),
            OperationStatus.NotFound  => NotFound(new { message = result.ErrorMessage }),
            OperationStatus.Forbidden => Forbid(),
            _                         => BadRequest(new { message = result.ErrorMessage })
        };

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications(
            [FromQuery] bool   unreadOnly = false,
            [FromQuery] int    page       = 1,
            [FromQuery] int    pageSize   = 20)
        {
            var result = await _mediator.Send(new GetMyNotificationsQuery(CurrentUserId(), unreadOnly, page, pageSize));
            return ToActionResult(result);
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var result = await _mediator.Send(new MarkNotificationReadCommand(id, CurrentUserId()));
            return ToActionResult(result);
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            await _mediator.Send(new MarkAllReadCommand(CurrentUserId()));
            return Ok(new { message = "All notifications marked as read" });
        }
    }
}
