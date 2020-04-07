namespace nuget_sdk_usage.Updater
{
    internal class UpdateAction
    {
        public UpdateAction(bool addAttribute)
        {
            AddAttribute = addAttribute;
            Actioned = false;
        }

        public bool AddAttribute { get; }

        public bool Actioned { get; private set; }

        public void SetActioned()
        {
            Actioned = true;
        }
    }
}
