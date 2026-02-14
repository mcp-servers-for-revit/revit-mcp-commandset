using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using RevitMCPCommandSet.Services;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests;

public class ColorSplashHandlerTests : RevitApiTest
{
    private static Document _doc = null!;
    private static string _tempPath = null!;
#pragma warning disable TUnit0023 // Revit elements are disposed when the document is closed
    private static ViewPlan _floorPlan = null!;
#pragma warning restore TUnit0023

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        var doc = Application.NewProjectDocument(UnitSystem.Imperial);

        using (var tx = new Transaction(doc, "Setup Color Splash Test Environment"))
        {
            tx.Start();

            var level = Level.Create(doc, 0.0);
            level.Name = "Color Test Level";

            var floorPlanType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan);

            if (floorPlanType != null)
            {
                ViewPlan.Create(doc, floorPlanType.Id, level.Id);
            }

            // Create walls with different Comments parameter values
            var wall1 = Wall.Create(doc, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), level.Id, false);
            var wall2 = Wall.Create(doc, Line.CreateBound(new XYZ(10, 0, 0), new XYZ(20, 0, 0)), level.Id, false);
            var wall3 = Wall.Create(doc, Line.CreateBound(new XYZ(20, 0, 0), new XYZ(30, 0, 0)), level.Id, false);

            // Set Comments parameter on walls
            wall1.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set("Group A");
            wall2.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set("Group A");
            wall3.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set("Group B");

            tx.Commit();
        }

        _tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.rvt");
        doc.SaveAs(_tempPath);
        doc.Close(false);
        _doc = Application.OpenDocumentFile(_tempPath);

        // Find the floor plan view associated with our level in the reopened document
        _floorPlan = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewPlan))
            .Cast<ViewPlan>()
            .First(v => v.ViewType == ViewType.FloorPlan &&
                        !v.IsTemplate &&
                        v.GenLevel != null &&
                        v.GenLevel.Name == "Color Test Level");
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
    public async Task Execute_WallsByComments_GroupsAndColorsCorrectly()
    {
        var handler = new ColorSplashHandler();
        handler.SetParameters("Walls", "Comments", useGradient: false, customColors: null);

        handler.RunOnDocument(_doc, _floorPlan);

        dynamic result = handler.ColoringResults;
        await Assert.That((bool)result.success).IsTrue();
        await Assert.That((int)result.totalElements).IsEqualTo(3);
        await Assert.That((int)result.coloredGroups).IsEqualTo(2);

        // Verify each group has the correct element count
        var results = (List<object>)result.results;
        var groupA = results.Cast<dynamic>().First(r => (string)r.parameterValue == "Group A");
        var groupB = results.Cast<dynamic>().First(r => (string)r.parameterValue == "Group B");
        await Assert.That((int)groupA.count).IsEqualTo(2);
        await Assert.That((int)groupB.count).IsEqualTo(1);
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_WithCustomColors_UsesProvidedColors()
    {
        var customColors = new JArray
        {
            new JObject { ["r"] = 255, ["g"] = 0, ["b"] = 0 },
            new JObject { ["r"] = 0, ["g"] = 255, ["b"] = 0 }
        };

        var handler = new ColorSplashHandler();
        handler.SetParameters("Walls", "Comments", useGradient: false, customColors: customColors);

        handler.RunOnDocument(_doc, _floorPlan);

        dynamic result = handler.ColoringResults;
        await Assert.That((bool)result.success).IsTrue();

        // Verify the assigned colors match what was requested
        var results = (List<object>)result.results;
        var colors = results.Cast<dynamic>().Select(r => r.color).ToList();
        var firstColor = colors[0];
        var secondColor = colors[1];
        await Assert.That((int)firstColor.r).IsEqualTo(255);
        await Assert.That((int)firstColor.g).IsEqualTo(0);
        await Assert.That((int)firstColor.b).IsEqualTo(0);
        await Assert.That((int)secondColor.r).IsEqualTo(0);
        await Assert.That((int)secondColor.g).IsEqualTo(255);
        await Assert.That((int)secondColor.b).IsEqualTo(0);
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_WithGradient_InterpolatesColors()
    {
        var handler = new ColorSplashHandler();
        handler.SetParameters("Walls", "Comments", useGradient: true, customColors: null);

        handler.RunOnDocument(_doc, _floorPlan);

        dynamic result = handler.ColoringResults;
        await Assert.That((bool)result.success).IsTrue();

        // Gradient goes blue (0,0,180) → red (180,0,0), so the two groups
        // should get distinct colors with the first more blue and the last more red
        var results = (List<object>)result.results;
        var colors = results.Cast<dynamic>().Select(r => r.color).ToList();
        var firstColor = colors[0];
        var lastColor = colors[colors.Count - 1];
        await Assert.That((int)firstColor.b).IsGreaterThan((int)firstColor.r);
        await Assert.That((int)lastColor.r).IsGreaterThan((int)lastColor.b);
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_InvalidCategory_ReportsError()
    {
        var handler = new ColorSplashHandler();
        handler.SetParameters("NonExistentCategory", "Comments", useGradient: false, customColors: null);

        handler.RunOnDocument(_doc, _floorPlan);

        dynamic result = handler.ColoringResults;
        await Assert.That((bool)result.success).IsFalse();
        await Assert.That((string)result.message).Contains("not found");
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_ResultStructure_ContainsExpectedFields()
    {
        var handler = new ColorSplashHandler();
        handler.SetParameters("Walls", "Comments", useGradient: false, customColors: null);

        handler.RunOnDocument(_doc, _floorPlan);

        dynamic result = handler.ColoringResults;
        await Assert.That((bool)result.success).IsTrue();

        // Verify each result entry has the expected structure
        var results = (List<object>)result.results;
        await Assert.That(results.Count).IsEqualTo(2);

        foreach (dynamic entry in results)
        {
            await Assert.That((string)entry.parameterValue).IsNotNull();
            await Assert.That((int)entry.count).IsGreaterThan(0);
            await Assert.That((int)entry.color.r).IsGreaterThanOrEqualTo(0).And.IsLessThanOrEqualTo(255);
            await Assert.That((int)entry.color.g).IsGreaterThanOrEqualTo(0).And.IsLessThanOrEqualTo(255);
            await Assert.That((int)entry.color.b).IsGreaterThanOrEqualTo(0).And.IsLessThanOrEqualTo(255);
            await Assert.That(((List<string>)entry.elementIds).Count).IsEqualTo((int)entry.count);
        }
    }
}
