namespace RouteGuardian.Model
{
    public class GuardRule
    {
        public GuardPolicy Policy { get; set; }
        public string Verb { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
    }
}
