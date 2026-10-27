using System.Net;
using HealthyFac.Shared.Application.Observability;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Web.Rendering;

/// <summary>
/// Renders a Razor component to a string outside an HTTP request, for example the bodies of an email. The renderer
/// holds no logic of its user. Each call creates its own <see cref="HtmlRenderer"/>, so calls share no state, and
/// each call writes one <c>Razor.Render</c> span. The span shows the component and the format, never a parameter
/// value, because a parameter can hold a secret (e.g., an invite link with its token).
/// </summary>
/// <param name="services">The service provider that the components resolve their services from.</param>
/// <param name="loggerFactory">The logger factory of the renderer.</param>
public sealed class RazorComponentRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
{
    /// <summary>
    /// Renders the component to an HTML string. <see cref="HtmlRenderer"/> takes no token, so the method checks
    /// the token once before the render starts.
    /// </summary>
    /// <typeparam name="TComponent">The component to render.</typeparam>
    /// <param name="parameters">The component parameters, keyed by parameter name.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe before the render
    /// starts.</param>
    /// <returns>The rendered HTML.</returns>
    public Task<string> RenderHtmlAsync<TComponent>(
        IDictionary<string, object?> parameters,
        CancellationToken cancellationToken
    )
        where TComponent : IComponent
        => RenderAsync<TComponent>(parameters, ObservabilityConstants.Rendering.Formats.Html, cancellationToken);

    /// <summary>
    /// Renders the component to a plain-text string. Razor encodes <c>&amp;</c>, <c>&lt;</c>, and quotes as
    /// entities, so the method decodes them. It trims the leading and trailing blank lines.
    /// </summary>
    /// <typeparam name="TComponent">The component to render.</typeparam>
    /// <param name="parameters">The component parameters, keyed by parameter name.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe before the render
    /// starts.</param>
    /// <returns>The rendered text.</returns>
    public async Task<string> RenderTextAsync<TComponent>(
        IDictionary<string, object?> parameters,
        CancellationToken cancellationToken
    )
        where TComponent : IComponent
    {
        var html = await RenderAsync<TComponent>(
            parameters,
            ObservabilityConstants.Rendering.Formats.Text,
            cancellationToken);

        return WebUtility.HtmlDecode(html).Trim();
    }

    private async Task<string> RenderAsync<TComponent>(
        IDictionary<string, object?> parameters,
        string format,
        CancellationToken cancellationToken
    )
        where TComponent : IComponent
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var activity = RenderingActivitySource.Instance
            .StartActivity(ObservabilityConstants.Spans.RazorRender)
            .WithTag(ObservabilityConstants.Rendering.Tags.Component, typeof(TComponent).Name)
            .WithTag(ObservabilityConstants.Rendering.Tags.Format, format);

        try
        {
            await using var renderer = new HtmlRenderer(services, loggerFactory);

            var html = await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var output = await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters));

                return output.ToHtmlString();
            });
            activity.MarkSuccess();

            return html;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            activity.MarkException(exception);

            throw;
        }
    }
}
