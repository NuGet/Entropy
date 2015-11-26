using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NuGet.Indexing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LuceneCustomization
{
    public class TestTokenizer : DotTokenizer
    {
        public TestTokenizer(TextReader input)
            : base(input)
        {
        }
    }

    class Program
    {
        static void Test(string q, string p)
        {
            var terms = new List<string>();

            var buf = new StringBuilder();

            foreach (char ch in q)
            {
                if (ch == '\"')
                {
                    terms.Add(buf.ToString());
                    buf.Clear();
                }
                else
                {
                }
            }
        }

        static void Test0()
        {
            Test("\"hello", "hello");
            Test("hello\"", "hello");
            Test("a:\"hello", "a:hello");
            Test("a:\"hello b:goodbye", "a:\"hello b:goodbye\"");

            //Query query = LuceneQueryCreator.Parse("hello", false);
            //Query query = LuceneQueryCreator.Parse(q, false);
        }

        static void TestTokenization(string s)
        {
            TokenStream tokenStream = new TestTokenizer(new StringReader(s));

            ITermAttribute termAttribute = tokenStream.AddAttribute<ITermAttribute>();

            while (tokenStream.IncrementToken())
            {
                Console.WriteLine(termAttribute.Term);
            }
        }

        static void Test1()
        {
            TestTokenization("[ngAnimate]");
            TestTokenization("(ngAnimate]");
            TestTokenization("(ngAnimate)");
            TestTokenization("(.,ngAnimate)");
            TestTokenization("(.,ngAnimate");
            TestTokenization("{ngAnimate}");
        }

        static void Test2()
        {
            //string query = "signalrClient";
            //string query = "microsoft";
            string query = "HttpAgilityPack";

            //string description = "Misc MPL Libraries from CodePlex: HttpAgilityPack, InputSimulator, Irony Parser, WCF Rest Start Kit, XObjects\n\nUses the FluentSharp APIs";

            DescriptionAnalyzer analyzer = new DescriptionAnalyzer();

            TokenStream tokenStream = analyzer.TokenStream("", new StringReader(query));

            ITermAttribute termAttribute = tokenStream.AddAttribute<ITermAttribute>();

            while (tokenStream.IncrementToken())
            {
                Console.WriteLine(termAttribute.Term);
            }
        }

        static void Test3()
        {
            string q = "hello world";

            /*
            Query query = LuceneQueryCreator.Parse(q, false);

            Console.WriteLine(query.ToString());
            
            Query q2 = (new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "Title", new PackageAnalyzer())).Parse(query.ToString());

            QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "Title", new PackageAnalyzer());

            Query q3 = parser.Parse("(a:x a:y) (b:x b:y)");

            Query q4 = new TermQuery(new Lucene.Net.Index.Term("a", "x"));

            var clause = new BooleanClause(new TermQuery(new Lucene.Net.Index.Term("a", "x")), Occur.SHOULD);
            */

            BooleanQuery query = new BooleanQuery();

            BooleanQuery sub1 = new BooleanQuery();

            sub1.Add(new TermQuery(new Term("a", "x1 x2 x3")) { Boost = 1.0f }, Occur.MUST);
            sub1.Add(new TermQuery(new Term("a", "y")) { Boost = 1.0f }, Occur.MUST);

            sub1.Boost = 2.0f;

            BooleanQuery sub2 = new BooleanQuery();

            sub2.Add(new TermQuery(new Term("b", "x")) { Boost = 1.0f }, Occur.SHOULD);
            sub2.Add(new TermQuery(new Term("b", "y")) { Boost = 1.0f }, Occur.SHOULD);

            query.Add(sub1, Occur.SHOULD);
            query.Add(sub2, Occur.SHOULD);

            string s = query.ToString();

            QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "Title", new PackageAnalyzer());
            Query q2 = parser.Parse(s);

            Console.WriteLine(s);
        }

        static void Test4()
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            for (int i = 0; i < 10000; i++)
            {
                //Query query = LuceneQueryCreator.Parse("DotNetZip", false);
                Query query = NuGetQuery.MakeQuery("DotNetZip");
            }

            stopwatch.Stop();

            Console.WriteLine("{0} seconds", stopwatch.Elapsed.TotalSeconds);
        }

        static void Test5()
        {
            //TermQuery query = new TermQuery(new Term("Title", "dot"));

            string text = "DotNetZip";
            //string text = "NetZip";
            //string text = "dot";

            QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "Title", new PackageAnalyzer());

            Query q1 = parser.Parse(text);

            Console.WriteLine(q1.GetType().Name);

            Query q2 = ExecuteAnalyzer(new DescriptionAnalyzer(), "Title", text);

            Console.WriteLine(q2.GetType().Name);
        }

        static Query ExecuteAnalyzer(Analyzer analyzer, string field, string text)
        {
            TokenStream tokenStream = analyzer.TokenStream(field, new StringReader(text));

            ITermAttribute termAttribute = tokenStream.AddAttribute<ITermAttribute>();
            IPositionIncrementAttribute positionIncrementAttribute = tokenStream.AddAttribute<IPositionIncrementAttribute>();

            List<List<Term>> terms = new List<List<Term>>();
            List<Term> current = null;
            while (tokenStream.IncrementToken())
            {
                if (positionIncrementAttribute.PositionIncrement > 0)
                {
                    current = new List<Term>();
                    terms.Add(current);
                }
                if (current != null)
                {
                    current.Add(new Term(field, termAttribute.Term));
                }
            }

            if (terms.Count == 1 && terms[0].Count == 1)
            {
                return new TermQuery(terms[0][0]);
            }
            else if (terms.Select(l => l.Count).Sum() == terms.Count)
            {
                PhraseQuery phraseQuery = new PhraseQuery();
                foreach (var positionList in terms)
                {
                    phraseQuery.Add(positionList[0]);
                }
                return phraseQuery;
            }
            else
            {
                MultiPhraseQuery multiPhraseQuery = new MultiPhraseQuery();
                foreach (var positionList in terms)
                {
                    multiPhraseQuery.Add(positionList.ToArray());
                }
                return multiPhraseQuery;
            }
        }

        static void Test6()
        {
            SimpleFSDirectory directory = new SimpleFSDirectory(new DirectoryInfo(@"c:\data\index20151113int"));

            //using (IndexReader reader = IndexReader.Open(directory, true))
            //{
            //}

            using (IndexWriter writer = new IndexWriter(directory, new PackageAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED))
            {
                using (IndexReader reader = writer.GetReader())
                {
                    Console.WriteLine(new IndexSearcher(writer.GetReader()).Search(new MatchAllDocsQuery(), 10).TotalHits);

                    Document document = new Document();
                    document.Add(new Field("test", "test", Field.Store.NO, Field.Index.NOT_ANALYZED));
                    writer.AddDocument(document);
                    writer.Commit();

                    Console.WriteLine(new IndexSearcher(writer.GetReader()).Search(new MatchAllDocsQuery(), 10).TotalHits);
                }
            }
        }

        static void Test7()
        {
            string path = @"c:\data\nupkgs";

            var errors = new Dictionary<string, List<string>>();

            foreach (var package in PackageMetadataExtraction.GetPackages(path, errors))
            {
                Console.WriteLine("{0}/{1}", package["id"], package["version"]);

                foreach (var property in package)
                {
                    Console.WriteLine("\t{0} : {1}", property.Key, property.Value);
                }
            }

            foreach (var packageErrors in errors)
            {
                if (packageErrors.Value.Count > 0)
                {
                    Console.WriteLine(packageErrors.Key);
                    foreach (var error in packageErrors.Value)
                    {
                        Console.WriteLine("\t{0}", error);
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            try
            {
                //Test0();
                //Test1();
                //Test2();
                //Test3();
                //Test4();
                //Test5();
                //Test6();
                Test7();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
