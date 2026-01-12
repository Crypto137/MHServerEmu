namespace MHServerEmu.WebFrontend.Models
{
    public readonly struct AccountOperationResponse(int result)
    {
        public const int Success = 0;
        public const int GenericFailure = 1;

        public int Result { get; } = result;
    }
}
