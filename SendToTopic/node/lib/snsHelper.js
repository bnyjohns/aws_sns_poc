const config = require('../config.json');
const axios = require('axios').default;
const _ = require('lodash');
const AWS = require('aws-sdk');
const sns = new AWS.SNS(config.aws.clientConfiguration);

const snsConfig = {};

async function readSNSS3Config(){
    const snsConfigS3Url = config.aws.snsS3Config.url;
    const response = await axios.get(snsConfigS3Url, { method: 'GET' });
    return response.data;
}

async function createTopic(topicName){
    return await sns.createTopic({
                        Name : topicName,
                    }).promise();
}

async function createSubscriber(topicArn, subscriber){
    return await sns.subscribe({
                        TopicArn : topicArn,
                        Endpoint : subscriber.endpoint,
                        Protocol : subscriber.protocol
                    }, null).promise();
}

async function setTopicDisplayName(topicArn, topicName){
    await sns.setTopicAttributes({
        TopicArn: topicArn,
        AttributeName: 'DisplayName',
        AttributeValue: topicName
    }).promise();
}

async function checkIfTopicExists(topicName){
    let nextToken = null;
    do{
        const listTopicsResponse = await sns.listTopics({ NextToken: nextToken }).promise();
        for(const topic of listTopicsResponse.Topics){
            snsConfig[topicName] = {
                ...topic,
                subscribers: {}
            };
            const topicAttributesResponse = await sns.getTopicAttributes({TopicArn : topic.TopicArn}).promise();
            if(topicAttributesResponse.Attributes['DisplayName'] === topicName)
                return true;
        }        
        nextToken = listTopicsResponse.NextToken;
    }while(nextToken)
    
    return false;
}

async function initializeSNS(){    
    const snsS3Config = await readSNSS3Config();
    for(const topic of snsS3Config.topics){
        if(!await checkIfTopicExists(topic.topicName)){
            const createTopicResponse = await createTopic(topic.topicName);
            snsConfig[topic.topicName] = {
                ...createTopicResponse,
                subscribers : {}            
            };            
            await setTopicDisplayName(createTopicResponse.TopicArn, topic.topicName); 
        }       

        for(const subscriber of topic.subscribers){
            const subscriptionResponse = await createSubscriber(snsConfig[topic.topicName].TopicArn, subscriber);
            const subscriberKey = `${subscriber.protocol}_${subscriber.endpoint}`;
            snsConfig[topic.topicName].subscribers[subscriberKey] = {
                ...subscriptionResponse
            };
            console.log(`subscription ${subscriptionResponse.SubscriptionArn} created under topic: ${snsConfig[topic.topicName].TopicArn}`);
        }
    }
}

async function publishToTopic({topic, subject, message}){
    const topicArn = snsConfig[topic].TopicArn;
    const publishResponse = await sns.publish({
                                    TopicArn : topicArn,
                                    Subject: subject,
                                    Message: message
                                }).promise();
    return publishResponse.MessageId;
}

module.exports = {
    initializeSNS,
    publishToTopic
}