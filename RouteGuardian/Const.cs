namespace RouteGuardian
{
    // References:
    // - https://www.geeksforgeeks.org/how-to-validate-guid-globally-unique-identifier-using-regular-expression/
    
    public static class Const
    {
        public const char SeparatorPipe = '|';
        public const char SeparatorSpace = ' ';
        public const string Allow = "allow";
        public const string Deny = "deny";
        public const string AuthHeader = "Authorization";
        public const string BearerTokenPrefix = "Bearer ";
        public const string KerberosTokenPrefix = "Negotiate ";
        public const string WinAuthTypes = "Negotiate|NTLM";
        public const string JwtClaimTypeRole = "rol";
        public const string JwtClaimTypeIssuedAt = "iat";
        public const string JwtClaimTypeUsername = "name";
        public const string JwtClaimTypeUserId = "sub";
        public const string JwtDbLookupRole = "DB^";
        public const string AnonymousRoleName = "ANONYMOUS";
        public const string WildCard = "*";
        public const string WildCardRegEx = @"[\/?\w+]*";
        public const string IntegerWildCard = "{int}";
        public const string IntegerWildCardRegEx = @"[+-]?(?<!\.)\b[0-9]+\b(?!\.[0-9])";
        public const string DecimalWildCard = "{dec}";
        public const string DecimalWildCardRegEx = @"[+-]?(?:\d*\.)?\d+";
        public const string AlphaNumericWildCard = "{str}";
        public const string AlphaNumericWildCardRegEx = @"[a-zA-Z0-9_-]+";
        public const string GuidWildCard = "{guid}";
        public const string GuidWildCardRegEx = "[{]?[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}[}]?";
        public static readonly string[] HttpVerbs = {"GET","HEAD","POST","PUT","PATCH","DELETE","CONNECT"};
        public const string WinAuthRegisterAdditionalGroupsFromDb = "RouteGuardian:WinAuthentication:ReigsterAdditionalGroupsFromDb";
        public const string WinAuthActiveDirectoryDomain = "RouteGuardian:WinAuthentication:ActiveDirectoryDomain"; 
        public const string GlobalException = "A global exception occurred and was logged!";
        public const string HeaderClientId = "x-client-id";
        public const string HeaderClientKey = "x-client-key";
        public const string DefaultAccessFile = "access.json";
        public const string DefaultApiKeysFile = "apikeys.json";
        public const string ApiKeyDefaultAuthScheme = "ApiKeyAuthenticationScheme";
    }
}
