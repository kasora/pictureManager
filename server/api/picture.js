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
    if (err) throw err;
    var filesUrl = [];
    var errCount = 0;
    var keys = Object.keys(files);
    keys.forEach(function (key) {
      var filePath = files[key].path;
      var fileExt = filePath.substring(filePath.lastIndexOf('.'));
      if (('.jpg.jpeg.png.gif').indexOf(fileExt.toLowerCase()) === -1) {
        errCount += 1;
      } else {
        //以当前时间戳对上传文件进行重命名
        var fileName = new Date().getTime() + fileExt;
        var targetFile = path.join(targetDir, fileName);
        //移动文件
        fs.renameSync(filePath, targetFile);
        // 文件的Url（相对路径）
        filesUrl.push('/upload/' + fileName)
      }
    });
  });
}

router.post('/', uploadPicture);
module.exports = router;
