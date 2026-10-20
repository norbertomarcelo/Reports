namespace Reports.Api.Services;

/// <summary>Classifica membros de time para o relatório: grupos do Azure DevOps e usuários externos (spec 016).</summary>
public static class MemberClassifier
{
    /// <summary>Grupos vêm com <c>uniqueName</c> no formato <c>[escopo]\nome</c>, e não com um e-mail.</summary>
    public static bool IsGroup(string? uniqueName) =>
        uniqueName is not null && uniqueName.StartsWith('[') && uniqueName.Contains("]\\", StringComparison.Ordinal);

    /// <summary>
    /// Externo = domínio do e-mail (depois do último <c>@</c>) fora da lista, por igualdade exata e sem
    /// diferenciar maiúsculas. Lista vazia ou e-mail sem <c>@</c> não marcam ninguém.
    /// </summary>
    public static bool IsExternal(string? email, IReadOnlyCollection<string> internalDomains)
    {
        if (internalDomains.Count == 0 || email is null)
            return false;

        var at = email.LastIndexOf('@');
        if (at < 0)
            return false;

        var domain = email[(at + 1)..];
        return !internalDomains.Contains(domain, StringComparer.OrdinalIgnoreCase);
    }
}
