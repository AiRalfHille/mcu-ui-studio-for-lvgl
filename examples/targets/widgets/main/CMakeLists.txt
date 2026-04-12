file(GLOB GENERATED_SOURCES "generated/*.c")
list(FILTER GENERATED_SOURCES EXCLUDE REGEX ".*/ui_.*_event\\.c$")

idf_component_register(
    SRCS
        "app_logic.c"
        "display.c"
        "main.c"
        ${GENERATED_SOURCES}
    INCLUDE_DIRS
        "include"
        "generated"
)

idf_component_get_property(LVGL_LIB lvgl__lvgl COMPONENT_LIB)
target_include_directories(${LVGL_LIB} PUBLIC ${CMAKE_CURRENT_LIST_DIR})
target_compile_definitions(${COMPONENT_LIB} PRIVATE LV_CONF_INCLUDE_SIMPLE)
