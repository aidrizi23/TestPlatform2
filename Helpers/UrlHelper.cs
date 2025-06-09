namespace TestPlatform2.Helpers;

public static class UrlHelper
{
    public static string GenerateTestUrl(HttpContext context, string testId, string token)
    {
        var request = context.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}"; // Automatically handles localhost/production
        return $"{baseUrl}/testattempt/starttest?testId={testId}&token={token}";
    }

    public static string BuildAbsoluteUrl(HttpRequest request, string? relativeUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl))
            return $"{request.Scheme}://{request.Host}";
            
        var baseUrl = $"{request.Scheme}://{request.Host}";
        return $"{baseUrl}{relativeUrl}";
    }
}