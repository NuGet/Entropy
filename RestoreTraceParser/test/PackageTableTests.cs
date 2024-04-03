using RestoreTraceParser;

namespace RestoreTraceParserTests
{
    public class PackageTableTests
    {
        [Fact]
        public void SingleRun()
        {
            PackageTable table = new PackageTable();
            table.IncrementBy("A", 1);
            table.IncrementBy("B", 1);
            table.IncrementBy("A", 1);
            table.IncrementBy("C", 1);
            table.IncrementBy("B", 1);
            table.IncrementBy("A", 1);

            int prevIndex = table.IncrementRunIndex();

            Assert.Equal(3, table.GetNodeCount("A", prevIndex));
            Assert.Equal(2, table.GetNodeCount("B", prevIndex));
            Assert.Equal(1, table.GetNodeCount("C", prevIndex));
        }

        [Fact]
        public void TwoRuns()
        {
            PackageTable table = new PackageTable();
            table.IncrementBy("A", 1);
            table.IncrementBy("B", 1);
            table.IncrementBy("A", 1);
            table.IncrementBy("C", 1);
            table.IncrementBy("B", 1);
            table.IncrementBy("A", 1);

            int firstIndex = table.IncrementRunIndex();

            table.IncrementBy("D", 1);
            table.IncrementBy("E", 1);
            table.IncrementBy("F", 1);
            table.IncrementBy("E", 1);
            table.IncrementBy("D", 1);
            table.IncrementBy("F", 3);

            int secondIndex = table.IncrementRunIndex();

            Assert.Equal(3, table.GetNodeCount("A", firstIndex));
            Assert.Equal(2, table.GetNodeCount("B", firstIndex));
            Assert.Equal(1, table.GetNodeCount("C", firstIndex));

            Assert.Equal(2, table.GetNodeCount("D", secondIndex));
            Assert.Equal(2, table.GetNodeCount("E", secondIndex));
            Assert.Equal(4, table.GetNodeCount("F", secondIndex));
            Assert.Equal(0, table.GetNodeCount("A", secondIndex));
            Assert.Equal(0, table.GetNodeCount("B", secondIndex));
            Assert.Equal(0, table.GetNodeCount("C", secondIndex));
        }
    }
}