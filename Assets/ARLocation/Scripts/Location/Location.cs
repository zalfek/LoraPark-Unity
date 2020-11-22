using UnityEngine;
using System;
using UnityEngine.Serialization;

namespace ARLocation
{
    public enum AltitudeMode {
        GroundRelative,
        DeviceRelative,
        Absolute,
        Ignore
    };

    /// <summary>
    /// Represents a geographical location.
    /// </summary>
    [Serializable]
    public class Location
    {
        [FormerlySerializedAs("latitude")] [Tooltip("The latitude, in degrees.")]
        public double Latitude;

        [FormerlySerializedAs("longitude")] [Tooltip("The longitude, in degrees.")]
        public double Longitude;

        [FormerlySerializedAs("altitude")] [Tooltip("The altitude, in meters.")]
        public double Altitude;

        [FormerlySerializedAs("altitudeMode")]
        [Space(4)]

        [Tooltip("The altitude mode. 'Absolute' means absolute altitude, relative to the sea level. 'DeviceRelative' meas it is " +
            "relative to the device's initial position. 'GroundRelative' means relative to the nearest detected plane, and 'Ignore' means the " +
            "altitude is ignored (equivalent to setting it to zero).")]
        public AltitudeMode AltitudeMode = AltitudeMode.GroundRelative;

        [FormerlySerializedAs("label")] [Tooltip("An optional label for the location.")]
        public string Label = "";

        public bool IgnoreAltitude => AltitudeMode == AltitudeMode.Ignore;

        /// <summary>
        /// Gets the horizontal vector.
        /// </summary>
        /// <value>The horizontal vector.</value>
        public DVector2 HorizontalVector => new DVector2(Latitude, Longitude);

        public Location(double latitude = 0.0, double longitude = 0.0, double altitude = 0.0)
        {
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>The clone.</returns>
        public Location Clone()
        {
            return new Location()
            {
                Label = Label,
                Latitude = Latitude,
                Longitude = Longitude,
                Altitude = Altitude,
                AltitudeMode = AltitudeMode
            };
        }

        public override string ToString()
        {
            return "(" + Latitude + ", " + Longitude + ", " + Altitude + ")";
        }

        public DVector3 ToDVector3()
        {
            return new DVector3(Longitude, Altitude, Latitude);
        }

        public Vector3 ToVector3()
        {
            return ToDVector3().toVector3();
        }

        public static DVector3 LocationToEcef(Location l) {
            var rad = Math.PI / 180;

            var lat = l.Latitude * rad;
            var lon = l.Longitude * rad;
            var a = ARLocation.Config.EarthEquatorialRadiusInKM * 1000;
            var e2 = ARLocation.Config.EarthFirstEccentricitySquared;
            var N = a / Math.Sqrt(1 - e2 * Math.Pow(Math.Sin(lat), 2));

            var x = N * Math.Cos(lat) * Math.Cos(lon);
            var y = N * Math.Cos(lat) * Math.Sin(lon);
            var z = (1 - e2) * N * Math.Sin(lat);

            return new DVector3(x, y, z);
        }
    
        public static DVector2 VectorFromToEcefEnu(Location l1, Location l2) {
            var rad = Math.PI / 180;
            var lat = l1.Latitude * rad; 
            var lon = l1.Longitude * rad;
            var p1 = LocationToEcef(l1);
            var p2 = LocationToEcef(l2);
            var delta = p2 - p1;

            var slat = Math.Sin(lat);
            var clat = Math.Cos(lat);
            var slon = Math.Sin(lon);
            var clon = Math.Cos(lon);

            var e = -slon * delta.x + clon * delta.y;
            var n = -clon * slat * delta.x -slat * slon * delta.y+ clat*delta.z;
            
            return new DVector2(n, e);
        }


        /// <summary>
        /// Calculates the horizontal distance according to the current function
        /// set in the configuration.
        /// </summary>
        /// <returns>The distance, in meters.</returns>
        /// <param name="l1">L1.</param>
        /// <param name="l2">L2.</param>
        public static double HorizontalDistance(Location l1, Location l2)
        {
#if ARGPS_CUSTOM_GEO_CALC
            return ArGpsCustomGeoCalc.HorizontalVectorFromTo(l1, l2).magnitude;
#else
            return VectorFromToEcefEnu(l1, l2).magnitude;
#endif
        }

        /// <summary>
        /// Calculates the full distance between locations, taking altitude into account.
        /// </summary>
        /// <returns>The with altitude.</returns>
        /// <param name="l1">L1.</param>
        /// <param name="l2">L2.</param>
        public static double DistanceWithAltitude(Location l1, Location l2)
        {
            var d = HorizontalDistance(l1, l2);
            var h = Math.Abs(l1.Altitude - l2.Altitude);

            return Math.Sqrt(d * d + h * h);
        }

        /// <summary>
        /// Calculates the horizontal vector pointing from l1 to l2, in meters.
        /// </summary>
        /// <returns>The vector from to.</returns>
        /// <param name="l1">L1.</param>
        /// <param name="l2">L2.</param>
        public static DVector2 HorizontalVectorFromTo(Location l1, Location l2)
        {
#if ARGPS_USE_CUSTOM_GEO_CALC
            return ArGpsCustomGeoCalc.HorizontalVectorFromTo(l1, l2);
#else
            return VectorFromToEcefEnu(l1, l2);
#endif
        }

        /// <summary>
        /// Calculates the vector from l1 to l2, in meters, taking altitude into account.
        /// </summary>
        /// <returns>The from to.</returns>
        /// <param name="l1">L1.</param>
        /// <param name="l2">L2.</param>
        /// <param name="ignoreHeight">If true, y = 0 in the output vector.</param>
        public static DVector3 VectorFromTo(Location l1, Location l2, bool ignoreHeight = false)
        {
            var horizontal = HorizontalVectorFromTo(l1, l2);
            var height = l2.Altitude - l1.Altitude;

            return new DVector3(horizontal.y, ignoreHeight ? 0 : height, horizontal.x);
        }

        /// <summary>
        /// Gets the game object world-position for location.
        /// </summary>
        /// <param name="arLocationRoot"></param>
        /// <param name="userPosition"></param>
        /// <param name="userLocation"></param>
        /// <param name="objectLocation"></param>
        /// <param name="heightIsRelative"></param>
        /// <returns></returns>
        public static Vector3 GetGameObjectPositionForLocation(Transform arLocationRoot, Vector3 userPosition, Location userLocation, Location objectLocation, bool heightIsRelative)
        {
            var displacementVector = VectorFromTo(userLocation, objectLocation, objectLocation.IgnoreAltitude || heightIsRelative)
                .toVector3();

            var displacementPosition = arLocationRoot ? arLocationRoot.TransformVector(displacementVector) : displacementVector;

            return userPosition + displacementPosition + new Vector3(0, (heightIsRelative && !objectLocation.IgnoreAltitude) ? ((float)objectLocation.Altitude - userPosition.y) : 0, 0);
        }

        /// <summary>
        /// Gets the game object world-position for location.
        /// </summary>
        /// <returns>The game object position for location.</returns>
        /// <param name="arLocationRoot"></param>
        /// <param name="user">User.</param>
        /// <param name="userLocation">User location.</param>
        /// <param name="objectLocation">Object location.</param>
        /// <param name="heightIsRelative">If set to <c>true</c> height is relative.</param>
        ///
        public static Vector3 GetGameObjectPositionForLocation(Transform arLocationRoot, Transform user, Location userLocation, Location objectLocation, bool heightIsRelative)
        {
            return GetGameObjectPositionForLocation(arLocationRoot, user.position, userLocation, objectLocation,
                heightIsRelative);
        }

        /// <summary>
        /// Places the game object at location.
        /// </summary>
        /// <param name="arLocationRoot"></param>
        /// <param name="transform">The GameObject's transform.</param>
        /// <param name="user">The user's point of view Transform, e.g., camera.</param>
        /// <param name="userLocation">User Location.</param>
        /// <param name="objectLocation">Object Location.</param>
        /// <param name="heightIsRelative"></param>
        public static void PlaceGameObjectAtLocation(Transform arLocationRoot, Transform transform, Transform user, Location userLocation, Location objectLocation, bool heightIsRelative)
        {
            transform.position = GetGameObjectPositionForLocation(arLocationRoot, user, userLocation, objectLocation, heightIsRelative);
        }

        public static bool Equal(Location a, Location b, double eps = 0.0000001)
        {
            return (Math.Abs(a.Latitude - b.Latitude) <= eps) &&
                (Math.Abs(a.Longitude - b.Longitude) <= eps) &&
                (Math.Abs(a.Altitude - b.Altitude) <= eps);
        }
    }
}
