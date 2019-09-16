using System;
using UtilAPIUnitTest.DocuSignService;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UtilAPIUnitTest
{
    [TestClass]
    public class DocuSignServiceUnitTest
    {
        private static DocuSignServiceClient _docuSignSvc;
        private static DocuSignCredential _credential;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _docuSignSvc = new DocuSignServiceClient();
            //Create Credential
            _credential = new DocuSignCredential();
            _credential.UserName = string.Empty;
            _credential.Password = string.Empty;
            _credential.IntegratorKey = string.Empty; //Guid
        }

        [TestMethod]
        public void DocuSign_CreateAndSedEnvelope()
        {
            //arrange
            //Create the recipient
            Recipient recipient = new Recipient();
            recipient.emailField = string.Empty;
            recipient.userNameField = string.Empty;
            recipient.typeField = RecipientTypeCode.Signer;
            recipient.idField = "1";
            recipient.routingOrderField = 1;

            //Attach the document(s)
            Document doc = new Document();
            doc.idField = "1";
            doc.nameField = "test.pdf";
            doc.pDFBytesField = new byte[1024];

            //Create the envelope content
            Envelope envelope = new Envelope();
            envelope.subjectField = "DocuSign Ask for sign";
            envelope.emailBlurbField = "Sign the document";
            envelope.recipientsField = new Recipient[] { recipient };
            envelope.accountIdField = string.Empty; //Guid
            envelope.documentsField = new Document[1];
            envelope.documentsField[0] = doc;
            //act
            EnvelopeStatus envStatus = _docuSignSvc.CreateAndSendEnvelope(_credential, envelope);
            //assert
            Assert.AreEqual(envStatus.statusField, EnvelopeStatusCode.Sent);
        }

        [TestMethod]
        public void DocuSign_RequestPDF()
        {
            //arrange
            string fileId = string.Empty;
            //act
            EnvelopePDF envPDF = _docuSignSvc.RequestPDF(_credential, string.Empty);
            //assert
            Assert.IsNotNull(envPDF);
        }

        [TestMethod]
        public void DocuSign_RequestDocumentPDFs()
        {
            //arrange
            string fileId = string.Empty;
            //act
            DocumentPDFs docPDFs = _docuSignSvc.RequestDocumentPDFs(_credential, fileId);
            //assert
            Assert.IsNotNull(docPDFs);
        }
    }
}