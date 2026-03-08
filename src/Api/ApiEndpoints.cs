using System.Diagnostics.CodeAnalysis;

namespace Presentation;

[ExcludeFromCodeCoverage]
public static class ApiEndpoints
{
    private const string ApiBase = "api/v{v:apiVersion}";

    public static class Cats
    {
        private const string Base = $"{ApiBase}/cats";

        public const string Fetch = $"{Base}/fetch";
        public const string Get = $"{Base}/{{id:int}}";
        public const string GetAll = Base;
    }

    public static class Jobs
    {
        private const string Base = $"{ApiBase}/jobs";

        public const string Get = $"{Base}/{{id:int}}";
    }
}
