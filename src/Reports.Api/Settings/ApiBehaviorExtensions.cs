using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Reports.Api.Settings;

public static class ApiBehaviorExtensions
{
    public const string ValidationTitle = "Requisição inválida.";
    public const string InvalidValueMessage = "Valor inválido ou propriedade não reconhecida.";

    /// <summary>
    /// Padroniza as respostas 400 de validação: título e mensagens em português, sem expor
    /// nomes de tipos .NET nem mensagens internas do serializador.
    /// </summary>
    public static IMvcBuilder AddPortugueseValidationResponses(this IMvcBuilder builder)
    {
        builder.AddJsonOptions(options => options.AllowInputFormatterExceptionMessages = false);
        builder.AddMvcOptions(options =>
        {
            var messages = options.ModelBindingMessageProvider;
            messages.SetMissingRequestBodyRequiredValueAccessor(() => "O corpo da requisição é obrigatório.");
            messages.SetValueIsInvalidAccessor(_ => InvalidValueMessage);
            messages.SetAttemptedValueIsInvalidAccessor((_, _) => InvalidValueMessage);
            messages.SetUnknownValueIsInvalidAccessor(_ => InvalidValueMessage);
            messages.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => InvalidValueMessage);
            messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => InvalidValueMessage);
            messages.SetValueMustBeANumberAccessor(_ => InvalidValueMessage);
            messages.SetNonPropertyValueMustBeANumberAccessor(() => InvalidValueMessage);
            messages.SetValueMustNotBeNullAccessor(_ => InvalidValueMessage);
            messages.SetMissingBindRequiredValueAccessor(name => $"O campo '{name}' é obrigatório.");
            messages.SetMissingKeyOrValueAccessor(() => InvalidValueMessage);
        });

        return builder.ConfigureApiBehaviorOptions(options =>
            options.InvalidModelStateResponseFactory = context =>
            {
                var modelState = new ModelStateDictionary();
                foreach (var (key, entry) in context.ModelState)
                {
                    foreach (var error in entry.Errors)
                    {
                        // Erros de desserialização trazem uma exceção: a mensagem dela é interna.
                        var message = error.Exception is null || !string.IsNullOrEmpty(error.ErrorMessage)
                            ? error.ErrorMessage
                            : InvalidValueMessage;
                        modelState.AddModelError(key, string.IsNullOrWhiteSpace(message) ? InvalidValueMessage : message);
                    }
                }

                var problem = context.HttpContext.RequestServices
                    .GetRequiredService<ProblemDetailsFactory>()
                    .CreateValidationProblemDetails(context.HttpContext, modelState,
                        StatusCodes.Status400BadRequest, ValidationTitle);

                return new BadRequestObjectResult(problem)
                {
                    ContentTypes = { "application/problem+json" }
                };
            });
    }
}
