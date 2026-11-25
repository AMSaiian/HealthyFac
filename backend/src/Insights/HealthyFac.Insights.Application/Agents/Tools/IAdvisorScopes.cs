namespace HealthyFac.Insights.Application.Agents.Tools;

/// <summary>
/// Creates the DI scope in which a tool runs, with the advisor actor of one company. The scope of a call is a new
/// scope whose actor is the advisor service flow of the company.
/// </summary>
public interface IAdvisorScopes : IToolScopes;
