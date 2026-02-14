using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.DataExtraction;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.DataExtraction
{
    /// <summary>
    /// Business logic for calculating material quantities.
    /// This class has no RevitAPIUI dependencies and can be used directly in tests.
    /// </summary>
    public class GetMaterialQuantitiesHandler
    {
        private List<string> _categoryFilters;
        private bool _selectedElementsOnly;
        protected ICollection<ElementId> _selectedElementIds;

        public GetMaterialQuantitiesResult ResultInfo { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(List<string> categoryFilters = null, bool selectedElementsOnly = false)
        {
            _categoryFilters = categoryFilters;
            _selectedElementsOnly = selectedElementsOnly;
            TaskCompleted = false;
            _resetEvent.Reset();
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public void RunOnDocument(Document doc)
        {
            try
            {
                // Dictionary to accumulate material quantities
                var materialData = new Dictionary<ElementId, MaterialQuantityModel>();

                // Get elements to analyze
                ICollection<Element> elements;
                if (_selectedElementsOnly && _selectedElementIds != null && _selectedElementIds.Count > 0)
                {
                    elements = _selectedElementIds.Select(id => doc.GetElement(id)).Where(e => e != null).ToList();
                }
                else
                {
                    var collector = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType();

                    // Apply category filters if specified
                    if (_categoryFilters != null && _categoryFilters.Count > 0)
                    {
                        var builtInCategories = new List<BuiltInCategory>();
                        foreach (var catName in _categoryFilters)
                        {
                            if (Enum.TryParse(catName, out BuiltInCategory cat))
                            {
                                builtInCategories.Add(cat);
                            }
                        }
                        if (builtInCategories.Count > 0)
                        {
                            var filter = new ElementMulticategoryFilter(builtInCategories);
                            collector = collector.WherePasses(filter);
                        }
                    }

                    elements = collector.ToElements();
                }

                // Process each element
                foreach (Element element in elements)
                {
                    // Get all material ids in the element
                    var materialIds = element.GetMaterialIds(false);

                    foreach (ElementId matId in materialIds)
                    {
                        Material material = doc.GetElement(matId) as Material;
                        if (material == null) continue;

                        // Initialize material data if not exists
                        if (!materialData.ContainsKey(matId))
                        {
                            materialData[matId] = new MaterialQuantityModel
                            {
#if REVIT2024_OR_GREATER
                                MaterialId = matId.Value,
#else
                                MaterialId = matId.IntegerValue,
#endif
                                MaterialName = material.Name,
                                MaterialClass = material.MaterialClass
                            };
                        }

                        // Get material area and volume for this element
                        double area = element.GetMaterialArea(matId, false);
                        double volume = element.GetMaterialVolume(matId);

                        materialData[matId].Area += area;
                        materialData[matId].Volume += volume;

#if REVIT2024_OR_GREATER
                        if (!materialData[matId].ElementIds.Contains(element.Id.Value))
                        {
                            materialData[matId].ElementIds.Add(element.Id.Value);
#else
                        if (!materialData[matId].ElementIds.Contains(element.Id.IntegerValue))
                        {
                            materialData[matId].ElementIds.Add(element.Id.IntegerValue);
#endif
                            materialData[matId].ElementCount++;
                        }
                    }
                }

                var materials = materialData.Values.ToList();
                double totalArea = materials.Sum(m => m.Area);
                double totalVolume = materials.Sum(m => m.Volume);

                ResultInfo = new GetMaterialQuantitiesResult
                {
                    TotalMaterials = materials.Count,
                    TotalArea = totalArea,
                    TotalVolume = totalVolume,
                    Materials = materials,
                    Success = true,
                    Message = $"Successfully calculated quantities for {materials.Count} materials"
                };
            }
            catch (Exception ex)
            {
                ResultInfo = new GetMaterialQuantitiesResult
                {
                    Success = false,
                    Message = $"Error calculating material quantities: {ex.Message}"
                };
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }
    }

    /// <summary>
    /// Event handler for calculating material quantities in Revit
    /// </summary>
    public class GetMaterialQuantitiesEventHandler : GetMaterialQuantitiesHandler, IExternalEventHandler, IWaitableExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            var uiDoc = app.ActiveUIDocument;
            _selectedElementIds = uiDoc.Selection.GetElementIds();
            RunOnDocument(uiDoc.Document);
        }

        public string GetName()
        {
            return "Get Material Quantities";
        }
    }
}
