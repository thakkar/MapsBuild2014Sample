using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;


/// http://alienryderflex.com/polygon/
namespace MapGeolocationSample
{
    class PolygonContainsPoint
    {
        public PolygonContainsPoint(Geopath path)
        {
            polySides = path.Positions.Count;
            foreach(BasicGeoposition point in path.Positions)
            {
                polyX.Add(point.Longitude);
                polyY.Add(point.Latitude);
            }
            precalc_values();
        }

        private int polySides = 0;
        private List<double> polyX = new List<double>();
        private List<double> polyY = new List<double>();
        private List<double> constant = new List<double>();
        private List<double> multiple = new List<double>();

        //  Globals which should be set before calling these functions:
        //
        //  int    polySides  =  how many corners the polygon has
        //  float  polyX[]    =  horizontal coordinates of corners
        //  float  polyY[]    =  vertical coordinates of corners
        //  float  x, y       =  point to be tested
        //
        //  The following global arrays should be allocated before calling these functions:
        //
        //  float  constant[] = storage for precalculated constants (same size as polyX)
        //  float  multiple[] = storage for precalculated multipliers (same size as polyX)
        //
        //  (Globals are used in this example for purposes of speed.  Change as
        //  desired.)
        //
        //  USAGE:
        //  Call precalc_values() to initialize the constant[] and multiple[] arrays,
        //  then call pointInPolygon(x, y) to determine if the point is in the polygon.
        //
        //  The function will return YES if the point x,y is inside the polygon, or
        //  NO if it is not.  If the point is exactly on the edge of the polygon,
        //  then the function may return YES or NO.
        //
        //  Note that division by zero is avoided because the division is protected
        //  by the "if" clause which surrounds it.

        private void precalc_values()
        {

            int i, j = polySides - 1;

            for (i = 0; i < polySides; i++)
            {
                if (polyY[j] == polyY[i])
                {
                    //constant[i] = polyX[i];
                    //multiple[i] = 0;
                    constant.Add(polyX[i]);
                    multiple.Add(0);
                }
                else
                {
                    //constant[i] = polyX[i] - (polyY[i] * polyX[j]) / (polyY[j] - polyY[i]) + (polyY[i] * polyX[i]) / (polyY[j] - polyY[i]);
                    //multiple[i] = (polyX[j] - polyX[i]) / (polyY[j] - polyY[i]);
                    constant.Add(polyX[i] - (polyY[i] * polyX[j]) / (polyY[j] - polyY[i]) + (polyY[i] * polyX[i]) / (polyY[j] - polyY[i]));
                    multiple.Add((polyX[j] - polyX[i]) / (polyY[j] - polyY[i]));
                }
                j = i;
            }
        }

        public bool pointInPolygon(double x, double y)
        {

            int i, j = polySides - 1;
            bool oddNodes = false;

            for (i = 0; i < polySides; i++)
            {
                if ((polyY[i] < y && polyY[j] >= y
                || polyY[j] < y && polyY[i] >= y))
                {
                    oddNodes ^= (y * multiple[i] + constant[i] < x);
                }
                j = i;
            }

            return oddNodes;
        } 

    }
}
