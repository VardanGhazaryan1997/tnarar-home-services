using HomeServices.Application.Abstractions;

namespace HomeServices.Application.Tests.Support;

public sealed class FakeCurrentLanguage(string code, string defaultCode = "hy") : ICurrentLanguage
{
    public string Code { get; } = code;

    public string DefaultCode { get; } = defaultCode;
}
