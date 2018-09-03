using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SNSSample1
{
    public class SnsPoc
    {
        AmazonSimpleNotificationServiceClient sns = null;
        public SnsPoc()
        {
            sns = new AmazonSimpleNotificationServiceClient();
        }

        public string CreateTopic(string topicName, string topicDisplayName)
        {
            string topicArn = null;
            try
            {
                Console.WriteLine("Creating topic...");
                topicArn = sns.CreateTopic(new CreateTopicRequest
                {
                    Name = topicName
                }).TopicArn;
                Console.WriteLine($"Topic created with Arn: {topicArn}");

                
                // Set display name to a friendly value
                Console.WriteLine();
                Console.WriteLine("Setting topic attributes...");
                sns.SetTopicAttributes(new SetTopicAttributesRequest
                {
                    TopicArn = topicArn,
                    AttributeName = "DisplayName",
                    AttributeValue = topicDisplayName
                });                
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
            return topicArn;
        }

        public void CreateSubscription(string topicArn, List<Subscriber> subscribers)
        {
            foreach (var subscriber in subscribers)
            {
                Console.WriteLine();
                Console.WriteLine($"Subscribing {subscriber.Value} to topic - {topicArn}");

                sns.Subscribe(new SubscribeRequest
                {
                    TopicArn = topicArn,
                    Protocol = subscriber.Protocol.ToString(),
                    Endpoint = subscriber.Value
                });
            }      
        }

        public void PublishToTopic(string topicArn, string subject, string message)
        {
            Console.WriteLine();
            Console.WriteLine($"Publishing message to topic - {topicArn}");
            sns.Publish(new PublishRequest
            {
                Subject = subject,
                Message = message,
                TopicArn = topicArn
            });
        }

        //public bool CheckIfTopicExists(string topicName)
        //{
        //    var listTopicsRequest = new ListTopicsRequest();
        //    ListTopicsResponse listTopicsResponse;
        //    do
        //    {
        //        listTopicsResponse = sns.ListTopics(listTopicsRequest);
        //        foreach (var topic in listTopicsResponse.Topics)
        //        {
        //            Console.WriteLine(" Topic: {0}", topic.TopicArn);
        //            // Get topic attributes
        //            var topicAttributes = sns.GetTopicAttributes(new GetTopicAttributesRequest
        //            {
        //                TopicArn = topic.TopicArn
        //            }).Attributes;
        //            if (topicAttributes.Count > 0)
        //            {
        //                Console.WriteLine(" Topic attributes");
        //                foreach (var topicAttribute in topicAttributes)
        //                {
        //                    Console.WriteLine(" -{0} : {1}", topicAttribute.Key, topicAttribute.Value);
        //                }
        //            }
        //            Console.WriteLine();
        //        }
        //        listTopicsRequest.NextToken = listTopicsResponse.NextToken;
        //    } while (listTopicsResponse.NextToken != null);
        //}

        public bool CheckIfSubscriptionExistsForTopic(string topicArn, string protocol, string endpoint)
        {
            var listSubscriptionsByTopicRequest = new ListSubscriptionsByTopicRequest(topicArn);
            ListSubscriptionsByTopicResponse response;
            do
            {
                response = sns.ListSubscriptionsByTopic(listSubscriptionsByTopicRequest);

                foreach (var subscription in response.Subscriptions)
                {
                    if (string.Equals(subscription.Endpoint, endpoint, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(subscription.Protocol, protocol, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                listSubscriptionsByTopicRequest.NextToken = response.NextToken;
            }
            while (response.NextToken != null);
            return false;
        }
    }

    public class Subscriber
    {
        public Protocol Protocol  { get; set; }
        public string Value { get; set; }
    }

    public enum Protocol
    {
        email,
        http,
        https
    }
}
