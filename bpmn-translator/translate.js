var _ = require('lodash');
var log4js = require('log4js');
var trans = require('./lib/bpmn_translator');
var fs = require('fs');
var log = log4js.getLogger('bpmn-translator');


function usage() {
    var v = [
        "usage:",
        "translate.js translate -input=[source file] -output=[dest file] [options]"
    ];
    _.forEach(v, function(x) {  
        console.log(x);
    });
};

function getArgs() {
	var rt = {};
	for (var i=0; i<process.argv.length; i++) {
		var n = process.argv[i];
		var ix = n.indexOf('=');
		if (n.indexOf('-') == 0 && ix > 0) {
			var nm = n.substr(1, ix - 1);
			var nv = n.substr(ix + 1);
			rt[nm] = nv;
		}
	}
	return rt;
};

function doTranslate(bpmn, cfg) {
    trans.translateBpmn2ToNginnBPM(bpmn, cfg, function(s, r) {
        log.info('done', s, r);
    });
};

if (process.argv.length < 3) {
    usage();
    process.exit(0);
};

var argz = getArgs();
var cmd = process.argv[2];

if (cmd == "translate") {
    if (!_.has(argz, 'inputFile')) throw new Error('inputFile missing');
    var ct = fs.readFileSync(argz.inputFile);
    log.info('read bpmn');
    doTranslate(ct, argz);
}
else {
    console.log('unknown command', cmd);
    process.exit(-1);
};