const app = require('express')();
const bodyParser = require('body-parser');
const snsHelper = require('./lib/snsHelper');
const MessageValidator = require('sns-validator');
const validator = new MessageValidator();
const https = require('https');
const port = 2532;

snsHelper.initializeSNS()
.then((_) => {
    console.log(`sns initialization success`);
})
.catch(e => {
    console.log(e);
});

app.use(function (req, res, next) {
    if (req.get('x-amz-sns-message-id')) {
        req.headers['content-type'] = 'application/json';
    }
    next();
});

app.use(bodyParser.urlencoded({ extended: false }));
app.use(bodyParser.json());

app.post('/publish', async (req, res) => {
    try{
        const messageId = await snsHelper.publishToTopic(req.body);
        res.send(`publish success. messageId: ${messageId}`).status(200);
    }
    catch(e){
        res.send(e.message).status(500);
    }        
});

app.post('/update', (req, res) => {
    console.log(`Data received: ${JSON.stringify(req.body)}`);
    console.log(`messageid: ${req.headers["x-amz-sns-message-id"]}`);
    validator.validate(req.body, function(error){
        if(!error){
            if(req.headers["x-amz-sns-message-type"] === 'SubscriptionConfirmation'){
                https.get(req.body.SubscribeURL, (response) => {                    
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

app.listen(port, () => {
    console.log(`app is listening on port ${port}`);
});

