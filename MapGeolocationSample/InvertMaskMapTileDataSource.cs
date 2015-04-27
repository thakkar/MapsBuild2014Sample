using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Helpers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml.Controls.Maps;

namespace MapGeolocationSample
{
    public class InvertMaskMapTileSource : MapTileSource
    {
        public InvertMaskMapTileSource(Geopath path, Color colorIn, Color colorOut) : base()
        {
            Path = path;
            ColorIn = colorIn;
            ColorOut = colorOut;
            var dataSource = new CustomMapTileDataSource();
            dataSource.BitmapRequested += BitmapRequested;
            DataSource = dataSource;

        }

        private PolygonContainsPoint PathCache;
        private Geopath _path;
        private Geopath Path { get { return _path; } set { _path = value; PathBox = GeoboundingBox.TryCompute(_path.Positions); PathCache = new PolygonContainsPoint(_path); } }
        private GeoboundingBox PathBox { get; set; }
        private Color ColorIn { get; set; }

        private Color ColorOut { get; set; }

        private InMemoryRandomAccessStream _outTile;
        private InMemoryRandomAccessStream _inTile;
        private InMemoryRandomAccessStream OutTile
        {
            get
            {
                if (_outTile == null)
                {
                    _outTile = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                    createTile(_outTile, ColorOut);
                }
                return _outTile;
            }
        }
        private InMemoryRandomAccessStream InTile
        {
            get
            {
                if (_inTile == null)
                {
                    _inTile = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                    createTile(_inTile, ColorIn);
                }
                return _inTile;
            }
        }

        void BitmapRequested(CustomMapTileDataSource sender, MapTileBitmapRequestedEventArgs args)
        {
            var deferral = args.Request.GetDeferral();
            IRandomAccessStreamReference referenceStream;
            double lat,lon = 0;
            int pixelX, pixelY = 0;
            Microsoft.MapPoint.TileSystem.TileXYToPixelXY(args.X, args.Y, out pixelX, out pixelY);
            Microsoft.MapPoint.TileSystem.PixelXYToLatLong(pixelX, pixelY, args.ZoomLevel, out lat, out lon);
            BasicGeoposition northWest = new BasicGeoposition{Latitude = lat, Longitude = lon};
            Microsoft.MapPoint.TileSystem.PixelXYToLatLong(pixelX + (int)sizeOfMapTile, pixelY + (int)sizeOfMapTile, args.ZoomLevel, out lat, out lon);
            BasicGeoposition southEast = new BasicGeoposition{Latitude = lat, Longitude = lon};
            GeoboundingBox tileBox = new GeoboundingBox(northWest, southEast);
            if(tileBox.CollidesWith(PathBox))
            {
                if (PathCache.pointInPolygon(northWest.Longitude, northWest.Latitude) &&
                    PathCache.pointInPolygon(southEast.Longitude, southEast.Latitude) &&
                    PathCache.pointInPolygon(northWest.Longitude, southEast.Latitude) &&
                    PathCache.pointInPolygon(southEast.Longitude, northWest.Latitude))
                {
                    referenceStream = RandomAccessStreamReference.CreateFromStream(InTile);
                }
                else
                {
                    var cutter = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                    cutTile(cutter, pixelX, pixelY, args.ZoomLevel);                 
                    referenceStream = RandomAccessStreamReference.CreateFromStream(cutter);
                }
            }
            else
            {
                referenceStream = RandomAccessStreamReference.CreateFromStream(OutTile);
            }

            args.Request.PixelData = referenceStream;
            deferral.Complete();
        }

        private uint sizeOfMapTile = 256;
        //private uint sizeOfPixel = 4;
        private async void createTile(IRandomAccessStream tileStream, Color color)
        {
            

            // Create the data writer object backed by the in-memory stream.
            using (var dataWriter = new Windows.Storage.Streams.DataWriter(tileStream))
            {
                for (uint y = 0; y < sizeOfMapTile; y++)
                {
                    for (uint x = 0; x < sizeOfMapTile; x++)
                    {
                        // RGBA
                        dataWriter.WriteByte(color.R);
                        dataWriter.WriteByte(color.G);
                        dataWriter.WriteByte(color.B);
                        dataWriter.WriteByte(color.A);
                    }
                }

                // Send the contents of the writer to the backing stream.
                await dataWriter.StoreAsync();

                // For the in-memory stream implementation we are using, the flushAsync call 
                // is superfluous,but other types of streams may require it.
                await dataWriter.FlushAsync();

                // In order to prolong the lifetime of the stream, detach it from the 
                // DataWriter so that it will not be closed when Dispose() is called on 
                // dataWriter. Were we to fail to detach the stream, the call to 
                // dataWriter.Dispose() would close the underlying stream, preventing 
                // its subsequent use by the DataReader below.
                dataWriter.DetachStream();
                
            }
        }

        private async void cutTile(IRandomAccessStream stream, int pixeloffsetX, int pixeloffsetY, int levelOfDetail)
        {
            

            // Create the data writer object backed by the in-memory stream.
            using (var dataWriter = new Windows.Storage.Streams.DataWriter(stream))
            {
                //dataWriter.dataWriter.ByteOrder = Windows.Storage.Streams.ByteOrder.LittleEndian;

                for (int y = 0; y < sizeOfMapTile; y++)
                {
                    for (int x = 0; x < sizeOfMapTile; x++)
                    {
                        double lat,lon = 0.0;
                        
                        Microsoft.MapPoint.TileSystem.PixelXYToLatLong(pixeloffsetX + x, pixeloffsetY + y, levelOfDetail, out lat, out lon);
                        BasicGeoposition point = new BasicGeoposition { Latitude = lat, Longitude = lon };

                        if (PathCache.pointInPolygon(point.Longitude,point.Latitude)) 
                        {
                            // RGBA
                            dataWriter.WriteByte(ColorIn.R);
                            dataWriter.WriteByte(ColorIn.G);
                            dataWriter.WriteByte(ColorIn.B);
                            dataWriter.WriteByte(ColorIn.A);
                        }
                        else
                        {
                            // RGBA
                            dataWriter.WriteByte(ColorOut.R);
                            dataWriter.WriteByte(ColorOut.G);
                            dataWriter.WriteByte(ColorOut.B);
                            dataWriter.WriteByte(ColorOut.A);
                        }
                    }
                }

                // Send the contents of the writer to the backing stream.
                await dataWriter.StoreAsync();

                // For the in-memory stream implementation we are using, the flushAsync call 
                // is superfluous,but other types of streams may require it.
                await dataWriter.FlushAsync();

                // In order to prolong the lifetime of the stream, detach it from the 
                // DataWriter so that it will not be closed when Dispose() is called on 
                // dataWriter. Were we to fail to detach the stream, the call to 
                // dataWriter.Dispose() would close the underlying stream, preventing 
                // its subsequent use by the DataReader below.
                dataWriter.DetachStream();
            }
        }
    }
}
