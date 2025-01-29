namespace Application.Features.Produits.Queries.PaginationQuery;

/// <summary>
/// ProduitsWithPaginationQueryHandler class
/// </summary>
public class ProduitsWithPaginationQueryHandler : IRequestHandler<ProduitsWithPaginationQuery, PaginatedData<ProduitDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor : Initializes a new instance of DefsWithPaginationQueryHandler
    /// </summary>
    /// <param name="context"></param>
    /// <param name="mapper"></param>
    public ProduitsWithPaginationQueryHandler(
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
    public async Task<PaginatedData<ProduitDto>> Handle(ProduitsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        //var filters = PredicateBuilder.FromFilter<Produit>(request.FilterRules);
        var data = await _context.Produits.AsNoTracking()
            //.Where(filters)
            .OrderBy($"{request.Sort} {request.Order}")
            .ProjectTo<ProduitDto>(_mapper.ConfigurationProvider)
            .PaginatedDataAsync(request.Page, request.Rows).ConfigureAwait(false);

        return data;
    }
}

