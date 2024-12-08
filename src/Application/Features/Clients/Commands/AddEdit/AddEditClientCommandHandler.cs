
namespace Application.Features.Clients.Commands.AddEdit;

/// <summary>
/// AddEditClientCommandHandler class
/// </summary>
public class AddEditClientCommandHandler : IRequestHandler<AddEditClientCommand, Result<int>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor : Initializes a new instance of AddEditClientCommandHandler 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="mapper"></param>
    public AddEditClientCommandHandler(
         IApplicationDbContext context,
         IMapper mapper
        )
    {
        _context = context;
        _mapper = mapper;
    }

    /// <summary>
    /// Handle
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<int>> Handle(AddEditClientCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Id > 0)
            {
                var clients = await _context.Clients.FindAsync(new object[] { request.Id }, cancellationToken);
                clients = _mapper.Map(request, clients);
                await _context.SaveChangesAsync(cancellationToken);
                return await Result<int>.SuccessAsync(clients.Id);
            }
            else
            {
                request.Created = DateTime.UtcNow;
                var clients = _mapper.Map<Client>(request);
                _context.Clients.Add(clients);
                await _context.SaveChangesAsync(cancellationToken);
                return await Result<int>.SuccessAsync(clients.Id);
            }
        }
        catch (Exception ex)
        {
            return await Result<int>.FailureAsync(new string[] { ex.Message }).ConfigureAwait(false);
        }
    }
}