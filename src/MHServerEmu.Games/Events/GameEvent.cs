using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Events
{
    public enum EventEnum
    {
        StartThrowing,
        EndThrowing,
        StartTravel,
        EndTravel,
        DiamondFormActivate,
        DiamondFormDeactivate,
        StartMagikUltimate,
        EndMagikUltimate,
        ToTeleport,
        EmoteDance,
        FinishCellLoading,
        PreInteractPower,
        PreInteractPowerEnd,
        UseInteractableObject,
        GetRegion,
        ErrorInRegion
    }

    public class GameEvent
    {
        private readonly DateTime _creationTime;
        private readonly TimeSpan _lifetime;

        public PlayerConnection PlayerConnection { get; }
        public EventEnum Event { get; }
        public bool IsRunning { get; set; }
        public object Data { get; set; }

        public GameEvent(PlayerConnection playerConnection, EventEnum gameEvent, long lifetimeMs, object data)
        {
            _creationTime = DateTime.Now;
            _lifetime = TimeSpan.FromMilliseconds(lifetimeMs);

            PlayerConnection = playerConnection;
            Event = gameEvent;
            Data = data;
            IsRunning = true;
        }

        public bool IsExpired()
        {
            return DateTime.Now.Subtract(_creationTime) >= _lifetime;
        }
    }
}
