using Microsoft.AspNetCore.Mvc;

namespace Reports.Api.Controllers;

public class AzureDevOpsController : ControllerBase
{
    public async Task<IActionResult> ListAllProjectsAsync()
    {
        var url = "https://dev.azure.com/MNS-Tech/_apis/projects?api-version=5.0";

        using (HttpClient client = new HttpClient())
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", "G3rF6XTzpxUBK0hk1toWJO82ZYKYsBv1icT8Svdpb4yOQNO4l6jtJQQJ99BBACAAAAAAAAAAAAASAZDO47sb");

            string resposta = await client.GetStringAsync(url);
            Console.WriteLine(resposta);
        }
        return Ok();
    }
}
