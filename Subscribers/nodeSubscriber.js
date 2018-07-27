const express = require('express');
const bodyParser = require('body-parser');
const https = require('https');
const MessageValidator = require('sns-validator');
const validator = new MessageValidator();
const app = express();

app.use(function (req, res, next) {
    if (req.get('x-amz-sns-message-id')) {
        req.headers['content-type'] = 'application/json';
    }
    next();
});

app.use(bodyParser.urlencoded({ extended: false }));
app.use(bodyParser.json());

app.post('/update', (req, res) => {
    console.log(`Data received: ${JSON.stringify(req.body)}`);
    console.log(`messageid: ${req.headers["x-amz-sns-message-id"]}`);
    validator.validate(req.body, function(error){
        if(!error){
            if(req.headers["x-amz-sns-message-type"] === 'SubscriptionConfirmation'){
                https.get(subscribeUrl, (response) => {                    
                    res.send(response.statusCode, response.statusMessage);                          
                });
            }
            else{
                const data = req.body.Message;
                //Process data
                res.send(200);
            }
        }
        else{
            res.send(401, 'invalid message');
        }     
    });
});