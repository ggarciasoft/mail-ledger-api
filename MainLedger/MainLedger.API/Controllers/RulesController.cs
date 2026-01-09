using MainLedger.Application.Authentication.Services;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RulesController : ControllerBase
{
    private readonly IRuleRepository _ruleRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RulesController> _logger;

    public RulesController(
        IRuleRepository ruleRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<RulesController> logger)
    {
        _ruleRepository = ruleRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Gets all rules for the current user (both active and inactive).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserRules(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("User ID not found in authentication context");
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var rules = await _ruleRepository.GetByUserIdAsync(userId.Value, cancellationToken);
            return Ok(rules.Select(r => new
            {
                id = r.Id,
                name = r.Name,
                senderPattern = r.SenderPattern,
                subjectPattern = r.SubjectPattern,
                keywordPattern = r.KeywordPattern,
                labelPattern = r.LabelPattern,
                priority = r.Priority,
                isActive = r.IsActive,
                createdAt = r.CreatedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rules for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new filtering rule.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRule([FromBody] CreateRuleRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("User ID not found in authentication context");
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            // Verify user exists
            var userExists = await _userRepository.ExistsAsync(userId.Value, cancellationToken);
            if (!userExists)
            {
                return BadRequest(new { error = "User not found." });
            }

            var rule = Rule.Create(
                userId.Value,
                request.Name,
                request.SenderPattern,
                request.SubjectPattern,
                request.KeywordPattern,
                request.LabelPattern,
                request.Priority ?? 0
            );

            await _ruleRepository.AddAsync(rule, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                id = rule.Id,
                message = "Rule created successfully",
                rule = new
                {
                    id = rule.Id,
                    name = rule.Name,
                    senderPattern = rule.SenderPattern,
                    subjectPattern = rule.SubjectPattern,
                    keywordPattern = rule.KeywordPattern,
                    labelPattern = rule.LabelPattern,
                    priority = rule.Priority,
                    isActive = rule.IsActive
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rule for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific rule by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRule(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
            if (rule == null)
            {
                return NotFound(new { error = "Rule not found." });
            }

            return Ok(new
            {
                id = rule.Id,
                userId = rule.UserId,
                name = rule.Name,
                senderPattern = rule.SenderPattern,
                subjectPattern = rule.SubjectPattern,
                keywordPattern = rule.KeywordPattern,
                labelPattern = rule.LabelPattern,
                priority = rule.Priority,
                isActive = rule.IsActive,
                createdAt = rule.CreatedAt,
                updatedAt = rule.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates a rule (name, patterns, and/or priority).
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRule(
        Guid id,
        [FromBody] UpdateRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
            if (rule == null)
            {
                return NotFound(new { error = "Rule not found." });
            }

            // Update name if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                rule.UpdateName(request.Name);
            }

            // Update patterns
            rule.UpdatePatterns(
                request.SenderPattern,
                request.SubjectPattern,
                request.KeywordPattern,
                request.LabelPattern
            );

            // Update priority if provided
            if (request.Priority.HasValue)
            {
                rule.UpdatePriority(request.Priority.Value);
            }

            _ruleRepository.Update(rule);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Rule updated successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid rule update request for rule {RuleId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rule {RuleId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates a rule's name.
    /// </summary>
    [HttpPut("{id}/name")]
    public async Task<IActionResult> UpdateRuleName(
        Guid id,
        [FromBody] UpdateRuleNameRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
            if (rule == null)
            {
                return NotFound(new { error = "Rule not found." });
            }

            rule.UpdateName(request.Name);
            _ruleRepository.Update(rule);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Rule name updated successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid rule name for rule {RuleId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rule name for {RuleId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates a rule's patterns.
    /// </summary>
    [HttpPut("{id}/patterns")]
    public async Task<IActionResult> UpdateRulePatterns(
        Guid id,
        [FromBody] UpdateRulePatternsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
            if (rule == null)
            {
                return NotFound(new { error = "Rule not found." });
            }

            rule.UpdatePatterns(
                request.SenderPattern,
                request.SubjectPattern,
                request.KeywordPattern,
                request.LabelPattern
            );

            _ruleRepository.Update(rule);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Rule patterns updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates a rule's priority.
    /// </summary>
    [HttpPut("{id}/priority")]
    public async Task<IActionResult> UpdateRulePriority(
        Guid id,
        [FromBody] UpdateRulePriorityRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
            if (rule == null)
            {
                return NotFound(new { error = "Rule not found." });
            }

            rule.UpdatePriority(request.Priority);
            _ruleRepository.Update(rule);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Rule priority updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Activates a rule.
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivateRule(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
            if (rule == null)
            {
                return NotFound(new { error = "Rule not found." });
            }

            rule.Activate();
            _ruleRepository.Update(rule);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Rule activated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deactivates a rule.
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateRule(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
            if (rule == null)
            {
                return NotFound(new { error = "Rule not found." });
            }

            rule.Deactivate();
            _ruleRepository.Update(rule);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Rule deactivated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

// Request DTOs
public record CreateRuleRequest(
    string Name,
    string? SenderPattern,
    string? SubjectPattern,
    string? KeywordPattern,
    string? LabelPattern,
    int? Priority
);

public record UpdateRuleRequest(
    string? Name,
    string? SenderPattern,
    string? SubjectPattern,
    string? KeywordPattern,
    string? LabelPattern,
    int? Priority
);

public record UpdateRuleNameRequest(string Name);

public record UpdateRulePatternsRequest(
    string? SenderPattern,
    string? SubjectPattern,
    string? KeywordPattern,
    string? LabelPattern
);

public record UpdateRulePriorityRequest(int Priority);

