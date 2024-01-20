using MediatR;
using Microsoft.EntityFrameworkCore;
using TNG.Users.API.Database;

namespace TNG.Users.API.Features.Notification;

public class NotifyActivityCreated
{
    public class Command : IRequest<Unit>
    {
        public int ActivityId { get; init; }
    }

    internal sealed class Handler : IRequestHandler<Command, Unit>
    {
        private readonly UserDbContext _context;

        public Handler(UserDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
        {
            var activity =
                await _context.Activities.FirstOrDefaultAsync(x => x.Id == request.ActivityId, cancellationToken);
            if (activity == null) throw new KeyNotFoundException($"Activity {request.ActivityId} not found");

            await Task.Delay(3000, cancellationToken);
            Console.WriteLine($"SEND PUSH NOTIFICATIONS ABOUT ACTIVITY {activity.Id} {activity.Description}");
            
            return Unit.Value;
        }
    }
}