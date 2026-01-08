using MainLedger.Domain.Common;

namespace MainLedger.Domain.ValueObjects;

/// <summary>
/// Represents a permission scope for authorization.
/// Scopes follow the pattern: {action}:{resource}
/// Example: read:transactions, write:rules, admin:all
/// </summary>
public sealed class Scope : ValueObject
{
    // Predefined scopes
    public static readonly Scope ReadTransactions = new("read:transactions");
    public static readonly Scope WriteTransactions = new("write:transactions");
    public static readonly Scope ReadRules = new("read:rules");
    public static readonly Scope WriteRules = new("write:rules");
    public static readonly Scope ReadEmails = new("read:emails");
    public static readonly Scope WriteEmails = new("write:emails");
    public static readonly Scope ReadUsers = new("read:users");
    public static readonly Scope WriteUsers = new("write:users");
    public static readonly Scope AdminAll = new("admin:all");

    public string Value { get; }
    public string Action { get; }
    public string Resource { get; }

    private Scope(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Scope cannot be empty.", nameof(value));

        var parts = value.Split(':');
        if (parts.Length != 2)
            throw new ArgumentException("Scope must follow the pattern 'action:resource'.", nameof(value));

        Value = value.ToLowerInvariant();
        Action = parts[0].ToLowerInvariant();
        Resource = parts[1].ToLowerInvariant();
    }

    /// <summary>
    /// Creates a scope from a string value.
    /// </summary>
    public static Scope Create(string scope)
    {
        return new Scope(scope);
    }

    /// <summary>
    /// Creates a scope from action and resource.
    /// </summary>
    public static Scope Create(string action, string resource)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be empty.", nameof(action));
        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource cannot be empty.", nameof(resource));

        return new Scope($"{action}:{resource}");
    }

    /// <summary>
    /// Checks if this scope grants access to the specified scope.
    /// admin:all grants access to everything.
    /// </summary>
    public bool Grants(Scope requiredScope)
    {
        if (requiredScope == null)
            throw new ArgumentNullException(nameof(requiredScope));

        // admin:all grants everything
        if (Value == "admin:all")
            return true;

        // Exact match
        if (Value == requiredScope.Value)
            return true;

        // Wildcard resource match (e.g., read:* grants read:transactions)
        if (Resource == "*" && Action == requiredScope.Action)
            return true;

        return false;
    }

    /// <summary>
    /// Checks if this scope is an admin scope.
    /// </summary>
    public bool IsAdmin()
    {
        return Value == "admin:all" || Action == "admin";
    }

    /// <summary>
    /// Checks if this scope is a read-only scope.
    /// </summary>
    public bool IsReadOnly()
    {
        return Action == "read";
    }

    /// <summary>
    /// Gets all predefined scopes.
    /// </summary>
    public static IEnumerable<Scope> GetAllPredefined()
    {
        yield return ReadTransactions;
        yield return WriteTransactions;
        yield return ReadRules;
        yield return WriteRules;
        yield return ReadEmails;
        yield return WriteEmails;
        yield return ReadUsers;
        yield return WriteUsers;
        yield return AdminAll;
    }

    public override string ToString()
    {
        return Value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
