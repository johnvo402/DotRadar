namespace DotRadar.Core;

public static class ExitCodes
{
    public const int Success = 0;
    public const int InvalidArguments = 1;
    public const int PolicyViolation = 2;

    // Giữ lại để tương thích với code cũ.
    public const int DiagnosticsFound = PolicyViolation;
    public const int ProjectLoadFailure = 3;
    public const int InternalError = 4;
}