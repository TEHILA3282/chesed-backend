namespace ChafetzChesed.BLL.Interfaces
{
    public interface IExternalUserSyncService
    {
        Task<int> SyncAsync(); 
    }
}
