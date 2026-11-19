using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Insights.Application.Agents.Tools.Invocation;

/// <summary>
/// The function that the model calls for one tool. The model arguments bind to <typeparamref name="TArguments"/> only,
/// and an unknown property (for example a company ID) is ignored. The call runs the use case through its decorated
/// handler inside the advisor scope of the company, so validation, the pre-handler check, and the downstream scopes
/// apply. A rejection goes back to the model as the result with the outcome <c>rejected</c>. Any other failure goes
/// back as a fixed text with the outcome <c>failed</c>, and never carries the exception text. After a downstream outage
/// the function loop ends at once, so the model gets no further turn. The application log
/// holds the exception at <c>Warning</c>, so a failure of a downstream service is visible. The log column holds
/// JSON, so the log keeps the text of a rejection or a failure as a JSON string.
/// </summary>
/// <typeparam name="TRequest">The tool request.</typeparam>
/// <typeparam name="TArguments">The record of the model arguments.</typeparam>
/// <typeparam name="TResponse">The response of the use case.</typeparam>
internal sealed class ToolFunction<TRequest, TArguments, TResponse> : AIFunction
    where TRequest : IAgentToolRequest<TRequest, TArguments, TResponse>
{
    private const string BadArgumentsText = "The arguments do not match the schema of the tool.";

    private const string FailedText =
        "The tool failed: an unexpected error. Correct the arguments and try again, "
        + "or finish without closing the case.";

    private readonly JsonSerializerOptions _json;
    private readonly AgentToolAttribute _tool;
    private readonly IAdvisorScopes _scopes;
    private readonly ToolContext _context;
    private readonly ILogger _logger;
    private readonly JsonElement _schema;

    public ToolFunction(
        AgentToolAttribute tool,
        IAdvisorScopes scopes,
        ToolContext context,
        ILogger logger,
        IAgentJson json
    )
    {
        _json = json.Tools;
        _tool = tool;
        _scopes = scopes;
        _context = context;
        _logger = logger;
        _schema = AIJsonUtilities.CreateJsonSchema(
            typeof(TArguments),
            _tool.Description,
            serializerOptions: _json,
            inferenceOptions: new AIJsonSchemaCreateOptions { TransformSchemaNode = HideGuidFormat });
    }

    public override string Name => _tool.Name;

    public override string Description => _tool.Description;

    public override JsonElement JsonSchema => _schema;

    public override JsonSerializerOptions JsonSerializerOptions => _json;

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken
    )
    {
        var callId = _context.CurrentCallId();
        var callContext = _context with { CurrentCallId = () => callId };
        var argumentsText = JsonSerializer.Serialize(arguments, _json);
        var stopwatch = Stopwatch.StartNew();
        ToolCallOutcome outcome;
        string result;
        ProviderUnavailableException? outage = null;

        try
        {
            var bound = JsonSerializer.Deserialize<TArguments>(argumentsText, _json);
            (outcome, result) = bound is null
                ? (ToolCallOutcome.Rejected, BadArgumentsText)
                : (ToolCallOutcome.Succeeded, await CallAsync(bound, callContext, cancellationToken));
        }
        catch (JsonException)
        {
            (outcome, result) = (ToolCallOutcome.Rejected, BadArgumentsText);
        }
        catch (ProviderUnavailableException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogFailure(exception);
            _context.ReadFailures.RecordOutage(exception);
            outage = exception;
            (outcome, result) = (ToolCallOutcome.Failed, FailedText);
        }
        catch (Exception exception) when (exception is IRejection)
        {
            (outcome, result) = (ToolCallOutcome.Rejected, exception.Message);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogFailure(exception);
            (outcome, result) = (ToolCallOutcome.Failed, FailedText);
        }

        _context.Streak?.Record(outcome != ToolCallOutcome.Succeeded);
        if (_context.Streak is { IsExceeded: true })
        {
            _context.EndLoop();
        }

        if (_tool.IsRead && outcome != ToolCallOutcome.Succeeded)
        {
            _context.ReadFailures.Record();
        }

        await _context.Recorder.RecordAsync(
            callId,
            Name,
            argumentsText,
            outcome,
            outcome == ToolCallOutcome.Succeeded ? result : JsonSerializer.Serialize(result, _json),
            stopwatch.Elapsed,
            cancellationToken);
        await _context.RenewLease(cancellationToken);

        if (outage is not null)
        {
            _context.EndLoop();
            ExceptionDispatchInfo.Throw(outage);
        }

        return ToolDataBlocks.Wrap(result);
    }

    private static JsonNode HideGuidFormat(AIJsonSchemaCreateContext context, JsonNode node)
    {
        if (node is JsonObject schema && schema[propertyName: "format"]?.GetValue<string>() == "uuid")
        {
            schema.Remove(propertyName: "format");
        }

        return node;
    }

    private void LogFailure(Exception exception) =>
        _logger.LogWarning(
            exception,
            message: "The tool {ToolName} failed in the agent run {RunId}.",
            Name,
            _context.RunId);

    private async Task<string> CallAsync(TArguments bound, ToolContext callContext, CancellationToken cancellationToken)
    {
        using var scope = _scopes.Create(_context.CompanyId);
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var response = await handler.HandleAsync(TRequest.Create(bound, callContext), cancellationToken);

        return JsonSerializer.Serialize(response, _json);
    }
}
