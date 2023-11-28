namespace RestoreTimeParser.UnitTests
{
    [TestClass]
    public class RestoreTimeParserTests
    {
        [TestMethod]
        public void ParseRestoreFile_Should_Return_Correct_Output()
        {
            // Arrange
            string[] lines = new string[]
            {
            "         Restored D:\\dbs\\el\\ctpl\\sources\\dev\\Torus\\src\\WorkflowSchedulePackage\\WorkflowSchedulePackage.csproj (in 86 ms).",
            "         Restored d:\\dbs\\el\\utff\\src\\sources\\dev\\mexagents\\src\\addressbookpolicyroutingagent\\Microsoft.Exchange.Transport.Agent.AddressBookPolicyRoutingAgent.csproj (in 18.45 sec).",
            "         Restored d:\\dbs\\el\\utff\\src\\sources\\dev\\mexagents\\src\\addressbookpolicyroutingagent\\Microsoft.Exchange.Transport.Agent.AddressBookPolicyRoutingAgent.csproj (in 1.45 min)."
            };

            List<string> expectedOutput = new List<string>()
            {
                "D:\\dbs\\el\\ctpl\\sources\\dev\\Torus\\src\\WorkflowSchedulePackage\\WorkflowSchedulePackage.csproj,86,0.09,0",
                "d:\\dbs\\el\\utff\\src\\sources\\dev\\mexagents\\src\\addressbookpolicyroutingagent\\Microsoft.Exchange.Transport.Agent.AddressBookPolicyRoutingAgent.csproj,18450,18.45,0.31",
                "d:\\dbs\\el\\utff\\src\\sources\\dev\\mexagents\\src\\addressbookpolicyroutingagent\\Microsoft.Exchange.Transport.Agent.AddressBookPolicyRoutingAgent.csproj,87000,87,1.45"
            };

            // Act
            List<string> actualOutput = Program.ParseRestoreFile(lines);

            // Assert
            Assert.AreEqual(expectedOutput.Count, actualOutput.Count);
            for (int i = 0; i < expectedOutput.Count; i++)
            {
                Assert.AreEqual(expectedOutput[i], actualOutput[i]);
            }
        }

        [TestMethod]
        public void ParseRestoreFile_Skip_Nonmatching_Lines()
        {
            // Arrange
            string[] lines = new string[]
            {
            "         Restoring packages for d:\\dbs\\el\\utff\\src\\sources\\test\\bigfunnel\\src\\bigfunnel.globalization.tests\\BigFunnel.Globalization.Tests.csproj..."
            };


            // Act
            List<string> actualOutput = Program.ParseRestoreFile(lines);

            // Assert
            Assert.AreEqual(0, actualOutput.Count);
        }

    }
}