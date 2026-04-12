#include "lvgl_simulator_protocol.h"
#include "lvgl_simulator_runtime.h"
#include "lvgl_simulator_server.h"

#include <pthread.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static int parse_port(int argc, char **argv);

typedef struct server_thread_context {
    int port;
    int result;
    lvgl_simulator_runtime_t *runtime;
} server_thread_context_t;

static void *server_thread_main(void *context);

int main(int argc, char **argv)
{
    const int port = parse_port(argc, argv);
    lvgl_simulator_runtime_t runtime;
    server_thread_context_t server_context;
    pthread_t server_thread;

    if (port <= 0) {
        fprintf(stderr, "Usage: %s --port <tcp-port>\n", argv[0]);
        return 1;
    }

    printf(
        "LVGL simulator host starting. protocol=%s version=%d port=%d\n",
        LVGL_PREVIEW_PROTOCOL_NAME,
        LVGL_PREVIEW_PROTOCOL_VERSION,
        port);
    fflush(stdout);

    if (!lvgl_simulator_runtime_init(&runtime)) {
        fprintf(stderr, "Failed to initialize simulator runtime.\n");
        return 1;
    }

    server_context.port = port;
    server_context.result = 0;
    server_context.runtime = &runtime;

    if(pthread_create(&server_thread, NULL, server_thread_main, &server_context) != 0) {
        fprintf(stderr, "Failed to start simulator server thread.\n");
        lvgl_simulator_runtime_shutdown(&runtime);
        return 1;
    }

    const int result = lvgl_simulator_runtime_run_main_loop(&runtime);
    lvgl_simulator_runtime_request_shutdown(&runtime);
    pthread_join(server_thread, NULL);
    lvgl_simulator_runtime_shutdown(&runtime);
    return result != 0 ? result : server_context.result;
}

static int parse_port(int argc, char **argv)
{
    for (int i = 1; i < argc - 1; i++) {
        if (strcmp(argv[i], "--port") == 0) {
            return atoi(argv[i + 1]);
        }
    }

    return -1;
}

static void *server_thread_main(void *context)
{
    server_thread_context_t *server_context = (server_thread_context_t *)context;
    server_context->result = lvgl_simulator_server_run(server_context->port, server_context->runtime);
    lvgl_simulator_runtime_request_shutdown(server_context->runtime);
    return NULL;
}
