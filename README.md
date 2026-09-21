# TinyRig
Tiny Rig is a little, floating island that lives on your desktop, inhabited by an opinionated console operator with a lighting rig that responds to live sACN levels 🏝️

# Build TransparenWindow plugin for macOS

Open command prompt in the TransparenWindow folder

1. mkdir build
2. cd build
3. cmake -S .. -B .
4. cmake --build .
5. rm -rf ../../Assets/Plugins/TransparentWindow.bundle
6. cp -rf TransparentWindow.bundle ../../Assets/Plugins