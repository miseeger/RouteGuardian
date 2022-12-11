namespace RouteGuardian.Model
{
    public class GuardAccess
    {
        public string Default { get; set; } = "deny";
        public string[] Rules { get; set; } = new string[0];
    }
}
