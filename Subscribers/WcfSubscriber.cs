using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.ServiceModel;
using Newtonsoft.Json;
using System.ServiceModel.Web;
using System.ServiceModel.Channels;

public interface IWcfSubscriber
{
    [OperationContract]
    [WebInvoke(
            Method = "POST",
            BodyStyle = WebMessageBodyStyle.Bare,
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/Update")]
    void Update(Stream data);
}

public class WcfSubscriber : IWcfSubscriber
{
    public void Update(Stream data)
    {
        StreamReader sr = new StreamReader(data);
        string input = sr.ReadToEnd();
        sr.Dispose();

        var snsMessage = JsonConvert.DeserializeObject<SnsMessage>(input);
        if (VerifySnsMessage(snsMessage))
        {
            switch (snsMessage.Type)
            {
                case "SubscriptionConfirmation":
                    GetHttpResponse(snsMessage.SubscribeURL);
                    break;

                case "UnsubscribeConfirmation":
                    GetHttpResponse(snsMessage.UnsubscribeURL);
                    break;

                case "Notification":
                    //Process snsMessage.Message
                    break;

                default:
                    break;
            }
        }
    }

    public string GetSnsMessageTypeFromHeader()
    {
        string messageType = null;
        var messageProperties = OperationContext.Current.RequestContext.RequestMessage.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
        if (messageProperties != null)
            messageType = messageProperties.Headers["x-amz-sns-message-type"];
        return messageType;
    }

    public bool VerifySnsMessage(SnsMessage snsMessage)
    {
        var certificateBytes = GetHttpResponse(snsMessage.SigningCertURL);

        var certificate = new X509Certificate2(certificateBytes);
        var verifier = (RSACryptoServiceProvider)certificate.PublicKey.Key;

        var msgBytes = GetMessageBytesToSign(snsMessage);
        var signedBytes = Convert.FromBase64String(snsMessage.Signature);

        return verifier.VerifyData(msgBytes, CryptoConfig.MapNameToOID("SHA1"), signedBytes);
    }


    private byte[] GetHttpResponse(string url)
    {
        System.Net.HttpWebRequest httpRequest = System.Net.WebRequest.Create(url) as System.Net.HttpWebRequest;
        httpRequest.Method = "GET";
        using (System.Net.HttpWebResponse resp = (System.Net.HttpWebResponse)httpRequest.GetResponse())
        {
            var responseStream = resp.GetResponseStream();
            using (MemoryStream ms = new MemoryStream())
            {
                responseStream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }

    private byte[] GetMessageBytesToSign(SnsMessage msg)
    {
        string messageToSign = null;
        if (msg.Type == "Notification")
            messageToSign = BuildNotificationStringToSign(msg);
        else if (msg.Type == "SubscriptionConfirmation" || msg.Type == "UnsubscribeConfirmation")
            messageToSign = BuildSubscriptionStringToSign(msg);

        return Encoding.UTF8.GetBytes(messageToSign);
    }

    //Build the string to sign for Notification messages.
    public string BuildNotificationStringToSign(SnsMessage msg)
    {
        string stringToSign = null;

        //Build the string to sign from the values in the message.
        //Name and values separated by newline characters
        //The name value pairs are sorted by name 
        //in byte sort order.
        stringToSign = "Message\n";
        stringToSign += msg.Message + "\n";
        stringToSign += "MessageId\n";
        stringToSign += msg.MessageId + "\n";
        if (msg.Subject != null)
        {
            stringToSign += "Subject\n";
            stringToSign += msg.Subject + "\n";
        }
        stringToSign += "Timestamp\n";
        stringToSign += msg.Timestamp + "\n";
        stringToSign += "TopicArn\n";
        stringToSign += msg.TopicArn + "\n";
        stringToSign += "Type\n";
        stringToSign += msg.Type + "\n";
        return stringToSign;
    }

    //Build the string to sign for SubscriptionConfirmation 
    //and UnsubscribeConfirmation messages.
    public string BuildSubscriptionStringToSign(SnsMessage msg)
    {
        string stringToSign = null;
        //Build the string to sign from the values in the message.
        //Name and values separated by newline characters
        //The name value pairs are sorted by name 
        //in byte sort order.
        stringToSign = "Message\n";
        stringToSign += msg.Message + "\n";
        stringToSign += "MessageId\n";
        stringToSign += msg.MessageId + "\n";
        stringToSign += "SubscribeURL\n";
        stringToSign += msg.SubscribeURL + "\n";
        stringToSign += "Timestamp\n";
        stringToSign += msg.Timestamp + "\n";
        stringToSign += "Token\n";
        stringToSign += msg.Token + "\n";
        stringToSign += "TopicArn\n";
        stringToSign += msg.TopicArn + "\n";
        stringToSign += "Type\n";
        stringToSign += msg.Type + "\n";
        return stringToSign;
    }

    public class SnsMessage
    {
        public string Type { get; set; }
        public string MessageId { get; set; }
        public string Token { get; set; }
        public string TopicArn { get; set; }
        public string Message { get; set; }
        public string SubscribeURL { get; set; }
        public string Timestamp { get; set; }
        public string SignatureVersion { get; set; }
        public string Signature { get; set; }
        public string SigningCertURL { get; set; }
        public string Subject { get; set; }
        public string UnsubscribeURL { get; set; }
    }
}