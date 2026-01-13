namespace MHServerEmu.WebFrontend.Models
{
    public readonly struct AccountOperationRequest
    {
        public string Email { get; init; }
        public string PlayerName { get; init; }
        public string Password { get; init; }
        public byte UserLevel { get; init; }
        public int Flags { get; init; }
    }
}
