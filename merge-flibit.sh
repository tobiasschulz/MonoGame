#!/bin/bash

# git remote add flibit https://github.com/flibitijibibo/MonoGame.git
git pull
git checkout -f monogame-sdl2
git fetch flibit
git reset --hard flibit/monogame-sdl2
git push origin monogame-sdl2  -f

git pull
git checkout -f mgsdl2-glshader
git pull
git merge monogame-sdl2 && git commit -a && git push origin mgsdl2-glshader
