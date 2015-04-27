using MapsSampleData.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Devices.Geolocation;
using Windows.Storage;

namespace MapsSampleData.DataModel
{
    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class SampleDataItem
    {
        public SampleDataItem(String uniqueId, String title, String subtitle, String imagePath, String description, String content)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Content = content;
        }

        public SampleDataItem(String uniqueId, String title, String subtitle, String imagePath, String description, String content, Geopoint location)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Content = content;
            this.Location = location;
        }

        public SampleDataItem(String uniqueId, String title, String subtitle, String imagePath, String description, String content, Geopoint location, String address)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Content = content;
            this.Location = location;
            this.Address = address;
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }
        public string Content { get; private set; }
        public Geopoint Location { get; private set; }
        public string Address { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Generic group data model.
    /// </summary>
    public class SampleDataGroup
    {
        public SampleDataGroup(String uniqueId, String title, String subtitle, String imagePath, String description)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Items = new ObservableCollection<SampleDataItem>();
        }

        public SampleDataGroup(String uniqueId, String title, String subtitle, String imagePath, String description, GeoboundingBox bestMapViewBoxList, Geopath shapePath)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.BestMapViewBoxList = bestMapViewBoxList;
            this.ShapePath = shapePath;
            this.Items = new ObservableCollection<SampleDataItem>();
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }
        public GeoboundingBox BestMapViewBoxList { get; private set; }
        public Geopath ShapePath { get; private set; }
        public ObservableCollection<SampleDataItem> Items { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Creates a collection of groups and items with content read from a static json file.
    /// 
    /// SampleDataSource initializes with data read from a static json file included in the 
    /// project.  This provides sample data at both design-time and run-time.
    /// </summary>
    public sealed class SampleDataSource
    {
        private static SampleDataSource _sampleDataSource = new SampleDataSource();

        private ObservableCollection<SampleDataGroup> _groups = new ObservableCollection<SampleDataGroup>();
        public ObservableCollection<SampleDataGroup> Groups
        {
            get { return this._groups; }
        }


        public static async Task<IEnumerable<SampleDataGroup>> GetGroupsAsync()
        {
            await _sampleDataSource.GetSampleDataAsync();

            return _sampleDataSource.Groups;
        }

        public static async Task<SampleDataGroup> GetGroupAsync(string uniqueId)
        {
            await _sampleDataSource.GetSampleDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static async Task<SampleDataItem> GetItemAsync(string uniqueId)
        {
            await _sampleDataSource.GetSampleDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.Groups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        private async Task GetSampleDataAsync()
        {
            if (this._groups.Count != 0)
                return;

            Uri dataUri = new Uri("ms-appx:///DataModel/SampleData.json");

            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
            string jsonText = await FileIO.ReadTextAsync(file);
            JsonObject jsonObject = JsonObject.Parse(jsonText);
            JsonArray jsonArray = jsonObject["PointsOfInterest"].GetArray();

            foreach (JsonValue groupValue in jsonArray)
            {
                JsonObject groupObject = groupValue.GetObject();

                IJsonValue shapeValue;
                List<BasicGeoposition> shapeList;
                Geopath shapePath = null;

                if (groupObject.TryGetValue("Shape", out shapeValue))
                {
                    //JsonObject shapeObject = shapeValue.GetObject();
                    string shape = shapeValue.GetString();
                    if (PathParser.TryParseEncodedValue(shape, out shapeList))
                    {
                        shapePath = new Geopath(shapeList);
                    }
                }



                IJsonValue bestMapViewBoxValue;
                List<BasicGeoposition> bestMapViewBoxList = new List<BasicGeoposition>();
                GeoboundingBox bestMapViewBox = null;
                if (groupObject.TryGetValue("BestMapViewBox", out bestMapViewBoxValue))
                {
                    foreach (JsonValue itemValue in bestMapViewBoxValue.GetArray())
                    {
                        JsonObject itemObject = itemValue.GetObject();
                        IJsonValue locationValue = itemObject["location"];
                        JsonObject locationObject = locationValue.GetObject();
                        BasicGeoposition location = new BasicGeoposition { Latitude = locationObject["lat"].GetNumber(), Longitude = locationObject["lng"].GetNumber() };
                        bestMapViewBoxList.Add(location);
                    }
                    bestMapViewBox = GeoboundingBox.TryCompute(bestMapViewBoxList);
                }

                SampleDataGroup group = new SampleDataGroup(groupObject["UniqueId"].GetString(),
                                                            groupObject["Title"].GetString(),
                                                            groupObject["Subtitle"].GetString(),
                                                            groupObject["ImagePath"].GetString(),
                                                            groupObject["Description"].GetString(),
                                                            bestMapViewBox,
                                                            shapePath);

                foreach (JsonValue itemValue in groupObject["Items"].GetArray())
                {
                    JsonObject itemObject = itemValue.GetObject();
                    IJsonValue geometryValue;
                    Geopoint location = null;
                    string address = null;
                    if (itemObject.TryGetValue("geometry", out geometryValue))
                    {
                        JsonObject geometryObject = geometryValue.GetObject();
                        IJsonValue locationValue = geometryObject["location"];
                        JsonObject locationObject = locationValue.GetObject();
                        location = new Geopoint(new BasicGeoposition { Latitude = locationObject["lat"].GetNumber(), Longitude = locationObject["lng"].GetNumber() });
                    }

                    IJsonValue addressValue = null;
                    if (itemObject.TryGetValue("formatted_address", out addressValue))
                    {
                        address = addressValue.GetString();
                    }

                    group.Items.Add(new SampleDataItem(itemObject["UniqueId"].GetString(),
                                                       itemObject["Title"].GetString(),
                                                       itemObject["Subtitle"].GetString(),
                                                       itemObject["ImagePath"].GetString(),
                                                       itemObject["Description"].GetString(),
                                                       itemObject["Content"].GetString(),
                                                       location,
                                                       address));
                }
                this.Groups.Add(group);
            }
        }
    }
}
