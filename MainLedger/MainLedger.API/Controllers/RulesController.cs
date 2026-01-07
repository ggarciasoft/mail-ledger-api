using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase
{
    private readonly IRuleRepository _ruleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RulesController(
        IRuleRepository ruleRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _ruleRepository = ruleRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Gets all active rules for a user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserRules([FromQuery] Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var rules = await _ruleRepository.GetActiveByUserIdAsync(userId, cancellationToken);
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
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new filtering rule.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRule([FromBody] CreateRuleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify user exists
            var userExists = await _userRepository.ExistsAsync(request.UserId, cancellationToken);
            if (!userExists)
            {
                return BadRequest(new { error = "User not found." });
            }

            var rule = Rule.Create(
                request.UserId,
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
    Guid UserId,
    string Name,
    string? SenderPattern,
    string? SubjectPattern,
    string? KeywordPattern,
    string? LabelPattern,
    int? Priority
);

public record UpdateRulePatternsRequest(
    string? SenderPattern,
    string? SubjectPattern,
    string? KeywordPattern,
    string? LabelPattern
);

public record UpdateRulePriorityRequest(int Priority);

