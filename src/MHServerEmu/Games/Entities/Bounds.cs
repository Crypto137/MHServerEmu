﻿using MHServerEmu.Common.Helpers;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;
using System.Runtime.InteropServices;

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
        public Vector3 Orientation { get => _orientation; set => SetOrientation(value); }
        public float Radius { get => GetRadius(); set => SetRadius(value); }
        public float HalfHeight { get => GetHalfHeight(); }
        public BoundsFlags Flags { get; private set; }

        private BoundData _params = new();
        private Vector3 _orientation;
        private Vector3 _orientation_offset;

        public Bounds()
        {
            Center = Vector3.Zero;
            _orientation = Vector3.Zero;
            _orientation_offset = Vector3.Zero;
            CollisionType = BoundsCollisionType.None;
            Flags = BoundsFlags.None;
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

        private void InitializeWedge(float angleDegrees, float heightFromCenter, float length, float baseWidth, BoundsCollisionType collisionType, BoundsFlags flags)
        {
            Geometry = GeometryType.Wedge;
            _params.WedgeBaseWidth = baseWidth;
            _params.WedgeBase = 2 * length * MathF.Tan(MathHelper.ToRadians(angleDegrees * 0.5f));
            _params.WedgeHalfHeight = heightFromCenter;
            _params.WedgeLength = length;
            CollisionType = collisionType;
            Flags = flags;
        }

        private void InitializeIsocelesTriangle(float angleDegrees, float heightFromCenter, float length, BoundsCollisionType collisionType, BoundsFlags flags)
        {
            Geometry = GeometryType.Triangle;
            _params.TriangleBase = 2 * length * MathF.Tan(MathHelper.ToRadians(angleDegrees * 0.5f));
            _params.TriangleHalfHeight = heightFromCenter;
            _params.TriangleLength = length;
            CollisionType = collisionType;
            Flags = flags;
        }

        private void InitializeSphere(float radius, BoundsCollisionType collisionType, BoundsFlags flags)
        {
            Geometry = GeometryType.Sphere;
            _params.SphereRadius = radius;
            CollisionType = collisionType;
            Flags = flags;
        }

        private void InitializeCapsule(float radius, float heightFromCenter, BoundsCollisionType collisionType, BoundsFlags flags)
        {
            Geometry = GeometryType.Capsule;
            _params.CapsuleRadius = radius;
            _params.CapsuleHalfHeight = heightFromCenter;
            CollisionType = collisionType;
            Flags = flags;
        }

        private void InitializeBox(float width, float length, float height, bool axisAligned, BoundsCollisionType collisionType, BoundsFlags flags)
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
            Matrix3 mat = Matrix3.AbsPerElem(Matrix3.GetMatrix3(_orientation));
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

        private float GetRadius()
        {
            switch (Geometry)
            {
                case GeometryType.OBB:
                    return Vector3.Length(new Vector3(_params.OBBHalfWidth, _params.OBBHalfLength, 0.0f));
                case GeometryType.AABB:
                    return Vector3.Length(new Vector3(_params.AABBOrientedWidth, _params.AABBOrientedLength, 0.0f));
                case GeometryType.Capsule:
                    return _params.CapsuleRadius;
                case GeometryType.Sphere:
                    return _params.SphereRadius;
                case GeometryType.Triangle:
                    Vector3[] triangle = ToTriangle2D();
                    float max = MathF.Max(
                        MathF.Max(Vector3.Length(triangle[0] - Center), Vector3.Length(triangle[1] - Center)),
                        Vector3.Length(triangle[2] - Center));
                    return max;
                case GeometryType.Wedge:
                    Vector3[] wedgeVertices = GetWedgeVertices();
                    max = Math.Max(
                        Vector3.DistanceSquared2D(wedgeVertices[1], Center),
                        Vector3.DistanceSquared2D(wedgeVertices[2], Center));
                    return MathF.Sqrt(max); // SquareRoot
                default:
                    return 0.0f;
            }
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

        private Vector3[] ToTriangle2D()
        {
            if (Geometry != GeometryType.Triangle) 
                return new Vector3[]
                {
                    Vector3.Zero,
                    Vector3.Zero,
                    Vector3.Zero
                };
            Transform3 transform = Transform3.BuildTransform(Center, _orientation);
            return new Vector3[]
            { 
                new(transform * new Point3(_params.TriangleLength * -0.66666669f, 0.0f, 0.0f)),
                new(transform * new Point3(_params.TriangleLength * 0.33333334f, _params.TriangleBase * -0.5f, 0.0f)),
                new(transform * new Point3(_params.TriangleLength * 0.33333334f, _params.TriangleBase * 0.5f, 0.0f))
            };           
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

        private void SetOrientation(Vector3 orientation)
        {
            _orientation = orientation + _orientation_offset;
            if (Geometry == GeometryType.AABB) UpdateAABBGeometry();
        }

        public Aabb ToAabb()
        {            
            switch (Geometry)
            {
                case GeometryType.OBB:
                    Matrix3 mat = Matrix3.AbsPerElem(Matrix3.GetMatrix3(_orientation));
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
    }
}