'use strict';

const express = require('express');
const bodyParser = require('body-parser')
const cookieParser = require('cookie-parser');
const path = require('path')

const config = require('./config');

let router = express.Router();
let app = express();

app.use(bodyParser());
app.use(cookieParser());
app.use(bodyParser.urlencoded({ extended: false }));
app.use(bodyParser.json({ type: 'application/*+json' }));
app.use(bodyParser.raw({ type: 'application/vnd.custom-type' }));
app.use(bodyParser.text({ type: 'text/html' }));

app.use(express.static(__dirname + '/static'));
app.use(require('./api/router'));
//app.get('*', function (req, res) { res.sendFile(path.resolve(__dirname, 'static', 'index.html')); })

app.listen(config.port);
console.log(`Service started at port ${config.port}.`);

module.exports = app;
