idf_component_register(
    SRCS
        "controller.c"
        "display.c"
        "display_font.c"
        "fieldbus.c"
        "theme_project.c"
        "generated/ui_start.c"
        "generated/ui_start_event.c"
        "generated/ui_start_update.c"
        "machine.c"
        "main.c"
        "message.c"
    INCLUDE_DIRS
        "include"
        "generated"
    EMBED_FILES
        "Arial.ttf"
)

idf_component_get_property(LVGL_LIB lvgl__lvgl COMPONENT_LIB)
target_include_directories(${LVGL_LIB} PUBLIC ${CMAKE_CURRENT_LIST_DIR})

target_compile_definitions(${LVGL_LIB} PUBLIC LV_CONF_INCLUDE_SIMPLE)
