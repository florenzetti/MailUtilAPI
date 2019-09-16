using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UtilAPIUnitTest.GoogleService;

namespace UtilAPIUnitTest
{
    [TestClass]
    public class GoogleServiceUnitTest
    {
        private static GoogleServiceClient _googleSvc;
        private static Initializer _initSvc;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _googleSvc = new GoogleServiceClient();

            _initSvc = new Initializer();
            //_initSvc.CertificateFileBytes = certificateBytes;
            //_initSvc.CertificatePassword = certificatePassword;
            //_initSvc.ServiceAccountEmail = serviceAccountEmail;
            //_initSvc.User = null;
            //_initSvc.Scopes = scopes;
            //_initSvc.ApplicationName = applicationName;
        }

        [TestMethod]
        public void GoogleService_Drive_GetFileList()
        {
            //arrange
            //act
            FileList flist = _googleSvc.GetFileList(_initSvc);
            //assert
            Assert.IsNotNull(flist);
        }

        [TestMethod]
        public void GoogleService_Drive_UploadFile()
        {
            string filePath = string.Empty;
            //arrange
            FileInfo fileInfo = new FileInfo(filePath);
            GoogleService.File fileResponse;
            //act
            using (MemoryStream uploadStream = new MemoryStream(System.IO.File.ReadAllBytes(filePath)))
            {
                fileResponse = _googleSvc.UploadFilePath(_initSvc, filePath, uploadStream);
            }
            //assert
            Assert.IsNotNull(fileResponse);
        }

        [TestMethod]
        public void GoogleService_Drive_UploadFilePath()
        {
            //arrange
            string filePath = string.Empty;
            FileInfo fileInfo = new FileInfo(filePath);
            GoogleService.File file = new GoogleService.File();
            file.Title = fileInfo.Name;
            file.MimeType = fileInfo.Extension;
            //act
            GoogleService.File responseFile;
            using (MemoryStream uploadStream = new MemoryStream(System.IO.File.ReadAllBytes(filePath)))
            {
                responseFile = _googleSvc.UploadFile(_initSvc, file, uploadStream, fileInfo.Extension);
            }
            //assert
            Assert.IsNotNull(responseFile);
        }

        [TestMethod]
        public void GoogleService_Drive_DownloadFile()
        {
            //arrange
            string fileId = string.Empty;
            string filePath = string.Empty;
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
            //act
            using (MemoryStream stream = _googleSvc.DownloadFile(_initSvc, fileId))
            {
                if (stream != null && stream.Length > 0)
                {
                    FileStream fileStream = System.IO.File.Create(filePath, (int)stream.Length);
                    // Initialize the bytes array with the stream length and then fill it with data
                    byte[] bytesInStream = new byte[stream.Length];
                    stream.Read(bytesInStream, 0, bytesInStream.Length);
                    // Use write method to write to the file specified above
                    fileStream.Write(bytesInStream, 0, bytesInStream.Length);
                    fileStream.Flush();
                    fileStream.Close();
                }
            }
            //assert
            Assert.IsTrue(System.IO.File.Exists(filePath));
        }

        [TestMethod]
        public void GoogleService_Drive_DeleteFile()
        {
            //arrange
            string fileId = string.Empty;
            //act
            string result = _googleSvc.DeleteFilePath(_initSvc, fileId);
            //assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void GoogleService_Gmail_GetThread()
        {
            //arrange
            string userId = string.Empty;
            _initSvc.User = userId;
            //act
            ListThreadsResponse lThread = _googleSvc.GetThreadList(_initSvc, userId);
            //assert
            foreach (Thread thread in lThread.Threads)
            {
                Thread t = _googleSvc.GetThread(_initSvc, userId, thread.Id);
                Assert.IsNotNull(t);
            }
        }

        [TestMethod]
        public void GoogleService_Gmail_SendMessage()
        {
            //arrange
            _initSvc.User = string.Empty;
            string emailTo = string.Empty;
            string emailCC = string.Empty;

            EmailMessage mail = new EmailMessage();
            mail.Subject = "Sent by gmail API";
            mail.Body = "<b>Sent by gmail API via UtilAPI </b><br/><h1>HTML Formatted</h1><br/>";
            mail.From = _initSvc.User;
            mail.IsBodyHtml = true;
            mail.To = new string[] { emailTo };
            mail.CC = new string[] { emailCC };

            //act
            //userId can be "me"
            Message returnMessage = _googleSvc.SendMessage(_initSvc, mail, _initSvc.User);
            //assert
            Assert.IsNotNull(returnMessage);
        }

        [TestMethod]
        public void GoogleService_Gmail_GetAttachments()
        {
            //arrange
            string userId = string.Empty;
            _initSvc.User = userId;
            //act
            ListThreadsResponse lThread = _googleSvc.GetThreadList(_initSvc, userId);
            //assert
            Thread firstThread = lThread.Threads[0];       
            if (firstThread.Messages != null)
            {
                foreach (Message message in firstThread.Messages)
                {
                    foreach (MessagePart part in message.Payload.Parts)
                    {
                        if (!string.IsNullOrEmpty(part.Filename))
                        {
                            string attId = part.Body.AttachmentId;
                            MessagePartBody attachPart = _googleSvc.GetMessageAttachment(_initSvc, userId, message.Id, attId);

                            // Converting from RFC 4648 base64-encoding
                            // see http://en.wikipedia.org/wiki/Base64#Implementations_and_history
                            string attachData = attachPart.Data.Replace('-', '+');
                            attachData = attachData.Replace('_', '/');

                            byte[] data = Convert.FromBase64String(attachData);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void GoogleService_Gmail_ReplyMessage()
        {
            //arrange
            _initSvc.User = string.Empty;
            string emailTo = string.Empty;
            string emailCC = string.Empty;
            string replyTo = string.Empty;
            string filePath = string.Empty;
            string emailSubject = string.Empty;

            EmailMessage replyMail = new EmailMessage();
            replyMail.Subject = "RE: " + emailSubject;
            replyMail.Body = "<b>Sent <i>reply</i> by gmail API via UtilAPI </b><br/><h1>HTML Formatted</h1><br/>";
            replyMail.From = _initSvc.User;
            replyMail.IsBodyHtml = true;
            replyMail.To = new string[] { emailTo };
            replyMail.CC = new string[] { emailCC };
            //SET ReplyToList
            replyMail.ReplyToList = new string[] { replyTo };
            //SET ThreadId
            //replyMail.ThreadId get from Message.ThreadId
            replyMail.AttachmentsFilePath = new string[] { filePath };

            //act
            Message returnReplyMessage = _googleSvc.SendMessage(_initSvc, replyMail, _initSvc.User);
            //assert
            Assert.IsNotNull(returnReplyMessage);
        }

        [TestMethod]
        public void GoogleService_Gmail_ForwardMessage()
        {
            //arrange
            string userId = string.Empty;
            string emailTo = string.Empty;
            _initSvc.User = userId;
            ListThreadsResponse threadList = _googleSvc.GetThreadList(_initSvc, _initSvc.User);
            Message message = null;
            //act
            foreach (Thread t in threadList.Threads)
            {
                message = _googleSvc.ForwardMessage(_initSvc, new EmailMessage() { To = new string[] { emailTo } }, t.Id, _initSvc.User);
            }
            //assert
            Assert.IsNotNull(message);
        }
    }
}
