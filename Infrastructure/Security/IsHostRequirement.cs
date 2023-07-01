using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Infrastructure.Security;

public class IsHostRequirement : IAuthorizationRequirement
{
}

public class IsHostRequirementHandler : AuthorizationHandler<IsHostRequirement>
{
    private readonly DataContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IsHostRequirementHandler(DataContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IsHostRequirement requirement)
    {
        // get the user id from the claims 
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Task.CompletedTask;

        // get the activity id from the httpcontext, parse with Guid to transform to Guid 
        // the id is gotten from the path I believe in the URL
        var activityId = Guid.Parse(_httpContextAccessor.HttpContext?.Request.RouteValues
            .SingleOrDefault(x => x.Key == "id").Value?.ToString() ?? string.Empty);

        // we cannot use await since we're overriding the HandleRequirementAsync, so we can use the .Result of the FindAsync
        var attendee = _dbContext.ActivityAttendees
            // do not track this entity in memory
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.AppUserId == userId && x.ActivityId == activityId).Result;
        if (attendee is null) return Task.CompletedTask;

        // if attendee is host then mark the AuthorizationHandlerContext as successful
        if (attendee.IsHost) context.Succeed(requirement);

        return Task.CompletedTask;
    }
}