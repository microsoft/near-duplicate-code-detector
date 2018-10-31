"""
Tokenize Python code and specifically from Barone et al.
https://github.com/EdinburghNLP/code-docstring-corpus

Usage:
   baronetokenizer.py [options] INPUT_FILE OUTPUT_FILE

Options:
    -h --help     Show this screen.

"""
import keyword

import os
import re
from tqdm import tqdm

from typing import Iterator, List
from docopt import docopt

from dpu_utils.utils import save_jsonl_gz

ID_SPLIT_RE=re.compile(r'[a-zA-Z_0-9]+')

def tokenize_line(line: str)-> List[str]:
    tokens = []
    for token_group in line.split(' '):
        if token_group in {'DCSP', 'DCNL'}: continue
        tokens.extend([t for t in ID_SPLIT_RE.findall(token_group) if not keyword.iskeyword(t) and len(t)>0])
    return tokens


def tokenize_all(input_file):
    with open(input_file) as f:
        for i, line in tqdm(enumerate(f)):
            tokens = tokenize_line(line)
            if len(tokens) == 0: continue
            yield dict(filename='dummyfile%s' % i, tokens=tokens)

if __name__ == '__main__':
    args = docopt(__doc__)
    save_jsonl_gz(tokenize_all(args['INPUT_FILE']), args['OUTPUT_FILE'])

