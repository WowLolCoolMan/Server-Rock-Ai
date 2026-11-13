namespace SkaiDesktop.Core.Models;

public static class SkillSourceReference
{
    public static string Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 512 || value.Any(char.IsControl))
            throw new ArgumentException("A pinned skill source reference is required.", nameof(value));
        var reference = value.Trim();
        if (reference.StartsWith("local-folder:", StringComparison.Ordinal))
        {
            if (reference.Length <= "local-folder:".Length || reference["local-folder:".Length..].Any(ch => ch is '/' or '\\' or ':' || char.IsControl(ch)))
                throw new ArgumentException("The local skill source reference is invalid.", nameof(value));
            return reference;
        }
        var commitSeparator = reference.LastIndexOf('@');
        if (commitSeparator <= 0 || commitSeparator == reference.Length - 1)
            throw new ArgumentException("A GitHub skill reference must include an immutable commit ref after @.", nameof(value));
        var repositoryReference = reference[..commitSeparator];
        var commit = reference[(commitSeparator + 1)..];
        if (!Uri.TryCreate(repositoryReference, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new ArgumentException("Skill GitHub references must use an HTTPS github.com URL.", nameof(value));
        var path = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (path.Length != 2 || path.Any(part => part.Length == 0 || part.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.'))))
            throw new ArgumentException("The GitHub skill repository reference is invalid.", nameof(value));
        if (!string.IsNullOrEmpty(uri.UserInfo)) throw new ArgumentException("Skill references cannot contain credentials.", nameof(value));
        if (commit.Length is < 6 or > 64 || commit.Any(ch => !Uri.IsHexDigit(ch)))
            throw new ArgumentException("The GitHub skill commit ref must be 6-64 hexadecimal characters.", nameof(value));
        return reference;
    }
}
