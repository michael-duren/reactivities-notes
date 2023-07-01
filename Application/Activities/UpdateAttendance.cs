using Application.Core;
using Application.Interfaces;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Activities;

public class UpdateAttendance
{
    public class Command : IRequest<Result<Unit>>
    {
        public Guid Id { get; set; }
    }

    public class Handler : IRequestHandler<Command, Result<Unit>>
    {
        private readonly DataContext _context;
        private readonly IUserAccessor _userAccessor;

        public Handler(DataContext context, IUserAccessor userAccessor)
        {
            _context = context;
            _userAccessor = userAccessor;
        }

        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            // get the activity based on the requests activity id
            var activity = await _context.Activities
                // include attendees (join table) 
                .Include(a => a.Attendees)
                // include from the attendees the app user
                .ThenInclude(u => u.AppUser)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (activity is null) return null;

            // get the user obj that is making the request
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName == _userAccessor.GetUsername(),
                cancellationToken);

            if (user is null) return null;

            // get host of activity from join table 
            var hostUsername = activity.Attendees.FirstOrDefault(x => x.IsHost)?.AppUser.UserName;

            // get attendance for this user
            var attendence = activity.Attendees.FirstOrDefault(x => x.AppUser.UserName == user.UserName);

            // check to see if host is the user 
            if (attendence is not null && hostUsername == user.UserName)
                activity.IsCancelled = !activity.IsCancelled;

            // check to see if the user needs to be removed from the attendees
            if (attendence is not null && hostUsername != user.UserName)
                activity.Attendees.Remove(attendence);
            // add user to the attendance if they are not currently added
            if (attendence is null)
            {
                attendence = new ActivityAttendee
                {
                    AppUser = user,
                    Activity = activity,
                    IsHost = false
                };

                activity.Attendees.Add(attendence);
            }

            var result = await _context.SaveChangesAsync(cancellationToken) > 0;

            return result ? Result<Unit>.Success(Unit.Value) : Result<Unit>.Failure("Problem updating attendance");
        }
    }
}