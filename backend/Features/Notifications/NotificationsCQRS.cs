using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AssignmentBackend.Common;
using AssignmentBackend.Data;
using AssignmentBackend.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssignmentBackend.Features.Notifications
{
    public record GetMyNotificationsQuery(
        int UserId,
        bool UnreadOnly,
        int Page,
        int PageSize
    ) : IRequest<OperationResult<object>>;

    public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, OperationResult<object>>
    {
        private readonly AppDbContext _db;
        public GetMyNotificationsQueryHandler(AppDbContext db) => _db = db;

        public async Task<OperationResult<object>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
        {
            var query = _db.Notifications
                .Where(n => n.UserId == request.UserId)
                .AsQueryable();

            if (request.UnreadOnly)
                query = query.Where(n => !n.IsRead);

            var total    = await query.CountAsync(cancellationToken);
            var unread   = await _db.Notifications.CountAsync(n => n.UserId == request.UserId && !n.IsRead, cancellationToken);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var page     = Math.Max(1, request.Page);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new { n.Id, n.Title, n.Message, n.Type, n.ReferenceId, n.IsRead, n.CreatedAt })
                .ToListAsync(cancellationToken);

            return OperationResult<object>.Success(new
            {
                items,
                page, pageSize, total,
                totalPages  = pageSize > 0 ? (int)Math.Ceiling((double)total / pageSize) : 0,
                unreadCount = unread
            });
        }
    }

    public record MarkNotificationReadCommand(int NotificationId, int UserId) : IRequest<OperationResult<bool>>;

    public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, OperationResult<bool>>
    {
        private readonly AppDbContext _db;
        public MarkNotificationReadCommandHandler(AppDbContext db) => _db = db;

        public async Task<OperationResult<bool>> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
        {
            var n = await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == request.UserId, cancellationToken);

            if (n == null)
                return OperationResult<bool>.NotFound("Notification not found");

            n.IsRead = true;
            await _db.SaveChangesAsync(cancellationToken);
            return OperationResult<bool>.Success(true);
        }
    }

    public record MarkAllReadCommand(int UserId) : IRequest<OperationResult<bool>>;

    public class MarkAllReadCommandHandler : IRequestHandler<MarkAllReadCommand, OperationResult<bool>>
    {
        private readonly AppDbContext _db;
        public MarkAllReadCommandHandler(AppDbContext db) => _db = db;

        public async Task<OperationResult<bool>> Handle(MarkAllReadCommand request, CancellationToken cancellationToken)
        {
            await _db.Notifications
                .Where(n => n.UserId == request.UserId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}
