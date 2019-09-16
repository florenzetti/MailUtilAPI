using System.Runtime.Serialization;
using System.ServiceModel;

namespace UtilAPIService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IeSignService" in both code and config file together.
    [ServiceContract]
    public interface IDocuSignService
    {
        [OperationContract]
        DSAPIService.EnvelopeStatus CreateAndSendEnvelope(DocuSignCredential credential, DSAPIService.Envelope envelope);

        [OperationContract]
        DSAPIService.EnvelopePDF RequestPDF(DocuSignCredential credential, string envelopeID);

        [OperationContract]
        DSAPIService.DocumentPDFs RequestDocumentPDFs(DocuSignCredential credential, string envelopeID);
    }

    [DataContract]
    public class DocuSignCredential
    {
        private string userName;
        [DataMember]
        public string UserName
        {
            get { return userName; }
            set { userName = value; }
        }
        private string password;

        [DataMember]
        public string Password
        {
            get { return password; }
            set { password = value; }
        }
        private string integratorKey;

        [DataMember]
        public string IntegratorKey
        {
            get { return integratorKey; }
            set { integratorKey = value; }
        }
    }
}
