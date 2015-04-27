using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace MapsSampleData.Helpers
{
    public static class PathParser
    {
        public const string safeCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";

        public static bool TryParseEncodedValue(string value, out List<BasicGeoposition> parsedValue)
        {
            parsedValue = null;
            var list = new List<BasicGeoposition>();
            int index = 0;
            int xsum = 0, ysum = 0;

            while (index < value.Length)        // While we have more data,
            {
                long n = 0;                     // initialize the accumulator
                int k = 0;                      // initialize the count of bits

                while (true)
                {
                    if (index >= value.Length)  // If we ran out of data mid-number
                        return false;           // indicate failure.

                    int b = safeCharacters.IndexOf(value[index++]);

                    if (b == -1)                // If the character wasn't on the valid list,
                        return false;           // indicate failure.

                    n |= ((long)b & 31) << k;   // mask off the top bit and append the rest to the accumulator
                    k += 5;                     // move to the next position
                    if (b < 32) break;          // If the top bit was not set, we're done with this number.
                }

                // The resulting number encodes an x, y pair in the following way:
                //
                //  ^ Y
                //  |
                //  14
                //  9 13
                //  5 8 12
                //  2 4 7 11
                //  0 1 3 6 10 ---> X

                // determine which diagonal it's on
                int diagonal = (int)((Math.Sqrt(8 * n + 5) - 1) / 2);

                // subtract the total number of points from lower diagonals
                n -= diagonal * (diagonal + 1L) / 2;

                // get the X and Y from what's left over
                int ny = (int)n;
                int nx = diagonal - ny;

                // undo the sign encoding
                nx = (nx >> 1) ^ -(nx & 1);
                ny = (ny >> 1) ^ -(ny & 1);

                // undo the delta encoding
                xsum += nx;
                ysum += ny;

                // position the decimal point
                list.Add(new BasicGeoposition { Latitude = ysum * 0.00001, Longitude = xsum * 0.00001 });
            }

            parsedValue = list;
            return true;
        }
    }
}
