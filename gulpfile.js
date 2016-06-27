/// <binding AfterBuild='artifact' />
/*
This file in the main entry point for defining Gulp tasks and using Gulp plugins.
Click here to learn more. http://go.microsoft.com/fwlink/?LinkId=518007
*/
const gulp = require('gulp');
const zip = require('gulp-zip');

gulp.task('artifact', function(cb) {
    return gulp.src(['src/Hoppla.Deployer.Agent/bin/Release/*Agent.exe',
        'src/Hoppla.Deployer.Agent/bin/Release/*.dll',
        'src/Hoppla.Deployer.Agent/bin/Release/Resources/*'])
		.pipe(zip('artifact.zip'))
		.pipe(gulp.dest('dist'));
});