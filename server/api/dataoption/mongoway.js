"use strict"

const MongoClient = require('mongodb').MongoClient
const config = require('../../config');
const randomstring = require('randomstring');

let getCollection = (docName) => {
    return new Promise((resolve, reject) => {
        MongoClient.connect(config.dbUrl, (err, db) => {
            if (err) {
                reject(err);
            }
            else {
                resolve({
                    db: db,
                    collection: db.collection(docName)
                });
            }
        });
    })
}
let createToken = () => {
    return getCollection(config.token).then(({ db, collection }) => {
        return new Promise((resolve, reject) => {
            let token = randomstring.generate();
            collection.findOne({ token: token }, function (err, result) {
                if (err) {
                    reject(err);
                }
                else if (result) {
                    reject('token is exist');
                }
                else {
                    resolve(token);
                }
                db.close();
            });
        }).then((token) => {
            return Promise.resolve(token);
        }, (err) => {
            if (err === 'token is exist') {
                return createToken();
            }
        });
    });
}
let getUidByToken = (token) => {
    return getCollection(config.token).then(({ db, collection }) => {
        return new Promise((resolve, reject) => {
            collection.findOne({ token }, function (err, result) {
                if (err) {
                    reject(err);
                }
                else {
                    resolve(result._uid);
                }
                db.close();
            });
        });
    });
}
let getPasswordByEmail = (email) => {
    return getCollection(config.user).then(({ db, collection }) => {
        return new Promise((resolve, reject) => {
            collection.findOne({ email }, function (err, result) {
                if (err) {
                    reject("database error.");
                }
                else if (result === null) {
                    reject("userinfo error.");
                }
                else {
                    resolve(result.password);
                }
            });
            db.close();
        });
    });
}




let insertUser = (userInfo) => {
    return getCollection(config.user).then(({ db, collection }) => {
        return new Promise((resolve, reject) => {
            collection.insertOne({
                name: userInfo.name,
                email: userInfo.email,
                password: userInfo.password,
                purview: "user",
            }, function (err, result) {
                if (err) {
                    reject(err);
                }
                else {
                    resolve({
                        _id: result.ops[0]._id,
                        email: userInfo.email,
                        name: userInfo.name,
                        purview: "user",
                    });
                }
                db.close();
            });
        });
    });
}
let removeUserById = (_id) => {
    return removeToken(_id).then(() => {
        return getCollection(config.user).then(({ db, collection }) => {
            return new Promise((resolve, reject) => {
                collection.removeOne({ _id }, function (err, result) {
                    if (err) {
                        reject(err);
                    }
                    else {
                        resolve(result);
                    }
                    db.close();
                });
            });
        });
    });
}
let removeUserByEmail = (email) => {
    return getUserByEmail(email).then(userResult => {
        return removeToken(userResult._id).then(() => {
            return getCollection(config.user).then(({ db, collection }) => {
                return new Promise((resolve, reject) => {
                    collection.removeOne({ email }, function (err, result) {
                        if (err) {
                            reject(err);
                        }
                        else {
                            resolve(result);
                        }
                        db.close();
                    });
                });
            });
        });
    }, () => { });
}
let updateUserById = (id, userInfo) => {
    return getCollection(config.user).then(({ db, collection }) => {
        return new Promise((resolve, reject) => {
            collection.updateOne({ _id: id }, {
                $set: {
                    name: userInfo.name,
                    password: userInfo.password,
                }
            }, function (err, result) {
                if (err) {
                    reject(err);
                }
                else {
                    resolve(result);
                }
                db.close();
            });
        });
    });
}
let updateUserByEmail = (email, userInfo) => {
    return getCollection(config.user).then(({ db, collection }) => {
        return new Promise((resolve, reject) => {
            collection.updateOne({ email }, {
                $set: {
                    name: userInfo.name,
                    password: userInfo.password,
                }
            }, function (err, result) {
                if (err) {
                    reject(err);
                }
                else {
                    resolve(result);
                }
                db.close();
            });
        });
    });
}
let getUserById = (_id) => {
    return getCollection(config.user).then(({ db, collection }) => {
        return new Promise((resolve, reject) => {
            collection.findOne({ _id: _id }, function (err, result) {
                if (err) {
                    reject(err);
                }
                else if (result === null) {
                    reject("user error.");
                }
                else {
                    resolve({
                        _id: result._id,
                        email: result.email,
                        name: result.name,
                        purview: result.purview,
                        checked: result.checked,
                        emailToken: result.emailToken,
                    });
                }
                db.close();
            });
        });
    });
}
let getUserByEmail = (email) => {
    return getCollection(config.user).then(({ db, collection }) => {
        return new Promise((resolve, reject) => {
            collection.findOne({ email: email }, function (err, result) {
                if (err) {
                    reject(err);
                }
                else if (result === null) {
                    reject("userinfo error.");
                }
                else {
                    resolve({
                        _id: result._id,
                        email: result.email,
                        name: result.name,
                        purview: result.purview,
                        checked: result.checked,
                        emailToken: result.emailToken,
                    });
                }
                db.close();
            });
        });
    });
}





let insertToken = (_uid) => {
    return createToken().then((token) => {
        return getCollection(config.token).then(({ db, collection }) => {
            return new Promise((resolve, reject) => {
                let now = new Date();
                let dispose = new Date();
                dispose.setDate(dispose.getDate() + config.renewTime);

                collection.insertOne({
                    _uid,
                    create: now.getTime(),
                    dispose: dispose.getTime(),
                    token: token,
                }, function (err, result) {
                    if (err) {
                        reject(err);
                    }
                    else {
                        resolve({
                            _id: result.ops[0]._id,
                            token,
                            dispose: result.ops[0].dispose,
                        });
                    }
                });
            });
        });
    });
}
let removeToken = (_uid) => {
    return getCollection(config.token).then(({ db, collection }) => {
        return new Promise((resolve, reject) => {
            collection.remove({ _uid }, function (err, result) {
                if (err) {
                    reject(err);
                }
                else {
                    resolve(result);
                }
                db.close();
            });
        });
    });
}
let renewTokenByUid = (_uid) => {
    return getCollection(config.token).then(({ db, collection }) => {
        return new Promise((resolve, reject) => {
            let dispose = new Date();
            dispose.setDate(dispose.getDate() + config.renewTime);
            collection.updateOne({ _uid }, {
                $set: {
                    dispose: dispose.getTime(),
                }
            }, function (err, result) {
                if (err) {
                    reject(err);
                }
                else if (result.result.n === 1) {
                    resolve(result);
                }
                else {
                    reject("database error.");
                }
                db.close();
            });
        });
    });
}
let renewTokenByToken = (token) => {
    return getCollection(config.token).then(({ db, collection }) => {
        return new Promise((resolve, reject) => {
            let dispose = new Date();
            dispose.setDate(dispose.getDate() + config.renewTime);
            collection.updateOne({ token }, {
                $set: {
                    dispose: dispose.getTime(),
                }
            }, function (err, result) {
                if (err) {
                    reject(err);
                }
                else if (result.result.n === 1) {
                    resolve(result);
                }
                else {
                    reject("database error.");
                }
                db.close();
            });
        });
    });
}
let getTokenByUid = (_uid) => {
    return getCollection(config.token).then(({ db, collection }) => {
        return new Promise((resolve, reject) => {
            collection.findOne({ _uid }, function (err, result) {
                if (err) {
                    reject(err);
                }
                else if (result === null) {
                    reject("token error.");
                }
                else {
                    resolve(result);
                }
            });
        });
    });
}
let getTokenByToken = (token) => {
    return getCollection(config.token).then(({ db, collection }) => {
        return new Promise((resolve, reject) => {
            collection.findOne({ token }, function (err, result) {
                if (err) {
                    reject(err);
                }
                else if (result === null) {
                    reject("token error.");
                }
                else {
                    resolve(result);
                }
            });
        });
    });
}




let insertPicture = async (pictureInfo) => {
    let { db, collection } = await getCollection(config.picture);
    return new Promise((resolve, reject) => {
        collection.insertOne(pictureInfo).then((result) => {
            resolve({
                _pid: result.ops[0]._id,
            });
        }, () => {
            reject("insert picture error.")
        }).then(() => {
            db.close();
        });

    });
}

let getPictures = async () => {
    let { db, collection } = await getCollection(config.picture);
    return new Promise((resolve, reject) => {
        collection.find().toArray(function (err, docs) {
            if (err) {
                reject(err)
            }
            else {
                resolve(docs)
            }
        })
    })
}




let setAdmin = (email) => {
    return getCollection(config.user).then(({ db, collection }) => {
        return new Promise((resolve, reject) => {
            collection.updateOne({ email }, {
                $set: { purview: "admin" }
            }, function (err, result) {
                if (err) {
                    reject(err);
                }
                else if (result.result.ok === 1) {
                    resolve("ok");
                }
                else {
                    reject("database error.");
                }
            });
        });
    });
}

let deAdmin = (email) => {
    return getCollection(config.user).then(({ db, collection }) => {
        return new Promise((resolve, reject) => {
            collection.updateOne({ email }, {
                $set: { purview: "user" }
            }, function (err, result) {
                if (err) {
                    reject(err);
                }
                else if (result.result.ok === 1) {
                    resolve("ok");
                }
                else {
                    reject("database error.");
                }
            });
        });
    });
}




module.exports = {
    getCollection,

    insertUser,
    getUserById,
    getUserByEmail,
    updateUserById,
    updateUserByEmail,
    removeUserById,
    removeUserByEmail,

    getUidByToken,
    getPasswordByEmail,

    createToken,
    insertToken,
    removeToken,
    renewTokenByUid,
    renewTokenByToken,
    getTokenByUid,
    getTokenByToken,

    insertPicture,
    getPictures,

    setAdmin,
    deAdmin,
}
