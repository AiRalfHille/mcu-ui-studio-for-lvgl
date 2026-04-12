#include "lvgl_simulator_build_config.h"
#include "lvgl_simulator_runtime_lvgl_internal.h"

#include <ctype.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
#include "lvgl.h"
#include "core/lv_obj_tree.h"
#if LV_USE_QRCODE
#include "libs/qrcode/lv_qrcode.h"
#endif

static lv_obj_t *g_current_screen;
static int32_t *g_grid_arrays[128];
static size_t g_grid_array_count;
static lv_point_precise_t *g_point_arrays[128];
static size_t g_point_array_count;
static char *g_callback_names[256];
static size_t g_callback_name_count;

typedef struct keyboard_autobind_context_t {
    lv_obj_t *first_textarea;
} keyboard_autobind_context_t;

typedef struct simulator_object_binding_t {
    char name[128];
    lv_obj_t *object;
} simulator_object_binding_t;

typedef struct simulator_scale_section_binding_t {
    char name[128];
    lv_scale_section_t *section;
} simulator_scale_section_binding_t;

typedef struct simulator_grid_array_binding_t {
    char name[128];
    int32_t *values;
} simulator_grid_array_binding_t;

typedef struct simulator_point_array_binding_t {
    char name[128];
    lv_point_precise_t *values;
    uint32_t count;
} simulator_point_array_binding_t;

static simulator_object_binding_t g_object_bindings[256];
static size_t g_object_binding_count;
static lv_obj_t *g_highlighted_object;

static void free_registered_grid_arrays(void)
{
    for(size_t i = 0; i < g_grid_array_count; i++) {
        free(g_grid_arrays[i]);
    }

    memset(g_grid_arrays, 0, sizeof(g_grid_arrays));
    g_grid_array_count = 0;
}

static void free_registered_point_arrays(void)
{
    for(size_t i = 0; i < g_point_array_count; i++) {
        free(g_point_arrays[i]);
    }

    memset(g_point_arrays, 0, sizeof(g_point_arrays));
    g_point_array_count = 0;
}

static void free_registered_callback_names(void)
{
    for(size_t i = 0; i < g_callback_name_count; i++) {
        free(g_callback_names[i]);
    }

    memset(g_callback_names, 0, sizeof(g_callback_names));
    g_callback_name_count = 0;
}

static void trim_whitespace(char *text)
{
    if(text == NULL) {
        return;
    }

    char *start = text;
    while(*start != '\0' && isspace((unsigned char)*start)) {
        start++;
    }

    if(start != text) {
        memmove(text, start, strlen(start) + 1);
    }

    size_t length = strlen(text);
    while(length > 0 && isspace((unsigned char)text[length - 1])) {
        text[--length] = '\0';
    }
}

static const char *map_event_code_name(lv_event_code_t code)
{
    switch(code) {
        case LV_EVENT_PRESSED: return "pressed";
        case LV_EVENT_PRESSING: return "pressing";
        case LV_EVENT_RELEASED: return "released";
        case LV_EVENT_CLICKED: return "clicked";
        case LV_EVENT_VALUE_CHANGED: return "value_changed";
        case LV_EVENT_FOCUSED: return "focused";
        case LV_EVENT_DEFOCUSED: return "defocused";
        case LV_EVENT_READY: return "ready";
        case LV_EVENT_CANCEL: return "cancel";
        default: return NULL;
    }
}

static void simulator_event_log_cb(lv_event_t *e)
{
    if(e == NULL) {
        return;
    }

    const char *event_name = map_event_code_name(lv_event_get_code(e));
    if(event_name == NULL) {
        return;
    }

    const char *object_name = lv_event_get_user_data(e);
    if(object_name == NULL || object_name[0] == '\0') {
        object_name = "unnamed";
    }

    printf("event callback fired: object='%s' event='%s'\n", object_name, event_name);
    fflush(stdout);
}

static void register_object_event_callbacks(lv_obj_t *object, const char *object_name)
{
    if(object == NULL || object_name == NULL || object_name[0] == '\0') {
        return;
    }

    char *name_copy = strdup(object_name);
    if(name_copy == NULL) {
        return;
    }

    if(g_callback_name_count < (sizeof(g_callback_names) / sizeof(g_callback_names[0]))) {
        g_callback_names[g_callback_name_count++] = name_copy;
    }

    lv_obj_add_event_cb(object, simulator_event_log_cb, LV_EVENT_PRESSED, name_copy);
    lv_obj_add_event_cb(object, simulator_event_log_cb, LV_EVENT_RELEASED, name_copy);
    lv_obj_add_event_cb(object, simulator_event_log_cb, LV_EVENT_CLICKED, name_copy);
    lv_obj_add_event_cb(object, simulator_event_log_cb, LV_EVENT_VALUE_CHANGED, name_copy);
}

static lv_obj_tree_walk_res_t find_first_textarea_cb(lv_obj_t *obj, void *user_data)
{
    keyboard_autobind_context_t *context = user_data;
    if(context == NULL) {
        return LV_OBJ_TREE_WALK_END;
    }

    if(lv_obj_check_type(obj, &lv_textarea_class)) {
        context->first_textarea = obj;
        return LV_OBJ_TREE_WALK_END;
    }

    return LV_OBJ_TREE_WALK_NEXT;
}

static lv_obj_tree_walk_res_t bind_keyboards_to_textarea_cb(lv_obj_t *obj, void *user_data)
{
    keyboard_autobind_context_t *context = user_data;
    if(context == NULL || context->first_textarea == NULL) {
        return LV_OBJ_TREE_WALK_END;
    }

    if(lv_obj_check_type(obj, &lv_keyboard_class)) {
        lv_keyboard_set_textarea(obj, context->first_textarea);
        printf(
            "Auto-bound keyboard '%s' to textarea '%s'.\n",
            lv_obj_get_name(obj),
            lv_obj_get_name(context->first_textarea));
        fflush(stdout);
    }

    return LV_OBJ_TREE_WALK_NEXT;
}

static void auto_bind_keyboards(lv_obj_t *screen)
{
    keyboard_autobind_context_t context = { 0 };
    lv_obj_tree_walk(screen, find_first_textarea_cb, &context);
    if(context.first_textarea == NULL) {
        return;
    }

    lv_obj_tree_walk(screen, bind_keyboards_to_textarea_cb, &context);
}

static lv_obj_t *create_base_screen(const char *title, const char *subtitle)
{
    lv_obj_t *screen = lv_obj_create(NULL);
    lv_obj_set_style_bg_color(screen, lv_color_hex(0xf7f7f7), 0);
    lv_obj_set_style_bg_opa(screen, LV_OPA_COVER, 0);
    lv_obj_set_layout(screen, LV_LAYOUT_FLEX);
    lv_obj_set_flex_flow(screen, LV_FLEX_FLOW_COLUMN);
    lv_obj_set_style_pad_all(screen, 24, 0);
    lv_obj_set_style_pad_gap(screen, 16, 0);

    lv_obj_t *header = lv_label_create(screen);
    lv_label_set_text(header, title);
    lv_obj_set_style_text_font(header, &lv_font_montserrat_32, 0);
    lv_obj_set_style_text_color(header, lv_color_hex(0x1f2937), 0);

    lv_obj_t *sub = lv_label_create(screen);
    lv_label_set_text(sub, subtitle);
    lv_obj_set_style_text_font(sub, &lv_font_montserrat_16, 0);
    lv_obj_set_style_text_color(sub, lv_color_hex(0x4b5563), 0);

    lv_obj_t *panel = lv_obj_create(screen);
    lv_obj_set_width(panel, LV_PCT(100));
    lv_obj_set_height(panel, LV_SIZE_CONTENT);
    lv_obj_set_style_pad_all(panel, 16, 0);
    lv_obj_set_style_radius(panel, 8, 0);
    lv_obj_set_style_bg_color(panel, lv_color_hex(0xffffff), 0);
    lv_obj_set_style_border_color(panel, lv_color_hex(0xd1d5db), 0);
    lv_obj_set_layout(panel, LV_LAYOUT_FLEX);
    lv_obj_set_flex_flow(panel, LV_FLEX_FLOW_COLUMN);
    lv_obj_set_style_pad_gap(panel, 8, 0);

    return panel;
}

static void detach_current_screen_for_component_reload(void)
{
    if(g_current_screen == NULL) {
        return;
    }

    lv_obj_t *old_screen = g_current_screen;
    lv_obj_t *placeholder = lv_obj_create(NULL);
    lv_obj_set_style_bg_opa(placeholder, LV_OPA_TRANSP, 0);

    g_current_screen = placeholder;
    lv_screen_load(placeholder);
    lv_obj_delete(old_screen);
}

static void load_new_screen(lv_obj_t *screen)
{
    lv_obj_t *old_screen = g_current_screen;
    g_current_screen = screen;
    lv_screen_load(screen);
    if(old_screen != NULL && old_screen != screen) {
        lv_obj_delete(old_screen);
    }
}

static lv_obj_t *find_object_binding(
    simulator_object_binding_t *bindings,
    size_t binding_count,
    const char *name)
{
    if(name == NULL) {
        return NULL;
    }

    for(size_t i = 0; i < binding_count; i++) {
        if(strcmp(bindings[i].name, name) == 0) {
            return bindings[i].object;
        }
    }

    return NULL;
}

static bool add_object_binding(
    simulator_object_binding_t *bindings,
    size_t *binding_count,
    const char *name,
    lv_obj_t *object)
{
    if(bindings == NULL || binding_count == NULL || name == NULL || object == NULL || *binding_count >= 256) {
        return false;
    }

    snprintf(bindings[*binding_count].name, sizeof(bindings[*binding_count].name), "%s", name);
    bindings[*binding_count].object = object;
    (*binding_count)++;
    return true;
}

static lv_scale_section_t *find_scale_section_binding(
    simulator_scale_section_binding_t *bindings,
    size_t binding_count,
    const char *name)
{
    if(name == NULL) {
        return NULL;
    }

    for(size_t i = 0; i < binding_count; i++) {
        if(strcmp(bindings[i].name, name) == 0) {
            return bindings[i].section;
        }
    }

    return NULL;
}

static bool add_scale_section_binding(
    simulator_scale_section_binding_t *bindings,
    size_t *binding_count,
    const char *name,
    lv_scale_section_t *section)
{
    if(bindings == NULL || binding_count == NULL || name == NULL || section == NULL || *binding_count >= 128) {
        return false;
    }

    snprintf(bindings[*binding_count].name, sizeof(bindings[*binding_count].name), "%s", name);
    bindings[*binding_count].section = section;
    (*binding_count)++;
    return true;
}

static int32_t *find_grid_array_binding(
    simulator_grid_array_binding_t *bindings,
    size_t binding_count,
    const char *name)
{
    if(name == NULL) {
        return NULL;
    }

    for(size_t i = 0; i < binding_count; i++) {
        if(strcmp(bindings[i].name, name) == 0) {
            return bindings[i].values;
        }
    }

    return NULL;
}

static bool add_grid_array_binding(
    simulator_grid_array_binding_t *bindings,
    size_t *binding_count,
    const char *name,
    int32_t *values)
{
    if(bindings == NULL || binding_count == NULL || name == NULL || values == NULL || *binding_count >= 128) {
        return false;
    }

    snprintf(bindings[*binding_count].name, sizeof(bindings[*binding_count].name), "%s", name);
    bindings[*binding_count].values = values;
    if(g_grid_array_count < 128) {
        g_grid_arrays[g_grid_array_count++] = values;
    }
    (*binding_count)++;
    return true;
}

static lv_point_precise_t *find_point_array_binding(
    simulator_point_array_binding_t *bindings,
    size_t binding_count,
    const char *name,
    uint32_t *point_count)
{
    if(name == NULL) {
        return NULL;
    }

    for(size_t i = 0; i < binding_count; i++) {
        if(strcmp(bindings[i].name, name) == 0) {
            if(point_count != NULL) {
                *point_count = bindings[i].count;
            }
            return bindings[i].values;
        }
    }

    return NULL;
}

static bool add_point_array_binding(
    simulator_point_array_binding_t *bindings,
    size_t *binding_count,
    const char *name,
    lv_point_precise_t *values,
    uint32_t count)
{
    if(bindings == NULL || binding_count == NULL || name == NULL || values == NULL || *binding_count >= 128) {
        return false;
    }

    snprintf(bindings[*binding_count].name, sizeof(bindings[*binding_count].name), "%s", name);
    bindings[*binding_count].values = values;
    bindings[*binding_count].count = count;
    if(g_point_array_count < 128) {
        g_point_arrays[g_point_array_count++] = values;
    }
    (*binding_count)++;
    return true;
}

static bool split_arguments(const char *text, char args[][256], int max_args, int *arg_count)
{
    int count = 0;
    int depth = 0;
    bool in_string = false;
    char current[256];
    size_t current_len = 0;

    for(const char *cursor = text; *cursor != '\0'; cursor++) {
        char ch = *cursor;
        if(ch == '"' && (cursor == text || cursor[-1] != '\\')) {
            in_string = !in_string;
        }
        else if(!in_string && (ch == '(' || ch == '{')) {
            depth++;
        }
        else if(!in_string && (ch == ')' || ch == '}')) {
            if(depth > 0) {
                depth--;
            }
        }

        if(!in_string && depth == 0 && ch == ',') {
            current[current_len] = '\0';
            trim_whitespace(current);
            if(count < max_args) {
                snprintf(args[count], 256, "%s", current);
                count++;
            }
            current_len = 0;
            current[0] = '\0';
            continue;
        }

        if(current_len + 1 < sizeof(current)) {
            current[current_len++] = ch;
        }
    }

    current[current_len] = '\0';
    trim_whitespace(current);
    if(current[0] != '\0' && count < max_args) {
        snprintf(args[count], 256, "%s", current);
        count++;
    }

    if(arg_count != NULL) {
        *arg_count = count;
    }

    return count > 0;
}

static bool extract_call_arguments(const char *line, const char *function_name, char args[][256], int max_args, int *arg_count)
{
    const char *start = strstr(line, function_name);
    if(start == NULL) {
        return false;
    }

    start += strlen(function_name);
    if(*start != '(') {
        return false;
    }
    start++;

    const char *end = strrchr(start, ')');
    if(end == NULL || end <= start) {
        return false;
    }

    char buffer[2048];
    size_t length = (size_t)(end - start);
    if(length >= sizeof(buffer)) {
        length = sizeof(buffer) - 1;
    }

    memcpy(buffer, start, length);
    buffer[length] = '\0';
    return split_arguments(buffer, args, max_args, arg_count);
}

static bool parse_coord_expression(const char *text, lv_coord_t *value)
{
    if(text == NULL || value == NULL) {
        return false;
    }

    if(strncmp(text, "LV_PCT(", 7) == 0) {
        int percentage = 0;
        if(sscanf(text, "LV_PCT(%d)", &percentage) == 1) {
            *value = LV_PCT(percentage);
            return true;
        }
    }

    int parsed = 0;
    if(sscanf(text, "%d", &parsed) == 1) {
        *value = (lv_coord_t)parsed;
        return true;
    }

    return false;
}

static bool parse_color_expression(const char *text, lv_color_t *value)
{
    if(text == NULL || value == NULL) {
        return false;
    }

    unsigned int hex = 0;
    if(sscanf(text, "lv_color_hex(%x)", &hex) == 1) {
        *value = lv_color_hex(hex);
        return true;
    }

    return false;
}

static lv_align_t parse_align_expression(const char *text)
{
    if(strcmp(text, "LV_ALIGN_CENTER") == 0) return LV_ALIGN_CENTER;
    if(strcmp(text, "LV_ALIGN_TOP_MID") == 0) return LV_ALIGN_TOP_MID;
    if(strcmp(text, "LV_ALIGN_BOTTOM_MID") == 0) return LV_ALIGN_BOTTOM_MID;
    if(strcmp(text, "LV_ALIGN_LEFT_MID") == 0) return LV_ALIGN_LEFT_MID;
    if(strcmp(text, "LV_ALIGN_RIGHT_MID") == 0) return LV_ALIGN_RIGHT_MID;
    return LV_ALIGN_DEFAULT;
}

static uint32_t parse_layout_expression(const char *text)
{
    if(strcmp(text, "LV_LAYOUT_GRID") == 0) return LV_LAYOUT_GRID;
    if(strcmp(text, "LV_LAYOUT_FLEX") == 0) return LV_LAYOUT_FLEX;
    if(strcmp(text, "LV_LAYOUT_NONE") == 0) return LV_LAYOUT_NONE;
    return 0;
}

static lv_flex_flow_t parse_flex_flow_expression(const char *text)
{
    if(strcmp(text, "LV_FLEX_FLOW_ROW") == 0) return LV_FLEX_FLOW_ROW;
    if(strcmp(text, "LV_FLEX_FLOW_COLUMN") == 0) return LV_FLEX_FLOW_COLUMN;
    return LV_FLEX_FLOW_ROW;
}

static lv_flex_align_t parse_flex_align_expression(const char *text)
{
    if(strcmp(text, "LV_FLEX_ALIGN_START") == 0) return LV_FLEX_ALIGN_START;
    if(strcmp(text, "LV_FLEX_ALIGN_CENTER") == 0) return LV_FLEX_ALIGN_CENTER;
    if(strcmp(text, "LV_FLEX_ALIGN_END") == 0) return LV_FLEX_ALIGN_END;
    if(strcmp(text, "LV_FLEX_ALIGN_SPACE_EVENLY") == 0) return LV_FLEX_ALIGN_SPACE_EVENLY;
    if(strcmp(text, "LV_FLEX_ALIGN_SPACE_AROUND") == 0) return LV_FLEX_ALIGN_SPACE_AROUND;
    if(strcmp(text, "LV_FLEX_ALIGN_SPACE_BETWEEN") == 0) return LV_FLEX_ALIGN_SPACE_BETWEEN;
    return LV_FLEX_ALIGN_START;
}

static lv_text_align_t parse_text_align_expression(const char *text)
{
    if(strcmp(text, "LV_TEXT_ALIGN_CENTER") == 0) return LV_TEXT_ALIGN_CENTER;
    if(strcmp(text, "LV_TEXT_ALIGN_RIGHT") == 0) return LV_TEXT_ALIGN_RIGHT;
    if(strcmp(text, "LV_TEXT_ALIGN_AUTO") == 0) return LV_TEXT_ALIGN_AUTO;
    return LV_TEXT_ALIGN_LEFT;
}

static lv_label_long_mode_t parse_label_long_mode_expression(const char *text)
{
    if(strcmp(text, "LV_LABEL_LONG_MODE_DOTS") == 0) return LV_LABEL_LONG_MODE_DOTS;
    if(strcmp(text, "LV_LABEL_LONG_MODE_SCROLL") == 0) return LV_LABEL_LONG_MODE_SCROLL;
    if(strcmp(text, "LV_LABEL_LONG_MODE_SCROLL_CIRCULAR") == 0) return LV_LABEL_LONG_MODE_SCROLL_CIRCULAR;
    if(strcmp(text, "LV_LABEL_LONG_MODE_CLIP") == 0) return LV_LABEL_LONG_MODE_CLIP;
    return LV_LABEL_LONG_MODE_WRAP;
}

static lv_base_dir_t parse_base_dir_expression(const char *text)
{
    if(strcmp(text, "LV_BASE_DIR_LTR") == 0) return LV_BASE_DIR_LTR;
    if(strcmp(text, "LV_BASE_DIR_RTL") == 0) return LV_BASE_DIR_RTL;
    return LV_BASE_DIR_AUTO;
}

static lv_bar_mode_t parse_bar_mode_expression(const char *text)
{
    if(strcmp(text, "LV_BAR_MODE_SYMMETRICAL") == 0) return LV_BAR_MODE_SYMMETRICAL;
    if(strcmp(text, "LV_BAR_MODE_RANGE") == 0) return LV_BAR_MODE_RANGE;
    return LV_BAR_MODE_NORMAL;
}

static lv_bar_orientation_t parse_bar_orientation_expression(const char *text)
{
    if(strcmp(text, "LV_BAR_ORIENTATION_HORIZONTAL") == 0) return LV_BAR_ORIENTATION_HORIZONTAL;
    if(strcmp(text, "LV_BAR_ORIENTATION_VERTICAL") == 0) return LV_BAR_ORIENTATION_VERTICAL;
    return LV_BAR_ORIENTATION_AUTO;
}

static lv_slider_mode_t parse_slider_mode_expression(const char *text)
{
    if(strcmp(text, "LV_SLIDER_MODE_RANGE") == 0) return LV_SLIDER_MODE_RANGE;
    if(strcmp(text, "LV_SLIDER_MODE_SYMMETRICAL") == 0) return LV_SLIDER_MODE_SYMMETRICAL;
    return LV_SLIDER_MODE_NORMAL;
}

static lv_slider_orientation_t parse_slider_orientation_expression(const char *text)
{
    if(strcmp(text, "LV_SLIDER_ORIENTATION_HORIZONTAL") == 0) return LV_SLIDER_ORIENTATION_HORIZONTAL;
    if(strcmp(text, "LV_SLIDER_ORIENTATION_VERTICAL") == 0) return LV_SLIDER_ORIENTATION_VERTICAL;
    return LV_SLIDER_ORIENTATION_AUTO;
}

static lv_switch_orientation_t parse_switch_orientation_expression(const char *text)
{
    if(strcmp(text, "LV_SWITCH_ORIENTATION_HORIZONTAL") == 0) return LV_SWITCH_ORIENTATION_HORIZONTAL;
    if(strcmp(text, "LV_SWITCH_ORIENTATION_VERTICAL") == 0) return LV_SWITCH_ORIENTATION_VERTICAL;
    return LV_SWITCH_ORIENTATION_AUTO;
}

static lv_obj_flag_t parse_flag_expression(const char *text)
{
    if(text == NULL) return 0;
    if(strcmp(text, "LV_OBJ_FLAG_HIDDEN") == 0) return LV_OBJ_FLAG_HIDDEN;
    if(strcmp(text, "LV_OBJ_FLAG_CLICKABLE") == 0) return LV_OBJ_FLAG_CLICKABLE;
    if(strcmp(text, "LV_OBJ_FLAG_SCROLLABLE") == 0) return LV_OBJ_FLAG_SCROLLABLE;
    return 0;
}

static lv_state_t parse_state_expression(const char *text)
{
    if(text == NULL) return 0;
    if(strcmp(text, "LV_STATE_CHECKED") == 0) return LV_STATE_CHECKED;
    if(strcmp(text, "LV_STATE_DISABLED") == 0) return LV_STATE_DISABLED;
    return 0;
}

static lv_dir_t parse_dir_expression(const char *text)
{
    if(text == NULL) return LV_DIR_NONE;
    if(strcmp(text, "LV_DIR_LEFT") == 0) return LV_DIR_LEFT;
    if(strcmp(text, "LV_DIR_RIGHT") == 0) return LV_DIR_RIGHT;
    if(strcmp(text, "LV_DIR_TOP") == 0) return LV_DIR_TOP;
    if(strcmp(text, "LV_DIR_BOTTOM") == 0) return LV_DIR_BOTTOM;
    if(strcmp(text, "LV_DIR_HOR") == 0) return LV_DIR_HOR;
    if(strcmp(text, "LV_DIR_VER") == 0) return LV_DIR_VER;
    if(strcmp(text, "LV_DIR_ALL") == 0) return LV_DIR_ALL;
    return LV_DIR_NONE;
}

static lv_scale_mode_t parse_scale_mode_expression(const char *text)
{
    if(text == NULL) return LV_SCALE_MODE_HORIZONTAL_TOP;
    if(strcmp(text, "LV_SCALE_MODE_HORIZONTAL_BOTTOM") == 0) return LV_SCALE_MODE_HORIZONTAL_BOTTOM;
    if(strcmp(text, "LV_SCALE_MODE_VERTICAL_LEFT") == 0) return LV_SCALE_MODE_VERTICAL_LEFT;
    if(strcmp(text, "LV_SCALE_MODE_VERTICAL_RIGHT") == 0) return LV_SCALE_MODE_VERTICAL_RIGHT;
    if(strcmp(text, "LV_SCALE_MODE_ROUND_INNER") == 0) return LV_SCALE_MODE_ROUND_INNER;
    if(strcmp(text, "LV_SCALE_MODE_ROUND_OUTER") == 0) return LV_SCALE_MODE_ROUND_OUTER;
    return LV_SCALE_MODE_HORIZONTAL_TOP;
}

static lv_grid_align_t parse_grid_align_expression(const char *text)
{
    if(strcmp(text, "LV_GRID_ALIGN_START") == 0) return LV_GRID_ALIGN_START;
    if(strcmp(text, "LV_GRID_ALIGN_CENTER") == 0) return LV_GRID_ALIGN_CENTER;
    if(strcmp(text, "LV_GRID_ALIGN_END") == 0) return LV_GRID_ALIGN_END;
    if(strcmp(text, "LV_GRID_ALIGN_SPACE_EVENLY") == 0) return LV_GRID_ALIGN_SPACE_EVENLY;
    if(strcmp(text, "LV_GRID_ALIGN_SPACE_AROUND") == 0) return LV_GRID_ALIGN_SPACE_AROUND;
    if(strcmp(text, "LV_GRID_ALIGN_SPACE_BETWEEN") == 0) return LV_GRID_ALIGN_SPACE_BETWEEN;
    return LV_GRID_ALIGN_STRETCH;
}

static bool parse_string_literal(const char *text, char *output, size_t output_size)
{
    if(text == NULL || output == NULL || output_size == 0 || text[0] != '"') {
        return false;
    }

    size_t out_index = 0;
    for(const char *cursor = text + 1; *cursor != '\0' && out_index + 1 < output_size; cursor++) {
        if(*cursor == '"' && cursor[-1] != '\\') {
            output[out_index] = '\0';
            return true;
        }

        if(*cursor == '\\') {
            cursor++;
            if(*cursor == 'n') output[out_index++] = '\n';
            else if(*cursor == 'r') output[out_index++] = '\r';
            else if(*cursor == 't') output[out_index++] = '\t';
            else if(*cursor != '\0') output[out_index++] = *cursor;
            else break;
            continue;
        }

        output[out_index++] = *cursor;
    }

    output[out_index] = '\0';
    return true;
}

static int32_t parse_grid_track_expression(const char *text)
{
    if(strcmp(text, "LV_GRID_TEMPLATE_LAST") == 0) {
        return LV_GRID_TEMPLATE_LAST;
    }

    if(strcmp(text, "LV_GRID_CONTENT") == 0) {
        return LV_GRID_CONTENT;
    }

    int fr_value = 0;
    if(sscanf(text, "LV_GRID_FR(%d)", &fr_value) == 1) {
        return LV_GRID_FR(fr_value);
    }

    int direct_value = 0;
    if(sscanf(text, "%d", &direct_value) == 1) {
        return direct_value;
    }

    return LV_GRID_TEMPLATE_LAST;
}

static bool parse_grid_array_definition(
    const char *line,
    simulator_grid_array_binding_t *array_bindings,
    size_t *array_binding_count)
{
    const char *prefix = "static int32_t ";
    if(strncmp(line, prefix, strlen(prefix)) != 0) {
        return false;
    }

    char name[128] = { 0 };
    const char *name_start = line + strlen(prefix);
    const char *name_end = strstr(name_start, "[]");
    const char *brace_start = strchr(line, '{');
    const char *brace_end = strrchr(line, '}');
    if(name_end == NULL || brace_start == NULL || brace_end == NULL || name_end <= name_start || brace_end <= brace_start) {
        return false;
    }

    size_t name_length = (size_t)(name_end - name_start);
    if(name_length >= sizeof(name)) {
        name_length = sizeof(name) - 1;
    }

    memcpy(name, name_start, name_length);
    name[name_length] = '\0';
    trim_whitespace(name);

    char values_text[1024];
    size_t values_length = (size_t)(brace_end - brace_start - 1);
    if(values_length >= sizeof(values_text)) {
        values_length = sizeof(values_text) - 1;
    }
    memcpy(values_text, brace_start + 1, values_length);
    values_text[values_length] = '\0';

    char parts[64][256];
    int part_count = 0;
    if(!split_arguments(values_text, parts, 64, &part_count) || part_count <= 0) {
        return false;
    }

    int32_t *values = malloc((size_t)part_count * sizeof(int32_t));
    if(values == NULL) {
        return false;
    }

    for(int i = 0; i < part_count; i++) {
        values[i] = parse_grid_track_expression(parts[i]);
    }

    return add_grid_array_binding(array_bindings, array_binding_count, name, values);
}

static bool parse_point_array_definition(
    const char *line,
    simulator_point_array_binding_t *array_bindings,
    size_t *array_binding_count)
{
    const char *prefix = "static lv_point_precise_t ";
    if(strncmp(line, prefix, strlen(prefix)) != 0) {
        return false;
    }

    char name[128] = { 0 };
    const char *name_start = line + strlen(prefix);
    const char *name_end = strstr(name_start, "[]");
    const char *brace_start = strchr(line, '{');
    const char *brace_end = strrchr(line, '}');
    if(name_end == NULL || brace_start == NULL || brace_end == NULL || name_end <= name_start || brace_end <= brace_start) {
        return false;
    }

    size_t name_length = (size_t)(name_end - name_start);
    if(name_length >= sizeof(name)) {
        name_length = sizeof(name) - 1;
    }

    memcpy(name, name_start, name_length);
    name[name_length] = '\0';
    trim_whitespace(name);

    char values_text[1024];
    size_t values_length = (size_t)(brace_end - brace_start - 1);
    if(values_length >= sizeof(values_text)) {
        values_length = sizeof(values_text) - 1;
    }
    memcpy(values_text, brace_start + 1, values_length);
    values_text[values_length] = '\0';

    char parts[64][256];
    int part_count = 0;
    if(!split_arguments(values_text, parts, 64, &part_count) || part_count <= 0) {
        return false;
    }

    lv_point_precise_t *values = malloc((size_t)part_count * sizeof(lv_point_precise_t));
    if(values == NULL) {
        return false;
    }

    for(int i = 0; i < part_count; i++) {
        int x = 0;
        int y = 0;
        if(sscanf(parts[i], "{ %d , %d }", &x, &y) != 2 &&
           sscanf(parts[i], "{%d , %d}", &x, &y) != 2 &&
           sscanf(parts[i], "{ %d, %d }", &x, &y) != 2 &&
           sscanf(parts[i], "{%d,%d}", &x, &y) != 2) {
            free(values);
            return false;
        }

        values[i].x = (float)x;
        values[i].y = (float)y;
    }

    return add_point_array_binding(array_bindings, array_binding_count, name, values, (uint32_t)part_count);
}

static bool interpret_generated_c_line(
    const char *line,
    simulator_object_binding_t *object_bindings,
    size_t *object_binding_count,
    simulator_scale_section_binding_t *section_bindings,
    size_t *section_binding_count,
    simulator_grid_array_binding_t *array_bindings,
    size_t *array_binding_count,
    simulator_point_array_binding_t *point_array_bindings,
    size_t *point_array_binding_count,
    lv_obj_t **root_screen)
{
    char trimmed[2048];
    snprintf(trimmed, sizeof(trimmed), "%s", line);
    trim_whitespace(trimmed);
    if(trimmed[0] == '\0' || strncmp(trimmed, "//", 2) == 0 || strncmp(trimmed, "#", 1) == 0 || strncmp(trimmed, "/*", 2) == 0) {
        return true;
    }

    if(parse_grid_array_definition(trimmed, array_bindings, array_binding_count)) {
        return true;
    }

    if(parse_point_array_definition(trimmed, point_array_bindings, point_array_binding_count)) {
        return true;
    }

    if(strncmp(trimmed, "lv_scale_section_t *", 20) == 0) {
        char variable_name[128] = { 0 };
        char function_name[128] = { 0 };
        char owner_name[128] = { 0 };
        if(sscanf(trimmed, "lv_scale_section_t *%127[^ =] = %127[^ (](%127[^)]);", variable_name, function_name, owner_name) == 3) {
            lv_obj_t *scale = find_object_binding(object_bindings, *object_binding_count, owner_name);
            lv_scale_section_t *section = NULL;

            if(scale != NULL && strcmp(function_name, "lv_scale_add_section") == 0) {
                section = lv_scale_add_section(scale);
            }

            if(section == NULL) {
                return false;
            }

            return add_scale_section_binding(section_bindings, section_binding_count, variable_name, section);
        }
    }

    if(strncmp(trimmed, "lv_obj_t *", 10) == 0) {
        char variable_name[128] = { 0 };
        char function_name[128] = { 0 };
        char parent_name[128] = { 0 };
        if(sscanf(trimmed, "lv_obj_t *%127[^ =] = %127[^ (](%127[^)]);", variable_name, function_name, parent_name) == 3) {
            lv_obj_t *parent = strcmp(parent_name, "NULL") == 0 ? NULL : find_object_binding(object_bindings, *object_binding_count, parent_name);
            lv_obj_t *object = NULL;

            if(strcmp(function_name, "lv_obj_create") == 0) object = lv_obj_create(parent);
            else if(strcmp(function_name, "lv_animimg_create") == 0) object = lv_animimg_create(parent);
            else if(strcmp(function_name, "lv_arc_create") == 0) object = lv_arc_create(parent);
            else if(strcmp(function_name, "lv_arclabel_create") == 0) object = lv_arclabel_create(parent);
            else if(strcmp(function_name, "lv_bar_create") == 0) object = lv_bar_create(parent);
            else if(strcmp(function_name, "lv_button_create") == 0) object = lv_button_create(parent);
            else if(strcmp(function_name, "lv_buttonmatrix_create") == 0) object = lv_buttonmatrix_create(parent);
            else if(strcmp(function_name, "lv_canvas_create") == 0) object = lv_canvas_create(parent);
            else if(strcmp(function_name, "lv_checkbox_create") == 0) object = lv_checkbox_create(parent);
            else if(strcmp(function_name, "lv_dropdown_create") == 0) object = lv_dropdown_create(parent);
            else if(strcmp(function_name, "lv_label_create") == 0) object = lv_label_create(parent);
            else if(strcmp(function_name, "lv_image_create") == 0) object = lv_image_create(parent);
            else if(strcmp(function_name, "lv_imagebutton_create") == 0) object = lv_imagebutton_create(parent);
            else if(strcmp(function_name, "lv_led_create") == 0) object = lv_led_create(parent);
            else if(strcmp(function_name, "lv_line_create") == 0) object = lv_line_create(parent);
#if LV_USE_QRCODE
            else if(strcmp(function_name, "lv_qrcode_create") == 0) object = lv_qrcode_create(parent);
#endif
            else if(strcmp(function_name, "lv_roller_create") == 0) object = lv_roller_create(parent);
            else if(strcmp(function_name, "lv_scale_create") == 0) object = lv_scale_create(parent);
            else if(strcmp(function_name, "lv_slider_create") == 0) object = lv_slider_create(parent);
            else if(strcmp(function_name, "lv_spinbox_create") == 0) object = lv_spinbox_create(parent);
            else if(strcmp(function_name, "lv_spinner_create") == 0) object = lv_spinner_create(parent);
            else if(strcmp(function_name, "lv_switch_create") == 0) object = lv_switch_create(parent);
            else if(strcmp(function_name, "lv_textarea_create") == 0) object = lv_textarea_create(parent);

            if(object == NULL) {
                return false;
            }

            if(parent == NULL && root_screen != NULL && *root_screen == NULL) {
                *root_screen = object;
            }

            if(parent != NULL) {
                register_object_event_callbacks(object, variable_name);
            }

            return add_object_binding(object_bindings, object_binding_count, variable_name, object);
        }
    }

    char args[8][256];
    int arg_count = 0;

    if(extract_call_arguments(trimmed, "lv_obj_set_grid_dsc_array", args, 8, &arg_count) && arg_count == 3) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        int32_t *col_values = strcmp(args[1], "NULL") == 0 ? NULL : find_grid_array_binding(array_bindings, *array_binding_count, args[1]);
        int32_t *row_values = strcmp(args[2], "NULL") == 0 ? NULL : find_grid_array_binding(array_bindings, *array_binding_count, args[2]);
        if(object == NULL) return false;
        lv_obj_set_grid_dsc_array(object, col_values, row_values);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_grid_cell", args, 8, &arg_count) && arg_count == 7) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_obj_set_grid_cell(
            object,
            parse_grid_align_expression(args[1]),
            atoi(args[2]),
            atoi(args[3]),
            parse_grid_align_expression(args[4]),
            atoi(args[5]),
            atoi(args[6]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_style_bg_color", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        lv_color_t color;
        if(object == NULL || !parse_color_expression(args[1], &color)) return false;
        lv_obj_set_style_bg_color(object, color, LV_PART_MAIN | LV_STATE_DEFAULT);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_style_border_color", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        lv_color_t color;
        if(object == NULL || !parse_color_expression(args[1], &color)) return false;
        lv_obj_set_style_border_color(object, color, LV_PART_MAIN | LV_STATE_DEFAULT);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_style_text_color", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        lv_color_t color;
        if(object == NULL || !parse_color_expression(args[1], &color)) return false;
        lv_obj_set_style_text_color(object, color, LV_PART_MAIN | LV_STATE_DEFAULT);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_led_set_color", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        lv_color_t color;
        if(object == NULL || !parse_color_expression(args[1], &color)) return false;
        lv_led_set_color(object, color);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_led_set_brightness", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_led_set_brightness(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_style_border_width", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_obj_set_style_border_width(object, atoi(args[1]), LV_PART_MAIN | LV_STATE_DEFAULT);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_style_bg_opa", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_obj_set_style_bg_opa(object, atoi(args[1]), LV_PART_MAIN | LV_STATE_DEFAULT);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_style_opa", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_obj_set_style_opa(object, atoi(args[1]), LV_PART_MAIN | LV_STATE_DEFAULT);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_style_pad_all", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_obj_set_style_pad_all(object, atoi(args[1]), LV_PART_MAIN | LV_STATE_DEFAULT);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_style_pad_column", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_obj_set_style_pad_column(object, atoi(args[1]), LV_PART_MAIN | LV_STATE_DEFAULT);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_style_pad_row", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_obj_set_style_pad_row(object, atoi(args[1]), LV_PART_MAIN | LV_STATE_DEFAULT);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_style_radius", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_obj_set_style_radius(object, atoi(args[1]), LV_PART_MAIN | LV_STATE_DEFAULT);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_size", args, 8, &arg_count) && arg_count == 3) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        lv_coord_t width;
        lv_coord_t height;
        if(object == NULL || !parse_coord_expression(args[1], &width) || !parse_coord_expression(args[2], &height)) return false;
        lv_obj_set_size(object, width, height);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_width", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        lv_coord_t width;
        if(object == NULL || !parse_coord_expression(args[1], &width)) return false;
        lv_obj_set_width(object, width);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_height", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        lv_coord_t height;
        if(object == NULL || !parse_coord_expression(args[1], &height)) return false;
        lv_obj_set_height(object, height);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_x", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_obj_set_x(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_y", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_obj_set_y(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_layout", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_obj_set_layout(object, parse_layout_expression(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_flex_flow", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_obj_set_flex_flow(object, parse_flex_flow_expression(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_flex_align", args, 8, &arg_count) && arg_count == 4) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_obj_set_flex_align(
            object,
            parse_flex_align_expression(args[1]),
            parse_flex_align_expression(args[2]),
            parse_flex_align_expression(args[3]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_align", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_obj_set_align(object, parse_align_expression(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_add_flag", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        lv_obj_flag_t flag = parse_flag_expression(args[1]);
        if(object == NULL || flag == 0) return false;
        lv_obj_add_flag(object, flag);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_remove_flag", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        lv_obj_flag_t flag = parse_flag_expression(args[1]);
        if(object == NULL || flag == 0) return false;
        lv_obj_remove_flag(object, flag);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_add_state", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        lv_state_t state = parse_state_expression(args[1]);
        if(object == NULL || state == 0) return false;
        lv_obj_add_state(object, state);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_remove_state", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        lv_state_t state = parse_state_expression(args[1]);
        if(object == NULL || state == 0) return false;
        lv_obj_remove_state(object, state);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_style_text_font", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        /* Keep the interpreter tolerant here: the generated code may reference fonts
           that are not linked into the native host. In that case we keep LV_FONT_DEFAULT. */
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_style_text_align", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_obj_set_style_text_align(object, parse_text_align_expression(args[1]), LV_PART_MAIN | LV_STATE_DEFAULT);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_obj_set_style_base_dir", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_obj_set_style_base_dir(object, parse_base_dir_expression(args[1]), LV_PART_MAIN | LV_STATE_DEFAULT);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_label_set_text", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        char text[512];
        if(object == NULL || !parse_string_literal(args[1], text, sizeof(text))) return false;
        lv_label_set_text(object, text);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_label_set_long_mode", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_label_set_long_mode(object, parse_label_long_mode_expression(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_checkbox_set_text", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        char text[512];
        if(object == NULL || !parse_string_literal(args[1], text, sizeof(text))) return false;
        lv_checkbox_set_text(object, text);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_dropdown_set_text", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        char text[512];
        if(object == NULL || !parse_string_literal(args[1], text, sizeof(text))) return false;
        lv_dropdown_set_text(object, text);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_dropdown_set_options", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        char text[2048];
        if(object == NULL || !parse_string_literal(args[1], text, sizeof(text))) return false;
        lv_dropdown_set_options(object, text);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_dropdown_set_selected", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_dropdown_set_selected(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_dropdown_set_dir", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_dropdown_set_dir(object, parse_dir_expression(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_dropdown_set_symbol", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        char text[512];
        if(object == NULL || !parse_string_literal(args[1], text, sizeof(text))) return false;
        lv_dropdown_set_symbol(object, text);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_dropdown_set_selected_highlight", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_dropdown_set_selected_highlight(object, strcmp(args[1], "true") == 0);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_line_set_points", args, 8, &arg_count) && arg_count == 3) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        uint32_t point_count = 0;
        lv_point_precise_t *points = find_point_array_binding(point_array_bindings, *point_array_binding_count, args[1], &point_count);
        if(object == NULL || points == NULL) return false;
        lv_line_set_points(object, points, point_count);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_line_set_y_invert", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_line_set_y_invert(object, strcmp(args[1], "true") == 0);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_arclabel_set_text", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        char text[2048];
        if(object == NULL || !parse_string_literal(args[1], text, sizeof(text))) return false;
        lv_arclabel_set_text(object, text);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_arclabel_set_angle_start", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_arclabel_set_angle_start(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_arclabel_set_radius", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_arclabel_set_radius(object, (uint32_t)atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_bar_set_range", args, 8, &arg_count) && arg_count == 3) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_bar_set_range(object, atoi(args[1]), atoi(args[2]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_bar_set_mode", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_bar_set_mode(object, parse_bar_mode_expression(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_bar_set_orientation", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_bar_set_orientation(object, parse_bar_orientation_expression(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_bar_set_value", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_bar_set_value(object, atoi(args[1]), arg_count >= 3 && strcmp(args[2], "LV_ANIM_ON") == 0 ? LV_ANIM_ON : LV_ANIM_OFF);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_bar_set_start_value", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_bar_set_start_value(object, atoi(args[1]), arg_count >= 3 && strcmp(args[2], "LV_ANIM_ON") == 0 ? LV_ANIM_ON : LV_ANIM_OFF);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_slider_set_range", args, 8, &arg_count) && arg_count == 3) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_slider_set_range(object, atoi(args[1]), atoi(args[2]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_slider_set_mode", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_slider_set_mode(object, parse_slider_mode_expression(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_slider_set_orientation", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_slider_set_orientation(object, parse_slider_orientation_expression(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_slider_set_value", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_slider_set_value(object, atoi(args[1]), arg_count >= 3 && strcmp(args[2], "LV_ANIM_ON") == 0 ? LV_ANIM_ON : LV_ANIM_OFF);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_slider_set_start_value", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_slider_set_start_value(object, atoi(args[1]), arg_count >= 3 && strcmp(args[2], "LV_ANIM_ON") == 0 ? LV_ANIM_ON : LV_ANIM_OFF);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_image_set_src", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        char source[512];
        if(object == NULL) return false;
        if(args[1][0] == '"') {
            if(!parse_string_literal(args[1], source, sizeof(source))) return false;
            lv_image_set_src(object, source);
        }
        else {
            lv_image_set_src(object, args[1]);
        }
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_arc_set_range", args, 8, &arg_count) && arg_count == 3) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_arc_set_range(object, atoi(args[1]), atoi(args[2]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_arc_set_value", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_arc_set_value(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_arc_set_mode", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_arc_set_mode(object, strcmp(args[1], "LV_ARC_MODE_REVERSE") == 0 ? LV_ARC_MODE_REVERSE : strcmp(args[1], "LV_ARC_MODE_SYMMETRICAL") == 0 ? LV_ARC_MODE_SYMMETRICAL : LV_ARC_MODE_NORMAL);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_arc_set_rotation", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_arc_set_rotation(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_arc_set_angles", args, 8, &arg_count) && arg_count == 3) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_arc_set_angles(object, atoi(args[1]), atoi(args[2]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_arc_set_bg_angles", args, 8, &arg_count) && arg_count == 3) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_arc_set_bg_angles(object, atoi(args[1]), atoi(args[2]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_roller_set_options", args, 8, &arg_count) && arg_count == 3) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        char text[2048];
        if(object == NULL || !parse_string_literal(args[1], text, sizeof(text))) return false;
        lv_roller_set_options(object, text, strcmp(args[2], "LV_ROLLER_MODE_INFINITE") == 0 ? LV_ROLLER_MODE_INFINITE : LV_ROLLER_MODE_NORMAL);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_roller_set_selected", args, 8, &arg_count) && arg_count >= 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_roller_set_selected(object, atoi(args[1]), arg_count >= 3 && strcmp(args[2], "LV_ANIM_ON") == 0 ? LV_ANIM_ON : LV_ANIM_OFF);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_roller_set_visible_row_count", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_roller_set_visible_row_count(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_scale_set_mode", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_scale_set_mode(object, parse_scale_mode_expression(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_scale_set_total_tick_count", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_scale_set_total_tick_count(object, (uint32_t)atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_scale_set_major_tick_every", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_scale_set_major_tick_every(object, (uint32_t)atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_scale_set_label_show", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_scale_set_label_show(object, strcmp(args[1], "true") == 0);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_scale_set_range", args, 8, &arg_count) && arg_count == 3) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_scale_set_range(object, atoi(args[1]), atoi(args[2]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_scale_set_angle_range", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_scale_set_angle_range(object, (uint32_t)atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_scale_set_rotation", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_scale_set_rotation(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_scale_set_post_draw", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_scale_set_post_draw(object, strcmp(args[1], "true") == 0);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_scale_set_draw_ticks_on_top", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_scale_set_draw_ticks_on_top(object, strcmp(args[1], "true") == 0);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_scale_set_section_min_value", args, 8, &arg_count) && arg_count == 3) {
        lv_obj_t *scale = find_object_binding(object_bindings, *object_binding_count, args[0]);
        lv_scale_section_t *section = find_scale_section_binding(section_bindings, *section_binding_count, args[1]);
        if(scale == NULL || section == NULL) return false;
        lv_scale_set_section_min_value(scale, section, atoi(args[2]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_scale_set_section_max_value", args, 8, &arg_count) && arg_count == 3) {
        lv_obj_t *scale = find_object_binding(object_bindings, *object_binding_count, args[0]);
        lv_scale_section_t *section = find_scale_section_binding(section_bindings, *section_binding_count, args[1]);
        if(scale == NULL || section == NULL) return false;
        lv_scale_set_section_max_value(scale, section, atoi(args[2]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_spinbox_set_range", args, 8, &arg_count) && arg_count == 3) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_spinbox_set_range(object, atoi(args[1]), atoi(args[2]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_spinbox_set_value", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_spinbox_set_value(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_spinbox_set_digit_format", args, 8, &arg_count) && arg_count == 3) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_spinbox_set_digit_format(object, atoi(args[1]), atoi(args[2]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_spinbox_set_step", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_spinbox_set_step(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_spinbox_set_rollover", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_spinbox_set_rollover(object, strcmp(args[1], "true") == 0);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_spinbox_set_cursor_pos", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_spinbox_set_cursor_pos(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_textarea_set_text", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        char text[2048];
        if(object == NULL || !parse_string_literal(args[1], text, sizeof(text))) return false;
        lv_textarea_set_text(object, text);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_textarea_set_placeholder_text", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        char text[2048];
        if(object == NULL || !parse_string_literal(args[1], text, sizeof(text))) return false;
        lv_textarea_set_placeholder_text(object, text);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_textarea_set_cursor_pos", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_textarea_set_cursor_pos(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_textarea_set_one_line", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_textarea_set_one_line(object, strcmp(args[1], "true") == 0);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_textarea_set_password_mode", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_textarea_set_password_mode(object, strcmp(args[1], "true") == 0);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_textarea_set_password_show_time", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_textarea_set_password_show_time(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_textarea_set_text_selection", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_textarea_set_text_selection(object, strcmp(args[1], "true") == 0);
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_textarea_set_align", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_textarea_set_align(object, parse_text_align_expression(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_spinner_set_anim_params", args, 8, &arg_count) && arg_count == 3) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_spinner_set_anim_params(object, atoi(args[1]), atoi(args[2]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_image_set_pivot", args, 8, &arg_count) && arg_count == 3) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_image_set_pivot(object, atoi(args[1]), atoi(args[2]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_image_set_rotation", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_image_set_rotation(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_image_set_scale_x", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_image_set_scale_x(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_image_set_scale_y", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_image_set_scale_y(object, atoi(args[1]));
        return true;
    }

    if(extract_call_arguments(trimmed, "lv_switch_set_orientation", args, 8, &arg_count) && arg_count == 2) {
        lv_obj_t *object = find_object_binding(object_bindings, *object_binding_count, args[0]);
        if(object == NULL) return false;
        lv_switch_set_orientation(object, parse_switch_orientation_expression(args[1]));
        return true;
    }

    if(strncmp(trimmed, "return ", 7) == 0 || strncmp(trimmed, "void ", 5) == 0 || strncmp(trimmed, "lv_screen_load(", 15) == 0 || strcmp(trimmed, "}") == 0 || strcmp(trimmed, "{") == 0) {
        return true;
    }

    /* Handle alias assignment: handle_name = local_var_name;
       Generated for exported handles, e.g. "m1_cmd_backward = m1_cmd_backward_obj;" */
    {
        char lhs[128] = {0};
        char rhs[128] = {0};
        if(sscanf(trimmed, "%127[A-Za-z0-9_] = %127[A-Za-z0-9_];", lhs, rhs) == 2) {
            lv_obj_t *existing = find_object_binding(object_bindings, *object_binding_count, rhs);
            if(existing != NULL) {
                add_object_binding(object_bindings, object_binding_count, lhs, existing);
            }
            return true;
        }
    }

    return true;
}

static bool lvgl_runtime_build_screen_from_generated_c(
    const char *content,
    lv_obj_t **root_screen)
{
    memset(g_object_bindings, 0, sizeof(g_object_bindings));
    g_object_binding_count = 0;
    g_highlighted_object = NULL;

    simulator_scale_section_binding_t section_bindings[128];
    simulator_grid_array_binding_t array_bindings[128];
    simulator_point_array_binding_t point_array_bindings[128];
    size_t section_binding_count = 0;
    size_t array_binding_count = 0;
    size_t point_array_binding_count = 0;
    memset(section_bindings, 0, sizeof(section_bindings));
    memset(array_bindings, 0, sizeof(array_bindings));
    memset(point_array_bindings, 0, sizeof(point_array_bindings));

    char *content_copy = strdup(content);
    if(content_copy == NULL) {
        return false;
    }

    bool success = true;
    char *save_ptr = NULL;
    char *line = strtok_r(content_copy, "\n", &save_ptr);
    while(line != NULL) {
        if(!interpret_generated_c_line(
                line,
                g_object_bindings,
                &g_object_binding_count,
                section_bindings,
                &section_binding_count,
                array_bindings,
                &array_binding_count,
                point_array_bindings,
                &point_array_binding_count,
                root_screen)) {
            success = false;
            break;
        }
        line = strtok_r(NULL, "\n", &save_ptr);
    }

    free(content_copy);
    return success && root_screen != NULL && *root_screen != NULL;
}

void lvgl_runtime_apply_highlight(const char *object_id)
{
    if(g_highlighted_object != NULL) {
        lv_obj_set_style_outline_width(g_highlighted_object, 0, LV_PART_MAIN);
        lv_obj_invalidate(g_highlighted_object);
        g_highlighted_object = NULL;
    }

    if(object_id == NULL || object_id[0] == '\0') {
        return;
    }

    lv_obj_t *obj = find_object_binding(g_object_bindings, g_object_binding_count, object_id);
    if(obj == NULL) {
        return;
    }

    g_highlighted_object = obj;
    lv_obj_set_style_outline_color(obj, lv_color_hex(0xFF0000), LV_PART_MAIN);
    lv_obj_set_style_outline_width(obj, 2, LV_PART_MAIN);
    lv_obj_set_style_outline_pad(obj, 2, LV_PART_MAIN);
    lv_obj_invalidate(obj);
}

#endif

#if !(LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL)
void lvgl_runtime_apply_highlight(const char *object_id)
{
    (void)object_id;
}
#endif

bool lvgl_runtime_present_boot_screen(lvgl_simulator_runtime_t *runtime)
{
    if (runtime == NULL) {
        return false;
    }

#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
    printf("Presenting initial LVGL boot screen.\n");

    if(!lvgl_runtime_lock()) {
        return false;
    }

    lv_obj_t *panel = create_base_screen("LVGL C Simulator", "Runtime initialized. Waiting for generated C data.");

    lv_obj_t *label = lv_label_create(panel);
    lv_label_set_text(label, "The first real LVGL screen is alive.");
    lv_obj_set_style_text_font(label, &lv_font_montserrat_20, 0);

    load_new_screen(lv_obj_get_parent(panel));
    lvgl_runtime_unlock();
#else
    printf("Boot screen placeholder: simulator runtime is ready for the first real screen.\n");
#endif

    fflush(stdout);
    runtime->first_screen_presented = true;
    return true;
}

bool lvgl_runtime_load_screen_from_generated_c(
    lvgl_simulator_runtime_t *runtime,
    const char *document_name,
    const char *content,
    bool force_full_reload,
    char *status_message,
    size_t status_message_size)
{
    if (runtime == NULL || document_name == NULL) {
        if (status_message != NULL && status_message_size > 0) {
            snprintf(status_message, status_message_size, "Invalid runtime or document name.");
        }

        return false;
    }

#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
    if(!lvgl_runtime_lock()) {
        if(status_message != NULL && status_message_size > 0) {
            snprintf(status_message, status_message_size, "Could not lock the LVGL runtime.");
        }

        return false;
    }

    detach_current_screen_for_component_reload();
    free_registered_grid_arrays();
    free_registered_point_arrays();
    free_registered_callback_names();

    lv_obj_t *screen = NULL;
    if(!lvgl_runtime_build_screen_from_generated_c(content, &screen)) {
        lvgl_runtime_unlock();

        printf("Generated C screen creation failed for '%s'. Falling back to demo screen.\n", document_name);
        fflush(stdout);
        if(status_message != NULL && status_message_size > 0) {
            snprintf(
                status_message,
                status_message_size,
                "Generated C screen creation failed for '%s'. Falling back to demo screen.",
                document_name);
        }
        return lvgl_runtime_present_demo_screen(runtime, document_name, strlen(content), status_message, status_message_size);
    }

    printf("Loading LVGL screen for '%s' from generated C content.\n", document_name);
    fflush(stdout);
    auto_bind_keyboards(screen);
    load_new_screen(screen);
    lvgl_runtime_show_window();
    lvgl_runtime_unlock();

    printf(
        "LVGL C screen rendered for '%s' (%s).\n",
        document_name,
        force_full_reload ? "reload" : "render");
    fflush(stdout);

    if(status_message != NULL && status_message_size > 0) {
        snprintf(
            status_message,
            status_message_size,
            "LVGL C screen rendered for '%s'.",
            document_name);
    }

    return true;
#else
    return lvgl_runtime_present_demo_screen(runtime, document_name, strlen(content), status_message, status_message_size);
#endif
}

bool lvgl_runtime_present_demo_screen(
    lvgl_simulator_runtime_t *runtime,
    const char *document_name,
    size_t content_length,
    char *status_message,
    size_t status_message_size)
{
    if (runtime == NULL || document_name == NULL) {
        if (status_message != NULL && status_message_size > 0) {
            snprintf(status_message, status_message_size, "Demo screen could not be created.");
        }

        return false;
    }

#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
    if(!lvgl_runtime_lock()) {
        if (status_message != NULL && status_message_size > 0) {
            snprintf(status_message, status_message_size, "Could not lock the LVGL runtime.");
        }

        return false;
    }

    lv_obj_t *panel = create_base_screen("LVGL C Preview", "Current document rendered via native LVGL runtime.");

    char document_line[640];
    snprintf(document_line, sizeof(document_line), "Document: %s", document_name);
    lv_obj_t *doc_label = lv_label_create(panel);
    lv_label_set_text(doc_label, document_line);
    lv_obj_set_style_text_font(doc_label, &lv_font_montserrat_20, 0);

    char content_line[640];
    snprintf(content_line, sizeof(content_line), "Generated content size: %zu bytes", content_length);
    lv_obj_t *content_label = lv_label_create(panel);
    lv_label_set_text(content_label, content_line);

    lv_obj_t *hint_label = lv_label_create(panel);
    lv_label_set_text(hint_label, "Next step: replace this demo screen with generated C driven LVGL object creation.");

    load_new_screen(lv_obj_get_parent(panel));
    lvgl_runtime_show_window();
    lvgl_runtime_unlock();

    printf("Demo LVGL screen rendered for '%s'.\n", document_name);
#else
    printf("Demo screen placeholder for document '%s'.\n", document_name);
    printf("Planned UI: header label, status line and basic root container.\n");
    fflush(stdout);
#endif

    if (status_message != NULL && status_message_size > 0) {
        snprintf(
            status_message,
            status_message_size,
            "Demo screen prepared for '%s'. Real generated-C driven LVGL screen creation is the next step.",
            document_name);
    }

    return true;
}
