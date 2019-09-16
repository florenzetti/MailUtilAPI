using System.ServiceModel;


namespace UtilAPIService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IMailService" in both code and config file together.
    [ServiceContract]
    public interface IMailService
    {
        [OperationContract]
        FedexTrackService.TrackReply GetFedexTrack(FedexTrackService.TrackRequest request);

        [OperationContract]
        FedexTrackService.TrackRequest CreateFedexTrackRequest(string key, string password, string accountNumber, string meterNumber, string trackingNumber);

        [OperationContract]
        UPSTrackService.TrackResponse GetUPSTrack(string userName, string password, string accessKey, UPSTrackService.TrackRequest request);

        [OperationContract]
        UPSTrackService.TrackRequest CreateUPSTrackRequest(string trackingNumber);
    }
}
