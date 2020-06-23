namespace PackageHelper.Csv
{
    public static class ExtensionMethods
    {
        public static bool IsWarmUp(this RestoreResultRecord record)
        {
            return record.ScenarioName == "warmup";
        }
    }
}
