namespace HealthyFac.Insights.Application.Agents.Tools;

/// <summary>
/// Marks a tool that calls no other service, so it needs no <c>[DownstreamScopes]</c> declaration.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class LocalToolAttribute : Attribute;
