using MainLedger.Contracts.Categories;
using MediatR;

namespace MainLedger.Application.Categories.Queries;

/// <summary>
/// Query to get all categories for a user.
/// </summary>
public record GetCategoriesQuery(Guid UserId) : IRequest<CategoryListResponse>;
