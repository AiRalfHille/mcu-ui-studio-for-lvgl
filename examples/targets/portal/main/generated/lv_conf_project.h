#ifndef LV_CONF_PROJECT_H
#define LV_CONF_PROJECT_H

/*
 * Projektbezogene LVGL-Overrides.
 *
 * Diese Datei ergänzt die LVGL-Konfiguration aus sdkconfig/Kconfig.
 * Plattform- und Backend-Auswahl bleiben in ESP-IDF, projektbezogene
 * LVGL-Features werden hier darübergelegt.
 */

#define LV_DEF_REFR_PERIOD 33
#define LV_DPI_DEF 130

#define LV_DRAW_LAYER_SIMPLE_BUF_SIZE (24 * 1024U)

#define LV_USE_LOG 1
#define LV_LOG_LEVEL LV_LOG_LEVEL_INFO
#define LV_USE_OBJ_NAME 1

#define LV_USE_SYSMON 0
#define LV_USE_PERF_MONITOR 0
#define LV_USE_MEM_MONITOR 0

#define LV_USE_ASSERT_NULL 1
#define LV_USE_ASSERT_MALLOC 1
#define LV_USE_ASSERT_STYLE 0
#define LV_USE_ASSERT_MEM_INTEGRITY 0
#define LV_USE_ASSERT_OBJ 0

#define LV_USE_THEME_DEFAULT 1
#define LV_THEME_DEFAULT_DARK 0
#define LV_THEME_DEFAULT_GROW 1
#define LV_THEME_DEFAULT_TRANSITION_TIME 80

#define LV_USE_LODEPNG 1
#define LV_USE_BMP 1
#define LV_USE_XML 1

#define LV_FONT_MONTSERRAT_14 1
#define LV_FONT_MONTSERRAT_16 1
#define LV_FONT_MONTSERRAT_20 1
#define LV_FONT_MONTSERRAT_24 1
#define LV_FONT_MONTSERRAT_28 1
#define LV_FONT_MONTSERRAT_32 1
#define LV_FONT_MONTSERRAT_36 1
#define LV_FONT_MONTSERRAT_40 1
#define LV_FONT_MONTSERRAT_48 1

#define LV_USE_LABEL 1
#define LV_USE_BUTTON 1
#define LV_USE_IMAGE 1
#define LV_USE_LINE 1
#define LV_USE_ARC 1
#define LV_USE_BAR 1
#define LV_USE_SLIDER 1
#define LV_USE_SWITCH 1
#define LV_USE_TABLE 1
#define LV_USE_SPAN 1
#define LV_USE_SPINBOX 1
#define LV_USE_CHECKBOX 1
#define LV_USE_DROPDOWN 1
#define LV_USE_ROLLER 1
#define LV_USE_KEYBOARD 1
#define LV_USE_LIST 1
#define LV_USE_MENU 1
#define LV_USE_MSGBOX 1
#define LV_USE_TABVIEW 1
#define LV_USE_TILEVIEW 1
#define LV_USE_WIN 1
#define LV_USE_CHART 1
#define LV_USE_LED 1
#define LV_USE_SCALE 1
#define LV_USE_CALENDAR 1
#define LV_USE_CALENDAR_HEADER_ARROW 1
#define LV_USE_CALENDAR_HEADER_DROPDOWN 1
#define LV_USE_TEXTAREA 1
#define LV_USE_BUTTONMATRIX 1
#define LV_USE_CANVAS 1
#define LV_USE_ANIMIMG 1
#define LV_USE_IMAGEBUTTON 1
#define LV_USE_SPINNER 1
#define LV_USE_LOTTIE 0
#define LV_USE_3DTEXTURE 0
#define LV_USE_ARCLABEL 1

#endif
