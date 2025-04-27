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
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<CircleId, CommunityCircle> _circleDict = new();
        private readonly List<CircleId> _archiveCircles = new();     // A collection of circle ids that need to be written to archives

        public Community Community { get; }
        public int NumCircles { get => _circleDict.Count; }

        /// <summary>
        /// Constructs a new <see cref="CommunityCircleManager"/>.
        /// </summary>
        public CommunityCircleManager(Community community)
        {
            Community = community;
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
                    if (Enum.TryParse(circleName, out CircleId circleId) == false)
                        return Logger.ErrorReturn(false, $"Serialize(): Unable to find system circle enum value for name {circleName}");

                    CommunityCircle circle = GetCircle(circleId);

                    if (circle == null)
                        Logger.ErrorReturn(false, $"Serialize(): Unable to get community circle for header. name={circleName}, id=0x{(int)circleId:X}, community={Community}");

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
            while (_circleDict.Count > 0)
            {
                CommunityCircle circle = _circleDict.Values.First();
                DestroyCircle(circle);
            }
        }

        /// <summary>
        /// Returns the <see cref="CommunityCircle"/> with the specified id.
        /// </summary>
        public CommunityCircle GetCircle(CircleId id)
        {
            if (_circleDict.TryGetValue(id, out CommunityCircle circle) == false)
                return null;

            return circle;
        }

        /// <summary>
        /// Returns the <see cref="CommunityCircle"/> with the specified archive circle id.
        /// </summary>
        public CommunityCircle GetCircleByArchiveCircleId(int archiveCircleId)
        {
            if ((archiveCircleId >= 0 && archiveCircleId < _archiveCircles.Count) == false)
                return Logger.WarnReturn<CommunityCircle>(null, $"GetCircleByArchiveCircleId(): Invalid circle id {archiveCircleId}");

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

            Logger.Warn($"GetArchiveCircleId(): circleId not found");
            return -1;
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            foreach (CommunityCircle circle in _circleDict.Values)
                sb.AppendLine(circle.ToString());

            return sb.ToString();
        }

        /// <summary>
        /// Create a <see cref="CommunityCircle"/> for the specified id.
        /// </summary>
        private CommunityCircle CreateCircle(CircleId circleId)
        {
            // Verify "Trying to create a new circle while iterating them in the community %s"
            // We probably don't need this because it seems user circles were never implemented,
            // and all system circles are created during initalization.

            if (_circleDict.ContainsKey(circleId))
                return Logger.WarnReturn((CommunityCircle)null, "CreateCircle(): Cannot create circle that already exists");

            string circleName = Community.GetLocalizedSystemCircleName(circleId);
            CommunityCircle circle = new(Community, circleName, circleId, CircleType.System);
            _circleDict.Add(circleId, circle);
            return circle;
        }

        /// <summary>
        /// Destroys the specified <see cref="CommunityCircle"/>.
        /// </summary>
        private void DestroyCircle(CommunityCircle circle)
        {
            // Verify "Trying to destroy circle while iterating them in the community %s"
            // We probably don't need this because it seems user circles were never implemented,
            // and all system circles are destroyed during shutdown.

            _circleDict.Remove(circle.Id);
            _archiveCircles.Remove(circle.Id);
        }

        /// <summary>
        /// Generates the collection of circle ids that need to be serialized.
        /// </summary>
        private void CreateArchiveCircleIds(Archive archive = null)
        {
            foreach(CommunityCircle circle in _circleDict.Values)
            {
                if (circle.ShouldArchiveTo(archive))
                {
                    if (_archiveCircles.Contains(circle.Id))
                    {
                        Logger.Warn($"CreateArchiveCircleIds(): Trying to add archive circle twice");
                        continue;
                    }

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
                _enumerator = _circleManager._circleDict.Values.GetEnumerator();
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Dispose();
                _enumerator = _circleManager._circleDict.Values.GetEnumerator();
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }
        }
    }
}
