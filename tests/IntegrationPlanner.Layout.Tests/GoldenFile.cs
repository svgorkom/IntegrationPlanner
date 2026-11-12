using System.Runtime.CompilerServices;

namespace IntegrationPlanner.Layout.Tests;

internal static class GoldenFile
{
    private const string UpdateVariable = "IPLAN_UPDATE_GOLDEN";

    // Golden files live next to the sources so an intentional layout change is reviewed as a diff.
    public static string Read(string fileName, string actual, [CallerFilePath] string callerPath = "")
    {
        string path = Path.Combine(Path.GetDirectoryName(callerPath)!, "Golden", fileName);
        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            File.WriteAllText(path, actual);
        }

        return File.ReadAllText(path).ReplaceLineEndings();
    }
}
