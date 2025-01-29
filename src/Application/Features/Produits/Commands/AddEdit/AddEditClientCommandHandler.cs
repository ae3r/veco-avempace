
namespace Application.Features.Produits.Commands.AddEdit;

/// <summary>
/// AddEditProduitCommandHandler class
/// </summary>
public class AddEditProduitCommandHandler : IRequestHandler<AddEditProduitCommand, Result<int>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IMailService _mailService;


    /// <summary>
    /// Constructor : Initializes a new instance of AddEditProduitCommandHandler 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="mapper"></param>
    public AddEditProduitCommandHandler(
         IApplicationDbContext context,
         IMapper mapper, IMailService mailService
        )
    {
        _context = context;
        _mapper = mapper;
        _mailService = mailService;
    }

    /// <summary>
    /// Handle
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<int>> Handle(AddEditProduitCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Id > 0)
            {
                var produits = await _context.Produits.FindAsync(new object[] { request.Id }, cancellationToken);
                produits = _mapper.Map(request, produits);
                await _context.SaveChangesAsync(cancellationToken);
                return await Result<int>.SuccessAsync(produits.Id);
            }
            else
            {
                request.Created = DateTime.UtcNow;
                var Produits = _mapper.Map<Produit>(request);
                _context.Produits.Add(Produits);
                await _context.SaveChangesAsync(cancellationToken);
                
                return await Result<int>.SuccessAsync(Produits.Id);
            }
        }
        catch (Exception ex)
        {
            return await Result<int>.FailureAsync(new string[] { ex.Message }).ConfigureAwait(false);
        }
    }
}
