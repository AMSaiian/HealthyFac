using System.Net.Http.Headers;
using System.Text;
using HealthyFac.Testing.Authentication;

namespace HealthyFac.Document.ApiTests.Hosting;

public static class DocumentApiClientExtensions
{
    private const string BearerScheme = "Bearer";
    private const string JsonMediaType = "application/json";

    extension(DocumentApiFixture fixture)
    {
        public TestTokenClaims CreateClaims(params string[] permissions) =>
            new()
            {
                Subject = Guid.NewGuid(),
                Organizations = [fixture.CompanyId.ToString()],
                Permissions = permissions,
            };

        public async Task<HttpResponseMessage> SendMultipartAsync(
            string path,
            TestTokenClaims? claims,
            IEnumerable<MultipartFile> files,
            string? description = null,
            HttpClient? client = null
        )
        {
            using var ownedClient = client is null ? fixture.CreateClient() : null;
            using var request = new HttpRequestMessage(HttpMethod.Post, path);
            using var form = new MultipartFormDataContent();

            foreach (var file in files)
            {
                var part = new StreamContent(file.Content);
                part.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                form.Add(part, name: "file", file.FileName);
            }

            if (description is not null)
            {
                form.Add(new StringContent(description), name: "description");
            }

            if (claims is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    BearerScheme,
                    fixture.Tokens.CreateToken(claims));
            }

            request.Content = form;

            return await (client ?? ownedClient!).SendAsync(request);
        }

        public async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string path,
            TestTokenClaims claims,
            string? jsonBody = null
        )
        {
            using var client = fixture.CreateClient();
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                BearerScheme,
                fixture.Tokens.CreateToken(claims));

            if (jsonBody is not null)
            {
                request.Content = new StringContent(jsonBody, Encoding.UTF8, JsonMediaType);
            }

            return await client.SendAsync(request);
        }
    }
}

/// <summary>
/// One file part of a multipart test request.
/// </summary>
/// <param name="FileName">The file name of the part.</param>
/// <param name="ContentType">The declared media type of the part.</param>
/// <param name="Content">The bytes of the part.</param>
public sealed record MultipartFile(string FileName, string ContentType, Stream Content);
