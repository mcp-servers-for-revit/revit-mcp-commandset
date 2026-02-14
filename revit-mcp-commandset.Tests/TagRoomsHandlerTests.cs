using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using RevitMCPCommandSet.Services;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests;

public class TagRoomsHandlerTests : RevitApiTest
{
    private static Document _doc = null!;
    private static string _tempPath = null!;
#pragma warning disable TUnit0023 // Revit elements are disposed when the document is closed
    private static ViewPlan _floorPlan = null!;
#pragma warning restore TUnit0023
    private static Room _room1 = null!;
    private static Room _room2 = null!;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        // Use default template which includes room tag annotation families
        var doc = Application.NewProjectDocument(Application.DefaultProjectTemplate);

        // Use the first level from the template (Level 1)
        var level = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .OrderBy(l => l.Elevation)
            .First();

        string levelName = level.Name;

        using (var tx = new Transaction(doc, "Setup Tag Test Environment"))
        {
            tx.Start();

            // Create primary enclosure (0,0)-(10,10) with a room
            CreateEnclosure(doc, level.Id, 0, 0, 10);
            _room1 = doc.Create.NewRoom(level, new UV(5.0, 5.0));

            // Create secondary enclosure (20,0)-(30,10) with another room
            CreateEnclosure(doc, level.Id, 20, 0, 10);
            _room2 = doc.Create.NewRoom(level, new UV(25.0, 5.0));

            tx.Commit();
        }

        _tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.rvt");
        doc.SaveAs(_tempPath);
        doc.Close(false);
        _doc = Application.OpenDocumentFile(_tempPath);

        // Find the floor plan view for our level in the reopened document
        _floorPlan = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewPlan))
            .Cast<ViewPlan>()
            .First(v => v.ViewType == ViewType.FloorPlan &&
                        !v.IsTemplate &&
                        v.GenLevel != null &&
                        v.GenLevel.Name == levelName);

        // Re-find rooms in the reopened document
        var rooms = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .Cast<Room>()
            .Where(r => r.Area > 0)
            .ToList();

        _room1 = rooms[0];
        _room2 = rooms[1];
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
    public async Task Execute_AllRooms_TagsCreatedSuccessfully()
    {
        var handler = new TagRoomsHandler();
        handler.SetParameters(useLeader: false, tagTypeId: null);

        handler.RunOnDocument(_doc, _floorPlan);

        dynamic result = handler.TaggingResults;
        await Assert.That((bool)result.success).IsTrue();
        await Assert.That((int)result.totalRooms).IsEqualTo(2);
        await Assert.That((int)result.taggedRooms).IsGreaterThan(0);
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_WithLeader_TagsHaveLeaders()
    {
        // Clear any existing room tags so this test doesn't depend on execution order
        using (var tx = new Transaction(_doc, "Clear tags for test"))
        {
            tx.Start();
            var existingTags = new FilteredElementCollector(_doc, _floorPlan.Id)
                .OfCategory(BuiltInCategory.OST_RoomTags)
                .WhereElementIsNotElementType()
                .ToElementIds();
            foreach (var id in existingTags)
                _doc.Delete(id);
            tx.Commit();
        }

        var handler = new TagRoomsHandler();
        handler.SetParameters(useLeader: true, tagTypeId: null);

        handler.RunOnDocument(_doc, _floorPlan);

        dynamic result = handler.TaggingResults;
        await Assert.That((bool)result.success).IsTrue();
        await Assert.That((int)result.taggedRooms).IsEqualTo(2);

        // Verify the tags actually have leaders set
        var tags = new FilteredElementCollector(_doc, _floorPlan.Id)
            .OfCategory(BuiltInCategory.OST_RoomTags)
            .WhereElementIsNotElementType()
            .Cast<RoomTag>()
            .ToList();
        await Assert.That(tags.Count).IsEqualTo(2);
        foreach (var tag in tags)
        {
            await Assert.That(tag.HasLeader).IsTrue();
        }
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_SpecificRoomIds_OnlyThoseRoomsTagged()
    {
        await Assert.That(_room1).IsNotNull();

        var handler = new TagRoomsHandler();
        handler.SetParameters(
            useLeader: false,
            tagTypeId: null,
            roomIds: new List<int> { (int)_room1.Id.Value });

        handler.RunOnDocument(_doc, _floorPlan);

        dynamic result = handler.TaggingResults;
        await Assert.That((bool)result.success).IsTrue();
        // Only the one specified room should be processed
        int tagged = (int)result.taggedRooms;
        int skipped = (int)result.skippedCount;
        await Assert.That(tagged + skipped).IsEqualTo(1);
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_AlreadyTaggedRooms_SkippedCorrectly()
    {
        // First call: tag all rooms
        var handler1 = new TagRoomsHandler();
        handler1.SetParameters(useLeader: false, tagTypeId: null);
        handler1.RunOnDocument(_doc, _floorPlan);

        dynamic result1 = handler1.TaggingResults;
        await Assert.That((bool)result1.success).IsTrue();

        // Second call: same rooms should be skipped
        var handler2 = new TagRoomsHandler();
        handler2.SetParameters(useLeader: false, tagTypeId: null);
        handler2.RunOnDocument(_doc, _floorPlan);

        dynamic result2 = handler2.TaggingResults;
        await Assert.That((bool)result2.success).IsTrue();
        // Skipped count should be > 0 since rooms already have tags
        await Assert.That((int)result2.skippedCount).IsGreaterThan(0);
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_ResultContainsRoomInfo_MessagePopulated()
    {
        var handler = new TagRoomsHandler();
        handler.SetParameters(useLeader: false, tagTypeId: null);

        handler.RunOnDocument(_doc, _floorPlan);

        dynamic result = handler.TaggingResults;
        await Assert.That((string)result.message).IsNotNull();
        await Assert.That((string)result.message).IsNotEmpty();
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
