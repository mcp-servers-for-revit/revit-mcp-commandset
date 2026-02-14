using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using RevitMCPCommandSet.Services.DataExtraction;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.DataExtraction;

public class GetMaterialQuantitiesHandlerTests : RevitApiTest
{
    private static Document _doc = null!;
    private static string _tempPath = null!;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        var doc = Application.NewProjectDocument(UnitSystem.Imperial);

        using (var tx = new Transaction(doc, "Setup Material Quantities Test"))
        {
            tx.Start();

            var level = Level.Create(doc, 0.0);
            level.Name = "Material Test Level";

            // Create walls that will have materials
            Wall.Create(doc, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), level.Id, false);
            Wall.Create(doc, Line.CreateBound(new XYZ(10, 0, 0), new XYZ(10, 10, 0)), level.Id, false);
            Wall.Create(doc, Line.CreateBound(new XYZ(10, 10, 0), new XYZ(0, 10, 0)), level.Id, false);

            tx.Commit();
        }

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
    public async Task Execute_AllCategories_MaterialsFound()
    {
        var handler = new GetMaterialQuantitiesHandler();
        handler.SetParameters(categoryFilters: null, selectedElementsOnly: false);

        handler.RunOnDocument(_doc);

        await Assert.That(handler.ResultInfo).IsNotNull();
        await Assert.That(handler.ResultInfo.Success).IsTrue();
        await Assert.That(handler.ResultInfo.TotalMaterials).IsGreaterThan(0);
        await Assert.That(handler.ResultInfo.Materials.Count).IsGreaterThan(0);

        // Verify material names are populated
        foreach (var material in handler.ResultInfo.Materials)
        {
            await Assert.That(material.MaterialName).IsNotNullOrEmpty();
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_FilterByWalls_OnlyWallMaterialsReturned()
    {
        var handler = new GetMaterialQuantitiesHandler();
        handler.SetParameters(
            categoryFilters: new List<string> { "OST_Walls" },
            selectedElementsOnly: false);

        handler.RunOnDocument(_doc);

        await Assert.That(handler.ResultInfo.Success).IsTrue();
        await Assert.That(handler.ResultInfo.TotalMaterials).IsGreaterThan(0);

        // Since setup only creates walls, filtered results should match unfiltered
        var allHandler = new GetMaterialQuantitiesHandler();
        allHandler.SetParameters(categoryFilters: null, selectedElementsOnly: false);
        allHandler.RunOnDocument(_doc);

        await Assert.That(handler.ResultInfo.TotalMaterials).IsEqualTo(allHandler.ResultInfo.TotalMaterials);
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_AreaAndVolume_NonNegative()
    {
        var handler = new GetMaterialQuantitiesHandler();
        handler.SetParameters(categoryFilters: null, selectedElementsOnly: false);

        handler.RunOnDocument(_doc);

        await Assert.That(handler.ResultInfo.Success).IsTrue();
        await Assert.That(handler.ResultInfo.TotalArea).IsGreaterThanOrEqualTo(0);
        await Assert.That(handler.ResultInfo.TotalVolume).IsGreaterThanOrEqualTo(0);

        foreach (var material in handler.ResultInfo.Materials)
        {
            await Assert.That(material.Area).IsGreaterThanOrEqualTo(0);
            await Assert.That(material.Volume).IsGreaterThanOrEqualTo(0);
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_ElementCountPerMaterial_TracksUniqueElements()
    {
        var handler = new GetMaterialQuantitiesHandler();
        handler.SetParameters(categoryFilters: null, selectedElementsOnly: false);

        handler.RunOnDocument(_doc);

        await Assert.That(handler.ResultInfo.Success).IsTrue();

        foreach (var material in handler.ResultInfo.Materials)
        {
            await Assert.That(material.ElementCount).IsGreaterThan(0);
            await Assert.That(material.ElementIds.Count).IsEqualTo(material.ElementCount);
            // Verify element IDs are unique
            await Assert.That(material.ElementIds.Distinct().Count()).IsEqualTo(material.ElementIds.Count);
        }
    }
}
