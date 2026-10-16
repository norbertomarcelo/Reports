using System.Text;
using Reports.Api.Services.Pdf;

namespace Reports.Api.Tests.Fakes;

public class FakePdfRenderer : IPdfRenderer
{
    public static readonly byte[] Output = Encoding.ASCII.GetBytes("%PDF-fake");

    public string? Html { get; private set; }

    public Task<byte[]> RenderAsync(string html)
    {
        Html = html;
        return Task.FromResult(Output);
    }
}
