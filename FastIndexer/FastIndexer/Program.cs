using NuGet.Indexing;
using System;

namespace FastIndexer
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "";
            string path = @"";

            Sql2Lucene.Export(connectionString, path, Console.Out);
        }
    }
}
