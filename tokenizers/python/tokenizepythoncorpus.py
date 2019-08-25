"""
Tokenize Python code.

Usage:
   tokenizepythoncorpus.py [options] PROJECTS_FOLDER OUTPUT_FOLDER

Options:
    --all-tokens  Return only identifiers
    -h --help     Show this screen.

"""
from tokenize import tokenize, NAME, STRING
import keyword

import os
import glob
import multiprocessing as mp


from typing import Iterator
from docopt import docopt

from dpu_utils.utils import save_jsonl_gz

def tokenize_file(filepath: str, all_tokens: bool=False)-> Iterator[str]:
    tokens = []
    try:
        with open(filepath, 'rb') as f:
            for toknum, tokval, _, _, _ in tokenize(f.readline):
                if all_tokens or toknum in {NAME, STRING}:
                    if not keyword.iskeyword(tokval):
                        tokens.append(tokval)
    except Exception as e:
        print('Error tokenizing %s because %s' % (filepath, e))
    return dict(filename=filepath, tokens=tokens) 


def tokenize_all_files(directory: str, output_folder: str, only_ids: bool=False):
    print('Tokenizing in folder %s.' % directory)
    def all_file_tokenizer():
        for file in glob.iglob(os.path.join(directory, '**', '*.py'), recursive=True):
            if os.path.isdir(file): continue
            yield tokenize_file(file, only_ids)

    directory_name = os.path.basename(directory)
    save_jsonl_gz(all_file_tokenizer(), os.path.join(output_folder, directory_name + '-tokens.jsonl.gz'))


def tokenize_all_project_folders(directory: str, output_folder: str, only_ids: bool=False):
    os.makedirs(output_folder, exist_ok=True)
    all_dirs = [(os.path.join(directory, d), output_folder, only_ids) for d in os.listdir(directory) if os.path.isdir(os.path.join(directory, d))]
    with mp.Pool() as p:
        p.starmap(tokenize_all_files, all_dirs)

if __name__ == '__main__':
    args = docopt(__doc__)
    tokenize_all_project_folders(args['PROJECTS_FOLDER'], args['OUTPUT_FOLDER'], args['--all-tokens'])

