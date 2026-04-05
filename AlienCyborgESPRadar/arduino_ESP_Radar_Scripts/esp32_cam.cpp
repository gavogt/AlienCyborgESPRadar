#include "esp_camera.h"
#include <WiFi.h>
#include <PubSubClient.h>
#include <HTTPClient.h>

// ===== WiFi =====
const char* WIFI_SSID = "";
const char* WIFI_PASS = "";

// ===== MQTT =====
const char* MQTT_HOST = "192.168.1.xxx";
const int   MQTT_PORT = 1883;

// ===== Topics =====
const char* NODE_ID = "RADR-esp-1";
const char* TOPIC_CAPTURE = "cam/RADR-esp-1/capture";
const char* TOPIC_STATUS = "cam/RADR-esp-1/status";

// ===== .NET upload endpoint =====
// Example: http://192.168.1.197:5000/api/cam/upload
const char* UPLOAD_URL = "http://192.168.1.197:5000/api/cam/upload";

// ===== AI Thinker ESP32-CAM pins =====
#define PWDN_GPIO_NUM     32
#define RESET_GPIO_NUM    -1
#define XCLK_GPIO_NUM      0
#define SIOD_GPIO_NUM     26
#define SIOC_GPIO_NUM     27

#define Y9_GPIO_NUM       35
#define Y8_GPIO_NUM       34
#define Y7_GPIO_NUM       39
#define Y6_GPIO_NUM       36
#define Y5_GPIO_NUM       21
#define Y4_GPIO_NUM       19
#define Y3_GPIO_NUM       18
#define Y2_GPIO_NUM        5
#define VSYNC_GPIO_NUM    25
#define HREF_GPIO_NUM     23
#define PCLK_GPIO_NUM     22

WiFiClient wifiClient;
PubSubClient mqtt(wifiClient);

static unsigned long lastShotAt = 0;
static const unsigned long SHOT_COOLDOWN_MS = 5000;

void publishStatus(const char* msg) {
    mqtt.publish(TOPIC_STATUS, msg, false);
}

bool initCamera() {
    camera_config_t config;
    config.ledc_channel = LEDC_CHANNEL_0;
    config.ledc_timer = LEDC_TIMER_0;
    config.pin_d0 = Y2_GPIO_NUM;
    config.pin_d1 = Y3_GPIO_NUM;
    config.pin_d2 = Y4_GPIO_NUM;
    config.pin_d3 = Y5_GPIO_NUM;
    config.pin_d4 = Y6_GPIO_NUM;
    config.pin_d5 = Y7_GPIO_NUM;
    config.pin_d6 = Y8_GPIO_NUM;
    config.pin_d7 = Y9_GPIO_NUM;
    config.pin_xclk = XCLK_GPIO_NUM;
    config.pin_pclk = PCLK_GPIO_NUM;
    config.pin_vsync = VSYNC_GPIO_NUM;
    config.pin_href = HREF_GPIO_NUM;
    config.pin_sccb_sda = SIOD_GPIO_NUM;
    config.pin_sccb_scl = SIOC_GPIO_NUM;
    config.pin_pwdn = PWDN_GPIO_NUM;
    config.pin_reset = RESET_GPIO_NUM;
    config.xclk_freq_hz = 20000000;
    config.pixel_format = PIXFORMAT_JPEG;

    // Reasonable defaults
    config.frame_size = FRAMESIZE_VGA;     // 640x480
    config.jpeg_quality = 12;                // lower = better quality
    config.fb_count = 1;

    esp_err_t err = esp_camera_init(&config);
    return (err == ESP_OK);
}

bool uploadJpeg(camera_fb_t* fb, const String& nodeId, const String& tsMs) {
    HTTPClient http;
    http.begin(UPLOAD_URL);

    // We'll send as multipart/form-data manually (simple, compatible)
    String boundary = "----ESP32CAMBOUNDARY";
    http.addHeader("Content-Type", "multipart/form-data; boundary=" + boundary);

    String head =
        "--" + boundary + "\r\n"
        "Content-Disposition: form-data; name=\"nodeId\"\r\n\r\n" + nodeId + "\r\n" +
        "--" + boundary + "\r\n"
        "Content-Disposition: form-data; name=\"tsMs\"\r\n\r\n" + tsMs + "\r\n" +
        "--" + boundary + "\r\n"
        "Content-Disposition: form-data; name=\"file\"; filename=\"capture.jpg\"\r\n"
        "Content-Type: image/jpeg\r\n\r\n";

    String tail = "\r\n--" + boundary + "--\r\n";

    int totalLen = head.length() + fb->len + tail.length();

    WiFiClient* stream = http.getStreamPtr();
    int code = http.sendRequest("POST"); // start request
    // NOTE: sendRequest without payload won't include body; instead use low-level:
    // We'll use sendRequest with payload through setSize + writeToStream approach.

    http.end();
    // Above approach is awkward with Arduino HTTPClient.
    // A simpler reliable approach: send raw JPEG to an endpoint with query params.
    return false;
}

void captureAndPost(const String& nodeId, const String& tsMs) {
    unsigned long now = millis();
    if (now - lastShotAt < SHOT_COOLDOWN_MS) return;
    lastShotAt = now;

    camera_fb_t* fb = esp_camera_fb_get();
    if (!fb) {
        publishStatus("capture_failed");
        return;
    }

    // Simpler upload: POST raw JPEG with headers, include nodeId/tsMs as headers
    HTTPClient http;
    http.begin(UPLOAD_URL);
    http.addHeader("Content-Type", "image/jpeg");
    http.addHeader("X-NodeId", nodeId);
    http.addHeader("X-TsMs", tsMs);

    int code = http.POST(fb->buf, fb->len);
    if (code > 0) {
        publishStatus("capture_uploaded");
    }
    else {
        publishStatus("upload_failed");
    }
    http.end();

    esp_camera_fb_return(fb);
}

void onMqttMessage(char* topic, byte* payload, unsigned int length) {
    // Payload is small JSON. We'll extract tsMs naïvely.
    String msg;
    msg.reserve(length + 1);
    for (unsigned int i = 0; i < length; i++) msg += (char)payload[i];

    // Minimal parse: find tsMs value between quotes after "tsMs":
    String tsMs = "";
    int p = msg.indexOf("\"tsMs\"");
    if (p >= 0) {
        int q1 = msg.indexOf("\"", p + 6);
        int q2 = msg.indexOf("\"", q1 + 1);
        if (q1 >= 0 && q2 > q1) tsMs = msg.substring(q1 + 1, q2);
    }
    if (tsMs.length() == 0) tsMs = String(millis());

    captureAndPost(String(NODE_ID), tsMs);
}

void connectWiFi() {
    if (WiFi.status() == WL_CONNECTED) return;
    WiFi.mode(WIFI_STA);
    WiFi.begin(WIFI_SSID, WIFI_PASS);
    while (WiFi.status() != WL_CONNECTED) delay(250);
}

void connectMQTT() {
    if (mqtt.connected()) return;
    mqtt.setServer(MQTT_HOST, MQTT_PORT);
    mqtt.setCallback(onMqttMessage);

    while (!mqtt.connected()) {
        String clientId = "esp32cam-" + String((uint32_t)ESP.getEfuseMac(), HEX);
        if (mqtt.connect(clientId.c_str())) {
            mqtt.subscribe(TOPIC_CAPTURE);
            publishStatus("online");
        }
        else {
            delay(1000);
        }
    }
}

void setup() {
    Serial.begin(115200);
    connectWiFi();

    if (!initCamera()) {
        Serial.println("Camera init failed");
        while (true) delay(1000);
    }

    connectMQTT();
}

void loop() {
    connectWiFi();
    connectMQTT();
    mqtt.loop();
    delay(10);
}