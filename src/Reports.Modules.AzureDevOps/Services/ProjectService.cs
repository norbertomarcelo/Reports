using System.Collections;
using System.Globalization;
using System.Text;
using Refit;
using Reports.Modules.AzureDevOps.Common;
using Reports.Modules.AzureDevOps.Entities;
using DotNetEnv;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Reports.Modules.AzureDevOps.Services;

public class ProjectService
{
    public string PersonalAccessToken { get; set; }
    public string Organization { get; set; }
    public string Url { get; set; }

    private readonly IRequestCollection _requestCollection;

    public ProjectService(string personalAccessToken, string organization, string url)
    {
        PersonalAccessToken = personalAccessToken;
        Organization = organization;
        Url = url;

        _requestCollection = RestService
            .For<IRequestCollection>(Url);
    }

    public async Task<SuccessfulResponse> Request()
    {
        var token =
            $"Basic {Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes($":{PersonalAccessToken}"))}";
        var response = await _requestCollection.GetProjects(Organization, token);
        return response;
    }

    public string ToCsvString(IEnumerable<Project> projects)
    {
        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("Id,Name,Url,State,Revision,Visibility,LastUpdateTime");

        foreach (var project in projects)
        {
            csvBuilder.AppendLine($"{project.Id}," +
                                  $"{EscapeForCsv(project.Name)}," +
                                  $"{EscapeForCsv(project.Url)}," +
                                  $"{EscapeForCsv(project.State)}," +
                                  $"{project.Revision}," +
                                  $"{EscapeForCsv(project.Visibility)}," +
                                  $"{project.LastUpdateTime}");
        }

        return csvBuilder.ToString();
    }

    private static string EscapeForCsv(string field)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;

        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        {
            field = field.Replace("\"", "\"\"");
            return $"\"{field}\"";
        }

        return field;
    }

    public IDocumentContainer ToPdfList(IDocumentContainer container, IEnumerable<Project> projects)
    {
        container.Page(page =>
        {
            page.Margin(40);
            page.Size(PageSizes.A4);

            page.Header()
                .Text("Relatório de Projetos")
                .FontSize(20)
                .Bold()
                .FontColor(Colors.Blue.Medium)
                .AlignCenter();

            page.Content().PaddingVertical(20).Column(column =>
            {
                foreach (var project in projects)
                {
                    column.Item().Text($"{project.Name}")
                        .FontSize(16)
                        .Bold()
                        .FontColor(Colors.Black);

                    column.Item().Text($"ID: {project.Id}")
                        .FontSize(12)
                        .FontColor(Colors.Grey.Darken1);

                    column.Item().Text($"URL: {project.Url}")
                        .FontSize(12)
                        .FontColor(Colors.Blue.Medium);

                    column.Item().Text($"Estado: {project.State}")
                        .FontSize(12)
                        .FontColor(Colors.Grey.Darken2);

                    column.Item().Text($"Última atualização: {project.LastUpdateTime:yyyy-MM-dd}")
                        .FontSize(12)
                        .FontColor(Colors.Grey.Darken3);
                    
                    column.Item().PaddingTop(10);
                }
            });
        });

        return container;
    }
}