using Microsoft.AspNetCore.Authorization;

namespace DigitalDynamics.Foundation.Authorization.Attributes;

/// <summary>
/// Shorthand for <c>[Authorize("PermissionName")]</c> that integrates with
/// <see cref="DigitalDynamics.Foundation.Authorization.Authorization.DynamicPermissionPolicyProvider"/>.
/// </summary>
/// <example>
/// <code>
/// [Permission("Invoices.Delete")]
/// public IActionResult Delete(Guid id) { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class PermissionAttribute(string permission) : AuthorizeAttribute(permission);
