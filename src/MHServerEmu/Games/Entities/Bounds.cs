using MHServerEmu.Common.Logging;
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

    public class Bounds
    {
        public static readonly Logger Logger = LogManager.CreateLogger();

        public GeometryType Geometry { get; private set; }
        public BoundsCollisionType CollisionType { get; set; }
        public float Radius { get; set; }
        public float HalfHeight { get; set; }
        public BoundsFlags Flags { get; set; }

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
            throw new NotImplementedException();
        }

        private void InitializeIsocelesTriangle(float angleDegrees, float heightFromCenter, float length, BoundsCollisionType collisionType, BoundsFlags flags)
        {
            throw new NotImplementedException();
        }

        private void InitializeSphere(float radius, BoundsCollisionType collisionType, BoundsFlags flags)
        {
            throw new NotImplementedException();
        }

        private void InitializeCapsule(float radius, float heightFromCenter, BoundsCollisionType collisionType, BoundsFlags flags)
        {
            throw new NotImplementedException();
        }

        private void InitializeBox(float width, float length, float height, bool axisAligned, BoundsCollisionType collisionType, BoundsFlags flags)
        {
            throw new NotImplementedException();
        }
    }
}
