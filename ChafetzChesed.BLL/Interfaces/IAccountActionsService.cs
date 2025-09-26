namespace ChafetzChesed.BLL.Interfaces
{
    public interface IAccountActionsService
    {
        Task FetchAndParseExcelFromExternalAsync(int institutionId);
    }
}
