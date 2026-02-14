using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Services.Architecture;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateLevelHandlerTests : RevitApiTest
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
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_SingleLevel_CreatesLevelAtCorrectElevation()
    {
        var handler = new CreateLevelHandler();
        handler.SetParameters(new List<LevelInfo>
        {
            new LevelInfo { Name = "Single Level Test", Elevation = 3000 }
        });

        handler.RunOnDocument(_doc);

        await Assert.That(handler.Result).IsNotNull();
        await Assert.That(handler.Result.Success).IsTrue();
        await Assert.That(handler.Result.Response).IsNotNull();
        await Assert.That(handler.Result.Response.Count).IsEqualTo(1);
        await Assert.That(handler.Result.Response[0].Elevation).IsEqualTo(3000);
        await Assert.That(handler.Result.Response[0].AlreadyExisted).IsFalse();
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_SetName_LevelNameApplied()
    {
        var handler = new CreateLevelHandler();
        handler.SetParameters(new List<LevelInfo>
        {
            new LevelInfo { Name = "Custom Name Level", Elevation = 5000 }
        });

        handler.RunOnDocument(_doc);

        await Assert.That(handler.Result.Success).IsTrue();
        await Assert.That(handler.Result.Response[0].Name).IsEqualTo("Custom Name Level");
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_WithFloorPlan_FloorPlanViewCreated()
    {
        var handler = new CreateLevelHandler();
        handler.SetParameters(new List<LevelInfo>
        {
            new LevelInfo { Name = "FloorPlan Level", Elevation = 7000, CreateFloorPlan = true, CreateCeilingPlan = false }
        });

        handler.RunOnDocument(_doc);

        await Assert.That(handler.Result.Success).IsTrue();
        await Assert.That(handler.Result.Response[0].FloorPlanViewName).IsNotNull();
        await Assert.That(handler.Result.Response[0].CeilingPlanViewName).IsNull();
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_WithCeilingPlan_CeilingPlanViewCreated()
    {
        var handler = new CreateLevelHandler();
        handler.SetParameters(new List<LevelInfo>
        {
            new LevelInfo { Name = "CeilingPlan Level", Elevation = 9000, CreateFloorPlan = false, CreateCeilingPlan = true }
        });

        handler.RunOnDocument(_doc);

        await Assert.That(handler.Result.Success).IsTrue();
        await Assert.That(handler.Result.Response[0].CeilingPlanViewName).IsNotNull();
        await Assert.That(handler.Result.Response[0].FloorPlanViewName).IsNull();
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_DuplicateName_ReportsAlreadyExisted()
    {
        // First call: create the level
        var handler1 = new CreateLevelHandler();
        handler1.SetParameters(new List<LevelInfo>
        {
            new LevelInfo { Name = "Dup Test Level", Elevation = 11000 }
        });
        handler1.RunOnDocument(_doc);
        await Assert.That(handler1.Result.Success).IsTrue();
        await Assert.That(handler1.Result.Response[0].AlreadyExisted).IsFalse();

        // Second call: same name should report already existed
        var handler2 = new CreateLevelHandler();
        handler2.SetParameters(new List<LevelInfo>
        {
            new LevelInfo { Name = "Dup Test Level", Elevation = 12000 }
        });
        handler2.RunOnDocument(_doc);

        await Assert.That(handler2.Result.Success).IsTrue();
        await Assert.That(handler2.Result.Response[0].AlreadyExisted).IsTrue();
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_MultipleLevels_AllCreatedAtCorrectElevations()
    {
        var levels = new List<LevelInfo>
        {
            new LevelInfo { Name = "Batch A", Elevation = 0 },
            new LevelInfo { Name = "Batch B", Elevation = 3000 },
            new LevelInfo { Name = "Batch C", Elevation = 6000 },
            new LevelInfo { Name = "Batch D", Elevation = 9000 }
        };

        var handler = new CreateLevelHandler();
        handler.SetParameters(levels);
        handler.RunOnDocument(_doc);

        await Assert.That(handler.Result.Success).IsTrue();
        await Assert.That(handler.Result.Response.Count).IsEqualTo(4);

        for (int i = 0; i < levels.Count; i++)
        {
            await Assert.That(handler.Result.Response[i].Name).IsEqualTo(levels[i].Name);
            await Assert.That(handler.Result.Response[i].Elevation).IsEqualTo(levels[i].Elevation);
        }
    }
}
