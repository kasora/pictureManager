'use strict';

const express = require('express');
const database = require('./dataoption/mongoway');
const ObjectID = require('mongodb').ObjectID;
const config = require('../config');
const fs = require('fs');
const cookieParser = require('cookie-parser');
const randomstring = require('randomstring');

let router = express.Router();

let replaceMongoId = (req, res, next) => {
    if (req.query.uid !== undefined) {
        if (config.dataWay === "mongodb") {
            try {
                req.query._uid = new ObjectID(req.query.uid);
            } catch (err) {
                res.status(404).send({
                    err: "uid error.",
                });
                return;
            }
        }
    }
    if (req.query.linkid != undefined) {
        if (config.dataWay === "mongodb") {
            try {
                req.query._linkid = new ObjectID(req.query.linkid);
            } catch (err) {
                res.status(404).send({
                    err: "linkid error.",
                });
                return;
            }
        }
    }
    next();
}
let updateToken = (req, res, next) => {
    if (req.cookies.token !== undefined) {
        database.getTokenByToken(req.cookies.token).then((tokenResult) => {
            let now = new Date();
            if ((now.getTime() > tokenResult.dispose) ||
                (now.getTime() - tokenResult.create > config.disposeTime * 24 * 60 * 60 * 1000)) {
                next();
            }
            else {
                database.renewTokenByToken(req.cookies.token).then((result) => {
                    next();
                }, (err) => {
                    res.status(500).send({ err: "database error." })
                });
            }
        }, (err) => {
            next();
        });
    }
    else {
        next();
    }
}
let getUid = (req, res, next) => {
    if (!req.cookies.token) {
        req.query._uid = undefined;
        next();
    }
    else {
        database.getTokenByToken(req.cookies.token).then((tokenResult) => {
            req.query._uid = tokenResult._uid;
            next();
        }, (err) => {
            if (err === "token error.") {
                req.query._uid = undefined;
                next();
            }
            else {
                res.status(500).send({ err: "database error." });
            }
        });
    }
}
let checkUserPurview = (req, res, next) => {
    if (req.cookies.token === undefined) {
        req.query.purview = "guest";
        next();
    }
    else {
        database.getTokenByToken(req.cookies.token).then((tokenResult) => {
            database.getUserById(tokenResult._uid).then((result) => {
                if (result.email === req.query.email) {
                    req.query.purview = "owner";
                    next();
                }
                else if (result.purview === "admin") {
                    req.query.purview = "admin";
                    next();
                }
                else {
                    req.query.purview = "guest";
                    next();
                }
            }, (err) => {
                if (err === "user error.") {
                    req.query.purview = "guest";
                    next();
                }
                else {
                    res.status(500).send({ err: "database error." });
                }
            });
        }, (err) => {
            if (err === "token error.") {
                req.query.purview = "guest";
                next();
            }
            else {
                res.status(500).send({ err: "database error." })
            }
        });
    }
}
let compatibleParams = (req, res, next) => {
    Object.assign(req.query, req.body);
    next();
}
let printLog = (req, res, next) => {
  if (config.log === "console") {
    console.log(`A ${req.method} request to ${req._parsedUrl.path}.`);
  }
  next();
}

router.use(printLog);

router.use(compatibleParams);
router.use(replaceMongoId);
router.use(updateToken);
router.use(checkUserPurview);
router.use(getUid);

router.use('/user', require('./user'));
router.use('/login', require('./login'));
router.use('/picture', require('./picture'));

module.exports = router;
