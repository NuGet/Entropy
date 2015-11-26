using NuGet.Indexing;
using System;

namespace FastIndexer
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Data Source=jvhowicii4.database.windows.net;Initial Catalog=nuget-int-0-v2gallery;Integrated Security=False;User ID=legacy_2014May15;Password=NmU0NTc5ZGE1YmIyNDA3M2EzZjE1OTU3M2ZhNzBiZmY;Connect Timeout=30;Encrypt=True";
            string path = @"c:\data\index20151120";

            Sql2Lucene.Export(connectionString, path, Console.Out);
        }
    }
}
