using System.Collections;
using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Social.Communities
{
    /// <summary>
    /// Manages <see cref="CommunityCircle"/> instances.
    /// </summary>
    public class CommunityCircleManager : ISerialize
    {
        public const int ArchiveCircleIdInvalid = -1;

        private readonly Dictionary<CircleId, CommunityCircle> _circles = new();
        private readonly List<CircleId> _archiveCircles = new();     // A collection of circle ids that need to be written to archives

        private int _numCircleIteratorsInScope = 0;

        public Community Community { get; }
        public int NumCircles { get => _circles.Count; }

        /// <summary>
        /// Constructs a new <see cref="CommunityCircleManager"/>.
        /// </summary>
        public CommunityCircleManager(Community community)
        {
            Community = community;
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            foreach (CommunityCircle circle in _circles.Values)
                sb.AppendLine(circle.ToString());

            return sb.ToString();
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            _archiveCircles.Clear();

            if (archive.IsPacking)
                CreateArchiveCircleIds(archive);

            int numCircles = _archiveCircles.Count;
            success &= Serializer.Transfer(archive, ref numCircles);

            string circleName = string.Empty;
            for (int i = 0; i < numCircles; i++)
            {
                if (archive.IsPacking)
                    circleName = GetCircle(_archiveCircles[i]).Name;

                success &= Serializer.Transfer(archive, ref circleName);

                if (archive.IsUnpacking)
                {
                    bool found = Enum.TryParse(circleName, out CircleId circleId);
                    if (!Verify.IsTrue(found, $"Unable to find system circle enum value for name {circleName}"))
                        return false;

                    CommunityCircle circle = GetCircle(circleId);
                    if (!Verify.IsNotNull(circle, $"Unable to get community circle for header. name={circleName}, id=0x{(int)circleId:X}, community={Community}"))
                        return false;

                    _archiveCircles.Add(circle.Id);
                }
            }

            return success;
        }

        /// <summary>
        /// Creates default system <see cref="CommunityCircle"/> instances in this <see cref="CommunityCircleManager"/>.
        /// </summary>
        public bool Initialize()
        {
            for (CircleId circleId = CircleId.__Friends; circleId < CircleId.NumCircles; circleId++)
                CreateCircle(circleId);

            return true;
        }

        /// <summary>
        /// Destroys all <see cref="CommunityCircle"/> instances in this <see cref="CommunityCircleManager"/>.
        /// </summary>
        public void Shutdown()
        {
            while (_circles.Count > 0)
            {
                CommunityCircle circle = _circles.Values.First();
                DestroyCircle(circle);
            }
        }

        /// <summary>
        /// Returns the <see cref="CommunityCircle"/> with the specified id.
        /// </summary>
        public CommunityCircle GetCircle(CircleId id)
        {
            if (_circles.TryGetValue(id, out CommunityCircle circle) == false)
                return null;

            return circle;
        }

        /// <summary>
        /// Returns the <see cref="CommunityCircle"/> with the specified archive circle id.
        /// </summary>
        public CommunityCircle GetCircleByArchiveCircleId(int archiveCircleId)
        {
            if (!Verify.IsTrue(archiveCircleId >= 0 && archiveCircleId < _archiveCircles.Count, $"Invalid archive circle id {archiveCircleId}"))
                return null;

            CircleId circleId = _archiveCircles[archiveCircleId];
            return GetCircle(circleId);
        }

        /// <summary>
        /// Returns the archive circle id for the provided <see cref="CommunityCircle"/>.
        /// </summary>
        public int GetArchiveCircleId(CommunityCircle circle)
        {
            for (int i = 0; i < _archiveCircles.Count; i++)
            {
                CircleId circleId = _archiveCircles[i];
                if (circle.Id == circleId)
                    return i;
            }

            Verify.IsTrue(false, $"Asked for archive circle id for circle that is not persistent. circle={circle}");
            return ArchiveCircleIdInvalid;
        }

        /// <summary>
        /// Create a <see cref="CommunityCircle"/> for the specified id.
        /// </summary>
        private CommunityCircle CreateCircle(CircleId circleId)
        {
            if (!Verify.IsTrue(_numCircleIteratorsInScope == 0, $"Trying to create a new circle while iterating them in the community {Community}"))
                return null;

            CommunityCircle existingCircle = GetCircle(circleId);
            if (!Verify.IsTrue(existingCircle == null, $"Cannot create circle that already exists. circle={existingCircle}, community={Community}"))
                return null;

            string circleName = Community.GetLocalizedSystemCircleName(circleId);
            CommunityCircle circle = new(Community, circleName, circleId, CircleType.System);
            // verify: Unable to allocate system circle %s

            _circles.Add(circleId, circle);
            return circle;
        }

        /// <summary>
        /// Destroys the specified <see cref="CommunityCircle"/>.
        /// </summary>
        private void DestroyCircle(CommunityCircle circle)
        {
            if (!Verify.IsTrue(_numCircleIteratorsInScope == 0, $"Trying to destroy circle while iterating them in the community {Community}"))
                return;

            _circles.Remove(circle.Id);
            _archiveCircles.Remove(circle.Id);
        }

        /// <summary>
        /// Generates the collection of circle ids that need to be serialized.
        /// </summary>
        private void CreateArchiveCircleIds(Archive archive = null)
        {
            foreach(CommunityCircle circle in _circles.Values)
            {
                if (circle.ShouldArchiveTo(archive))
                {
                    if (!Verify.IsTrue(_archiveCircles.Contains(circle.Id) == false, $"Trying to add archive circle twice.  circle={circle}"))
                        continue;

                    _archiveCircles.Add(circle.Id);                        
                }
            }

            _archiveCircles.Sort();
        }

        // Use Community.Iterate() methods instead of this
        public Enumerator GetEnumerator()
        {
            return new(this);
        }

        public struct Enumerator : IEnumerator<CommunityCircle>
        {
            // Simple wrapper around Dictionary<CircleId, CommunityCircle>.ValueCollection.Enumerator for readability
            private readonly CommunityCircleManager _circleManager;

            private Dictionary<CircleId, CommunityCircle>.ValueCollection.Enumerator _enumerator;

            public CommunityCircle Current { get => _enumerator.Current; }
            object IEnumerator.Current { get => Current; }

            public Enumerator(CommunityCircleManager circleManager)
            {
                _circleManager = circleManager;
                _enumerator = _circleManager._circles.Values.GetEnumerator();
                _circleManager._numCircleIteratorsInScope++;
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Dispose();
                _enumerator = _circleManager._circles.Values.GetEnumerator();
            }

            public void Dispose()
            {
                _enumerator.Dispose();
                _circleManager._numCircleIteratorsInScope--;
            }
        }
    }
}
