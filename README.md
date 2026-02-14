# revit-mcp-commandset
🔄 Revit-MCP Client | Core implementation of the Revit-MCP protocol that connects LLMs with Revit. Includes essential CRUD commands for Revit elements enabling AI-driven BIM automation.

# Custom Commands Setup

## Installation

1. Create folder: `RevitMCPCommandSet` at the end of the usual Revit addins directory like so `C:\Users\[USERNAME]\AppData\Roaming\Autodesk\Revit\Addins\20XX\RevitMCPCommandSet\`

2. Add files:
   - Copy `command.json` from this repo to the `RevitMCPCommandSet` folder
   - Create `20XX` subfolder
   - Place compiled output from this repo in the `20XX` subfolder

3. In Revit: Go to **Add-ins** > **Settings** > **Refresh** > **Save**

## Testing

The test project uses [Nice3point.TUnit.Revit](https://github.com/Nice3point/RevitUnit) to run integration tests against a live Revit instance. No separate addin installation is required — the framework injects into the running Revit process automatically.

### Prerequisites

- **.NET 8 SDK** — install via `winget install Microsoft.DotNet.SDK.8`
- **Autodesk Revit 2026** — must be installed and licensed on your machine

### Running Tests

1. Open Revit 2026 and wait for it to fully load
2. Run the tests from the command line:

```bash
dotnet test -c Debug.R26 --project revit-mcp-commandset.Tests -r win-x64
```

> **Note:** The `-r win-x64` flag is required on ARM64 machines because the Revit API assemblies are x64-only.

Alternatively, you can use `dotnet run`:

```bash
cd revit-mcp-commandset.Tests
dotnet run -c Debug.R26
```

### Project Structure

| File | Purpose |
|------|---------|
| `revit-mcp-commandset.Tests/AssemblyInfo.cs` | Global `[assembly: TestExecutor<RevitThreadExecutor>]` — applies to all test methods, but hooks still need their own `[HookExecutor<RevitThreadExecutor>]` |
| `revit-mcp-commandset.Tests/Architecture/` | Tests for level and room creation commands |
| `revit-mcp-commandset.Tests/DataExtraction/` | Tests for model statistics, room data export, and material quantities |
| `revit-mcp-commandset.Tests/ColorSplashHandlerTests.cs` | Tests for color override functionality |
| `revit-mcp-commandset.Tests/TagRoomsHandlerTests.cs` | Tests for room tagging functionality |

### Writing New Tests

Test classes inherit from `RevitApiTest` and use TUnit's async assertion API. All assertions **must be awaited** — without `await`, assertions silently pass without checking anything.

Setup and cleanup hooks require `[HookExecutor<RevitThreadExecutor>]` to run on the Revit thread (the assembly-level `TestExecutor` only covers test methods, not hooks).

```csharp
public class MyTests : RevitApiTest
{
    private static Document _doc = null!;
    private static string _tempPath = null!;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        var doc = Application.NewProjectDocument(UnitSystem.Imperial);
        _tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.rvt");
        doc.SaveAs(_tempPath);
        doc.Close(false);
        _doc = Application.OpenDocumentFile(_tempPath);
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup()
    {
        _doc?.Close(false);
        try { File.Delete(_tempPath); } catch { }
    }

    [Test]
    public async Task MyTest_Condition_ExpectedResult()
    {
        var elements = new FilteredElementCollector(_doc)
            .WhereElementIsNotElementType()
            .ToElements();

        await Assert.That(elements.Count).IsGreaterThan(0);
    }
}
```

## Important Note

   - Command names must be identical between `revit-mcp` and `revit-mcp-commandset` repositories, otherwise Claude cannot find them.
   - The `commandRegistry.json` is created automatically, do not import it from the installer.