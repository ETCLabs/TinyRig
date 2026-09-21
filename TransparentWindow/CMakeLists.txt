cmake_minimum_required(VERSION 3.16)

project(TransparentWindow LANGUAGES OBJCXX)

set(CMAKE_OSX_ARCHITECTURES arm64)

add_library(TransparentWindow MODULE TransparentWindow.mm)

set_target_properties(TransparentWindow PROPERTIES
    BUNDLE TRUE
    BUNDLE_EXTENSION bundle
)

target_link_libraries(TransparentWindow
    "-framework Cocoa"
    "-framework QuartzCore"
)
