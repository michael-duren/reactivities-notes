using Application.Core;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Activities
{
    public class List
    {
        public class Query : IRequest<Result<List<ActivityDto>>>
        {
        }

        public class Handler : IRequestHandler<Query, Result<List<ActivityDto>>>
        {
            private readonly DataContext _context;
            private readonly IMapper _mapper;

            public Handler(DataContext context, IMapper mapper)
            {
                _context = context;
                _mapper = mapper;
            }

            public async Task<Result<List<ActivityDto>>> Handle(Query request, CancellationToken cancellationToken)
            {
                var activites = await _context.Activities
                    // automapper will project to our ActivityDto instead of using the .Include() and the .Theninclude
                    .ProjectTo<ActivityDto>(_mapper.ConfigurationProvider)
                    // from the join table include the users
                    .ToListAsync(cancellationToken: cancellationToken);

                // map the activities to an activitydto which is essentially a combination of the join table
                // and the activities model. 

                return Result<List<ActivityDto>>.Success(activites);
            }
        }
    }
}