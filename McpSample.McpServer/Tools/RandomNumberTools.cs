using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public class RandomNumberTools
{
    [McpServerTool]
    [Description("Generates a random number between the specified minimum and maximum values.")]
    public int GetRandomNumber(
        [Description("Minimum value (inclusive)")] int min = 0,
        [Description("Maximum value (exclusive)")] int max = 100)
    {
        if (min >= max)
            throw new ArgumentOutOfRangeException(nameof(min), "Minimum value must be less than maximum value.");
        return Random.Shared.Next(min, max);
    }
}
