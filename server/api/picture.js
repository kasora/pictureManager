'use strict';

const express = require('express');
const fs = require('fs');
const path = require('path');
const randomstring = require('randomstring');
var formidable = require('formidable');

const config = require('../config');
const database = require('./dataoption/mongoway');

let router = express.Router();

let checkInPicture = async (picture) => {
  let result = await database.insertPicture({
    type: picture.type,
    valid: 1,
    user: picture._id,
    time: picture.time || Date.now(),
    groups: picture.groups || [],
    tags: picture.tags || [],
  });
  return result._pid;
}

let uploadPicture = (req, res) => {
  var form = formidable.IncomingForm({
    encoding: 'utf-8',
    uploadDir: config.todoPath,
    keepExtensions: true,
    maxFieldsSize: 2 * 4096 * 4096,
  });

  fs.access(config.picturePath, function (err) {
    if (err) {
      fs.mkdirSync(config.picturePath);
    }
  });
  fs.access(config.todoPath, function (err) {
    if (err) {
      fs.mkdirSync(config.todoPath);
    }
  });

  var allFile = [];
  form.on('file', function (filed, file) {
    allFile.push([filed, file]);
  }).on('error', function (err) {
    console.error('上传失败：', err.message);
    res.status(500).send({ err: "upload error." });
  }).parse(req, function (err, fields, files) {
    if (err) {
      res.status(500).send({ err: "system error." });
    }
    var errCount = 0;
    var keys = Object.keys(files);
    keys.forEach(async function (key) {
      var filePath = files[key].path;
      var fileExt = filePath.substring(filePath.lastIndexOf('.'));
      if (('.jpg.png.gif').indexOf(fileExt.toLowerCase()) === -1) {
        errCount += 1;
      } else {
        Object.assign(fields, req.query);
        let picture = {
          type: fileExt,
          user: req.query._uid,
          tags: fields.tags.split(','),
        }
        let fileName = await checkInPicture(picture) + fileExt;
        let targetFile = path.join(config.picturePath, fileName);

        fs.renameSync(filePath, targetFile);
      }
    });
    res.status(201).send({ status: "success" });
  });
}

router.post('/', uploadPicture);
module.exports = router;
