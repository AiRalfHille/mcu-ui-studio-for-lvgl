cmake_minimum_required(VERSION 3.20)

project(lvgl_simulator_host C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

set(PROJECT_ROOT "${CMAKE_CURRENT_LIST_DIR}/../..")
set(PROJECT_THIRD_PARTY_ROOT "${PROJECT_ROOT}/third_party")
set(PROJECT_LVGL_ROOT "${PROJECT_THIRD_PARTY_ROOT}/lvgl-9.4")
set(PROJECT_SDL2_FRAMEWORK "${PROJECT_THIRD_PARTY_ROOT}/SDL2.framework")
set(LVGL_SIMULATOR_LV_CONF_PATH "${CMAKE_CURRENT_LIST_DIR}/config/lv_conf.h")

option(LVGL_SIMULATOR_WITH_RUNTIME "Enable the future LVGL/SDL2 runtime implementation." OFF)

set(LVGL_SIMULATOR_RUNTIME_SOURCE "src/lvgl_simulator_runtime_stub.c")

if(LVGL_SIMULATOR_WITH_RUNTIME)
    set(LVGL_SIMULATOR_RUNTIME_SOURCE "src/lvgl_simulator_runtime_lvgl.c")
endif()

set(LVGL_SIMULATOR_SDL2_INCLUDE_DIR "" CACHE PATH "Path to the SDL2 include directory")
set(LVGL_SIMULATOR_SDL2_LIBRARY "" CACHE FILEPATH "Path to the SDL2 library or framework")
set(LVGL_SIMULATOR_LVGL_INCLUDE_DIR "" CACHE PATH "Path to the LVGL include directory")
set(LVGL_SIMULATOR_LVGL_LIBRARY "" CACHE FILEPATH "Path to the LVGL library")
set(LVGL_SIMULATOR_SDL2_TARGET "" CACHE STRING "Optional SDL2 CMake target to link against")

set(LVGL_SIMULATOR_HAS_SDL2 0)
set(LVGL_SIMULATOR_HAS_LVGL 0)

if("${LVGL_SIMULATOR_LVGL_INCLUDE_DIR}" STREQUAL "" AND EXISTS "${PROJECT_LVGL_ROOT}/lvgl.h")
    set(LVGL_SIMULATOR_LVGL_INCLUDE_DIR "${PROJECT_LVGL_ROOT}")
endif()

if(NOT "${LVGL_SIMULATOR_SDL2_INCLUDE_DIR}" STREQUAL "" AND NOT "${LVGL_SIMULATOR_SDL2_LIBRARY}" STREQUAL "")
    set(LVGL_SIMULATOR_HAS_SDL2 1)
elseif(NOT "${LVGL_SIMULATOR_SDL2_TARGET}" STREQUAL "")
    set(LVGL_SIMULATOR_HAS_SDL2 1)
else()
    find_package(SDL2 CONFIG QUIET)

    if(TARGET SDL2::SDL2)
        set(LVGL_SIMULATOR_SDL2_TARGET "SDL2::SDL2")
        get_target_property(_lvgl_simulator_sdl2_include_dirs SDL2::SDL2 INTERFACE_INCLUDE_DIRECTORIES)
        if(_lvgl_simulator_sdl2_include_dirs)
            list(GET _lvgl_simulator_sdl2_include_dirs 0 LVGL_SIMULATOR_SDL2_INCLUDE_DIR)
        endif()
        set(LVGL_SIMULATOR_HAS_SDL2 1)
    endif()

    if(NOT LVGL_SIMULATOR_HAS_SDL2)
        find_package(SDL2 QUIET)
        if(SDL2_FOUND)
            if(TARGET SDL2::SDL2)
                set(LVGL_SIMULATOR_SDL2_TARGET "SDL2::SDL2")
            elseif(TARGET SDL2::SDL2-static)
                set(LVGL_SIMULATOR_SDL2_TARGET "SDL2::SDL2-static")
            endif()

            if("${LVGL_SIMULATOR_SDL2_INCLUDE_DIR}" STREQUAL "" AND DEFINED SDL2_INCLUDE_DIRS)
                list(GET SDL2_INCLUDE_DIRS 0 LVGL_SIMULATOR_SDL2_INCLUDE_DIR)
            elseif("${LVGL_SIMULATOR_SDL2_INCLUDE_DIR}" STREQUAL "" AND DEFINED SDL2_INCLUDE_DIR)
                set(LVGL_SIMULATOR_SDL2_INCLUDE_DIR "${SDL2_INCLUDE_DIR}")
            endif()

            if("${LVGL_SIMULATOR_SDL2_LIBRARY}" STREQUAL "" AND DEFINED SDL2_LIBRARIES)
                list(GET SDL2_LIBRARIES 0 LVGL_SIMULATOR_SDL2_LIBRARY)
            elseif("${LVGL_SIMULATOR_SDL2_LIBRARY}" STREQUAL "" AND DEFINED SDL2_LIBRARY)
                set(LVGL_SIMULATOR_SDL2_LIBRARY "${SDL2_LIBRARY}")
            endif()

            if(NOT "${LVGL_SIMULATOR_SDL2_TARGET}" STREQUAL "" OR
               (NOT "${LVGL_SIMULATOR_SDL2_INCLUDE_DIR}" STREQUAL "" AND NOT "${LVGL_SIMULATOR_SDL2_LIBRARY}" STREQUAL ""))
                set(LVGL_SIMULATOR_HAS_SDL2 1)
            endif()
        endif()
    endif()

    if(NOT LVGL_SIMULATOR_HAS_SDL2)
        find_path(
            LVGL_SIMULATOR_SDL2_INCLUDE_DIR
            NAMES SDL.h SDL2/SDL.h
            PATH_SUFFIXES include include/SDL2 SDL2 Headers
        )
        find_library(
            LVGL_SIMULATOR_SDL2_LIBRARY
            NAMES SDL2 SDL2-static
            PATH_SUFFIXES lib lib64 lib/x64
        )

        if(NOT "${LVGL_SIMULATOR_SDL2_INCLUDE_DIR}" STREQUAL "" AND NOT "${LVGL_SIMULATOR_SDL2_LIBRARY}" STREQUAL "")
            set(LVGL_SIMULATOR_HAS_SDL2 1)
        endif()
    endif()

    if(NOT LVGL_SIMULATOR_HAS_SDL2 AND APPLE)
        if(EXISTS "${PROJECT_THIRD_PARTY_ROOT}")
            set(LVGL_SIMULATOR_SDL2_INCLUDE_DIR "${PROJECT_THIRD_PARTY_ROOT}")
        endif()
        if(EXISTS "${PROJECT_SDL2_FRAMEWORK}/Versions/A/SDL2")
            set(LVGL_SIMULATOR_SDL2_LIBRARY "${PROJECT_SDL2_FRAMEWORK}/Versions/A/SDL2")
        endif()
        if(NOT "${LVGL_SIMULATOR_SDL2_INCLUDE_DIR}" STREQUAL "" AND NOT "${LVGL_SIMULATOR_SDL2_LIBRARY}" STREQUAL "")
            set(LVGL_SIMULATOR_HAS_SDL2 1)
        endif()
    endif()
endif()

if(EXISTS "${LVGL_SIMULATOR_LVGL_INCLUDE_DIR}/lvgl.h" OR EXISTS "${LVGL_SIMULATOR_LVGL_INCLUDE_DIR}/src/lvgl.h")
    set(LVGL_SIMULATOR_HAS_LVGL 1)
endif()

configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/include/lvgl_simulator_build_config.h.in
    ${CMAKE_CURRENT_BINARY_DIR}/generated/lvgl_simulator_build_config.h
    @ONLY
)

add_executable(lvgl_simulator_host
    src/main.c
    src/lvgl_simulator_server.c
    ${LVGL_SIMULATOR_RUNTIME_SOURCE}
)

if(LVGL_SIMULATOR_WITH_RUNTIME)
    target_sources(lvgl_simulator_host
        PRIVATE
            src/lvgl_simulator_runtime_lvgl_platform.c
            src/lvgl_simulator_runtime_lvgl_screen.c
    )
endif()

target_include_directories(lvgl_simulator_host
    PRIVATE
        include
        ${CMAKE_CURRENT_BINARY_DIR}/generated
)

if(APPLE)
    target_compile_definitions(lvgl_simulator_host PRIVATE _DARWIN_C_SOURCE)
endif()

if(WIN32)
    target_link_libraries(lvgl_simulator_host PRIVATE ws2_32)
endif()

if(LVGL_SIMULATOR_WITH_RUNTIME)
    target_compile_definitions(lvgl_simulator_host PRIVATE LVGL_SIMULATOR_WITH_RUNTIME=1)
    if(LVGL_SIMULATOR_HAS_SDL2)
        if(NOT "${LVGL_SIMULATOR_SDL2_INCLUDE_DIR}" STREQUAL "")
            target_include_directories(lvgl_simulator_host PRIVATE ${LVGL_SIMULATOR_SDL2_INCLUDE_DIR})
        endif()

        if(NOT "${LVGL_SIMULATOR_SDL2_TARGET}" STREQUAL "")
            target_link_libraries(lvgl_simulator_host PRIVATE ${LVGL_SIMULATOR_SDL2_TARGET})
        elseif(NOT "${LVGL_SIMULATOR_SDL2_LIBRARY}" STREQUAL "")
            target_link_libraries(lvgl_simulator_host PRIVATE ${LVGL_SIMULATOR_SDL2_LIBRARY})
        endif()
    endif()

    if(LVGL_SIMULATOR_HAS_LVGL)
        set(LV_CONF_PATH "${LVGL_SIMULATOR_LV_CONF_PATH}")
        set(CONFIG_LV_BUILD_DEMOS OFF CACHE BOOL "" FORCE)
        set(CONFIG_LV_BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
        set(CONFIG_LV_USE_THORVG_INTERNAL OFF CACHE BOOL "" FORCE)
        set(CONFIG_LV_USE_SDL ON CACHE BOOL "" FORCE)
        set(CONFIG_LV_USE_DRAW_SDL OFF CACHE BOOL "" FORCE)
        set(CONFIG_LV_USE_OS LV_OS_NONE CACHE STRING "" FORCE)
        set(CONFIG_LV_COLOR_DEPTH 16 CACHE STRING "" FORCE)

        if(EXISTS "${LVGL_SIMULATOR_LVGL_INCLUDE_DIR}/CMakeLists.txt")
            add_subdirectory("${LVGL_SIMULATOR_LVGL_INCLUDE_DIR}" "${CMAKE_CURRENT_BINARY_DIR}/lvgl")
            target_link_libraries(lvgl_simulator_host PRIVATE lvgl)

            if(LVGL_SIMULATOR_HAS_SDL2 AND NOT "${LVGL_SIMULATOR_SDL2_INCLUDE_DIR}" STREQUAL "")
                target_include_directories(lvgl SYSTEM PUBLIC ${LVGL_SIMULATOR_SDL2_INCLUDE_DIR})
                if(EXISTS "${PROJECT_SDL2_FRAMEWORK}/Headers")
                    target_include_directories(lvgl SYSTEM PUBLIC "${PROJECT_SDL2_FRAMEWORK}/Headers")
                endif()
            endif()

            if(LVGL_SIMULATOR_HAS_SDL2)
                if(NOT "${LVGL_SIMULATOR_SDL2_TARGET}" STREQUAL "")
                    target_link_libraries(lvgl PRIVATE ${LVGL_SIMULATOR_SDL2_TARGET})
                elseif(NOT "${LVGL_SIMULATOR_SDL2_LIBRARY}" STREQUAL "")
                    target_link_libraries(lvgl PRIVATE ${LVGL_SIMULATOR_SDL2_LIBRARY})
                endif()
            endif()
        endif()

        target_include_directories(lvgl_simulator_host PRIVATE ${LVGL_SIMULATOR_LVGL_INCLUDE_DIR})
        if(EXISTS "${LVGL_SIMULATOR_LVGL_INCLUDE_DIR}/src")
            target_include_directories(lvgl_simulator_host PRIVATE "${LVGL_SIMULATOR_LVGL_INCLUDE_DIR}/src")
        endif()
        if(EXISTS "${LVGL_SIMULATOR_LVGL_INCLUDE_DIR}/src/drivers/sdl")
            target_include_directories(lvgl_simulator_host PRIVATE "${LVGL_SIMULATOR_LVGL_INCLUDE_DIR}/src/drivers/sdl")
        endif()
        if(EXISTS "${LVGL_SIMULATOR_LVGL_LIBRARY}")
            target_link_libraries(lvgl_simulator_host PRIVATE ${LVGL_SIMULATOR_LVGL_LIBRARY})
        endif()
    endif()

    message(STATUS "LVGL runtime skeleton selected.")
    message(STATUS "SDL2 configured: ${LVGL_SIMULATOR_HAS_SDL2}")
    message(STATUS "LVGL headers configured: ${LVGL_SIMULATOR_HAS_LVGL}")
    message(STATUS "Third-party root: ${PROJECT_THIRD_PARTY_ROOT}")
    message(STATUS "LVGL root: ${LVGL_SIMULATOR_LVGL_INCLUDE_DIR}")
    message(STATUS "SDL2 target: ${LVGL_SIMULATOR_SDL2_TARGET}")
    message(STATUS "SDL2 include dir: ${LVGL_SIMULATOR_SDL2_INCLUDE_DIR}")
    message(STATUS "SDL2 library: ${LVGL_SIMULATOR_SDL2_LIBRARY}")
    message(STATUS "lv_conf.h path: ${LVGL_SIMULATOR_LV_CONF_PATH}")
else()
    target_compile_definitions(lvgl_simulator_host PRIVATE LVGL_SIMULATOR_WITH_RUNTIME=0)
endif()
