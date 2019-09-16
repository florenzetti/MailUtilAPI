using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace UtilAPIService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "GoogleService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select GoogleService.svc or GoogleService.svc.cs at the Solution Explorer and start debugging.
    public class GoogleService : IGoogleService
    {
        public System.IO.MemoryStream DownloadFile(Initializer initializer, string fileId)
        {
            if (String.IsNullOrEmpty(fileId))
                return null;

            System.IO.MemoryStream streamResponse = new System.IO.MemoryStream();

            Google.Apis.Drive.v2.DriveService svc = new DriveService(CreateInitializer(initializer));
            svc.Files.Get(fileId).Download(streamResponse);
            streamResponse.Position = 0;
            return streamResponse;
        }

        public File UploadFile(Initializer initializer, File file, System.IO.MemoryStream stream, String fileExtension)
        {
            DriveService svc = new DriveService(CreateInitializer(initializer));
            file.MimeType = GetMimeType(fileExtension);

            FilesResource.InsertMediaUpload request = svc.Files.Insert(file, stream, GetMimeType(fileExtension));
            request.Upload();
            return request.ResponseBody;
        }

        public File UploadFilePath(Initializer initializer, string filePath, System.IO.MemoryStream stream)
        {
            //filepath is empty or is not a full file path
            if (string.IsNullOrEmpty(filePath) || !filePath.Contains("\\"))
                return null;

            DriveService svc = new DriveService(CreateInitializer(initializer));
            string fileName = filePath.Substring(filePath.LastIndexOf('\\') + 1);

            //create folder path
            string[] folders = filePath.Split('\\');
            bool folderExist = true;
            string fileId = string.Empty;
            //remove filename from path
            for (int i = 0; i < folders.Length - 1; i++)
            {
                string folder = folders[i];

                //skip root
                if (folder.Contains(":"))
                    continue;

                //verify if the folder already exist
                if (folderExist)
                {
                    FilesResource.ListRequest listRequest = svc.Files.List();
                    listRequest.Spaces = "drive";
                    listRequest.Fields = "items(id)";
                    //Query folder name
                    listRequest.Q = "Title='" + folder + "' and mimeType='application/vnd.google-apps.folder'";
                    //get root folder id
                    if (string.IsNullOrEmpty(fileId))
                    {
                        fileId = svc.About.Get().Execute().RootFolderId;
                    }
                    listRequest.Q += " and '" + fileId + "' in parents";

                    FileList fileList = listRequest.Execute();
                    //file exist
                    if (fileList.Items != null && fileList.Items.Count > 0)
                    {
                        fileId = fileList.Items[0].Id;
                        continue;
                    }
                    else
                        folderExist = false;
                }

                //create file folder
                File fileFolder = new File();
                fileFolder.Title = folder;
                fileFolder.MimeType = "application/vnd.google-apps.folder";
                //verfy if is the root folder
                if (!string.IsNullOrEmpty(fileId))
                {
                    fileFolder.Parents = new List<ParentReference>();
                    fileFolder.Parents.Add(new ParentReference() { Id = fileId });
                }

                FilesResource.InsertRequest request = svc.Files.Insert(fileFolder);
                fileId = request.Execute().Id;
            }

            //insert file
            string title = fileName;
            string mimeType = null;
            if (fileName.Contains("."))
            {
                title = fileName.Split('.')[0];
                mimeType = GetMimeType(fileName.Split('.')[1]);
            }
            File fileMetaData = new File()
            {
                Title = fileName.Split('.')[0],
                MimeType = mimeType,
                Parents = new List<ParentReference>()
            };
            fileMetaData.Parents.Add(new ParentReference() { Id = fileId });

            FilesResource.InsertMediaUpload insertRequest = svc.Files.Insert(fileMetaData, stream, mimeType);
            insertRequest.Upload();
            return insertRequest.ResponseBody;
        }

        public File DeleteFile(Initializer initializer, string fileId)
        {
            if (String.IsNullOrEmpty(fileId))
                return null;

            Google.Apis.Drive.v2.DriveService svc = new DriveService(CreateInitializer(initializer));
            return svc.Files.Trash(fileId).Execute();
        }

        public string DeleteFilePath(Initializer initializer, string fileId)
        {
            if (String.IsNullOrEmpty(fileId))
                return null;

            DriveService svc = new DriveService(CreateInitializer(initializer));

            File deletedFile = svc.Files.Get(fileId).Execute();
            string sDeletedFile = svc.Files.Delete(fileId).Execute();

            foreach (ParentReference parent in deletedFile.Parents)
            {
                //skip root files or root folder
                if (parent.IsRoot == null || parent.IsRoot == true)
                    continue;
                //check if the folder is empty
                FilesResource.ListRequest listRequest = svc.Files.List();
                listRequest.Spaces = "drive";
                listRequest.Fields = "items(id)";
                //Query all items with parent equals current folder
                listRequest.Q = "'" + parent.Id + "' in parents";

                //delete recursively the current folder
                FileList listFiles = listRequest.Execute();
                if (listFiles.Items.Count == 0)
                    DeleteFilePath(initializer, parent.Id);
            }

            return sDeletedFile;
        }

        public FileList GetFileList(Initializer initializer)
        {
            DriveService svc = new DriveService(CreateInitializer(initializer));

            FilesResource.ListRequest list = svc.Files.List();
            FileList listResponse = list.Execute();

            return listResponse;
        }

        public Permission SetFileOwner(Initializer initializer, string fileId, string role, string userEmail, bool setParent)
        {
            if (String.IsNullOrEmpty(fileId))
                return null;

            DriveService svc = new DriveService(CreateInitializer(initializer));
            Permission permission = new Permission();
            permission.Type = "user";
            permission.Role = role;
            permission.Value = userEmail;
            var request = svc.Permissions.Insert(permission, fileId);
            request.Fields = "id";
            var perm = request.Execute();

            //find file parents
            File file = svc.Files.Get(fileId).Execute();

            //recursively set parents permissions
            if (setParent)
            {
                foreach (ParentReference parent in file.Parents)
                {
                    if (parent.IsRoot == null || parent.IsRoot == true)
                        continue;
                    SetFileOwner(initializer, parent.Id, role, userEmail, setParent);
                }
            }

            return perm;
        }

        public Thread GetThread(Initializer initializer, string userId, string threadId)
        {
            if (String.IsNullOrEmpty(userId) || String.IsNullOrEmpty(threadId))
                return null;

            GmailService svc = new GmailService(CreateInitializer(initializer));

            return svc.Users.Threads.Get(userId, threadId).Execute();
        }

        public ListThreadsResponse GetThreadList(Initializer initializer, string userId)
        {
            if (String.IsNullOrEmpty(userId))
                return null;

            GmailService svc = new GmailService(CreateInitializer(initializer));

            UsersResource.ThreadsResource.ListRequest lRequest = svc.Users.Threads.List(userId);
            return lRequest.Execute();
        }

        public Message GetMessage(Initializer initializer, string userId, string messageId, UsersResource.MessagesResource.GetRequest.FormatEnum format)
        {
            GmailService svc = new GmailService(CreateInitializer(initializer));

            var getRequest = svc.Users.Messages.Get(userId, messageId);
            getRequest.Format = format;
            return getRequest.Execute();
        }

        public MessagePartBody GetMessageAttachment(Initializer initializer, string userId, string messageId, string attachmentId)
        {
            GmailService svc = new GmailService(CreateInitializer(initializer));

            MessagePartBody attachPart = svc.Users.Messages.Attachments.Get(userId, messageId, attachmentId).Execute();
            return attachPart;
        }

        //public List<byte[]> GetMessageAttachments(Initializer initializer, string userId, string messageId)
        //{
        //    GmailService svc = new GmailService(CreateInitializer(initializer));

        //    List<byte[]> attachments = new List<byte[]>();

        //    Message message = svc.Users.Messages.Get(userId, messageId).Execute();
        //    IList<MessagePart> parts = message.Payload.Parts;
        //    foreach (MessagePart part in parts)
        //    {
        //        if (!String.IsNullOrEmpty(part.Filename))
        //        {
        //            String attId = part.Body.AttachmentId;
        //            MessagePartBody attachPart = svc.Users.Messages.Attachments.Get(userId, messageId, attId).Execute();

        //            // Converting from RFC 4648 base64-encoding
        //            // see http://en.wikipedia.org/wiki/Base64#Implementations_and_history
        //            String attachData = attachPart.Data.Replace('-', '+');
        //            attachData = attachData.Replace('_', '/');

        //            byte[] data = Convert.FromBase64String(attachData);
        //            attachments.Add(data);
        //        }
        //    }
        //    return attachments;
        //}

        public Message SendMessage(Initializer initializer, EmailMessage eMessage, string userId)
        {
            GmailService svc = new GmailService(CreateInitializer(initializer));
            System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();

            if (eMessage.To == null)
                return null;

            foreach (string emailTo in eMessage.To)
            {
                if (string.IsNullOrEmpty(emailTo))
                    continue;

                message.To.Add(new System.Net.Mail.MailAddress(emailTo));
            }
            if (eMessage.CC != null)
            {
                foreach (string emailCC in eMessage.CC)
                {
                    if (string.IsNullOrEmpty(emailCC))
                        continue;

                    message.CC.Add(new System.Net.Mail.MailAddress(emailCC));
                }
            }
            message.Subject = eMessage.Subject;
            message.Body = eMessage.Body;
            message.IsBodyHtml = eMessage.IsBodyHtml;
            //reply To
            if (eMessage.ReplyToList != null && eMessage.ReplyToList.Length > 0)
            {
                foreach (string replyTo in eMessage.ReplyToList)
                {
                    message.ReplyToList.Add(replyTo);
                }
            }
            //attachments
            if (eMessage.AttachmentsFilePath != null)
            {
                foreach (string path in eMessage.AttachmentsFilePath)
                {
                    if (string.IsNullOrEmpty(path))
                        continue;

                    System.Net.Mail.Attachment attachment = new System.Net.Mail.Attachment(path);
                    message.Attachments.Add(attachment);
                }
            }
            MimeKit.MimeMessage MimeMessage = MimeKit.MimeMessage.CreateFromMailMessage(message);
            Message googleMessage = new Message();
            googleMessage.Raw = Base64UrlEncode(MimeMessage.ToString());
            googleMessage.ThreadId = eMessage.ThreadId;
            Message returnMsg = svc.Users.Messages.Send(googleMessage, userId).Execute();

            return returnMsg;
        }

        public Message ForwardMessage(Initializer initializer, EmailMessage eMessage, string messageId, string userId)
        {
            Message googleMessage = this.GetMessage(initializer, userId, messageId, UsersResource.MessagesResource.GetRequest.FormatEnum.Raw);

            //Decode Base64url
            string incoming = googleMessage.Raw.Replace('_', '/').Replace('-', '+');
            switch (incoming.Length % 4)
            {
                case 2: incoming += "=="; break;
                case 3: incoming += "="; break;
            }
            byte[] bytes = Convert.FromBase64String(incoming);
            var mimeMessage = MimeKit.MimeMessage.Load(new System.IO.MemoryStream(bytes));
            //string originalText = Encoding.ASCII.GetString(bytes);

            mimeMessage.Headers["to"] = "";
            foreach (string emailTo in eMessage.To)
            {
                if (string.IsNullOrEmpty(emailTo))
                    continue;

                mimeMessage.Headers["to"] += emailTo;
            }
            mimeMessage.Subject = "Fwd: " + mimeMessage.Subject;

            GmailService svc = new GmailService(CreateInitializer(initializer));
            Message newGoogleMessage = new Message();
            newGoogleMessage.Raw = Base64UrlEncode(mimeMessage.ToString());
            newGoogleMessage.ThreadId = eMessage.ThreadId;

            return svc.Users.Messages.Send(newGoogleMessage, userId).Execute();
        }

        public BaseClientService.Initializer CreateInitializer(Initializer initializer)
        {
            var certificate = new X509Certificate2(initializer.CertificateFileBytes, initializer.CertificatePassword, X509KeyStorageFlags.Exportable);

            var credential = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(initializer.ServiceAccountEmail)
            {
                User = initializer.User,
                Scopes = initializer.Scopes
            }.FromCertificate(certificate));

            BaseClientService.Initializer init = new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = initializer.ApplicationName
            };

            return init;
        }

        public GoogleScope GetGoogleScope()
        {
            return new GoogleScope();
        }

        private static string Base64UrlEncode(string input)
        {
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            // Special "url-safe" base64 encode.
            return Convert.ToBase64String(inputBytes)
              .Replace('+', '-')
              .Replace('/', '_')
              .Replace("=", "");
        }

        private static Dictionary<string, string> _mappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {

        #region list of mime types
        // combination of values from Windows 7 Registry and 
        // from C:\Windows\System32\inetsrv\config\applicationHost.config
        // some added, including .7z and .dat
        {".323", "text/h323"},
        {".3g2", "video/3gpp2"},
        {".3gp", "video/3gpp"},
        {".3gp2", "video/3gpp2"},
        {".3gpp", "video/3gpp"},
        {".7z", "application/x-7z-compressed"},
        {".aa", "audio/audible"},
        {".AAC", "audio/aac"},
        {".aaf", "application/octet-stream"},
        {".aax", "audio/vnd.audible.aax"},
        {".ac3", "audio/ac3"},
        {".aca", "application/octet-stream"},
        {".accda", "application/msaccess.addin"},
        {".accdb", "application/msaccess"},
        {".accdc", "application/msaccess.cab"},
        {".accde", "application/msaccess"},
        {".accdr", "application/msaccess.runtime"},
        {".accdt", "application/msaccess"},
        {".accdw", "application/msaccess.webapplication"},
        {".accft", "application/msaccess.ftemplate"},
        {".acx", "application/internet-property-stream"},
        {".AddIn", "text/xml"},
        {".ade", "application/msaccess"},
        {".adobebridge", "application/x-bridge-url"},
        {".adp", "application/msaccess"},
        {".ADT", "audio/vnd.dlna.adts"},
        {".ADTS", "audio/aac"},
        {".afm", "application/octet-stream"},
        {".ai", "application/postscript"},
        {".aif", "audio/x-aiff"},
        {".aifc", "audio/aiff"},
        {".aiff", "audio/aiff"},
        {".air", "application/vnd.adobe.air-application-installer-package+zip"},
        {".amc", "application/x-mpeg"},
        {".application", "application/x-ms-application"},
        {".art", "image/x-jg"},
        {".asa", "application/xml"},
        {".asax", "application/xml"},
        {".ascx", "application/xml"},
        {".asd", "application/octet-stream"},
        {".asf", "video/x-ms-asf"},
        {".ashx", "application/xml"},
        {".asi", "application/octet-stream"},
        {".asm", "text/plain"},
        {".asmx", "application/xml"},
        {".aspx", "application/xml"},
        {".asr", "video/x-ms-asf"},
        {".asx", "video/x-ms-asf"},
        {".atom", "application/atom+xml"},
        {".au", "audio/basic"},
        {".avi", "video/x-msvideo"},
        {".axs", "application/olescript"},
        {".bas", "text/plain"},
        {".bcpio", "application/x-bcpio"},
        {".bin", "application/octet-stream"},
        {".bmp", "image/bmp"},
        {".c", "text/plain"},
        {".cab", "application/octet-stream"},
        {".caf", "audio/x-caf"},
        {".calx", "application/vnd.ms-office.calx"},
        {".cat", "application/vnd.ms-pki.seccat"},
        {".cc", "text/plain"},
        {".cd", "text/plain"},
        {".cdda", "audio/aiff"},
        {".cdf", "application/x-cdf"},
        {".cer", "application/x-x509-ca-cert"},
        {".chm", "application/octet-stream"},
        {".class", "application/x-java-applet"},
        {".clp", "application/x-msclip"},
        {".cmx", "image/x-cmx"},
        {".cnf", "text/plain"},
        {".cod", "image/cis-cod"},
        {".config", "application/xml"},
        {".contact", "text/x-ms-contact"},
        {".coverage", "application/xml"},
        {".cpio", "application/x-cpio"},
        {".cpp", "text/plain"},
        {".crd", "application/x-mscardfile"},
        {".crl", "application/pkix-crl"},
        {".crt", "application/x-x509-ca-cert"},
        {".cs", "text/plain"},
        {".csdproj", "text/plain"},
        {".csh", "application/x-csh"},
        {".csproj", "text/plain"},
        {".css", "text/css"},
        {".csv", "text/csv"},
        {".cur", "application/octet-stream"},
        {".cxx", "text/plain"},
        {".dat", "application/octet-stream"},
        {".datasource", "application/xml"},
        {".dbproj", "text/plain"},
        {".dcr", "application/x-director"},
        {".def", "text/plain"},
        {".deploy", "application/octet-stream"},
        {".der", "application/x-x509-ca-cert"},
        {".dgml", "application/xml"},
        {".dib", "image/bmp"},
        {".dif", "video/x-dv"},
        {".dir", "application/x-director"},
        {".disco", "text/xml"},
        {".dll", "application/x-msdownload"},
        {".dll.config", "text/xml"},
        {".dlm", "text/dlm"},
        {".doc", "application/msword"},
        {".docm", "application/vnd.ms-word.document.macroEnabled.12"},
        {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
        {".dot", "application/msword"},
        {".dotm", "application/vnd.ms-word.template.macroEnabled.12"},
        {".dotx", "application/vnd.openxmlformats-officedocument.wordprocessingml.template"},
        {".dsp", "application/octet-stream"},
        {".dsw", "text/plain"},
        {".dtd", "text/xml"},
        {".dtsConfig", "text/xml"},
        {".dv", "video/x-dv"},
        {".dvi", "application/x-dvi"},
        {".dwf", "drawing/x-dwf"},
        {".dwp", "application/octet-stream"},
        {".dxr", "application/x-director"},
        {".eml", "message/rfc822"},
        {".emz", "application/octet-stream"},
        {".eot", "application/octet-stream"},
        {".eps", "application/postscript"},
        {".etl", "application/etl"},
        {".etx", "text/x-setext"},
        {".evy", "application/envoy"},
        {".exe", "application/octet-stream"},
        {".exe.config", "text/xml"},
        {".fdf", "application/vnd.fdf"},
        {".fif", "application/fractals"},
        {".filters", "Application/xml"},
        {".fla", "application/octet-stream"},
        {".flr", "x-world/x-vrml"},
        {".flv", "video/x-flv"},
        {".fsscript", "application/fsharp-script"},
        {".fsx", "application/fsharp-script"},
        {".generictest", "application/xml"},
        {".gif", "image/gif"},
        {".group", "text/x-ms-group"},
        {".gsm", "audio/x-gsm"},
        {".gtar", "application/x-gtar"},
        {".gz", "application/x-gzip"},
        {".h", "text/plain"},
        {".hdf", "application/x-hdf"},
        {".hdml", "text/x-hdml"},
        {".hhc", "application/x-oleobject"},
        {".hhk", "application/octet-stream"},
        {".hhp", "application/octet-stream"},
        {".hlp", "application/winhlp"},
        {".hpp", "text/plain"},
        {".hqx", "application/mac-binhex40"},
        {".hta", "application/hta"},
        {".htc", "text/x-component"},
        {".htm", "text/html"},
        {".html", "text/html"},
        {".htt", "text/webviewhtml"},
        {".hxa", "application/xml"},
        {".hxc", "application/xml"},
        {".hxd", "application/octet-stream"},
        {".hxe", "application/xml"},
        {".hxf", "application/xml"},
        {".hxh", "application/octet-stream"},
        {".hxi", "application/octet-stream"},
        {".hxk", "application/xml"},
        {".hxq", "application/octet-stream"},
        {".hxr", "application/octet-stream"},
        {".hxs", "application/octet-stream"},
        {".hxt", "text/html"},
        {".hxv", "application/xml"},
        {".hxw", "application/octet-stream"},
        {".hxx", "text/plain"},
        {".i", "text/plain"},
        {".ico", "image/x-icon"},
        {".ics", "application/octet-stream"},
        {".idl", "text/plain"},
        {".ief", "image/ief"},
        {".iii", "application/x-iphone"},
        {".inc", "text/plain"},
        {".inf", "application/octet-stream"},
        {".inl", "text/plain"},
        {".ins", "application/x-internet-signup"},
        {".ipa", "application/x-itunes-ipa"},
        {".ipg", "application/x-itunes-ipg"},
        {".ipproj", "text/plain"},
        {".ipsw", "application/x-itunes-ipsw"},
        {".iqy", "text/x-ms-iqy"},
        {".isp", "application/x-internet-signup"},
        {".ite", "application/x-itunes-ite"},
        {".itlp", "application/x-itunes-itlp"},
        {".itms", "application/x-itunes-itms"},
        {".itpc", "application/x-itunes-itpc"},
        {".IVF", "video/x-ivf"},
        {".jar", "application/java-archive"},
        {".java", "application/octet-stream"},
        {".jck", "application/liquidmotion"},
        {".jcz", "application/liquidmotion"},
        {".jfif", "image/pjpeg"},
        {".jnlp", "application/x-java-jnlp-file"},
        {".jpb", "application/octet-stream"},
        {".jpe", "image/jpeg"},
        {".jpeg", "image/jpeg"},
        {".jpg", "image/jpeg"},
        {".js", "application/x-javascript"},
        {".json", "application/json"},
        {".jsx", "text/jscript"},
        {".jsxbin", "text/plain"},
        {".latex", "application/x-latex"},
        {".library-ms", "application/windows-library+xml"},
        {".lit", "application/x-ms-reader"},
        {".loadtest", "application/xml"},
        {".lpk", "application/octet-stream"},
        {".lsf", "video/x-la-asf"},
        {".lst", "text/plain"},
        {".lsx", "video/x-la-asf"},
        {".lzh", "application/octet-stream"},
        {".m13", "application/x-msmediaview"},
        {".m14", "application/x-msmediaview"},
        {".m1v", "video/mpeg"},
        {".m2t", "video/vnd.dlna.mpeg-tts"},
        {".m2ts", "video/vnd.dlna.mpeg-tts"},
        {".m2v", "video/mpeg"},
        {".m3u", "audio/x-mpegurl"},
        {".m3u8", "audio/x-mpegurl"},
        {".m4a", "audio/m4a"},
        {".m4b", "audio/m4b"},
        {".m4p", "audio/m4p"},
        {".m4r", "audio/x-m4r"},
        {".m4v", "video/x-m4v"},
        {".mac", "image/x-macpaint"},
        {".mak", "text/plain"},
        {".man", "application/x-troff-man"},
        {".manifest", "application/x-ms-manifest"},
        {".map", "text/plain"},
        {".master", "application/xml"},
        {".mda", "application/msaccess"},
        {".mdb", "application/x-msaccess"},
        {".mde", "application/msaccess"},
        {".mdp", "application/octet-stream"},
        {".me", "application/x-troff-me"},
        {".mfp", "application/x-shockwave-flash"},
        {".mht", "message/rfc822"},
        {".mhtml", "message/rfc822"},
        {".mid", "audio/mid"},
        {".midi", "audio/mid"},
        {".mix", "application/octet-stream"},
        {".mk", "text/plain"},
        {".mmf", "application/x-smaf"},
        {".mno", "text/xml"},
        {".mny", "application/x-msmoney"},
        {".mod", "video/mpeg"},
        {".mov", "video/quicktime"},
        {".movie", "video/x-sgi-movie"},
        {".mp2", "video/mpeg"},
        {".mp2v", "video/mpeg"},
        {".mp3", "audio/mpeg"},
        {".mp4", "video/mp4"},
        {".mp4v", "video/mp4"},
        {".mpa", "video/mpeg"},
        {".mpe", "video/mpeg"},
        {".mpeg", "video/mpeg"},
        {".mpf", "application/vnd.ms-mediapackage"},
        {".mpg", "video/mpeg"},
        {".mpp", "application/vnd.ms-project"},
        {".mpv2", "video/mpeg"},
        {".mqv", "video/quicktime"},
        {".ms", "application/x-troff-ms"},
        {".msi", "application/octet-stream"},
        {".mso", "application/octet-stream"},
        {".mts", "video/vnd.dlna.mpeg-tts"},
        {".mtx", "application/xml"},
        {".mvb", "application/x-msmediaview"},
        {".mvc", "application/x-miva-compiled"},
        {".mxp", "application/x-mmxp"},
        {".nc", "application/x-netcdf"},
        {".nsc", "video/x-ms-asf"},
        {".nws", "message/rfc822"},
        {".ocx", "application/octet-stream"},
        {".oda", "application/oda"},
        {".odc", "text/x-ms-odc"},
        {".odh", "text/plain"},
        {".odl", "text/plain"},
        {".odp", "application/vnd.oasis.opendocument.presentation"},
        {".ods", "application/oleobject"},
        {".odt", "application/vnd.oasis.opendocument.text"},
        {".one", "application/onenote"},
        {".onea", "application/onenote"},
        {".onepkg", "application/onenote"},
        {".onetmp", "application/onenote"},
        {".onetoc", "application/onenote"},
        {".onetoc2", "application/onenote"},
        {".orderedtest", "application/xml"},
        {".osdx", "application/opensearchdescription+xml"},
        {".p10", "application/pkcs10"},
        {".p12", "application/x-pkcs12"},
        {".p7b", "application/x-pkcs7-certificates"},
        {".p7c", "application/pkcs7-mime"},
        {".p7m", "application/pkcs7-mime"},
        {".p7r", "application/x-pkcs7-certreqresp"},
        {".p7s", "application/pkcs7-signature"},
        {".pbm", "image/x-portable-bitmap"},
        {".pcast", "application/x-podcast"},
        {".pct", "image/pict"},
        {".pcx", "application/octet-stream"},
        {".pcz", "application/octet-stream"},
        {".pdf", "application/pdf"},
        {".pfb", "application/octet-stream"},
        {".pfm", "application/octet-stream"},
        {".pfx", "application/x-pkcs12"},
        {".pgm", "image/x-portable-graymap"},
        {".pic", "image/pict"},
        {".pict", "image/pict"},
        {".pkgdef", "text/plain"},
        {".pkgundef", "text/plain"},
        {".pko", "application/vnd.ms-pki.pko"},
        {".pls", "audio/scpls"},
        {".pma", "application/x-perfmon"},
        {".pmc", "application/x-perfmon"},
        {".pml", "application/x-perfmon"},
        {".pmr", "application/x-perfmon"},
        {".pmw", "application/x-perfmon"},
        {".png", "image/png"},
        {".pnm", "image/x-portable-anymap"},
        {".pnt", "image/x-macpaint"},
        {".pntg", "image/x-macpaint"},
        {".pnz", "image/png"},
        {".pot", "application/vnd.ms-powerpoint"},
        {".potm", "application/vnd.ms-powerpoint.template.macroEnabled.12"},
        {".potx", "application/vnd.openxmlformats-officedocument.presentationml.template"},
        {".ppa", "application/vnd.ms-powerpoint"},
        {".ppam", "application/vnd.ms-powerpoint.addin.macroEnabled.12"},
        {".ppm", "image/x-portable-pixmap"},
        {".pps", "application/vnd.ms-powerpoint"},
        {".ppsm", "application/vnd.ms-powerpoint.slideshow.macroEnabled.12"},
        {".ppsx", "application/vnd.openxmlformats-officedocument.presentationml.slideshow"},
        {".ppt", "application/vnd.ms-powerpoint"},
        {".pptm", "application/vnd.ms-powerpoint.presentation.macroEnabled.12"},
        {".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"},
        {".prf", "application/pics-rules"},
        {".prm", "application/octet-stream"},
        {".prx", "application/octet-stream"},
        {".ps", "application/postscript"},
        {".psc1", "application/PowerShell"},
        {".psd", "application/octet-stream"},
        {".psess", "application/xml"},
        {".psm", "application/octet-stream"},
        {".psp", "application/octet-stream"},
        {".pub", "application/x-mspublisher"},
        {".pwz", "application/vnd.ms-powerpoint"},
        {".qht", "text/x-html-insertion"},
        {".qhtm", "text/x-html-insertion"},
        {".qt", "video/quicktime"},
        {".qti", "image/x-quicktime"},
        {".qtif", "image/x-quicktime"},
        {".qtl", "application/x-quicktimeplayer"},
        {".qxd", "application/octet-stream"},
        {".ra", "audio/x-pn-realaudio"},
        {".ram", "audio/x-pn-realaudio"},
        {".rar", "application/octet-stream"},
        {".ras", "image/x-cmu-raster"},
        {".rat", "application/rat-file"},
        {".rc", "text/plain"},
        {".rc2", "text/plain"},
        {".rct", "text/plain"},
        {".rdlc", "application/xml"},
        {".resx", "application/xml"},
        {".rf", "image/vnd.rn-realflash"},
        {".rgb", "image/x-rgb"},
        {".rgs", "text/plain"},
        {".rm", "application/vnd.rn-realmedia"},
        {".rmi", "audio/mid"},
        {".rmp", "application/vnd.rn-rn_music_package"},
        {".roff", "application/x-troff"},
        {".rpm", "audio/x-pn-realaudio-plugin"},
        {".rqy", "text/x-ms-rqy"},
        {".rtf", "application/rtf"},
        {".rtx", "text/richtext"},
        {".ruleset", "application/xml"},
        {".s", "text/plain"},
        {".safariextz", "application/x-safari-safariextz"},
        {".scd", "application/x-msschedule"},
        {".sct", "text/scriptlet"},
        {".sd2", "audio/x-sd2"},
        {".sdp", "application/sdp"},
        {".sea", "application/octet-stream"},
        {".searchConnector-ms", "application/windows-search-connector+xml"},
        {".setpay", "application/set-payment-initiation"},
        {".setreg", "application/set-registration-initiation"},
        {".settings", "application/xml"},
        {".sgimb", "application/x-sgimb"},
        {".sgml", "text/sgml"},
        {".sh", "application/x-sh"},
        {".shar", "application/x-shar"},
        {".shtml", "text/html"},
        {".sit", "application/x-stuffit"},
        {".sitemap", "application/xml"},
        {".skin", "application/xml"},
        {".sldm", "application/vnd.ms-powerpoint.slide.macroEnabled.12"},
        {".sldx", "application/vnd.openxmlformats-officedocument.presentationml.slide"},
        {".slk", "application/vnd.ms-excel"},
        {".sln", "text/plain"},
        {".slupkg-ms", "application/x-ms-license"},
        {".smd", "audio/x-smd"},
        {".smi", "application/octet-stream"},
        {".smx", "audio/x-smd"},
        {".smz", "audio/x-smd"},
        {".snd", "audio/basic"},
        {".snippet", "application/xml"},
        {".snp", "application/octet-stream"},
        {".sol", "text/plain"},
        {".sor", "text/plain"},
        {".spc", "application/x-pkcs7-certificates"},
        {".spl", "application/futuresplash"},
        {".src", "application/x-wais-source"},
        {".srf", "text/plain"},
        {".SSISDeploymentManifest", "text/xml"},
        {".ssm", "application/streamingmedia"},
        {".sst", "application/vnd.ms-pki.certstore"},
        {".stl", "application/vnd.ms-pki.stl"},
        {".sv4cpio", "application/x-sv4cpio"},
        {".sv4crc", "application/x-sv4crc"},
        {".svc", "application/xml"},
        {".swf", "application/x-shockwave-flash"},
        {".t", "application/x-troff"},
        {".tar", "application/x-tar"},
        {".tcl", "application/x-tcl"},
        {".testrunconfig", "application/xml"},
        {".testsettings", "application/xml"},
        {".tex", "application/x-tex"},
        {".texi", "application/x-texinfo"},
        {".texinfo", "application/x-texinfo"},
        {".tgz", "application/x-compressed"},
        {".thmx", "application/vnd.ms-officetheme"},
        {".thn", "application/octet-stream"},
        {".tif", "image/tiff"},
        {".tiff", "image/tiff"},
        {".tlh", "text/plain"},
        {".tli", "text/plain"},
        {".toc", "application/octet-stream"},
        {".tr", "application/x-troff"},
        {".trm", "application/x-msterminal"},
        {".trx", "application/xml"},
        {".ts", "video/vnd.dlna.mpeg-tts"},
        {".tsv", "text/tab-separated-values"},
        {".ttf", "application/octet-stream"},
        {".tts", "video/vnd.dlna.mpeg-tts"},
        {".txt", "text/plain"},
        {".u32", "application/octet-stream"},
        {".uls", "text/iuls"},
        {".user", "text/plain"},
        {".ustar", "application/x-ustar"},
        {".vb", "text/plain"},
        {".vbdproj", "text/plain"},
        {".vbk", "video/mpeg"},
        {".vbproj", "text/plain"},
        {".vbs", "text/vbscript"},
        {".vcf", "text/x-vcard"},
        {".vcproj", "Application/xml"},
        {".vcs", "text/plain"},
        {".vcxproj", "Application/xml"},
        {".vddproj", "text/plain"},
        {".vdp", "text/plain"},
        {".vdproj", "text/plain"},
        {".vdx", "application/vnd.ms-visio.viewer"},
        {".vml", "text/xml"},
        {".vscontent", "application/xml"},
        {".vsct", "text/xml"},
        {".vsd", "application/vnd.visio"},
        {".vsi", "application/ms-vsi"},
        {".vsix", "application/vsix"},
        {".vsixlangpack", "text/xml"},
        {".vsixmanifest", "text/xml"},
        {".vsmdi", "application/xml"},
        {".vspscc", "text/plain"},
        {".vss", "application/vnd.visio"},
        {".vsscc", "text/plain"},
        {".vssettings", "text/xml"},
        {".vssscc", "text/plain"},
        {".vst", "application/vnd.visio"},
        {".vstemplate", "text/xml"},
        {".vsto", "application/x-ms-vsto"},
        {".vsw", "application/vnd.visio"},
        {".vsx", "application/vnd.visio"},
        {".vtx", "application/vnd.visio"},
        {".wav", "audio/wav"},
        {".wave", "audio/wav"},
        {".wax", "audio/x-ms-wax"},
        {".wbk", "application/msword"},
        {".wbmp", "image/vnd.wap.wbmp"},
        {".wcm", "application/vnd.ms-works"},
        {".wdb", "application/vnd.ms-works"},
        {".wdp", "image/vnd.ms-photo"},
        {".webarchive", "application/x-safari-webarchive"},
        {".webtest", "application/xml"},
        {".wiq", "application/xml"},
        {".wiz", "application/msword"},
        {".wks", "application/vnd.ms-works"},
        {".WLMP", "application/wlmoviemaker"},
        {".wlpginstall", "application/x-wlpg-detect"},
        {".wlpginstall3", "application/x-wlpg3-detect"},
        {".wm", "video/x-ms-wm"},
        {".wma", "audio/x-ms-wma"},
        {".wmd", "application/x-ms-wmd"},
        {".wmf", "application/x-msmetafile"},
        {".wml", "text/vnd.wap.wml"},
        {".wmlc", "application/vnd.wap.wmlc"},
        {".wmls", "text/vnd.wap.wmlscript"},
        {".wmlsc", "application/vnd.wap.wmlscriptc"},
        {".wmp", "video/x-ms-wmp"},
        {".wmv", "video/x-ms-wmv"},
        {".wmx", "video/x-ms-wmx"},
        {".wmz", "application/x-ms-wmz"},
        {".wpl", "application/vnd.ms-wpl"},
        {".wps", "application/vnd.ms-works"},
        {".wri", "application/x-mswrite"},
        {".wrl", "x-world/x-vrml"},
        {".wrz", "x-world/x-vrml"},
        {".wsc", "text/scriptlet"},
        {".wsdl", "text/xml"},
        {".wvx", "video/x-ms-wvx"},
        {".x", "application/directx"},
        {".xaf", "x-world/x-vrml"},
        {".xaml", "application/xaml+xml"},
        {".xap", "application/x-silverlight-app"},
        {".xbap", "application/x-ms-xbap"},
        {".xbm", "image/x-xbitmap"},
        {".xdr", "text/plain"},
        {".xht", "application/xhtml+xml"},
        {".xhtml", "application/xhtml+xml"},
        {".xla", "application/vnd.ms-excel"},
        {".xlam", "application/vnd.ms-excel.addin.macroEnabled.12"},
        {".xlc", "application/vnd.ms-excel"},
        {".xld", "application/vnd.ms-excel"},
        {".xlk", "application/vnd.ms-excel"},
        {".xll", "application/vnd.ms-excel"},
        {".xlm", "application/vnd.ms-excel"},
        {".xls", "application/vnd.ms-excel"},
        {".xlsb", "application/vnd.ms-excel.sheet.binary.macroEnabled.12"},
        {".xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12"},
        {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
        {".xlt", "application/vnd.ms-excel"},
        {".xltm", "application/vnd.ms-excel.template.macroEnabled.12"},
        {".xltx", "application/vnd.openxmlformats-officedocument.spreadsheetml.template"},
        {".xlw", "application/vnd.ms-excel"},
        {".xml", "text/xml"},
        {".xmta", "application/xml"},
        {".xof", "x-world/x-vrml"},
        {".XOML", "text/plain"},
        {".xpm", "image/x-xpixmap"},
        {".xps", "application/vnd.ms-xpsdocument"},
        {".xrm-ms", "text/xml"},
        {".xsc", "application/xml"},
        {".xsd", "text/xml"},
        {".xsf", "text/xml"},
        {".xsl", "text/xml"},
        {".xslt", "text/xml"},
        {".xsn", "application/octet-stream"},
        {".xss", "application/xml"},
        {".xtp", "application/octet-stream"},
        {".xwd", "image/x-xwindowdump"},
        {".z", "application/x-compress"},
        {".zip", "application/x-zip-compressed"},
        #endregion

        };

        public string GetMimeType(string extension)
        {
            if (extension == null)
            {
                throw new ArgumentNullException("extension");
            }

            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            string mime;

            return _mappings.TryGetValue(extension, out mime) ? mime : "application/octet-stream";
        }
    }
}