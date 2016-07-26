namespace NuGet.TeamCity.AgentAuthorizer
{
    public class Agent
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TypeId { get; set; }
        public bool Connected { get; set; }
        public bool Enabled { get; set; }
        public bool Authorized { get; set; }
        public string Href { get; set; }
    }
}
