using Reports.Api.Services;
using Reports.Api.Services.Pdf;
using Reports.Api.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddPortugueseValidationResponses();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOptions<AzureDevOpsOptions>()
    .BindConfiguration(AzureDevOpsOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddAzureDevOpsClient();

builder.Services.AddScoped<AzureDevOpsService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddSingleton<IPdfRenderer, PlaywrightPdfRenderer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
