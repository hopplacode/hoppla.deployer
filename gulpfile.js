/// <binding />
/*
This file in the main entry point for defining Gulp tasks and using Gulp plugins.
Click here to learn more. http://go.microsoft.com/fwlink/?LinkId=518007
*/
const gulp = require('gulp');
const zip = require('gulp-zip');

gulp.task('artifact', () => {
    return gulp.src('src\Hoppla.Deployer.Agent\bin\Release\*')
		.pipe(zip('artiface.zip'))
		.pipe(gulp.dest('dist'));
});