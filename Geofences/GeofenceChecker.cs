using NetTopologySuite.Geometries;

namespace Geofences;

public class GeofenceChecker
{
    private readonly GeometryFactory _geometryFactory = new();
    private const double MetersPerDegree = 111_000;

    public Point CreatePoint(double longitude, double latitude)
    {
        return _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
    }

    public Geometry CreatePolygon(Coordinate[] coordinates)
    {
        if (coordinates == null || coordinates.Length < 4)
        {
            throw new ArgumentException("A polygon requires at least 4 coordinates (including the closing coordinate).");
        }
        return _geometryFactory.CreatePolygon(coordinates);
    }

    public Geometry CreateCircularGeofence(double longitude, double latitude, double radiusInMeters)
    {
        var radiusInDegrees = radiusInMeters / MetersPerDegree;

        var centerPoint = CreatePoint(longitude, latitude);
        return (Polygon)centerPoint.Buffer(radiusInDegrees);
    }

    public Geometry CreateBufferedLineGeofence(Coordinate[] lineCoordinates, double bufferWidthInMeters)
    {
        if (lineCoordinates == null || lineCoordinates.Length < 2)
        {
            throw new ArgumentException("A line requires at least two coordinates.");
        }

        var bufferWidthInDegrees = bufferWidthInMeters / MetersPerDegree;

        var line = _geometryFactory.CreateLineString(lineCoordinates);
        return (Polygon)line.Buffer(bufferWidthInDegrees);
    }

    public bool IsPointInside(Geometry geofence, Point point)
    {
        return geofence.Contains(point);
    }

    public Geometry ParseGeofence(string geofenceString)
    {
        // Example:
        // POLYGON=(48.85 2.347, 48.85 2.34, 48.34 2.324, 48.231 2.231, 48.123 2.2313)
        // ROUTE = (31.87 54.35, 31.87 54.34)
        // CIRCULAR = (31.87 54.35 200)

        var parts = geofenceString.Split('=', 2); // Split into "type" and "data"
        if (parts.Length != 2)
        {
            throw new ArgumentException("Invalid geofence string format.");
        }

        var type = parts[0].Trim().ToUpper();
        var data = parts[1].Trim();

        return type switch
        {
            "POLYGON" => ParsePolygon(data),
            "ROUTE" => ParseRoute(data),
            "CIRCULAR" => ParseCircular(data),
            _ => throw new ArgumentException($"Unsupported geofence type: {type}"),
        };
    }

    private Geometry ParsePolygon(string polygonString)
    {
        var coordinates = polygonString
            .Trim('(', ')')
            .Split(',')
            .Select(coordinates =>
            {
                var parts = coordinates.Trim().Split(' ');
                return new Coordinate(double.Parse(parts[1]), double.Parse(parts[0]));
            })
            .ToArray();

        if (coordinates.Length < 3 || !coordinates.First().Equals(coordinates.Last()))
        {
            // Ensure the polygon is closed
            coordinates = coordinates.Append(coordinates.First()).ToArray();
        }

        return CreatePolygon(coordinates);
    }

    private Geometry ParseRoute(string routeString)
    {
        var coordinates = routeString
            .Trim('(', ')')
            .Split(',')
            .Select(coordinates =>
            {
                var parts = coordinates.Trim().Split(' ');
                return new Coordinate(double.Parse(parts[1]), double.Parse(parts[0]));
            })
            .ToArray();

        return CreateBufferedLineGeofence(coordinates, 100); // Example buffer width in meters
    }

    private Geometry ParseCircular(string circularString)
    {
        var parts = circularString.Trim('(', ')').Split(' ');

        if (parts.Length != 3)
        {
            throw new ArgumentException("CIRCULAR geofence string must have exactly 3 values: latitude, longitude, and radius.");
        }

        var latitude = double.Parse(parts[0]);
        var longitude = double.Parse(parts[1]);
        var radius = double.Parse(parts[2]);

        return CreateCircularGeofence(longitude, latitude, radius);
    }
}