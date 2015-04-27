using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;


namespace Windows.Devices.Geolocation.Helpers
{


    public enum DistanceMeasure
    {
        Miles,
        Kilometers
    }

    public static class Geohelper
    {
        public const double EarthRadiusInMiles = 3956.0;
        public const double EarthRadiusInKilometers = 6367.0;

        public static double ToRadian(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        public static double ToDegrees(double radians)
        {
            return radians * (180 / Math.PI);
        }

        public static Geopath ToGeopath(this Geocircle geocircle)
        {
            return CreateGeopath(geocircle.Center, geocircle.Radius/1000.0, DistanceMeasure.Kilometers);
        }

        public static Geopath CreateGeopath(BasicGeoposition center, double radius, DistanceMeasure units)
        {
            var earthRadius = (units == DistanceMeasure.Miles ? Geohelper.EarthRadiusInMiles : Geohelper.EarthRadiusInKilometers);
            var lat = ToRadian(center.Latitude); //radians
            var lng = ToRadian(center.Longitude); //radians
            var d = radius / earthRadius; // d = angular distance covered on earth's surface
            var locations = new List<BasicGeoposition>();

            for (var x = 0; x <= 360; x++)
            {
                var brng = ToRadian(x);
                var latRadians = Math.Asin(Math.Sin(lat) * Math.Cos(d) + Math.Cos(lat) * Math.Sin(d) * Math.Cos(brng));
                var lngRadians = lng + Math.Atan2(Math.Sin(brng) * Math.Sin(d) * Math.Cos(lat), Math.Cos(d) - Math.Sin(lat) * Math.Sin(latRadians));

                locations.Add(new BasicGeoposition{Latitude = ToDegrees(latRadians), Longitude = ToDegrees(lngRadians)});
            }

            return new Geopath(locations);
        }

        public static bool CollidesWith(this GeoboundingBox boxA, GeoboundingBox boxB)
        {
            double boxAWidth  = boxA.SoutheastCorner.Longitude - boxA.NorthwestCorner.Longitude;
            double boxBWidth  = boxB.SoutheastCorner.Longitude - boxB.NorthwestCorner.Longitude;
            double boxAHeight = boxA.NorthwestCorner.Latitude  - boxA.SoutheastCorner.Latitude;
            double boxBHeight = boxB.NorthwestCorner.Latitude  - boxB.SoutheastCorner.Latitude;
            
            return  (Math.Abs(boxA.Center.Longitude - boxB.Center.Longitude) < (Math.Abs(boxAWidth  + boxBWidth ) / 2)) 
                 && (Math.Abs(boxA.Center.Latitude  - boxB.Center.Latitude ) < (Math.Abs(boxAHeight + boxBHeight) / 2));
        }
    }
}
