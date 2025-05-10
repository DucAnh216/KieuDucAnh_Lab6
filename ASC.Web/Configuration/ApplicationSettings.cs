namespace ASC.Web.Configuration
{
    public class ApplicationSettings
    {
        public  string? ApplicationTitle { get; set; }

        // Admin Information
        public  string? AdminEmail { get; set; }
        public  string? AdminName { get; set; }
        public  string? AdminPassword { get; set; }

        // User Roles
        public  string? Roles { get; set; }

        // Engineer Information
        public  string? EngineerEmail { get; set; }
        public  string? EngineerName { get; set; }
        public  string? EngineerPassword { get; set; }

        // SMTP Configuration
        public  string? SMTPServer { get; set; }
        public  int SMTPPort { get; set; }
        public  string? SMTPAccount { get; set; }
        public  string? SMTPPassword { get; set; }
    }
}
