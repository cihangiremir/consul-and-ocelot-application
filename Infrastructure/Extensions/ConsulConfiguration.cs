namespace Infrastructure.Extensions
{
    public class ConsulConfiguration
    {
        public string Host { get; set; }
        public int WaitTime { get; set; }
        public string Token { get; set; }
        public IEnumerable<AgentCheckRegistration> AgentCheckRegistrations { get; set; }
    }

    public class AgentCheckRegistration
    {
        public string Endpoint { get; set; }
        public string Notes { get; set; }
        public double Timeout { get; set; }
        public double Interval { get; set; }
        public bool TLSSkipVerify { get; set; }
        public string HttpMethod { get; set; }
    }
}
