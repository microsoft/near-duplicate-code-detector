var esprima = require('esprima');
const fs = require('fs');
const path = require('path');
const zlib = require('zlib');


// List all files in a directory in Node.js recursively in a synchronous fashion
function listAllFilesRecursive(dir, suffix, filelist) {
    var files = fs.readdirSync(dir);
    for (var i in files) {
        var filepath = path.join(dir, files[i])
        try{
            if (fs.statSync(filepath).isDirectory()) {
                filelist = listAllFilesRecursive(filepath, suffix, filelist);
            } else {
                filelist.push(filepath);
            }
        } catch (err) {
            console.warn("Failed to stat "+ filepath + " continuing...");
        }
    }
    return filelist;
};

function getTokens(codeText, identifiersOnly) {
    var tokenText = [];
    var tokens = esprima.tokenize(codeText)
    for (var i in tokens) {
        if (!identifiersOnly || tokens[i]['type'] == "Identifier") {
            tokenText.push(tokens[i]['value']);
        }
    }
    return tokenText;
}

function extractForFolder(dir, identifiersOnly, outpath, baseDir) {    
    var all_files = listAllFilesRecursive(dir, '.js', []);   

    var all_json = []

    for (var i in all_files) {
        console.log('Opening ' + all_files[i]);
        var filecontent = fs.readFileSync(all_files[i]).toString();
        try {
            var tokens = getTokens(filecontent, identifiersOnly);
            var jsonStr = JSON.stringify({'filename': path.relative(baseDir, all_files[i]), 'tokens': tokens});
            all_json.push(jsonStr);
        } catch (err) {
            console.warn('Error when extracting '+ all_files[i] + ' : ' + err);
        }
    }

    var gzipped = new Buffer(all_json.join('\n'));
    fs.writeFileSync(outpath, zlib.gzipSync(gzipped));
}

// For each project (folder in folder)
var base_dir = '/mnt/c/Users/t-mialla/Downloads/data';
var all_user_folders = fs.readdirSync(base_dir);
for (var i in all_user_folders) {
    var user_folder = path.join(base_dir, all_user_folders[i]);
    if (!fs.lstatSync(user_folder).isDirectory()) {
        continue;
    }
    all_project_folders = fs.readdirSync(user_folder);
    for (var j in all_project_folders) {        
        var project_folder = path.join(user_folder, all_project_folders[j]);        
        if (!fs.statSync(project_folder).isDirectory()) continue;

        console.log('Extracting '+ project_folder)

        var outpath = path.join('/mnt/c/Users/t-mialla/Downloads/parsedJs', all_user_folders[i] + "__" + all_project_folders[j] + '.jsonl.gz'); // TODO
        extractForFolder(project_folder, false, outpath, base_dir);
    }
}
