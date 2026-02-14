using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Services.Architecture;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateRoomHandlerTests : RevitApiTest
{
    private static Document _doc = null!;
    private static string _tempPath = null!;
#pragma warning disable TUnit0023 // Revit elements are disposed when the document is closed
    private static Level _level = null!;
#pragma warning restore TUnit0023

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        var doc = Application.NewProjectDocument(UnitSystem.Imperial);

        using (var tx = new Transaction(doc, "Setup Room Test Environment"))
        {
            tx.Start();

            _level = Level.Create(doc, 0.0);
            _level.Name = "Room Handler Test Level";

            // Each test gets its own exclusive enclosure(s) to avoid isolation issues
            // Test 1 (SingleRoom): enclosure at x=0
            CreateEnclosure(doc, _level.Id, 0, 0, 10);
            // Test 2 (SetNameAndNumber): enclosure at x=20
            CreateEnclosure(doc, _level.Id, 20, 0, 10);
            // Test 3 (SetDepartmentAndComments): enclosure at x=40
            CreateEnclosure(doc, _level.Id, 40, 0, 10);
            // Test 4 (DuplicateNumber): two enclosures at x=60, x=80
            CreateEnclosure(doc, _level.Id, 60, 0, 10);
            CreateEnclosure(doc, _level.Id, 80, 0, 10);
            // Test 5 (MultipleRooms): two enclosures at x=100, x=120
            CreateEnclosure(doc, _level.Id, 100, 0, 10);
            CreateEnclosure(doc, _level.Id, 120, 0, 10);
            // Test 6 (WithLevelId): enclosure at x=140
            CreateEnclosure(doc, _level.Id, 140, 0, 10);

            tx.Commit();
        }

        _tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.rvt");
        doc.SaveAs(_tempPath);
        doc.Close(false);
        _doc = Application.OpenDocumentFile(_tempPath);

        // Re-find the level in the reopened document
        _level = new FilteredElementCollector(_doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .First(l => l.Name == "Room Handler Test Level");
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
    public async Task Execute_SingleRoom_CreatesRoomWithArea()
    {
        var handler = new CreateRoomHandler();
        handler.SetParameters(new List<RoomCreationInfo>
        {
            new RoomCreationInfo
            {
                Name = "Office",
                Number = "100",
                Location = new JZPoint(5 * 304.8, 5 * 304.8, 0) // Center of first enclosure
            }
        });

        handler.RunOnDocument(_doc);

        await Assert.That(handler.Result).IsNotNull();
        await Assert.That(handler.Result.Success).IsTrue();
        await Assert.That(handler.Result.Response).IsNotNull();
        await Assert.That(handler.Result.Response.Count).IsEqualTo(1);
        await Assert.That(handler.Result.Response[0].Name).IsEqualTo("Office");
        await Assert.That(handler.Result.Response[0].Area).IsGreaterThan(0);
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_SetNameAndNumber_PropertiesApplied()
    {
        var handler = new CreateRoomHandler();
        handler.SetParameters(new List<RoomCreationInfo>
        {
            new RoomCreationInfo
            {
                Name = "Conference Room",
                Number = "200",
                Location = new JZPoint(25 * 304.8, 5 * 304.8, 0) // Center of second enclosure
            }
        });

        handler.RunOnDocument(_doc);

        await Assert.That(handler.Result.Success).IsTrue();
        await Assert.That(handler.Result.Response[0].Name).IsEqualTo("Conference Room");
        await Assert.That(handler.Result.Response[0].Number).IsEqualTo("200");
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_SetDepartmentAndComments_ParametersSet()
    {
        var handler = new CreateRoomHandler();
        handler.SetParameters(new List<RoomCreationInfo>
        {
            new RoomCreationInfo
            {
                Name = "Lab Room",
                Number = "300",
                Location = new JZPoint(45 * 304.8, 5 * 304.8, 0), // Center of third enclosure
                Department = "Engineering",
                Comments = "Test comment"
            }
        });

        handler.RunOnDocument(_doc);

        await Assert.That(handler.Result.Success).IsTrue();

        // Verify room was created and properties set by checking the Revit element
        var room = _doc.GetElement(new ElementId(handler.Result.Response[0].Id)) as Autodesk.Revit.DB.Architecture.Room;
        await Assert.That(room).IsNotNull();

        var deptParam = room.get_Parameter(BuiltInParameter.ROOM_DEPARTMENT);
        await Assert.That(deptParam?.AsString()).IsEqualTo("Engineering");

        var commentsParam = room.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
        await Assert.That(commentsParam?.AsString()).IsEqualTo("Test comment");
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_DuplicateNumber_UniqueNumberGenerated()
    {
        // Create first room with number "400" in enclosure at x=60
        var handler1 = new CreateRoomHandler();
        handler1.SetParameters(new List<RoomCreationInfo>
        {
            new RoomCreationInfo
            {
                Name = "Room A",
                Number = "400",
                Location = new JZPoint(65 * 304.8, 5 * 304.8, 0)
            }
        });
        handler1.RunOnDocument(_doc);
        await Assert.That(handler1.Result.Success).IsTrue();

        // Create second room with same number in enclosure at x=80 - should get a unique number
        var handler2 = new CreateRoomHandler();
        handler2.SetParameters(new List<RoomCreationInfo>
        {
            new RoomCreationInfo
            {
                Name = "Room B",
                Number = "400",
                Location = new JZPoint(85 * 304.8, 5 * 304.8, 0)
            }
        });
        handler2.RunOnDocument(_doc);

        await Assert.That(handler2.Result.Success).IsTrue();
        // The assigned number should differ from "400" since that number is already taken
        await Assert.That(handler2.Result.Response[0].RequestedNumber).IsEqualTo("400");
        await Assert.That(handler2.Result.Response[0].Number).IsNotEqualTo("400");
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_MultipleRooms_AllCreatedSuccessfully()
    {
        var handler = new CreateRoomHandler();
        handler.SetParameters(new List<RoomCreationInfo>
        {
            new RoomCreationInfo
            {
                Name = "Multi Room 1",
                Number = "500",
                Location = new JZPoint(105 * 304.8, 5 * 304.8, 0) // Enclosure at x=100
            },
            new RoomCreationInfo
            {
                Name = "Multi Room 2",
                Number = "501",
                Location = new JZPoint(125 * 304.8, 5 * 304.8, 0) // Enclosure at x=120
            }
        });

        handler.RunOnDocument(_doc);

        await Assert.That(handler.Result.Success).IsTrue();
        await Assert.That(handler.Result.Response.Count).IsEqualTo(2);
        await Assert.That(handler.Result.Response[0].Name).IsEqualTo("Multi Room 1");
        await Assert.That(handler.Result.Response[1].Name).IsEqualTo("Multi Room 2");
    }

    [Test]
    [TestExecutor<RevitThreadExecutor>]
    public async Task Execute_WithLevelId_RoomCreatedOnCorrectLevel()
    {
        var handler = new CreateRoomHandler();
        handler.SetParameters(new List<RoomCreationInfo>
        {
            new RoomCreationInfo
            {
                Name = "Level Room",
                Number = "600",
                Location = new JZPoint(145 * 304.8, 5 * 304.8, 0), // Enclosure at x=140
                LevelId = (int)_level.Id.Value
            }
        });

        handler.RunOnDocument(_doc);

        await Assert.That(handler.Result.Success).IsTrue();
        await Assert.That(handler.Result.Response[0].LevelName).IsEqualTo("Room Handler Test Level");
    }

    #region Helper Methods

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

    #endregion
}
