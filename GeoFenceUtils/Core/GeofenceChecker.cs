using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoFenceUtils.Core
{
    public class GeofenceChecker
    {
        public bool IsPointInside(Geometry geofence, Point point)
        {
            return geofence.Contains(point);
        }
    }
}
