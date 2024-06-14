namespace MHServerEmu.Games.Dialog
{
    public class ReportOption : InteractionOption
    {
        public ReportOption()
        {
            Priority = 50;
            MethodEnum = InteractionMethod.Report;
        }
    }
}
