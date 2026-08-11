using System;

namespace JsonCalculator.Core;

public sealed class CalculatorEngine
{
    public CalculationResult Calculate(CalculationRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        string operation = request.Operation.Trim().ToLowerInvariant();
        decimal value;

        switch (operation)
        {
            case "add":
                value = request.Left + request.Right;
                break;
            case "subtract":
                value = request.Left - request.Right;
                break;
            case "multiply":
                value = request.Left * request.Right;
                break;
            case "divide":
                if (request.Right == 0)
                {
                    throw new DivideByZeroException("The right operand cannot be zero.");
                }

                value = request.Left / request.Right;
                break;
            default:
                throw new ArgumentException(
                    $"Unsupported operation '{request.Operation}'.",
                    nameof(request));
        }

        return new CalculationResult(operation, request.Left, request.Right, value);
    }
}
