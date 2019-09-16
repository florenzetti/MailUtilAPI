using System.Web.Services.Protocols;
using UtilAPIService.FedexTrackService;
using UtilAPIService.UPSTrackService;

namespace UtilAPIService
{
    public class MailService : IMailService
    {
        public string UserName;

        public FedexTrackService.TrackRequest CreateFedexTrackRequest(string key, string password, string accountNumber, string meterNumber, string trackingNumber)
        {
            // Build the TrackRequest
            FedexTrackService.TrackRequest request = new FedexTrackService.TrackRequest();
            request.WebAuthenticationDetail = CreateFedexAuthentication(key, password);

            request.ClientDetail = new ClientDetail()
            {
                AccountNumber = accountNumber,
                MeterNumber = meterNumber
            };

            request.TransactionDetail = new TransactionDetail();
            request.TransactionDetail.CustomerTransactionId = "***Tracking using UtilAPI***";  //This is a reference field for the customer.  Any value can be used and will be provided in the response.
            request.Version = new VersionId();

            // Tracking information
            request.SelectionDetails = new TrackSelectionDetail[1] { new TrackSelectionDetail() };
            request.SelectionDetails[0].PackageIdentifier = new TrackPackageIdentifier();
            request.SelectionDetails[0].PackageIdentifier.Value = trackingNumber; //tracking number or door tag
            request.SelectionDetails[0].PackageIdentifier.Type = TrackIdentifierType.TRACKING_NUMBER_OR_DOORTAG;

            // Date range is optional.
            // If omitted, set to false

            // Include detailed scans is optional.
            // If omitted, set to false
            request.ProcessingOptions = new TrackRequestProcessingOptionType[1];
            request.ProcessingOptions[0] = TrackRequestProcessingOptionType.INCLUDE_DETAILED_SCANS;
            return request;
        }

        public WebAuthenticationDetail CreateFedexAuthentication(string key, string password)
        {
            WebAuthenticationDetail webAuth = new WebAuthenticationDetail();
            webAuth.UserCredential = new WebAuthenticationCredential()
            {
                Key = key,
                Password = password
            };

            return webAuth;
        }

        public TrackReply GetFedexTrack(FedexTrackService.TrackRequest request)
        {
            TrackReply reply = null;
            FedexTrackService.TrackService svc = new FedexTrackService.TrackService();
            reply = svc.track(request);

            return reply;
        }

        public UPSSecurity CreateUPSAuthentication(string userName, string password, string AccessLicenseNumber)
        {
            UPSSecurity auth = new UPSSecurity();
            auth.ServiceAccessToken = new UPSSecurityServiceAccessToken()
            {
                AccessLicenseNumber = AccessLicenseNumber
            };
            auth.UsernameToken = new UPSSecurityUsernameToken()
            {
                Username = userName,
                Password = password
            };

            return auth;
        }

        public UPSTrackService.TrackRequest CreateUPSTrackRequest(string trackingNumber)
        {
            UPSTrackService.TrackRequest request = new UPSTrackService.TrackRequest();
            RequestType requestType = new RequestType()
            {
                RequestOption = new string[] { "0", "1", "2", "3", "4", "5", "6", "7" }
                //SubVersion = "",
                //TransactionReference = new TransactionReferenceType() { }
            };

            request.Request = requestType;
            request.InquiryNumber = trackingNumber;

            return request;
        }

        public TrackResponse GetUPSTrack(string userName, string password, string accessKey, UPSTrackService.TrackRequest request)
        {
            TrackResponse response = null;
            UPSTrackService.TrackService svc = new UPSTrackService.TrackService();
            svc.UPSSecurityValue = CreateUPSAuthentication(userName, password, accessKey);

            response = svc.ProcessTrack(request);

            return response;
        }
    }
}