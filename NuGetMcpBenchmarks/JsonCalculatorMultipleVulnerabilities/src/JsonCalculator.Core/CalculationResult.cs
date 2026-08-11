namespace JsonCalculator.Core;

public sealed class CalculationResult
{
    public CalculationResult(string operation, decimal left, decimal right, decimal value)
    {
        Operation = operation;
        Left = left;
        Right = right;
        Value = value;
    }

    public string Operation { get; }

    public decimal Left { get; }

    public decimal Right { get; }

    public decimal Value { get; }
}
