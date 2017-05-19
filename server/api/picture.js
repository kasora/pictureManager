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

  var form = formidable.IncomingForm({
    encoding: 'utf-8',
    uploadDir: config.todoPath,
    keepExtensions: true,
    maxFieldsSize: 2 * 4096 * 4096,
  });

  let allFile = [];
  form.on('file', function (field, file) {
    allFile.push({ field, file });
  }).parse(req, function (err, fields) {
    if (err) {
      res.status(400).send({ err: "data error." });
      return;
    }
    new Promise((resolve, reject) => {
      let last = allFile.length;
      allFile.forEach(async function ({ field, file }) {
        var filePath = file.path;
        var fileExt = filePath.substring(filePath.lastIndexOf('.'));
        if (('.jpg.png.gif').indexOf(fileExt.toLowerCase()) === -1) {
          reject("unknown type.");
        } else {
          let picture = {
            type: fileExt,
            user: req.query._uid,
            tags: fields[field].split(','),
          }
          let fileName = await checkInPicture(picture) + fileExt;
          let targetFile = path.join(config.picturePath, fileName);

          fs.renameSync(filePath, targetFile);
          last--;
        }
        if (!last) {
          resolve();
        }
      });
    }).then(() => {
      res.status(201).send({ status: "success" });
    }, (err) => {
      res.status(500).send({ err });
    });

  });
}

let getPicture = async (req, res) => {
  let pics = await database.getPictures()
  res.status(200).send(pics)
}

router.post('/', uploadPicture);
router.get('/', getPicture)
module.exports = router;
