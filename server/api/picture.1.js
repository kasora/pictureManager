'use strict';

const express = require('express');
const fs = require('fs');
const randomstring = require('randomstring');
var formidable = require('formidable');

const config = require('../config');
const database = require('./dataoption/mongoway');

let router = express.Router();

let checkInPicture = async (picture, user) => {
  let path = await database.insertPicture({
    type: picture.type,
    valid: 1,
    user: user._id,
    time: picture.time || Date.now(),
    group: picture.group,
    tag: picture.tag,
  });
}

let uploadPicture = (req, res) => {
  var form = formidable.IncomingForm({
    encoding: 'utf-8',
    uploadDir: config.picturePath,
    keepExtensions: true,
    maxFieldsSize: 2 * 4096 * 4096,
  });

  fs.access(config.picturePath, function (err) {
    if (err) {
      fs.mkdirSync(targetDir);
    }
  });
  fs.access(config.todo, function (err) {
    if (err) {
      fs.mkdirSync(targetDir);
    }
  });

  var allFile = [];
  form.on('file', function (filed, file) {
    allFile.push([filed, file]);//收集传过来的所有文件
  }).on('error', function (err) {
    console.error('上传失败：', err.message);
    res.status(500).send({})
  }).parse(req, function (err, fields, files) {
    if (err) {
      console.log(err);
    }
    console.log(fields);
    console.log(files);
    allFile.forEach(function (file, index) {
      var fieldName = file[0];
      var types = file[1].name.split('.');
      var date = new Date();
      var ms = Date.parse(date);
      fs.renameSync(file[1].path, form.uploadDir + "/" + types[0] + "." + String(types[types.length - 1]));//重命名文件，默认的文件名是带有一串编码的，我们要把它还原为它原先的名字。
    });
  });
}

router.post('/', uploadPicture);
module.exports = router;
