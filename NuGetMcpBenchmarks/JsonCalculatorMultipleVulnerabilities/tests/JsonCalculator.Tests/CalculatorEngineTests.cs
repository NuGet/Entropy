using JsonCalculator.Core;

namespace JsonCalculator.Tests;

public sealed class CalculatorEngineTests
{
    [Theory]
    [InlineData("add", 12, 5, 17)]
    [InlineData("subtract", 12, 5, 7)]
    [InlineData("multiply", 6, 7, 42)]
    [InlineData("divide", 21, 3, 7)]
    public void Calculate_PerformsSupportedOperation(
        string operation,
        int left,
        int right,
        int expected)
    {
        var engine = new CalculatorEngine();

        CalculationResult result = engine.Calculate(new CalculationRequest
        {
            Operation = operation,
            Left = left,
            Right = right
        });

        Assert.Equal(expected, result.Value);
        Assert.Equal(operation, result.Operation);
    }

    [Fact]
    public void Calculate_DivisionByZeroThrows()
    {
        var engine = new CalculatorEngine();
        var request = new CalculationRequest
        {
            Operation = "divide",
            Left = 10,
            Right = 0
        };

        Assert.Throws<DivideByZeroException>(() => engine.Calculate(request));
    }
}
