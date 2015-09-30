namespace StagingWebApi.Resources
{
    public class StageResourceBase
    {
        public StageResourceBase(string ownerName, string stageName)
        {
            OwnerName = ownerName;
            StageName = stageName;
        }

        protected string OwnerName { get; private set; }
        protected string StageName { get; private set; }
    }
}