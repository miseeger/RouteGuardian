using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RouteGuardian.Model;

namespace RouteGuardian
{
    public class RouteGuardian
    {
        public GuardPolicy Policy { get; set; }
        public List<GuardRule> Rules { get; }


        public RouteGuardian(string accessFileName = "")
        {
            Policy = GuardPolicy.Deny;
            Rules = new List<GuardRule>();

            if (accessFileName != string.Empty)
            {
                InitAccessRulesFromJsonFile(accessFileName);
            }
            else
            {
                // Todo: Aus DB lesen? - Connectionstring vorausgesetzt.
            }
        }


        private void InitAccessRulesFromJsonFile(string accessFileName)
        {
            if (!File.Exists(accessFileName)) 
                return;
            
            var access = JsonConvert.DeserializeObject<GuardAccess>(File.ReadAllText(accessFileName));

            if (access == null) 
                return;
            
            Clear();
            DefaultPolicy(access.Default == Const.Allow ? GuardPolicy.Allow : GuardPolicy.Deny);

            foreach (var rule in access.Rules)
            {
                var splitRule = rule.Split(Const.SeparatorSpace);
                try
                {
                    Rule(splitRule[0].ToLower() == Const.Allow ? GuardPolicy.Allow : GuardPolicy.Deny,
                        splitRule[1].ToUpper(), splitRule[2], splitRule[3].ToUpper());
                }
                catch (Exception)
                {
                    // Todo: Log Exception when adding new Rule    
                }
            }
        }

        public RouteGuardian DefaultPolicy(GuardPolicy defaultPolicy)
        {
            Policy = defaultPolicy;
            return this;
        }

        public RouteGuardian Clear()
        {
            Rules.Clear();
            return this;
        }

        public RouteGuardian Rule(GuardPolicy policy, string verbs, string path, string subjects)
        {
            var verbsPos = verbs.ToUpper().Split(Const.SeparatorPipe);
            var subs = subjects.ToUpper().Split(Const.SeparatorPipe);

            if (!subs.Any())
            {
                subs.Append(Const.WildCard);
            }

            if (!verbsPos.Any() || verbsPos[0] == Const.WildCard)
            {
                verbsPos = Const.HttpVerbs; ;
            }

            var verbsNeg = Const.HttpVerbs
                .Where(v => verbsPos.All(vp => vp != v))
                .ToList();

            foreach (var subject in subs)
            {
                // alle gegebenen Verben mit der angegebene Policy eintragen
                foreach (var verb in verbsPos)
                {
                    Rules.Add(new GuardRule
                    {
                        Policy = policy,
                        Verb = verb,
                        Path = path.ToLower(),
                        Subject = subject

                    });
                }

                // alle nicht gegebenen Verben mit der umgekehrten Policy eintragen
                foreach (var verb in verbsNeg)
                {
                    Rules.Add(new GuardRule
                    {
                        Policy = policy == GuardPolicy.Allow ? GuardPolicy.Deny : GuardPolicy.Allow,
                        Verb = verb,
                        Path = path.ToLower(),
                        Subject = subject
                    });
                }
            }

            return this;
        }

        public RouteGuardian Allow(string verbs, string path, string subjects)
        {
            Rule(GuardPolicy.Allow, verbs, path, subjects);
            return this;
        }

        public RouteGuardian Deny(string verbs, string path, string subjects)
        {
            Rule(GuardPolicy.Deny, verbs, path, subjects);
            return this;
        }

        public bool IsGranted(string verb, string path, string subjects = Const.AnonymousRoleName)
        {
            if (verb == string.Empty)
            {
                return false;
            }

            verb = verb.ToUpper();
            path = path.ToLower();
            subjects = subjects.ToUpper();

            var rulesToMatch = Rules
                .Where(r => r.Verb == verb.ToUpper()
                    && (subjects.ToUpper().Split(Const.SeparatorPipe).Contains(r.Subject) || r.Subject == Const.WildCard)
                )
                .ToList();

            var matchingRules = new List<GuardRule>();

            // Jede Rule in ein RegEx umbauen und auf Match zu path prüfen
            foreach (var rule in rulesToMatch)
            {
                var pathPattern = rule.Path
                    .Replace(Const.IntegerWildCard, Const.IntegerWildCardRegEx)
                    .Replace(Const.DecimalWildCard, Const.DecimalWildCardRegEx)
                    .Replace(Const.AlphaNumericWildCard, Const.AlphaNumericWildCardRegEx)
                    .Replace(Const.GuidWildCard, Const.GuidWildCardRegEx)
                    .Replace(Const.WildCard, Const.WildCardRegEx);

                if (Regex.Match(path, $"^{pathPattern}$").Success)
                {
                    matchingRules.Add(rule);
                }
            }

            if (!matchingRules.Any())
            {
                return Policy == GuardPolicy.Allow;
            }
            
            var individualMatchingRules = matchingRules.Where(r => subjects.Contains(r.Subject)).ToList();
            var wildcardMatchingRules = matchingRules.Where(r => r.Subject == Const.WildCard).ToList();

            // Sobald matchingRules für eines der Subjects da sind, weden diese vorrangig behandelt:
            if (individualMatchingRules.Any())
            {
                var accessIv =
                (individualMatchingRules.Any(r => r.Policy == GuardPolicy.Allow)
                    ? GuardPolicy.Allow
                    : GuardPolicy.Deny) == GuardPolicy.Allow;
                return accessIv;
            }
            else
            {
                // Sind keine individuellen matchingrules vorhanden, werden die 
                // wildcardMatchingRules geprüft:
                if (wildcardMatchingRules.Any())
                {
                    var accessWc =
                    (wildcardMatchingRules.Any(r => r.Policy == GuardPolicy.Allow)
                        ? GuardPolicy.Allow
                        : GuardPolicy.Deny) == GuardPolicy.Allow;
                    return accessWc;
                }

                return Policy == GuardPolicy.Allow;
            }
        }

        public bool Authorize(HttpContext context, string subjects = Const.AnonymousRoleName,
            Action<string, string>? onDeny = null)
        {
            var path = context.Request.Path.Value!;

            if (!IsGranted(context.Request.Method, path, subjects))
            {
                if (onDeny != null)
                {
                    onDeny.Invoke(path, subjects);
                    return false;
                }

                return false;
            }

            return true;
        }
    }
}
