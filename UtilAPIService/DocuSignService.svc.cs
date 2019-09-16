using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace UtilAPIService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "eSignService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select eSignService.svc or eSignService.svc.cs at the Solution Explorer and start debugging.
    public class DocuSignService : IDocuSignService
    {
        public DSAPIService.EnvelopeStatus CreateAndSendEnvelope(DocuSignCredential credential, DSAPIService.Envelope envelope)
        {
            DSAPIService.DSAPIServiceSoapClient svc = new DSAPIService.DSAPIServiceSoapClient();

            using (OperationContextScope scope = new System.ServiceModel.OperationContextScope(svc.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers.Add("X-DocuSign-Authentication", GetAuthXML(credential));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                DSAPIService.EnvelopeStatus status = svc.CreateAndSendEnvelope(envelope);
                return status;
            }
        }

        public DSAPIService.EnvelopePDF RequestPDF(DocuSignCredential credential, string envelopeID)
        {
            DSAPIService.DSAPIServiceSoapClient svc = new DSAPIService.DSAPIServiceSoapClient();

            using (OperationContextScope scope = new OperationContextScope(svc.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers.Add("X-DocuSign-Authentication", GetAuthXML(credential));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                var retorno = svc.RequestPDF(envelopeID);
                return retorno;
            }
        }

        public DSAPIService.DocumentPDFs RequestDocumentPDFs(DocuSignCredential credential, string envelopeID)
        {
            DSAPIService.DSAPIServiceSoapClient svc = new DSAPIService.DSAPIServiceSoapClient();

            using (OperationContextScope scope = new OperationContextScope(svc.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers.Add("X-DocuSign-Authentication", GetAuthXML(credential));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                return svc.RequestDocumentPDFs(envelopeID);
            }
        }

        private string GetAuthXML(DocuSignCredential credential)
        {
            String auth = string.Format("<DocuSignCredentials><Username>{0}</Username>"
                         + "<Password>{1}</Password>"
                         + "<IntegratorKey>{2}</IntegratorKey></DocuSignCredentials>", credential.UserName, credential.Password, credential.IntegratorKey);

            return auth;
        }
    }
}