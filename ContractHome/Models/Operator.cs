﻿namespace ContractHome.Models
{
    public class Operator
    {
        public string PID { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public string Region { get; set; }

        public Operator(string pID, string email, string title, string region)
        {
            PID = pID;
            Email = email;
            Title = title;
            Region = region;
        }
    }
}