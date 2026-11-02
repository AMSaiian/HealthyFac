using System.Diagnostics;
using HealthyFac.Core.UnitTests.Employees.Data;
using HealthyFac.Core.Web.Employees.Emails;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Web.Rendering;
using HealthyFac.Testing.Tracing;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.UnitTests.Employees;

public sealed class RazorEmployeeInviteEmailBuilderTests : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly IServiceScope _scope;
    private readonly RazorEmployeeInviteEmailBuilder _builder;

    public RazorEmployeeInviteEmailBuilderTests()
    {
        _services = new ServiceCollection()
            .AddLogging()
            .AddRazorRendering()
            .BuildServiceProvider(validateScopes: true);
        _scope = _services.CreateScope();
        _builder = new RazorEmployeeInviteEmailBuilder(
            _scope.ServiceProvider.GetRequiredService<RazorComponentRenderer>());
    }

    [Fact]
    public async Task BuildAsync_WithModel_SetsRecipientAndSubject()
    {
        var message = await _builder.BuildAsync(InviteEmailBuilderTestData.Model(), CancellationToken.None);

        message.To.Should().Be(InviteEmailBuilderTestData.Recipient);
        message.Subject.Should().Be(RazorEmployeeInviteEmailBuilder.Subject);
        message.HtmlBody.Should().Contain(RazorEmployeeInviteEmailBuilder.Subject);
    }

    [Fact]
    public async Task BuildAsync_WithModel_PutsLinkIntoHtmlAndText()
    {
        var message = await _builder.BuildAsync(InviteEmailBuilderTestData.Model(), CancellationToken.None);

        message.HtmlBody.Should().Contain($"href=\"{InviteEmailBuilderTestData.Link}\"");
        message.TextBody.Should().Contain(InviteEmailBuilderTestData.Link);
    }

    [Fact]
    public async Task BuildAsync_WithAmpersandInLink_EncodesHtmlAndKeepsTextPlain()
    {
        var message = await _builder.BuildAsync(
            InviteEmailBuilderTestData.Model(InviteEmailBuilderTestData.LinkWithAmpersand),
            CancellationToken.None);

        message.HtmlBody.Should().Contain(InviteEmailBuilderTestData.AmpersandEntity);
        message.TextBody.Should().Contain(InviteEmailBuilderTestData.AmpersandInText);
        message.TextBody.Should().NotContain(InviteEmailBuilderTestData.AmpersandEntity);
    }

    [Fact]
    public async Task BuildAsync_TextBody_KeepsLineBreaks()
    {
        var message = await _builder.BuildAsync(InviteEmailBuilderTestData.Model(), CancellationToken.None);

        var lines = message.TextBody.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCountGreaterThan(1);
        lines.Should().Contain(line => line.Trim() == InviteEmailBuilderTestData.Link);
    }

    [Fact]
    public async Task BuildAsync_WithModel_WritesOneRenderSpanPerBody()
    {
        using var capture = new SpanCapture(ObservabilityConstants.Sources.Rendering);
        using var parent = new Activity(InviteEmailBuilderTestData.ParentName).Start();

        await _builder.BuildAsync(InviteEmailBuilderTestData.Model(), CancellationToken.None);

        var spans = capture.Spans
            .Where(span => span.TraceId == parent.TraceId
                && span.OperationName == ObservabilityConstants.Spans.RazorRender)
            .ToList();
        var formats = spans.Select(span => span.GetTagItem(ObservabilityConstants.Rendering.Tags.Format)?.ToString());
        var tagValues = spans
            .SelectMany(span => span.TagObjects)
            .Select(tag => tag.Value?.ToString() ?? string.Empty)
            .ToList();

        spans.Should().HaveCount(2);
        formats.Should()
            .BeEquivalentTo(
                [ObservabilityConstants.Rendering.Formats.Html, ObservabilityConstants.Rendering.Formats.Text]);
        spans.Should().OnlyContain(span => span.ParentId == parent.Id);
        tagValues.Should().NotContain(value => value.Contains(InviteEmailBuilderTestData.Link));
    }

    [Fact]
    public async Task BuildAsync_WithModel_ShowsExpiryInUtc()
    {
        var message = await _builder.BuildAsync(InviteEmailBuilderTestData.Model(), CancellationToken.None);

        message.HtmlBody.Should().Contain(InviteEmailBuilderTestData.ExpiryInUtc);
        message.TextBody.Should().Contain(InviteEmailBuilderTestData.ExpiryInUtc);
    }

    public void Dispose()
    {
        _scope.Dispose();
        _services.Dispose();
    }
}
