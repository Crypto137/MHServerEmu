namespace MHServerEmu.Games.Dialog
{
    public class ReportAsSpamOption : InteractionOption
    {
        public ReportAsSpamOption()
        {
            Priority = 50;
            MethodEnum = InteractionMethod.ReportAsSpam;
        }
    }
}
