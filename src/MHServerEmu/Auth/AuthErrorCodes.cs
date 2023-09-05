namespace MHServerEmu.Auth
{
    public enum AuthErrorCode
    {
        IncorrectUsernameOrPassword1 = 401,
        AccountBanned = 402,
        IncorrectUsernameOrPassword2 = 403,
        CouldNotReachAuthServer = 404,
        EmailNotVerified = 405,
        UnableToConnect1 = 406,
        NeedToAcceptLegal = 407,
        PatchRequired = 409,
        AccountArchived = 411,
        PasswordExpired = 412,
        UnableToConnect2 = 413,
        UnableToConnect3 = 414,
        UnableToConnect4 = 415,
        UnableToConnect5 = 416,
        AgeRestricted = 417,
        UnableToConnect6 = 418,
        TemporarilyUnavailable = 503
    }
}
