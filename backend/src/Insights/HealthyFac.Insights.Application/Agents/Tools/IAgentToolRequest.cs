using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Insights.Application.Agents.Tools;

/// <summary>
/// A tool request. The model fills only <typeparamref name="TArguments"/>. The server owns the other values of the
/// request (the company, the machine, the run), so a value that the model invents never reaches them.
/// </summary>
/// <typeparam name="TRequest">The request type itself.</typeparam>
/// <typeparam name="TArguments">The record that the model arguments bind to.</typeparam>
/// <typeparam name="TResponse">The response of the use case.</typeparam>
public interface IAgentToolRequest<TRequest, in TArguments, out TResponse> : IRequest<TResponse>
    where TRequest : IAgentToolRequest<TRequest, TArguments, TResponse>
{
    /// <summary>
    /// Builds the request from the arguments of the model and the values that the server owns.
    /// </summary>
    /// <param name="arguments">The arguments of the model.</param>
    /// <param name="context">The company, the machine, and the run of the call.</param>
    /// <returns>The request.</returns>
    static abstract TRequest Create(TArguments arguments, ToolContext context);
}
