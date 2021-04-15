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

if (process.argv.length != 4) {
    console.error(`Usage: ${process.argv[0]} PROJECTS_FOLDER OUTPUT_FOLDER`);
    process.exit(1);
}

const base_dir = process.argv[2];
if(!fs.existsSync(base_dir)) {
    console.error(`Error: Directory "${base_dir}" does not exist.`);
    process.exit(1);
}
if(!fs.lstatSync(base_dir).isDirectory()) {
    console.error(`Error: "${base_dir}" is not a directory.`);
    process.exit(1);
}

const output_dir = process.argv[3];
if(fs.existsSync(output_dir) && !fs.lstatSync(output_dir).isDirectory()) {
    console.error(`Error: "${output_dir}" is not a directory.`);
    process.exit(1);
}

if(!fs.existsSync(output_dir)) {
    fs.mkdirSync(output_dir);
}

fs.readdirSync(base_dir).forEach(project_name => {
    const project_dir = path.join(base_dir, project_name);
    if (!fs.lstatSync(project_dir).isDirectory()) {
        return;
    }

    console.log(`Extracting ${project_dir}`);
    
    const project_output_file = path.join(output_dir, project_name + ".json.gz");
    extractForFolder(project_dir, false, project_output_file, base_dir);
});

