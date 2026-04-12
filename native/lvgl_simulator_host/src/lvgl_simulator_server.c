#include "lvgl_simulator_server.h"
#include "lvgl_simulator_protocol.h"

#include <errno.h>
#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef _WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
typedef SOCKET simulator_socket_t;
#define SIMULATOR_INVALID_SOCKET INVALID_SOCKET
#else
#include <arpa/inet.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <sys/types.h>
#include <unistd.h>
typedef int simulator_socket_t;
#define SIMULATOR_INVALID_SOCKET (-1)
#endif

#define LISTEN_BACKLOG 4
#define MAX_LINE_LENGTH 65535
#define MAX_STRING_VALUE 512
#define MAX_STATUS_MESSAGE 512

static int handle_client(simulator_socket_t client_fd, lvgl_simulator_runtime_t *runtime);
static ssize_t read_line(simulator_socket_t fd, char *buffer, size_t buffer_size);
static bool json_contains_command(const char *line, const char *command);
static bool extract_json_string(const char *line, const char *key, char *output, size_t output_size);
static bool extract_json_int(const char *line, const char *key, int *value);
static bool extract_json_bool(const char *line, const char *key, bool *value);
static bool send_reply(simulator_socket_t fd, bool success, const char *status_message);
static void log_info(const char *message);
static void log_error(const char *message);
static void close_socket(simulator_socket_t fd);
static int initialize_socket_layer(void);
static void shutdown_socket_layer(void);

int lvgl_simulator_server_run(int port, lvgl_simulator_runtime_t *runtime)
{
    if (initialize_socket_layer() != 0) {
        return 1;
    }

    simulator_socket_t server_fd = socket(AF_INET, SOCK_STREAM, 0);
    if (server_fd == SIMULATOR_INVALID_SOCKET) {
        perror("socket");
        shutdown_socket_layer();
        return 1;
    }

    const int reuse = 1;
#ifdef _WIN32
    if (setsockopt(server_fd, SOL_SOCKET, SO_REUSEADDR, (const char *)&reuse, sizeof(reuse)) < 0) {
#else
    if (setsockopt(server_fd, SOL_SOCKET, SO_REUSEADDR, &reuse, sizeof(reuse)) < 0) {
#endif
        perror("setsockopt");
        close_socket(server_fd);
        shutdown_socket_layer();
        return 1;
    }

    struct sockaddr_in address;
    memset(&address, 0, sizeof(address));
    address.sin_family = AF_INET;
    address.sin_port = htons((uint16_t)port);
    address.sin_addr.s_addr = htonl(INADDR_LOOPBACK);

    if (bind(server_fd, (struct sockaddr *)&address, sizeof(address)) < 0) {
        perror("bind");
        close_socket(server_fd);
        shutdown_socket_layer();
        return 1;
    }

    if (listen(server_fd, LISTEN_BACKLOG) < 0) {
        perror("listen");
        close_socket(server_fd);
        shutdown_socket_layer();
        return 1;
    }

    printf("Simulator host listening on 127.0.0.1:%d\n", port);
    fflush(stdout);

    while (true) {
        const simulator_socket_t client_fd = accept(server_fd, NULL, NULL);
        if (client_fd == SIMULATOR_INVALID_SOCKET) {
            if (errno == EINTR) {
                continue;
            }

            perror("accept");
            close_socket(server_fd);
            shutdown_socket_layer();
            return 1;
        }

        const int client_result = handle_client(client_fd, runtime);
        close_socket(client_fd);

        if (client_result == 2) {
            break;
        }

        if (client_result != 0) {
            close_socket(server_fd);
            shutdown_socket_layer();
            return client_result;
        }
    }

    log_info("Simulator host shutting down.");
    close_socket(server_fd);
    shutdown_socket_layer();
    return 0;
}

static int handle_client(simulator_socket_t client_fd, lvgl_simulator_runtime_t *runtime)
{
    char line[MAX_LINE_LENGTH];

    while (true) {
        const ssize_t line_length = read_line(client_fd, line, sizeof(line));
        if (line_length == 0) {
            return 0;
        }

        if (line_length < 0) {
            log_error("Failed to read command line from client.");
            return 1;
        }

        if (json_contains_command(line, LVGL_PREVIEW_COMMAND_SHUTDOWN)) {
            log_info("Shutdown command received.");
            send_reply(client_fd, true, "Native simulator host will shut down.");
            return 2;
        }

        if (json_contains_command(line, LVGL_PREVIEW_COMMAND_HIGHLIGHT)) {
            char object_id[MAX_STRING_VALUE];
            if (!extract_json_string(line, "objectId", object_id, sizeof(object_id))) {
                object_id[0] = '\0';
            }

            lvgl_simulator_runtime_highlight(runtime, object_id);

            if (!send_reply(client_fd, true, "highlight applied")) {
                return 1;
            }

            continue;
        }

        if (json_contains_command(line, LVGL_PREVIEW_COMMAND_RENDER) ||
            json_contains_command(line, LVGL_PREVIEW_COMMAND_RELOAD)) {
            char document_name[MAX_STRING_VALUE];
            char content_buffer[MAX_LINE_LENGTH];
            char status_message[MAX_STATUS_MESSAGE];
            int screen_width = 0;
            int screen_height = 0;
            int zoom_percent = 0;
            bool reset_window_to_target_size = false;
            const bool force_full_reload = json_contains_command(line, LVGL_PREVIEW_COMMAND_RELOAD);

            if (!extract_json_string(line, "documentName", document_name, sizeof(document_name))) {
                snprintf(document_name, sizeof(document_name), "unnamed");
            }

            if (!extract_json_string(line, "content", content_buffer, sizeof(content_buffer))) {
                log_error("Render command did not include generated content.");
                if (!send_reply(client_fd, false, "Render command did not include generated content.")) {
                    return 1;
                }

                continue;
            }

            extract_json_int(line, "screenWidth", &screen_width);
            extract_json_int(line, "screenHeight", &screen_height);
            extract_json_int(line, "zoomPercent", &zoom_percent);
            extract_json_bool(line, "resetWindowToTargetSize", &reset_window_to_target_size);

            const bool success = lvgl_simulator_runtime_render(
                runtime,
                document_name,
                content_buffer,
                force_full_reload,
                screen_width,
                screen_height,
                zoom_percent,
                reset_window_to_target_size,
                status_message,
                sizeof(status_message));

            if (!send_reply(client_fd, success, status_message)) {
                return 1;
            }

            continue;
        }

        log_error("Unknown preview command received.");
        if (!send_reply(client_fd, false, "Unknown preview command.")) {
            return 1;
        }
    }
}

static ssize_t read_line(simulator_socket_t fd, char *buffer, size_t buffer_size)
{
    size_t offset = 0;

    while (offset + 1 < buffer_size) {
        char ch = '\0';
        const int bytes_read = recv(fd, &ch, 1, 0);
        if (bytes_read == 0) {
            if (offset == 0) {
                return 0;
            }

            break;
        }

        if (bytes_read < 0) {
            return -1;
        }

        if (ch == '\n') {
            break;
        }

        buffer[offset++] = ch;
    }

    buffer[offset] = '\0';
    return (ssize_t)offset;
}

static bool json_contains_command(const char *line, const char *command)
{
    char needle[128];
    snprintf(needle, sizeof(needle), "\"command\":\"%s\"", command);
    return strstr(line, needle) != NULL;
}

static bool extract_json_string(const char *line, const char *key, char *output, size_t output_size)
{
    char pattern[128];
    snprintf(pattern, sizeof(pattern), "\"%s\":\"", key);

    const char *start = strstr(line, pattern);
    if (start == NULL) {
        return false;
    }

    start += strlen(pattern);
    size_t output_index = 0;

    while (*start != '\0' && output_index + 1 < output_size) {
        if (*start == '"') {
            break;
        }

        if (*start == '\\') {
            start++;
            if (*start == '\0') {
                break;
            }

            switch (*start) {
                case 'n':
                    output[output_index++] = '\n';
                    start++;
                    continue;
                case 'r':
                    output[output_index++] = '\r';
                    start++;
                    continue;
                case 't':
                    output[output_index++] = '\t';
                    start++;
                    continue;
                case '"':
                    output[output_index++] = '"';
                    start++;
                    continue;
                case '\\':
                    output[output_index++] = '\\';
                    start++;
                    continue;
                case '/':
                    output[output_index++] = '/';
                    start++;
                    continue;
                default:
                    output[output_index++] = *start;
                    start++;
                    continue;
            }
        }

        output[output_index++] = *start;
        start++;
    }

    output[output_index] = '\0';
    return true;
}

static bool extract_json_int(const char *line, const char *key, int *value)
{
    char pattern[128];
    snprintf(pattern, sizeof(pattern), "\"%s\":", key);

    const char *start = strstr(line, pattern);
    if(start == NULL) {
        return false;
    }

    start += strlen(pattern);
    while(*start == ' ' || *start == '\t') {
        start++;
    }

    if(strncmp(start, "null", 4) == 0) {
        return false;
    }

    char *end_ptr = NULL;
    long parsed = strtol(start, &end_ptr, 10);
    if(end_ptr == start) {
        return false;
    }

    *value = (int)parsed;
    return true;
}

static bool extract_json_bool(const char *line, const char *key, bool *value)
{
    char true_pattern[128];
    char false_pattern[128];
    snprintf(true_pattern, sizeof(true_pattern), "\"%s\":true", key);
    snprintf(false_pattern, sizeof(false_pattern), "\"%s\":false", key);

    if(strstr(line, true_pattern) != NULL) {
        *value = true;
        return true;
    }

    if(strstr(line, false_pattern) != NULL) {
        *value = false;
        return true;
    }

    return false;
}

static bool send_reply(simulator_socket_t fd, bool success, const char *status_message)
{
    char escaped_message[MAX_STRING_VALUE];
    size_t write_index = 0;

    for (size_t read_index = 0;
         status_message[read_index] != '\0' && write_index + 2 < sizeof(escaped_message);
         read_index++) {
        const char current = status_message[read_index];
        if (current == '"' || current == '\\') {
            escaped_message[write_index++] = '\\';
        }

        escaped_message[write_index++] = current;
    }

    escaped_message[write_index] = '\0';

    char reply[MAX_STRING_VALUE + 64];
    snprintf(
        reply,
        sizeof(reply),
        "{\"success\":%s,\"statusMessage\":\"%s\"}\n",
        success ? "true" : "false",
        escaped_message);

    const size_t reply_length = strlen(reply);
    return send(fd, reply, (int)reply_length, 0) == (int)reply_length;
}

static void log_info(const char *message)
{
    printf("%s\n", message);
    fflush(stdout);
}

static void log_error(const char *message)
{
    fprintf(stderr, "%s\n", message);
    fflush(stderr);
}

static void close_socket(simulator_socket_t fd)
{
#ifdef _WIN32
    closesocket(fd);
#else
    close(fd);
#endif
}

static int initialize_socket_layer(void)
{
#ifdef _WIN32
    WSADATA wsa_data;
    if (WSAStartup(MAKEWORD(2, 2), &wsa_data) != 0) {
        log_error("WSAStartup failed.");
        return 1;
    }
#endif

    return 0;
}

static void shutdown_socket_layer(void)
{
#ifdef _WIN32
    WSACleanup();
#endif
}
