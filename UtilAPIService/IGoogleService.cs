using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Net.Mail;

namespace UtilAPIService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IGoogleService" in both code and config file together.
    [ServiceContract]
    public interface IGoogleService
    {
        [OperationContract]
        GoogleScope GetGoogleScope();

        [OperationContract]
        System.IO.MemoryStream DownloadFile(Initializer initializer, string fileId);

        [OperationContract]
        File UploadFile(Initializer initializer, File file, System.IO.MemoryStream stream, String fileExtension);

        [OperationContract]
        File UploadFilePath(Initializer initializer, string filePath, System.IO.MemoryStream stream);

        [OperationContract]
        File DeleteFile(Initializer initializer, string fileId);

        [OperationContract]
        string DeleteFilePath(Initializer initializer, string fileId);

        [OperationContract]
        FileList GetFileList(Initializer initializer);

        [OperationContract]
        Permission SetFileOwner(Initializer initializer, string fileId, string role, string userEmail, bool setParent);

        [OperationContract]
        Thread GetThread(Initializer initializer, String userId, String threadId);

        [OperationContract]
        ListThreadsResponse GetThreadList(Initializer initializer, string userId);

        [OperationContract]
        Message GetMessage(Initializer initializer, string userId, string messageId, UsersResource.MessagesResource.GetRequest.FormatEnum format);

        [OperationContract]
        MessagePartBody GetMessageAttachment(Initializer initializer, string userId, string messageId, string attachmentId);

        [OperationContract]
        Message SendMessage(Initializer initializer, EmailMessage eMessage, string userId);

        [OperationContract]
        Message ForwardMessage(Initializer initializer, EmailMessage eMessage, string messageId, string userId);
    }

    [DataContract]
    public class EmailMessage
    {
        private string[] to;
        private string[] cc;
        private string from;
        private string[] replyToList;
        private string subject;
        private string body;
        private string[] attachmentsFilePath;
        private bool isBodyHtml;
        private string threadId;

        [DataMember]
        public string[] To
        {
            get { return to; }
            set { to = value; }
        }

        [DataMember]
        public string[] CC
        {
            get { return cc; }
            set { cc = value; }
        }

        [DataMember]
        public string From
        {
            get { return from; }
            set { from = value; }
        }

        [DataMember]
        public string[] ReplyToList
        {
            get { return replyToList; }
            set { replyToList = value; }
        }

        [DataMember]
        public string Subject
        {
            get { return subject; }
            set { subject = value; }
        }

        [DataMember]
        public string Body
        {
            get { return body; }
            set { body = value; }
        }

        [DataMember]
        public string[] AttachmentsFilePath
        {
            get { return attachmentsFilePath; }
            set { attachmentsFilePath = value; }
        }

        [DataMember]
        public bool IsBodyHtml
        {
            get { return isBodyHtml; }
            set { isBodyHtml = value; }
        }

        [DataMember]
        public string ThreadId
        {
            get { return threadId; }
            set { threadId = value; }
        }
    }

    [DataContract]
    public class Initializer
    {
        private byte[] certificateFileBytes;

        [DataMember]
        public byte[] CertificateFileBytes
        {
            get { return certificateFileBytes; }
            set { certificateFileBytes = value; }
        }

        private string certificatePassword;

        [DataMember]
        public string CertificatePassword
        {
            get { return certificatePassword; }
            set { certificatePassword = value; }
        }

        private string serviceAccountEmail;

        [DataMember]
        public string ServiceAccountEmail
        {
            get { return serviceAccountEmail; }
            set { serviceAccountEmail = value; }
        }

        private string user;

        [DataMember]
        public string User
        {
            get { return user; }
            set { user = value; }
        }

        private string[] scopes;

        [DataMember]
        public string[] Scopes
        {
            get { return scopes; }
            set { scopes = value; }
        }

        private string applicationName;

        [DataMember]
        public string ApplicationName
        {
            get { return applicationName; }
            set { applicationName = value; }
        }

        public Initializer(byte[] certificateFileBytes, string certificatePassword, string serviceAccountEmail, string user, string[] scopes, string applicationName)
        {
            this.certificateFileBytes = certificateFileBytes;
            this.certificatePassword = certificatePassword;
            this.serviceAccountEmail = serviceAccountEmail;
            this.user = user;
            this.scopes = scopes;
            this.applicationName = applicationName;
        }
    }

    [DataContract]
    public class GoogleScope
    {
        private string drive;

        [DataMember]
        public string Drive
        {
            get { return drive; }
            set { drive = value; }
        }

        private string driveAppdata;

        [DataMember]
        public string DriveAppdata
        {
            get { return driveAppdata; }
            set { driveAppdata = value; }
        }

        private string driveAppsReadonly;

        [DataMember]
        public string DriveAppsReadonly
        {
            get { return driveAppsReadonly; }
            set { driveAppsReadonly = value; }
        }

        private string driveFile;

        [DataMember]
        public string DriveFile
        {
            get { return driveFile; }
            set { driveFile = value; }
        }

        private string driveMetadata;

        [DataMember]
        public string DriveMetadata
        {
            get { return driveMetadata; }
            set { driveMetadata = value; }
        }

        private string driveMetadataReadonly;

        [DataMember]
        public string DriveMetadataReadonly
        {
            get { return driveMetadataReadonly; }
            set { driveMetadataReadonly = value; }
        }

        private string drivePhotosReadonly;

        [DataMember]
        public string DrivePhotosReadonly
        {
            get { return drivePhotosReadonly; }
            set { drivePhotosReadonly = value; }
        }

        private string driveReadonly;

        [DataMember]
        public string DriveReadonly
        {
            get { return driveReadonly; }
            set { driveReadonly = value; }
        }

        private string driveScripts;

        [DataMember]
        public string DriveScripts
        {
            get { return driveScripts; }
            set { driveScripts = value; }
        }

        private string gmailCompose;

        [DataMember]
        public string GmailCompose
        {
            get { return gmailCompose; }
            set { gmailCompose = value; }
        }

        private string gmailInsert;

        [DataMember]
        public string GmailInsert
        {
            get { return gmailInsert; }
            set { gmailInsert = value; }
        }

        private string gmailLabels;

        [DataMember]
        public string GmailLabels
        {
            get { return gmailLabels; }
            set { gmailLabels = value; }
        }

        private string gmailModify;

        [DataMember]
        public string GmailModify
        {
            get { return gmailModify; }
            set { gmailModify = value; }
        }

        private string gmailReadonly;

        [DataMember]
        public string GmailReadonly
        {
            get { return gmailReadonly; }
            set { gmailReadonly = value; }
        }

        private string gmailSend;

        [DataMember]
        public string GmailSend
        {
            get { return gmailSend; }
            set { gmailSend = value; }
        }

        private string mailGoogleCom;

        [DataMember]
        public string MailGoogleCom
        {
            get { return mailGoogleCom; }
            set { mailGoogleCom = value; }
        }

        public GoogleScope()
        {
            this.drive = DriveService.Scope.Drive;
            this.driveAppdata = DriveService.Scope.DriveAppdata;
            this.driveAppsReadonly = DriveService.Scope.DriveAppsReadonly;
            this.driveFile = DriveService.Scope.DriveFile;
            this.driveMetadata = DriveService.Scope.DriveMetadata;
            this.driveMetadataReadonly = DriveService.Scope.DriveMetadataReadonly;
            this.drivePhotosReadonly = DriveService.Scope.DrivePhotosReadonly;
            this.driveReadonly = DriveService.Scope.DriveReadonly;
            this.driveScripts = DriveService.Scope.DriveScripts;

            this.gmailCompose = GmailService.Scope.GmailCompose;
            this.gmailInsert = GmailService.Scope.GmailInsert;
            this.gmailLabels = GmailService.Scope.GmailLabels;
            this.gmailModify = GmailService.Scope.GmailModify;
            this.gmailReadonly = GmailService.Scope.GmailReadonly;
            this.gmailSend = GmailService.Scope.GmailSend;
            this.mailGoogleCom = GmailService.Scope.MailGoogleCom;
        }
    }
}
