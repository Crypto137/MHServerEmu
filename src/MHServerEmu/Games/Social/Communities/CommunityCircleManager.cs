using System.Collections;
using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.Games.Social.Communities
{
    // If you are working on this: keep in mind that circles is a half-baked and severely overengineered feature. Tread carefully.

    /// <summary>
    /// Manages <see cref="CommunityCircle"/> instances.
    /// </summary>
    public class CommunityCircleManager : IEnumerable<CommunityCircle>
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

        public bool Decode(CodedInputStream stream)
        {
            int numCircles = stream.ReadRawInt32();
            for (int i = 0; i < numCircles; i++)
            {
                string circleName = stream.ReadRawString();

                if (Enum.TryParse(circleName, out CircleId circleId) == false)
                    return Logger.ErrorReturn(false, $"Decode(): Unable to find system circle enum value for name {circleName}");

                CommunityCircle circle = GetCircle(circleId);

                if (circle == null)
                    Logger.ErrorReturn(false, $"Decode(): Unable to get community circle for header. name={circleName}, id=0x{(int)circleId:X}, community={Community}");

                _archiveCircles.Add(circle.Id);
            }

            return true;
        }

        public void Encode(CodedOutputStream stream)
        {
            // Generate a set of circles that need to be serialized
            _archiveCircles.Clear();
            CreateArchiveCircleIds(/* Archive archive */);

            // Write all circle names
            stream.WriteRawInt32(_archiveCircles.Count);
            foreach (CircleId circleId in _archiveCircles)
            {
                string circleName = GetCircle(circleId).Name;
                stream.WriteRawString(circleName);
            }
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
            while (_circleDict.Any())
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
        private void CreateArchiveCircleIds(/* archive */)
        {
            foreach(CommunityCircle circle in _circleDict.Values)
            {
                if (circle.ShouldArchiveTo(/* archive */))
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

        // IEnumerable implementation - use the Community iterate methods instead of this!
        public IEnumerator<CommunityCircle> GetEnumerator() => _circleDict.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
