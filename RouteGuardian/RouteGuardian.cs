using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RouteGuardian.Model;

namespace RouteGuardian
{
    public class RouteGuardian
    {
        private GuardPolicy _defaultPolicy;
        public GuardPolicy Policy
        {
            get { return _defaultPolicy; }
            set { _defaultPolicy = value; }
        }

        private List<GuardRule> _rules;
        public List<GuardRule> Rules
        {
            get { return _rules; }
        }
               

        public RouteGuardian(string accessFileName = "")
        {  
            _defaultPolicy = GuardPolicy.Deny;
            _rules = new List<GuardRule>();

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
            if (File.Exists(accessFileName))
            {
                var access = JsonConvert.DeserializeObject<GuardAccess>(File.ReadAllText(accessFileName));

                if (access != null)
                {
                    Clear();
                    DefaultPolicy(access.Default == "allow" ? GuardPolicy.Allow : GuardPolicy.Deny);

                    foreach (var rule in access.Rules)
                    {
                        var splitRule = rule.Split(' ');
                        try
                        {
                        Rule(splitRule[0].ToLower() == "allow" ? GuardPolicy.Allow : GuardPolicy.Deny,
                            splitRule[1].ToUpper(), splitRule[2], splitRule[3].ToUpper());                            
                        }
                        catch (Exception)
                        {
                            // Todo: Log Exception when adding new Rule    
                        }
                    }
                }
            }
        }

        public RouteGuardian DefaultPolicy(GuardPolicy dflt)
        {
            _defaultPolicy = dflt;
            return this;
        }

        public RouteGuardian Clear()
        {
            _rules.Clear();
            return this;
        }

        public RouteGuardian Rule (GuardPolicy policy, string verbs, string path, string subjects)
        {
            var httpVerbs = "GET|HEAD|POST|PUT|PATCH|DELETE|CONNECT".Split('|');
            var verbsPos = verbs.Split('|');
            
            var subs = subjects.ToUpper().Split('|');

            if (!subs.Any())
            {
                subs.Append("*");
            }

            if (!verbsPos.Any() || verbsPos[0] == "*")
            {
                verbsPos = httpVerbs;
            }

            var verbsNeg = httpVerbs
                .Where(v => verbsPos.All(vp => vp != v))
                .ToList();

            foreach (var subject in subs)
            {
                // alle gegebenen Verben mit der angegebene Policy eintragen
                foreach (var verb in verbsPos)
                {
                    _rules.Add(new GuardRule
                    {
                        Policy = policy,
                        Verb = verb,
                        Path = path,
                        Subject = subject

                    });
                }

                // alle nicht gegebenen Verben mit der umgekehrten Policy eintragen
                foreach (var verb in verbsNeg)
                {
                    _rules.Add(new GuardRule
                    {
                        Policy = policy == GuardPolicy.Allow ? GuardPolicy.Deny : GuardPolicy.Allow,
                        Verb = verb,
                        Path = path,
                        Subject = subject
                    });
                }
            }
            
            return this;
        }

        public RouteGuardian Allow (string verbs, string path, string subjects)
        {
            Rule(GuardPolicy.Allow, verbs, path, subjects);
            return this;
        }

        public RouteGuardian Deny(string verbs, string path, string subjects) 
        {
            Rule(GuardPolicy.Deny, verbs, path, subjects);
            return this;
        }

        public bool isGranted(string verb, string path, string subjects = "ANONYMOUS")
         {
            if (verb == string.Empty)
            {
                return false;
            }

            var rulesToMatch = _rules
                .Where(r => r.Verb == verb.ToUpper()
                    && (subjects.ToUpper().Contains(r.Subject) || r.Subject == "*")
                )
                .ToList();

            var matchingRules = new List<GuardRule>();

            // jede Rule in ein RegEx umbauen und auf Match zu path prüfen
            foreach (var rule in rulesToMatch)
            {
                var pathPattern = rule.Path
                    .Replace("{num}", @"\d*")
                    .Replace("{str}", @"\w+")
                    .Replace("*", @"[\/?\w+]*");

                if (Regex.Match(path, $"^{pathPattern}$").Success)
                {
                    matchingRules.Add(rule);
                }
            }

            if (!matchingRules.Any())
            {
                return _defaultPolicy == GuardPolicy.Allow;
            }

            var access = _defaultPolicy;
            var individualMatchingRules = matchingRules.Where(r => subjects.Contains(r.Subject)).ToList();
            var wildcardMatchingRules = matchingRules.Where(r => r.Subject == "*").ToList();

            // sobald matchingRules für eines der Subjects da sind, weden diese vorrangig behandelt:
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
                // sind keine individuellen matchingrules vorhanden, werden die 
                // wildcardMatchingRules geprüft:
                if (wildcardMatchingRules.Any())
                {
                    var accessWc = 
                    (wildcardMatchingRules.Any(r => r.Policy == GuardPolicy.Allow)
                        ? GuardPolicy.Allow
                        : GuardPolicy.Deny) == GuardPolicy.Allow;
                    return accessWc;
                }

                return _defaultPolicy == GuardPolicy.Allow; 
            }
        }

        public bool Authorize(HttpContext context, string subjects = "ANONYMOUS",
            Action<string, string>? onDeny = null)
        {
            var path = context.Request.Path.Value!;

            if (!isGranted(context.Request.Method, path, subjects))
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
