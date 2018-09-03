using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SNSSample1
{
    class Program
    {
        public static void Main()
        {
            var snsPoc = new SnsPoc();
            var topicArn = snsPoc.CreateTopic("boney_test_topic", "boney_test");
            var subscribers = new List<Subscriber> { new Subscriber { Protocol = Protocol.https,
                                                                        Value = "https://6af13e8f.ngrok.io/update" } };
            snsPoc.CreateSubscription(topicArn, subscribers);
            Thread.Sleep(2000);
            snsPoc.PublishToTopic(topicArn, "subjectFromHpPoc", "hello");
        }
    }
}
