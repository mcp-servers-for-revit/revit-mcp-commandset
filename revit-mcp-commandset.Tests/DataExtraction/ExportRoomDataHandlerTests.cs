using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using RevitMCPCommandSet.Services.DataExtraction;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.DataExtraction;

public class ExportRoomDataHandlerTests : RevitApiTest
{
    private static Document _doc = null!;
    private static string _tempPath = null!;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        var doc = Application.NewProjectDocument(UnitSystem.Imperial);

        using (var tx = new Transaction(doc, "Setup Export Room Data Test"))
        {
            tx.Start();

            var level = Level.Create(doc, 0.0);
            level.Name = "Export Test Level";

            var floorPlanType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan);

            if (floorPlanType != null)
            {
                ViewPlan.Create(doc, floorPlanType.Id, level.Id);
            }

            // Create enclosure and placed room
            CreateEnclosure(doc, level.Id, 0, 0, 10);
            var room = doc.Create.NewRoom(level, new UV(5.0, 5.0));
            if (room != null)
            {
                room.get_Parameter(BuiltInParameter.ROOM_NAME)?.Set("Test Room");
                room.get_Parameter(BuiltInParameter.ROOM_DEPARTMENT)?.Set("Testing");
            }

            // Create an unplaced room
            var phases = new FilteredElementCollector(doc)
                .OfClass(typeof(Phase))
                .Cast<Phase>()
                .ToList();
            if (phases.Count > 0)
            {
                doc.Create.NewRoom(phases.First());
            }

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
    public async Task Execute_PlacedRooms_DataExported()
    {
        var handler = new ExportRoomDataHandler();
        handler.SetParameters(includeUnplacedRooms: false, includeNotEnclosedRooms: false);

        handler.RunOnDocument(_doc);

        await Assert.That(handler.ResultInfo).IsNotNull();
        await Assert.That(handler.ResultInfo.Success).IsTrue();
        await Assert.That(handler.ResultInfo.TotalRooms).IsGreaterThan(0);
        await Assert.That(handler.ResultInfo.Rooms.Count).IsGreaterThan(0);

        var room = handler.ResultInfo.Rooms.First();
        await Assert.That(room.Name).IsEqualTo("Test Room");
        await Assert.That(room.Number).IsNotNullOrEmpty();
        await Assert.That(room.Level).IsEqualTo("Export Test Level");
        await Assert.That(room.Department).IsEqualTo("Testing");
        await Assert.That(room.Area).IsGreaterThan(0);
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_SkipUnplaced_UnplacedRoomsExcluded()
    {
        var handler = new ExportRoomDataHandler();
        handler.SetParameters(includeUnplacedRooms: false, includeNotEnclosedRooms: false);

        handler.RunOnDocument(_doc);

        await Assert.That(handler.ResultInfo.Success).IsTrue();

        // All returned rooms should have area > 0
        foreach (var room in handler.ResultInfo.Rooms)
        {
            await Assert.That(room.Area).IsGreaterThan(0);
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_IncludeUnplaced_AllRoomsReturned()
    {
        // Baseline: count placed rooms only
        var placedOnly = new ExportRoomDataHandler();
        placedOnly.SetParameters(includeUnplacedRooms: false, includeNotEnclosedRooms: false);
        placedOnly.RunOnDocument(_doc);

        // Act: count all rooms including unplaced
        var includeAll = new ExportRoomDataHandler();
        includeAll.SetParameters(includeUnplacedRooms: true, includeNotEnclosedRooms: true);
        includeAll.RunOnDocument(_doc);

        await Assert.That(includeAll.ResultInfo.Success).IsTrue();
        await Assert.That(includeAll.ResultInfo.TotalRooms).IsGreaterThan(placedOnly.ResultInfo.TotalRooms);
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_TotalArea_MatchesSumOfRoomAreas()
    {
        var handler = new ExportRoomDataHandler();
        handler.SetParameters(includeUnplacedRooms: false, includeNotEnclosedRooms: false);

        handler.RunOnDocument(_doc);

        await Assert.That(handler.ResultInfo.Success).IsTrue();

        double expectedTotal = handler.ResultInfo.Rooms.Sum(r => r.Area);
        await Assert.That(handler.ResultInfo.TotalArea).IsEqualTo(expectedTotal).Within(0.001);
        await Assert.That(handler.ResultInfo.TotalArea).IsGreaterThan(0);
    }

    private static void CreateEnclosure(Document doc, ElementId levelId, double x, double y, double size)
    {
        var p1 = new XYZ(x, y, 0);
        var p2 = new XYZ(x + size, y, 0);
        var p3 = new XYZ(x + size, y + size, 0);
        var p4 = new XYZ(x, y + size, 0);

        Wall.Create(doc, Line.CreateBound(p1, p2), levelId, false);
        Wall.Create(doc, Line.CreateBound(p2, p3), levelId, false);
        Wall.Create(doc, Line.CreateBound(p3, p4), levelId, false);
        Wall.Create(doc, Line.CreateBound(p4, p1), levelId, false);
    }
}
