namespace ContractHome.Models
{
    public class Operator
    {
        public string UID { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public string Region { get; set; }
        public string? PID { get; set; }
        public bool IsOperator { get; set; }

        public Operator(string uid, string email, string title, string region, bool isOperator, string pid)
        {
            UID = uid;
            Email = email;
            Title = title;
            Region = region;
            IsOperator = isOperator;
            PID = pid;
        }
    }
}
