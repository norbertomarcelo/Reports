using System.Net;
using System.Text.Json;
using Reports.Api.Tests.Fakes;

namespace Reports.Api.Tests;

public class ReportRequestValidationTests
{
    public static TheoryData<string> CorposInvalidos => new()
    {
        "",
        "{}",
        """{ "include": null }""",
        """{ "include": {} }""",
        """{ "include": { "projects": false, "teams": false, "users": false, "workItems": false } }""",
        """{ "include": { "projects": "sim" } }""",
        """{ "include": { "projects": 1 } }""",
        """{ "include": { "projects": null } }""",
        """{ "include": { "projects": true, "boards": true } }""",
        """{ "includes": { "projects": true } }""",
        """{ "include": { "projects": true """
    };

    private static readonly string[] MensagensEsperadas =
    [
        "O corpo da requisição é obrigatório.",
        "Informe as seções do relatório em 'include'.",
        "Selecione ao menos uma seção do relatório.",
        "Valor inválido ou propriedade não reconhecida."
    ];

    [Theory]
    [MemberData(nameof(CorposInvalidos))]
    public async Task Corpo_invalido_retorna_400_em_portugues_sem_consultar_o_azure_devops(string body)
    {
        var api = new FakeAzureDevOpsApi();
        var pdf = new FakePdfRenderer();
        using var factory = ReportHost.Create(api, pdf);

        var response = await factory.CreateClient().PostAsync(ReportHost.Route, ReportHost.Json(body));
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var root = JsonDocument.Parse(content).RootElement;
        Assert.Equal("Requisição inválida.", root.GetProperty("title").GetString());
        Assert.True(root.TryGetProperty("traceId", out _));
        var mensagens = root.GetProperty("errors").EnumerateObject()
            .SelectMany(e => e.Value.EnumerateArray().Select(m => m.GetString()));
        Assert.All(mensagens, m => Assert.Contains(m, MensagensEsperadas));
        Assert.DoesNotContain("System.", content);
        Assert.DoesNotContain("The JSON", content);
        Assert.DoesNotContain("required", content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(api.Calls);
        Assert.Null(pdf.Html);
    }

    [Fact]
    public async Task Propriedade_desconhecida_aponta_o_campo()
    {
        using var factory = ReportHost.Create(new FakeAzureDevOpsApi(), new FakePdfRenderer());

        var response = await factory.CreateClient().PostAsync(ReportHost.Route,
            ReportHost.Json("""{ "include": { "projects": true, "boards": true } }"""));
        var errors = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("errors");

        Assert.Contains(errors.EnumerateObject(), e => e.Name.Contains("boards"));
    }

    [Fact]
    public async Task So_itens_de_trabalho_e_uma_secao_valida()
    {
        var pdf = new FakePdfRenderer();
        using var factory = ReportHost.Create(new FakeAzureDevOpsApi(), pdf);

        var response = await factory.CreateClient().PostAsync(ReportHost.Route,
            ReportHost.Json("""{ "include": { "workItems": true } }"""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(pdf.Html);
    }

    [Fact]
    public async Task Corpo_enviado_como_texto_retorna_415()
    {
        using var factory = ReportHost.Create(new FakeAzureDevOpsApi(), new FakePdfRenderer());

        var response = await factory.CreateClient().PostAsync(ReportHost.Route,
            ReportHost.Json("""{ "include": { "projects": true } }""", "text/plain"));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }
}
