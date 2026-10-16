namespace Reports.Api.Services.Pdf;

public interface IPdfRenderer
{
    Task<byte[]> RenderAsync(string html);
}
