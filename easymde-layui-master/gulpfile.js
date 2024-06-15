"use strict";

/**
 * gulp --gulpfile gulpfile-3.js
 */
var gulp = require("gulp"),
  uglify = require("gulp-uglify"),
  minifycss = require("gulp-clean-css"),
  concat = require("gulp-concat"),
  header = require("gulp-header"),
  buffer = require("vinyl-buffer"),
  pkg = require("./package.json"),
  debug = require("gulp-debug"),
  eslint = require("gulp-eslint"),
  prettify = require("gulp-jsbeautifier"),
  browserify = require("browserify"),
  gutil = require("gulp-util"),
  source = require("vinyl-source-stream");

var banner = [
  "/**",
  " * <%= pkg.name %> v<%= pkg.version %>",
  " * Copyright <%= pkg.company %>",
  " * @link <%= pkg.homepage %>",
  " * @license <%= pkg.license %>",
  " */",
  "",
].join("\n");

//格式化JS
gulp.task("prettify-js", gulp.series([], function () {
  return gulp
    .src("./src/mods/easymde/easymde.js")
    .pipe(
      prettify({
        js: {
          brace_style: "collapse",
          indent_char: "\t",
          indent_size: 1,
          max_preserve_newlines: 3,
          space_before_conditional: false,
        },
      })
    )
    .pipe(gulp.dest("./src/js"));
}));

//格式化CSS
gulp.task("prettify-css", gulp.series([], function () {
  return gulp
    .src("./src/css/easymde.css")
    .pipe(
      prettify({
        css: {
          indentChar: "\t",
          indentSize: 1,
        },
      })
    )
    .pipe(gulp.dest("./src/css"));
}));

gulp.task("lint", gulp.series(["prettify-js"], function () {
  gulp
    .src("./src/mods/easymde/easymde.js")
    .pipe(debug())
    .pipe(eslint())
    .pipe(eslint.format())
    .pipe(eslint.failAfterError());
}));

function taskBrowserify(opts) {
  return browserify("./src/mods/easymde/easymde.js", opts).bundle();
}

gulp.task("layui-scripts", function () {
  var js_files = [
    "./src/js/*.js",
    "./src/js/highlight-min.js",
    "./src/mods/easymde/easymde.js",
  ];

  return gulp
    .src(js_files)
    .pipe(concat("easymde.js"))
    .pipe(
      uglify().on("error", function (err) {
        gutil.log(gutil.colors.red("[Error]"), err.toString());
      })
    )
    .pipe(header(banner, { pkg: pkg }))
    .pipe(gulp.dest("./easymde-layui/mods/easymde/"));
});

// gulp.task("dependence-scripts", function () {
//   var js_files = [];

//   return gulp
//     .src(js_files)
//     .pipe(concat("dependence.js"))
//     .pipe(uglify())
//     .pipe(header(banner, { pkg: pkg }))
//     .pipe(gulp.dest("./easymde-layui/"));
// });

//编译easymdeJS
// gulp.task("browserify", ["lint"], function () {
// 	return taskBrowserify({
// 		standalone: "easymde"
// 	})
// 		.pipe(source("easymde.js"))
// 		.pipe(buffer())
// 		.pipe(header(banner, {
// 			pkg: pkg
// 		}))
// 		.pipe(gulp.dest("./temp/"));
// });

// 压缩CSS文件
gulp.task("styles", gulp.series(["prettify-css"], function () {
  var css_files = [
    "./node_modules/codemirror/lib/codemirror.css",
    "./src/css/*.css",
    "./node_modules/codemirror-spell-checker/src/css/spell-checker.css",
  ];
  return gulp
    .src(css_files)
    .pipe(concat("easymde.min.css"))
    .pipe(minifycss())
    .pipe(buffer())
    .pipe(
      header(banner, {
        pkg: pkg,
      })
    )
    .pipe(gulp.dest("./easymde-layui/mods/easymde/css/"));
}));

//复制fonts字体文件
gulp.task("copy-fonts", function () {
  return gulp
    .src("./src/css/fonts/*")
    .pipe(gulp.dest("./easymde-layui/mods/easymde/css/fonts"));
});

gulp.task("copy-spell-checker-data", function () {
  return gulp
    .src("./src/easymde/spell-checker-data/*")
    .pipe(gulp.dest("./easymde-layui/mods/easymde/spell-checker-data"));
});

// "browserify",
gulp.task("default", gulp.series([
  "styles",
  "layui-scripts",
  "copy-fonts",
  "copy-spell-checker-data",
]));
