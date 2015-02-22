var _ = require('lodash');
var log4js = require('log4js');
var BpmnModdle = require('bpmn-moddle');

var log = log4js.getLogger('bpmn-translator');

//args
// bpmn - text with bpmn model content(xml)
// cfg - configuration
// callback - function(err, json) that will be called when translation is done
//  configuration:
//  isFile - true if first argument contains a file name and not xml
//  packageName - package name
function translateBpmn2ToNginnBPM(bpmn, cfg, callback) {
    var moddle = new BpmnModdle();
    if (Buffer.isBuffer(bpmn)) bpmn = bpmn.toString();
    moddle.fromXML(bpmn, function(err, definitions) {
        if (err) {
            log.warn('bpmn parse error', err, definitions);
            callback(false, err);
            return;
        };
        log.info('moddle read', JSON.stringify(definitions, null, ' '));
    });
}

module.exports.translateBpmn2ToNginnBPM = translateBpmn2ToNginnBPM;