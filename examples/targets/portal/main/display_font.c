#include "display_font.h"

#include <stddef.h>
#include <stdint.h>

#include "esp_log.h"

static const char *TAG = "DISPLAY_FONT";

extern const uint8_t _binary_Arial_ttf_start[] asm("_binary_Arial_ttf_start");
extern const uint8_t _binary_Arial_ttf_end[] asm("_binary_Arial_ttf_end");

const lv_font_t *display_font_init(void)
{
#if LV_USE_TINY_TTF
    static lv_font_t *font = NULL;

    if(font != NULL) {
        return font;
    }

    const size_t font_size = (size_t)(_binary_Arial_ttf_end - _binary_Arial_ttf_start);
    font = lv_tiny_ttf_create_data(_binary_Arial_ttf_start, font_size, 14);

    if(font == NULL) {
        ESP_LOGW(TAG, "Tiny TTF konnte Arial.ttf nicht laden, nutze LVGL-Standardfont");
        return LV_FONT_DEFAULT;
    }

    ESP_LOGI(TAG, "TTF-Font geladen (%u Bytes)", (unsigned)font_size);
    return font;
#else
    ESP_LOGW(TAG, "LV_USE_TINY_TTF ist deaktiviert, nutze LVGL-Standardfont");
    return LV_FONT_DEFAULT;
#endif
}
