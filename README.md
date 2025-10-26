# Emuera Lazy Loading 

This is a port of the lazy loading code by 지나가던 to [Emuera EE](https://gitlab.com/EvilMask/emuera.em).

All the original lazy loading code belongs to 지나가던 which was adapted on commit 87dc1c0e, all further changes are either fixes or improvements (Hopefully) to this code.

## Usage
The executable requires a config file `lazyloading.cfg` which tells Emuera which files to load lazily.

The format of the config file is directories (relative to the ERB folder of the game) separated by newlines which contain
, the files you want emuera to lazy load, these directories can use either the windows directory separator
`\ ` or the sane one `/`.

#### Example
lazyloading.cfg:
```
口上
RPG\依頼
RPG\闘技場
RPG\ダンジョンアタック\ダンジョンデータ
```


With the config file, Emuera will create the lazy loading table (`lazyloading.dat`) to make future loadings of the game faster.

Keep in mind that **IF NEW FUNCTIONS ARE ADDED IN THE LAZY LOADED FILES, YOU MUST DELETE THIS FILE
AND RUN THE PROCESS AGAIN TO AVOID PROBLEMS**. 