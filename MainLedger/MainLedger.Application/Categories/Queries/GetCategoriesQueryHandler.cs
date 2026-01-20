using MainLedger.Contracts.Categories;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Categories.Queries;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, CategoryListResponse>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<GetCategoriesQueryHandler> _logger;

    public GetCategoriesQueryHandler(
        ICategoryRepository categoryRepository,
        ILogger<GetCategoriesQueryHandler> logger
    )
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<CategoryListResponse> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Getting categories for user {UserId}", request.UserId);

        var categories = await _categoryRepository.GetAllAsync(
            cancellationToken
        );

        var categoryDtos = categories
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
            })
            .ToList();

        return new CategoryListResponse { Categories = categoryDtos };
    }
}
