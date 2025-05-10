namespace ASC.Utilities
{
    public class CurrentUser
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
    }
}
