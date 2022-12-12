﻿namespace RouteGuardian
{
    public static class Const
    {
        public const char SeparatorPipe = '|';
        public const char SeparatorSpace = ' ';
        public const string Allow = "allow";
        public const string Deny = "deny";
        public const string AuthHeader = "Authorization";
        public const string BearerTokenPrefix = "Bearer ";
        public const string NtlmTokenPrefix = "Negotiation ";
        public const string JwtClaimTypeRole = "rol";
        public const string JwtDbLookupRole = "DB^";
        public const string AnonymousRoleName = "ANONYMOUS";
        public const string WildCard = "*";
        public const string WildCardRegEx = @"[\/?\w+]*";
        public const string NumericWildCard = "{num}";
        public const string NumericWildCardRegEx = @"\d*";
        public const string AlphaNumericWildCard = "{str}";
        public const string AlphaNumericWildCardRegEx = @"\w+";
        public static readonly string[] HttpVerbs = {"GET","HEAD","POST","PUT","PATCH","DELETE","CONNECT"};

        public const string SetRegisterGroupsAsRoles = "RouteGuardian:WinAuthentication:RegisterGroupsAsRoles";
    }
}