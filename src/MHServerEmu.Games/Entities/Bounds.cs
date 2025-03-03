using System.Runtime.InteropServices;
using System.Text;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities
{
    public enum GeometryType
    {
        None,
        OBB,
        AABB,
        Capsule,
        Sphere,
        Triangle,
        Wedge
    }

    public enum BoundsFlags
    {
        None = 0,
        ComplexPickingOnly = 1,
    }

    [Flags]
    public enum BlockingCheckFlags
    {
        None                        = 0,
        CheckSpawns                 = 1 << 0,
        CheckGroundMovementPowers   = 1 << 1,
        CheckAllMovementPowers      = 1 << 2,
        CheckLanding                = 1 << 3,
        CheckSelf                   = 1 << 4, // ??
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct BoundData
    {
        [FieldOffset(0)]
        public float OBBHalfWidth = 0f;
        [FieldOffset(4)]
        public float OBBHalfLength = 0f;
        [FieldOffset(8)]
        public float OBBHalfHeight = 0f;

        [FieldOffset(0)]
        public float AABBHalfWidth = 0f;
        [FieldOffset(4)]
        public float AABBHalfLength = 0f;
        [FieldOffset(8)]
        public float AABBHalfHeight = 0f;

        [FieldOffset(12)]
        public float AABBOrientedWidth = 0f;
        [FieldOffset(16)]
        public float AABBOrientedLength = 0f;
        [FieldOffset(20)]
        public float AABBOrientedHeight = 0f;

        [FieldOffset(0)]
        public float CapsuleRadius = 0f;
        [FieldOffset(4)]
        public float CapsuleHalfHeight = 0f;

        [FieldOffset(0)]
        public float SphereRadius = 0f;
        [FieldOffset(0)]
        public float TriangleBase = 0f;
        [FieldOffset(4)]
        public float TriangleHalfHeight = 0f;
        [FieldOffset(8)]
        public float TriangleLength = 0f;

        [FieldOffset(0)]
        public float WedgeBaseWidth = 0f;
        [FieldOffset(4)]
        public float WedgeBase = 0f;
        [FieldOffset(8)]
        public float WedgeHalfHeight = 0f;
        [FieldOffset(12)]
        public float WedgeLength = 0f;
        public BoundData() { }
    }

    public class Bounds
    {
        public static readonly Logger Logger = LogManager.CreateLogger();

        public GeometryType Geometry { get; private set; }
        public BoundsCollisionType CollisionType { get; set; }
        public Vector3 Center { get; set; }
        public Orientation Orientation { get => _orientation; set => SetOrientation(value); }
        public float Radius { get => GetRadius(); set => SetRadius(value); }
        public float HalfHeight { get => GetHalfHeight(); }
        public BoundsFlags Flags { get; private set; }
        public float EyeHeight { get => HalfHeight * 0.8333f; }

        private BoundData _params = new();
        private Orientation _orientation;
        private Orientation _orientation_offset;

        public Bounds()
        {
            Center = Vector3.Zero;
            _orientation = Orientation.Zero;
            _orientation_offset = Orientation.Zero;
            CollisionType = BoundsCollisionType.None;
            Flags = BoundsFlags.None;
        }

        public Bounds(Bounds bounds)
        {
            Geometry = bounds.Geometry;
            Center = bounds.Center;
            _orientation = bounds._orientation;
            _orientation_offset = bounds._orientation_offset;
            CollisionType = bounds.CollisionType;
            _params = bounds._params;
            Flags = bounds.Flags;
        }

        public Bounds(BoundsPrototype boundsProto, Vector3 position)
        {
            InitializeFromPrototype(boundsProto); 
            Center = position;
        }

        public void InitializeFromPrototype(BoundsPrototype boundsProto)
        {
            if (boundsProto == null)
            {
                Logger.Warn("Anything trying to initialize Bounds should have a valid bounds prototype!");
                Geometry = GeometryType.None;
                CollisionType = BoundsCollisionType.None;
                return;
            }

            Geometry = boundsProto.GetGeometryType();
            BoundsFlags flags = boundsProto.ComplexPickingOnly ? BoundsFlags.ComplexPickingOnly : BoundsFlags.None;

            switch (Geometry)
            {
                case GeometryType.OBB:
                case GeometryType.AABB:  
                    
                    if (boundsProto is BoxBoundsPrototype boxBoundsProto)
                        InitializeBox(boxBoundsProto.Width,
                                        boxBoundsProto.Length,
                                        boxBoundsProto.Height,
                                        boxBoundsProto.AxisAligned,
                                        boxBoundsProto.CollisionType,
                                        flags);

                    break;
                    
                case GeometryType.Capsule:
                    
                    if (boundsProto is CapsuleBoundsPrototype capsuleBoundsProto)
                        InitializeCapsule(capsuleBoundsProto.Radius,
                                            capsuleBoundsProto.HeightFromCenter,
                                            capsuleBoundsProto.CollisionType,
                                            flags);

                    break;
                    
                case GeometryType.Sphere:    
                    
                    if (boundsProto is SphereBoundsPrototype sphereBoundsProto)
                        InitializeSphere(sphereBoundsProto.Radius, sphereBoundsProto.CollisionType, flags);

                    break;
                    
                case GeometryType.Triangle:

                    if (boundsProto is TriangleBoundsPrototype triangleBoundsProto)
                        InitializeIsocelesTriangle(triangleBoundsProto.AngleDegrees,
                                                    triangleBoundsProto.HeightFromCenter,
                                                    triangleBoundsProto.Length,
                                                    triangleBoundsProto.CollisionType,
                                                    flags);

                    break;  

                case GeometryType.Wedge:

                    if (boundsProto is WedgeBoundsPrototype wedgeBoundsProto)
                        InitializeWedge(wedgeBoundsProto.AngleDegrees,
                                            wedgeBoundsProto.HeightFromCenter,
                                            wedgeBoundsProto.Length,
                                            wedgeBoundsProto.BaseWidth,
                                            wedgeBoundsProto.CollisionType,
                                            flags);

                    break;

                case GeometryType.None:

                    CollisionType = boundsProto.CollisionType;
                    Flags = flags;
                    break;

                default:

                    CollisionType = BoundsCollisionType.None;
                    Geometry = GeometryType.None;
                    Flags = flags;
                    break;

            }
        }

        public void Scale(float scaleMult)
        {
            switch (Geometry)
            {
                case GeometryType.OBB:
                    _params.OBBHalfWidth *= scaleMult;
                    _params.OBBHalfLength *= scaleMult;
                    _params.OBBHalfHeight *= scaleMult;
                    break;

                case GeometryType.AABB:
                    _params.AABBHalfWidth *= scaleMult;
                    _params.AABBHalfLength *= scaleMult;
                    _params.AABBHalfHeight *= scaleMult;
                    UpdateAABBGeometry();
                    break;

                case GeometryType.Capsule:
                    _params.CapsuleRadius *= scaleMult;
                    break;

                case GeometryType.Sphere:
                    _params.SphereRadius *= scaleMult;
                    break;

                case GeometryType.Triangle:
                    float angleRad = MathF.Atan((_params.TriangleBase * 0.5f) / _params.TriangleLength);
                    _params.TriangleLength *= scaleMult;
                    _params.TriangleBase = MathF.Tan(angleRad) * 2.0f * _params.TriangleLength;
                    break;

                case GeometryType.Wedge:
                    angleRad = MathF.Atan((_params.WedgeBase * 0.5f) / _params.WedgeLength);
                    _params.WedgeLength *= scaleMult;
                    _params.WedgeBase = MathF.Tan(angleRad) * 2.0f * _params.WedgeLength;
                    _params.WedgeBaseWidth *= scaleMult;
                    break;
            }
        }

        public void InitializeWedge(float angleDegrees, float heightFromCenter, float length, float baseWidth, BoundsCollisionType collisionType, BoundsFlags flags)
        {
            Geometry = GeometryType.Wedge;
            _params.WedgeBaseWidth = baseWidth;
            _params.WedgeBase = 2 * length * MathF.Tan(MathHelper.ToRadians(angleDegrees * 0.5f));
            _params.WedgeHalfHeight = heightFromCenter;
            _params.WedgeLength = length;
            CollisionType = collisionType;
            Flags = flags;
        }

        public void InitializeIsocelesTriangle(float angleDegrees, float heightFromCenter, float length, BoundsCollisionType collisionType, BoundsFlags flags)
        {
            Geometry = GeometryType.Triangle;
            _params.TriangleBase = 2 * length * MathF.Tan(MathHelper.ToRadians(angleDegrees * 0.5f));
            _params.TriangleHalfHeight = heightFromCenter;
            _params.TriangleLength = length;
            CollisionType = collisionType;
            Flags = flags;
        }

        public void InitializeSphere(float radius, BoundsCollisionType collisionType, BoundsFlags flags = BoundsFlags.None)
        {
            Geometry = GeometryType.Sphere;
            _params.SphereRadius = radius;
            CollisionType = collisionType;
            Flags = flags;
        }

        public void InitializeCapsule(float radius, float heightFromCenter, BoundsCollisionType collisionType, BoundsFlags flags)
        {
            Geometry = GeometryType.Capsule;
            _params.CapsuleRadius = radius;
            _params.CapsuleHalfHeight = heightFromCenter;
            CollisionType = collisionType;
            Flags = flags;
        }

        public void InitializeBox(float width, float length, float height, bool axisAligned, BoundsCollisionType collisionType, BoundsFlags flags = BoundsFlags.None)
        {
            if (axisAligned)
            {
                Geometry = GeometryType.AABB;
                _params.AABBHalfHeight = height * 0.5f;
                _params.AABBHalfLength = length * 0.5f;
                _params.AABBHalfWidth = width * 0.5f;
                UpdateAABBGeometry();
            }
            else
            {
                Geometry = GeometryType.OBB;
                _params.OBBHalfHeight = height * 0.5f;
                _params.OBBHalfLength = length * 0.5f;
                _params.OBBHalfWidth = width * 0.5f;
            }
            CollisionType = collisionType;
            Flags = flags;
        }

        private void UpdateAABBGeometry()
        {
            Matrix3 mat = Matrix3.AbsPerElem(_orientation.GetMatrix3());
            Vector3 oriented = mat * new Vector3(_params.AABBHalfWidth, _params.AABBHalfHeight, _params.AABBHalfLength);
            _params.AABBOrientedWidth = oriented[0];
            _params.AABBOrientedHeight = oriented[1];
            _params.AABBOrientedLength = oriented[2];
        }

        private void SetRadius(float radius)
        {
            switch (Geometry)
            {
                case GeometryType.Capsule:
                    _params.CapsuleRadius = radius;
                    break;

                case GeometryType.Sphere:
                    _params.SphereRadius = radius;
                    break;

                default:
                    Logger.Error($"Can't set the radius of a {Geometry}.");
                    break;
            }
        }

        public float GetRadius()
        {
            switch (Geometry)
            {
                case GeometryType.OBB:
                    return Vector3.LengthTest(new (_params.OBBHalfWidth, _params.OBBHalfLength, 0.0f));
                case GeometryType.AABB:
                    return Vector3.LengthTest(new (_params.AABBOrientedWidth, _params.AABBOrientedLength, 0.0f));
                case GeometryType.Capsule:
                    return _params.CapsuleRadius;
                case GeometryType.Sphere:
                    return _params.SphereRadius;
                case GeometryType.Triangle:
                    Triangle triangle = ToTriangle2D();
                    float max = MathF.Max(
                        MathF.Max(Vector3.Length(triangle[0] - Center), Vector3.Length(triangle[1] - Center)),
                        Vector3.Length(triangle[2] - Center));
                    return max;
                case GeometryType.Wedge:
                    Vector3[] wedgeVertices = GetWedgeVertices();
                    max = Math.Max(
                        Vector3.DistanceSquared2D(wedgeVertices[1], Center),
                        Vector3.DistanceSquared2D(wedgeVertices[2], Center));
                    return MathHelper.SquareRoot(max);
                default:
                    return 0.0f;
            }
        }

        private float GetSphereRadius()
        {
            switch (Geometry)
            {
                case GeometryType.OBB:
                    return Vector3.LengthTest(new(_params.OBBHalfWidth, _params.OBBHalfLength, _params.OBBHalfHeight));
                case GeometryType.AABB:
                    return Vector3.LengthTest(new(_params.AABBOrientedWidth, _params.AABBOrientedLength, _params.AABBOrientedHeight));
                case GeometryType.Capsule:
                    return _params.CapsuleHalfHeight + _params.CapsuleRadius;
                case GeometryType.Sphere:
                    return _params.SphereRadius;
                case GeometryType.Triangle:
                    float posZ = Center.Z + _params.TriangleHalfHeight;
                    Triangle triangle = ToTriangle2D();
                    triangle.Points[0].Z = triangle.Points[1].Z = triangle.Points[2].Z = posZ;
                    return MathF.Max( MathF.Max(
                        Vector3.Length(triangle[0] - Center), 
                        Vector3.Length(triangle[1] - Center)),
                        Vector3.Length(triangle[2] - Center)); 
                case GeometryType.Wedge:
                    Vector3[] wedgeVertices = GetWedgeVertices();
                    Vector3 heightPoint = new(0.0f, 0.0f, _params.WedgeHalfHeight);
                    float max = Math.Max(
                        Vector3.DistanceSquared2D(wedgeVertices[1] + heightPoint, Center),
                        Vector3.DistanceSquared2D(wedgeVertices[2] + heightPoint, Center));
                    return MathHelper.SquareRoot(max);
                default:
                    return 0.0f;
            }
        }

        public float GetCenterOffset()
        {
            return Geometry switch
            {
                GeometryType.Triangle => _params.TriangleLength * 0.66666669f,
                GeometryType.Wedge => _params.WedgeLength * 0.66666669f,
                _ => 0.0f
            };
        }

        private Vector3[] GetWedgeVertices()
        {
            if (Geometry != GeometryType.Wedge)
                return new Vector3[]
                {
                    Vector3.Zero,
                    Vector3.Zero,
                    Vector3.Zero,
                    Vector3.Zero
                };
            Transform3 transform = Transform3.BuildTransform(Center, _orientation);
            return new Vector3[]
            {
                new(transform * new Point3(_params.WedgeLength * -0.66666669f, _params.WedgeBaseWidth * -0.5f, 0.0f)),
                new(transform * new Point3(_params.WedgeLength * -0.66666669f, _params.WedgeBaseWidth * 0.5f, 0.0f)),
                new(transform * new Point3(_params.WedgeLength * 0.33333334f, _params.WedgeBase * 0.5f, 0.0f)),
                new(transform * new Point3(_params.WedgeLength * 0.33333334f, _params.WedgeBase * -0.5f, 0.0f))
            };
        }

        private Triangle ToTriangle2D()
        {
            if (Geometry != GeometryType.Triangle) return Triangle.Zero;
            Transform3 transform = Transform3.BuildTransform(Center, _orientation);
            return new Triangle (
                new(transform * new Point3(_params.TriangleLength * -0.66666669f, 0.0f, 0.0f)),
                new(transform * new Point3(_params.TriangleLength * 0.33333334f, _params.TriangleBase * -0.5f, 0.0f)),
                new(transform * new Point3(_params.TriangleLength * 0.33333334f, _params.TriangleBase * 0.5f, 0.0f))
                );           
        }

        private float GetHalfHeight()
        {
            return Geometry switch
            {
                GeometryType.OBB => _params.OBBHalfHeight,
                GeometryType.AABB => _params.AABBHalfHeight,
                GeometryType.Capsule => _params.CapsuleHalfHeight,
                GeometryType.Sphere => _params.SphereRadius,
                GeometryType.Triangle => _params.TriangleHalfHeight,
                GeometryType.Wedge => _params.WedgeHalfHeight,
                _ => 0.0f,
            };
        }

        private void SetOrientation(Orientation orientation)
        {
            _orientation = orientation + _orientation_offset;
            if (Geometry == GeometryType.AABB) UpdateAABBGeometry();
        }

        public Aabb ToAabb()
        {            
            switch (Geometry)
            {
                case GeometryType.OBB:
                    Matrix3 mat = Matrix3.AbsPerElem(_orientation.GetMatrix3());
                    Vector3 oobVector = mat * new Vector3(_params.OBBHalfWidth, _params.OBBHalfLength, _params.OBBHalfHeight);
                    return new(Center - oobVector, Center + oobVector);

                case GeometryType.AABB:
                    Vector3 aabbVector = new(_params.AABBOrientedWidth, _params.AABBOrientedLength, _params.AABBOrientedHeight);
                    return new(Center - aabbVector, Center + aabbVector);

                case GeometryType.Capsule:
                    Vector3 min = new(Center.X - _params.CapsuleRadius, Center.Y - _params.CapsuleRadius, Center.Z - _params.CapsuleHalfHeight);
                    Vector3 max = new(Center.X + _params.CapsuleRadius, Center.Y + _params.CapsuleRadius, Center.Z + _params.CapsuleHalfHeight);
                    return new(min, max);

                case GeometryType.Sphere:
                    min = new(Center.X - _params.SphereRadius, Center.Y - _params.SphereRadius, Center.Z - _params.SphereRadius);
                    max = new(Center.X + _params.SphereRadius, Center.Y + _params.SphereRadius, Center.Z + _params.SphereRadius);
                    return new(min, max);

                case GeometryType.Triangle:
                    var triangle = ToTriangle2D();
                    min = new(Math.Min(Math.Min(triangle[0][0], triangle[1][0]), triangle[2][0]),
                              Math.Min(Math.Min(triangle[0][1], triangle[1][1]), triangle[2][1]),
                              Center[2] - GetHalfHeight());
                    max = new(Math.Max(Math.Max(triangle[0][0], triangle[1][0]), triangle[2][0]),
                              Math.Max(Math.Max(triangle[0][1], triangle[1][1]), triangle[2][1]),
                              Center[2] + GetHalfHeight());
                    return new(min, max);

                case GeometryType.Wedge:
                    var wedgeVertices = GetWedgeVertices();
                    min = new(
                        Math.Min(Math.Min(Math.Min(wedgeVertices[0].X, wedgeVertices[1].X), wedgeVertices[2].X), wedgeVertices[3].X),
                        Math.Min(Math.Min(Math.Min(wedgeVertices[0].Y, wedgeVertices[1].Y), wedgeVertices[2].Y), wedgeVertices[3].Y),
                        Center[2] - GetHalfHeight());

                    max = new(
                        Math.Max(Math.Max(Math.Max(wedgeVertices[0].X, wedgeVertices[1].X), wedgeVertices[2].X), wedgeVertices[3].X),
                        Math.Max(Math.Max(Math.Max(wedgeVertices[0].Y, wedgeVertices[1].Y), wedgeVertices[2].Y), wedgeVertices[3].Y),
                        Center[2] + GetHalfHeight());

                    return new(min, max);

                default:
                    return Aabb.Zero;
            }            
        }

        public bool CanBeBlockedBy(Bounds entityBounds, bool selfBlocking = false, bool otherBlocking = false)
        {
            return (CollisionType == BoundsCollisionType.Blocking || selfBlocking)
                && (entityBounds.CollisionType == BoundsCollisionType.Blocking || otherBlocking);
        }

        public bool Contains(in Vector3 point)
        {
            switch (Geometry)
            {
                case GeometryType.OBB:
                    return ToObb().Contains(point) == ContainmentType.Contains;
                case GeometryType.AABB:
                    return ToAabb().Contains(point) == ContainmentType.Contains;
                case GeometryType.Capsule:
                    return ToCapsule().Contains(point);
                case GeometryType.Sphere:
                    return ToSphere().Contains(point) == ContainmentType.Contains;
                case GeometryType.Triangle:
                    return ToTriangle2D().Intersects(point);
                case GeometryType.Wedge:
                    Triangle[] triangles = GetWedgeTriangles();
                    return triangles[0].Intersects(point) || triangles[1].Intersects(point);
                default:
                    Logger.Warn($"Unknown bounds geometry. Geometry={Geometry}");
                    return false;
            }
        }

        public bool Intersects(Bounds other)
        {
            switch (other.Geometry)
            {
                case GeometryType.OBB:
                    return Intersects(other.ToObb());
                case GeometryType.AABB:
                    return Intersects(other.ToAabb());
                case GeometryType.Capsule:
                    return Intersects(other.ToCapsule());
                case GeometryType.Sphere:
                    return Intersects(other.ToSphere());
                case GeometryType.Triangle:
                    return Intersects(other.ToTriangle2D());
                case GeometryType.Wedge:
                    Triangle[] triangles = other.GetWedgeTriangles();
                    return Intersects(triangles[0]) || Intersects(triangles[1]);
                default:
                    Logger.Warn($"Unknown bounds geometry. Geometry={Geometry}, other.Geometry={other.Geometry}");
                    return false;
            }
        }

        // Fast way to make copies of Intersects than a confusing interface or slow dynamic
        public bool Intersects(in Obb bounds)
        {
            switch (Geometry)
            {
                case GeometryType.OBB: return ToObb().Intersects(bounds);
                case GeometryType.AABB: return ToAabb().Intersects(bounds);
                case GeometryType.Capsule: return ToCapsule().Intersects(bounds);
                case GeometryType.Sphere: return ToSphere().Intersects(bounds);
                case GeometryType.Triangle: return ToTriangle2D().Intersects(bounds);
                case GeometryType.Wedge:
                    Triangle[] triangles = GetWedgeTriangles();
                    return triangles[0].Intersects(bounds) || triangles[1].Intersects(bounds);
                default:
                    Logger.Warn($"Unknown bounds geometry. Geometry={Geometry}");
                    return false;
            }
        }

        public bool Intersects(in Aabb bounds)
        {
            switch (Geometry)
            {
                case GeometryType.OBB: return ToObb().Intersects(bounds);
                case GeometryType.AABB: return ToAabb().Intersects(bounds);
                case GeometryType.Capsule: return ToCapsule().Intersects(bounds);
                case GeometryType.Sphere: return ToSphere().Intersects(bounds);
                case GeometryType.Triangle: return ToTriangle2D().Intersects(bounds);
                case GeometryType.Wedge:
                    Triangle[] triangles = GetWedgeTriangles();
                    return triangles[0].Intersects(bounds) || triangles[1].Intersects(bounds);
                default:
                    Logger.Warn($"Unknown bounds geometry. Geometry={Geometry}");
                    return false;
            }
        }

        public bool Intersects(in Capsule bounds)
        {
            switch (Geometry)
            {
                case GeometryType.OBB: return ToObb().Intersects(bounds);
                case GeometryType.AABB: return ToAabb().Intersects(bounds);
                case GeometryType.Capsule: return ToCapsule().Intersects(bounds);
                case GeometryType.Sphere: return ToSphere().Intersects(bounds);
                case GeometryType.Triangle: return ToTriangle2D().Intersects(bounds);
                case GeometryType.Wedge:
                    Triangle[] triangles = GetWedgeTriangles();
                    return triangles[0].Intersects(bounds) || triangles[1].Intersects(bounds);
                default:
                    Logger.Warn($"Unknown bounds geometry. Geometry={Geometry}");
                    return false;
            }
        }

        public bool Intersects(in Sphere bounds)
        {
            switch (Geometry)
            {
                case GeometryType.OBB: return ToObb().Intersects(bounds);
                case GeometryType.AABB: return ToAabb().Intersects(bounds);
                case GeometryType.Capsule: return ToCapsule().Intersects(bounds);
                case GeometryType.Sphere: return ToSphere().Intersects(bounds);
                case GeometryType.Triangle: return ToTriangle2D().Intersects(bounds);
                case GeometryType.Wedge:
                    Triangle[] triangles = GetWedgeTriangles();
                    return triangles[0].Intersects(bounds) || triangles[1].Intersects(bounds);
                default:
                    Logger.Warn($"Unknown bounds geometry. Geometry={Geometry}");
                    return false;
            }
        }

        public bool Intersects(in Triangle bounds)
        {
            switch (Geometry)
            {
                case GeometryType.OBB: return ToObb().Intersects(bounds);
                case GeometryType.AABB: return ToAabb().Intersects(bounds);
                case GeometryType.Capsule: return ToCapsule().Intersects(bounds);
                case GeometryType.Sphere: return ToSphere().Intersects(bounds);
                case GeometryType.Triangle: return ToTriangle2D().Intersects(bounds);
                case GeometryType.Wedge:
                    Triangle[] triangles = GetWedgeTriangles();
                    return triangles[0].Intersects(bounds) || triangles[1].Intersects(bounds);
                default:
                    Logger.Warn($"Unknown bounds geometry. Geometry={Geometry}");
                    return false;
            }
        }

        private Triangle[] GetWedgeTriangles()
        {
            if (Geometry != GeometryType.Wedge)
                return new Triangle[]
                {
                    Triangle.Zero,
                    Triangle.Zero
                };

            Vector3[] wedgeVertices = GetWedgeVertices();
            return new Triangle[]
            {
                new Triangle(wedgeVertices[0], wedgeVertices[1], wedgeVertices[2]),
                new Triangle(wedgeVertices[2], wedgeVertices[3], wedgeVertices[0])
            };
        }

        public bool Intersects(Segment segment, ref float intersection)
        {
            switch (Geometry)
            {
                case GeometryType.OBB: return ToObb().Intersects(segment, ref intersection);
                case GeometryType.AABB: return ToAabb().Intersects(segment, ref intersection);
                case GeometryType.Capsule: return ToCapsule().Intersects(segment, ref intersection);
                case GeometryType.Sphere: return ToSphere().Intersects(segment, ref intersection);
                default: return Logger.WarnReturn(false, $"Segment intersect not implemented for bounds geometry={Geometry}"); ;
            }
        }

        private Sphere ToSphere()
        {
            if (Geometry == GeometryType.Sphere)
                return new Sphere(Center, _params.SphereRadius);
            else
                return Sphere.Zero;
        }

        private Capsule ToCapsule()
        {
            if (Geometry == GeometryType.Capsule)
                return new Capsule(
                    new Vector3(Center.X, Center.Y, Center.Z - _params.CapsuleHalfHeight),
                    new Vector3(Center.X, Center.Y, Center.Z + _params.CapsuleHalfHeight),
                    _params.CapsuleRadius
                );
            else
                return Capsule.Zero;
        }

        private Obb ToObb()
        {
            return new(Center, GetBoxExtents(), Orientation);
        }

        private Vector3 GetBoxExtents()
        {
            if (Geometry == GeometryType.OBB)
                return new Vector3(_params.OBBHalfWidth, _params.OBBHalfLength, _params.OBBHalfHeight);
            if (Geometry == GeometryType.AABB)
                return new Vector3(_params.AABBOrientedWidth, _params.AABBOrientedLength, _params.AABBOrientedHeight);

            return Vector3.Zero;  
        }
        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Bounds: [{Geometry}]");
            switch (Geometry)
            {
                case GeometryType.OBB: sb.Append(ToObb().ToString()); break;
                case GeometryType.AABB: sb.Append(ToAabb().BoxToString()); break;
                case GeometryType.Capsule: sb.Append(ToCapsule().ToString()); break;
                case GeometryType.Sphere: sb.Append(ToSphere().ToString()); break;
                case GeometryType.Triangle: sb.Append(ToTriangle2D().ToString()); break;
                case GeometryType.Wedge:
                    Triangle[] triangles = GetWedgeTriangles();
                    sb.Append(triangles[0].ToString());
                    sb.AppendLine(triangles[1].ToString());
                    break;
            }
            return sb.ToString();
        }

        public bool Sweep(Bounds other, Vector3 otherVelocity, Vector3 velocity, ref float resultTime, ref Vector3? resultNormal)
        {
            if (Geometry == GeometryType.Sphere && other.Geometry == GeometryType.Sphere)
            {
                Sphere sphere = ToSphere();
                Sphere otherSphere = other.ToSphere();
                bool result = sphere.Sweep(otherSphere, otherVelocity, velocity, ref resultTime);
                if (result && resultNormal != null)
                {
                    Vector3 position = Center + velocity * resultTime;
                    Vector3 otherPosition = other.Center + otherVelocity * resultTime;
                    resultNormal = Vector3.SafeNormalize2D(position - otherPosition, Vector3.ZAxis);
                }
                return result;
            }
            else if (Geometry == GeometryType.OBB && Vector3.IsNearZero(velocity))
                return SweepVsStationaryOBB(other, otherVelocity, ToObb(), ref resultTime, resultNormal);
            else if (other.Geometry == GeometryType.OBB && Vector3.IsNearZero(otherVelocity))
                return SweepVsStationaryOBB(this, velocity, other.ToObb(), ref resultTime, resultNormal);
            else if (Geometry == GeometryType.AABB && Vector3.IsNearZero(velocity))
                return SweepVsStationaryAABB(other, otherVelocity, ToAabb(), ref resultTime, resultNormal);
            else if (other.Geometry == GeometryType.AABB && Vector3.IsNearZero(otherVelocity))
                return SweepVsStationaryAABB(this, velocity, other.ToAabb(), ref resultTime, resultNormal);
            else if (Geometry == GeometryType.Triangle || other.Geometry == GeometryType.Triangle ||
                     Geometry == GeometryType.Wedge || other.Geometry == GeometryType.Wedge)
            {
                Bounds bounds = new(this);
                bounds.Center += velocity;
                Bounds otherBounds = new(other);
                otherBounds.Center += otherVelocity;

                if (bounds.Intersects(otherBounds))
                {
                    resultTime = 1.0f;
                    if (resultNormal != null) resultNormal = Vector3.ZAxis;
                    return true;
                }
                else
                    return false;
            }
            else
            {
                bool result = SweepAsCylinders(this, other, velocity, otherVelocity, ref resultTime);
                if (result && resultNormal != null)
                {
                    Vector3 position = Center + velocity * resultTime;
                    Vector3 otherPosition = other.Center + otherVelocity * resultTime;
                    resultNormal = Vector3.SafeNormalize2D(position - otherPosition, Vector3.ZAxis);
                }
                return result;
            }
        }

        private static bool SweepAsCylinders(Bounds bounds, Bounds other, Vector3 velocity, Vector3 otherVelocity, ref float resultTime)
        {
            Sphere sphere = new (bounds.Center, bounds.Radius);
            Sphere otherSphere = new (other.Center, other.Radius);
            float time = 0.0f;
            if (sphere.Sweep(otherSphere, otherVelocity, velocity, ref time, Axis.Z))
            {
                if (bounds.Geometry == GeometryType.Triangle || other.Geometry == GeometryType.Triangle) return true;
                Vector3 center = bounds.Center + velocity * time;
                Vector3 otherCenter = other.Center + otherVelocity * time;
                Range<float> rangeHeight = new (center.Z - bounds.HalfHeight, center.Z + bounds.HalfHeight);
                Range<float> otherRangeHeight = new (otherCenter.Z - other.HalfHeight, otherCenter.Z + other.HalfHeight);
                if (rangeHeight.Intersects(otherRangeHeight))
                {
                    resultTime = time;
                    return true;
                }
            }
            return false;
        }

        private static bool SweepVsStationaryAABB(Bounds bounds, Vector3 velocity, in Aabb aabb, ref float resultTime, Vector3? resultNormal)
        {
            switch (bounds.Geometry)
            {
                case GeometryType.Capsule:
                    {
                        Cylinder2 cylinder2 = new (bounds.Center, bounds._params.CapsuleHalfHeight, bounds._params.CapsuleRadius);
                        return cylinder2.Sweep(velocity, aabb, ref resultTime, ref resultNormal);
                    }
                case GeometryType.Sphere:
                    {
                        Cylinder2 cylinder2 = new (bounds.Center, bounds._params.SphereRadius, bounds._params.SphereRadius);
                        return cylinder2.Sweep(velocity, aabb, ref resultTime, ref resultNormal);
                    }
                case GeometryType.Triangle:
                    {
                        if (resultNormal != null) resultNormal = Vector3.ZAxis;
                        Sphere sphere = new (bounds.Center, bounds.GetSphereRadius());
                        return sphere.Sweep(aabb, velocity, ref resultTime);
                    }
                default:
                    {
                        Logger.Warn($"SweepVsStationaryAABB: Unsupported bounds geometry type: {bounds.Geometry}");
                        return false;
                    }
            }
        }

        private static bool SweepVsStationaryOBB(Bounds bounds, Vector3 velocity, in Obb obb, ref float resultTime, Vector3? resultNormal)
        {
            Aabb aabb = new (obb.Center - obb.Extents, obb.Center + obb.Extents);

            switch (bounds.Geometry)
            {
                case GeometryType.Capsule:
                    {
                        Vector3 oobCenter = obb.TransformPoint(bounds.Center);
                        Vector3 oobVelocity = obb.TransformVector(velocity);
                        Cylinder2 cylinder2 = new (oobCenter, bounds._params.CapsuleHalfHeight, bounds._params.CapsuleRadius);
                        bool result = cylinder2.Sweep(oobVelocity, aabb, ref resultTime, ref resultNormal);
                        if (result && resultNormal != null)
                            resultNormal = obb.RotationMatrix * resultNormal.Value;
                        return result;
                    }
                case GeometryType.Sphere:
                    {
                        Vector3 oobCenter = obb.TransformPoint(bounds.Center);
                        Vector3 oobVelocity = obb.TransformVector(velocity);
                        Cylinder2 cylinder2 = new (oobCenter, bounds._params.SphereRadius, bounds._params.SphereRadius);
                        bool result = cylinder2.Sweep(oobVelocity, aabb, ref resultTime, ref resultNormal);
                        if (result && resultNormal != null)
                            resultNormal = obb.RotationMatrix * resultNormal.Value;
                        return result;
                    }
                case GeometryType.Triangle:
                    {
                        if (resultNormal != null) resultNormal = Vector3.ZAxis;
                        Vector3 oobCenter = obb.TransformPoint(bounds.Center);
                        Vector3 oobVelocity = obb.TransformVector(velocity);
                        Sphere sphere = new (oobCenter, bounds.GetSphereRadius());
                        return sphere.Sweep(aabb, oobVelocity, ref resultTime);
                    }
                default:
                    Logger.Warn($"SweepVsStationaryOBB: Unsupported bounds geometry type: {bounds.Geometry}");
                    return false;
            }
        }
    }
}
