#include "display.h"
#include "bsp/esp-bsp.h"
#include "display_font.h"
#include "ui_start.h"
#include "ui_start_contract.h"

lv_theme_t *theme_project_create(lv_display_t *disp);

/* --------------------------------------------------------------------------
   MCU-Projekt — feste Datei, nicht generiert
   Zwischenschicht zwischen MCU-Projekt und generierten ui_start_* Dateien
   -------------------------------------------------------------------------- */

void display_init(void)
{
    /*
     * Reihenfolge ist wichtig:
     * 1. Projekt-Theme am Display registrieren
     * 2. generierte UI erzeugen
     * 3. Projektschrift auf den aktiven Screen legen
     *
     * So greifen Theme und generierte Styles konsistent auf dieselbe
     * Widget-Hierarchie.
     */
    lv_display_t *display = lv_display_get_default();
    if (display != NULL)
    {
        lv_display_set_theme(display, theme_project_create(display));
    }

    ui_start_init();
    lv_obj_set_style_text_font(lv_screen_active(), display_font_init(), LV_PART_MAIN | LV_STATE_DEFAULT);
}


void display_handle_message(const display_message_t *msg)
{
    if (msg == NULL) return;

    /*
     * BSP-Lock für thread-sicheren LVGL-Zugriff aus dem Display-Task.
     * ui_start_update_* sind generiert — weder Controller noch Fieldbus
     * muessen direkten LVGL-Code kennen.
     */
    bsp_display_lock(-1);

    switch (msg->type)
    {
        /* Die generierten Update-Funktionen kapseln die Zuordnung von
           Update-Ziel-Enum zu konkretem Widget. */
        case DISPLAY_UPDATE_TEXT:
            ui_start_update_text(msg->target, msg->payload.text);
            break;
        case DISPLAY_UPDATE_VALUE:
            ui_start_update_value(msg->target, msg->payload.value);
            break;
        case DISPLAY_UPDATE_BOOL:
            ui_start_update_bool(msg->target, msg->payload.flag);
            break;
        case DISPLAY_UPDATE_VISIBLE:
            ui_start_update_visible(msg->target, msg->payload.flag);
            break;
        case DISPLAY_UPDATE_ENABLED:
            ui_start_update_enabled(msg->target, msg->payload.flag);
            break;
        case DISPLAY_UPDATE_BRIGHTNESS:
            ui_start_update_brightness(msg->target, msg->payload.brightness);
            break;
    }
    
    bsp_display_unlock();
}
