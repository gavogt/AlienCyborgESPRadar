#include <WiFi.h>
#include <WiFiUdp.h>
#include <NTPClient.h>
#include <PubSubClient.h>
#include <TinyGPSPlus.h>
#include <Wire.h>
#include <Adafruit_MAX1704X.h>

// ====== WIFI ======
const char* WIFI_SSID = "";
const char* WIFI_PASS = "";

// ====== MQTT ======
const char* MQTT_HOST = "192.168.1.xxx";
const int   MQTT_PORT = 1883;
const char* MQTT_USER = "";
const char* MQTT_PASS = "";

// ====== NODE / TOPICS ======
const char* NODE_ID = "RADR-esp-1";
const char* TOPIC_EVENT = "/RADR-esp-1";
const char* TOPIC_STAT = "/RADR-esp-1/status";

// Camera trigger topic (ESP32-CAM subscribes to this)
const char* TOPIC_CAM_CAPTURE = "cam/RADR-esp-1/capture";

// ====== PINS (ESP32-S3 GPIO numbers) ======
static const int RADAR_PIN = 5;  // LD2410 OUT -> GPIO5
static const int PIR_PIN = 6;  // AM312 OUT  -> GPIO6

static const int I2C_SDA = 8;    // MAX SDA -> GPIO8
static const int I2C_SCL = 9;    // MAX SCL -> GPIO9

static const int GPS_TX_PIN = 17; // ESP -> GPS RX (optional)
static const int GPS_RX_PIN = 18; // ESP <- GPS TX

// ====== TIMING ======
const unsigned long PUBLISH_INTERVAL_MS = 3000;
static const uint32_t GPS_BAUD = 9600;
static const uint32_t GPS_FIX_STALE_MS = 15000;
static const uint32_t GPS_PRESENT_TIMEOUT_MS = 5000;

// Trigger behavior
const unsigned long CAPTURE_COOLDOWN_MS = 15000;
unsigned long lastCaptureAt = 0;
bool lastBothTrue = false;

// ====== NTP ======
WiFiUDP ntpUDP;
NTPClient timeClient(ntpUDP, "pool.ntp.org", 0 /*UTC*/, 60UL * 60UL * 1000UL);
bool ntpReady = false;

// ====== MQTT ======
WiFiClient wifiClient;
PubSubClient mqtt(wifiClient);

// ====== GPS ======
TinyGPSPlus gps;
HardwareSerial gpsSer(1);
unsigned long gpsLastByteAt = 0;
bool gpsPresent = false;

// ====== MAX17048 ======
Adafruit_MAX17048 max17048;
bool battOk = false;

unsigned long lastPublishAt = 0;

// ---------- helpers ----------
static inline void safeDelay(unsigned long ms) {
    unsigned long start = millis();
    while (millis() - start < ms) {
        mqtt.loop();
        delay(1);
        yield();
    }
}

void connectWiFi() {
    if (WiFi.status() == WL_CONNECTED) return;

    WiFi.mode(WIFI_STA);
    WiFi.setSleep(false);
    WiFi.setAutoReconnect(true);
    WiFi.begin(WIFI_SSID, WIFI_PASS);

    unsigned long start = millis();
    while (WiFi.status() != WL_CONNECTED) {
        if (millis() - start > 20000) {
            WiFi.disconnect(true);
            safeDelay(250);
            WiFi.begin(WIFI_SSID, WIFI_PASS);
            start = millis();
        }
        safeDelay(250);
    }
}

void ensureNtp() {
    if (ntpReady) {
        timeClient.update();
        return;
    }

    timeClient.begin();
    for (int i = 0; i < 10; i++) {
        if (timeClient.forceUpdate()) {
            uint32_t s = timeClient.getEpochTime();
            if (s > 1700000000UL) { ntpReady = true; return; }
        }
        safeDelay(300);
    }
}

void connectMQTT() {
    if (mqtt.connected()) return;

    mqtt.setServer(MQTT_HOST, MQTT_PORT);
    mqtt.setBufferSize(1024);

    while (!mqtt.connected()) {
        String clientId = String("esp32s3-") + NODE_ID + "-" + String((uint32_t)ESP.getEfuseMac(), HEX);

        bool ok;
        if (strlen(MQTT_USER) > 0) {
            ok = mqtt.connect(clientId.c_str(), MQTT_USER, MQTT_PASS, TOPIC_STAT, 1, true, "offline");
        }
        else {
            ok = mqtt.connect(clientId.c_str(), TOPIC_STAT, 1, true, "offline");
        }

        if (ok) {
            mqtt.publish(TOPIC_STAT, "online", true);
        }
        else {
            safeDelay(1500);
        }
    }
}

bool gpsHasFreshFix() {
    if (!gps.location.isValid()) return false;
    if (gps.location.age() > GPS_FIX_STALE_MS) return false;
    return true;
}

void initMax17048() {
    Wire.begin(I2C_SDA, I2C_SCL);
    Wire.setClock(100000);
    battOk = max17048.begin(&Wire);
}

void readGps() {
    bool gotAny = false;

    while (gpsSer.available() > 0) {
        gotAny = true;
        gps.encode((char)gpsSer.read());
    }

    if (gotAny) {
        gpsLastByteAt = millis();
        gpsPresent = true;
    }
    else {
        if (gpsPresent && (millis() - gpsLastByteAt > GPS_PRESENT_TIMEOUT_MS)) {
            gpsPresent = false;
        }
    }
}

void maybeTriggerCamera(bool radarMotion, bool pirMotion, const char* tsMsStr) {
    bool both = radarMotion && pirMotion;

    unsigned long now = millis();
    if (both && !lastBothTrue && (now - lastCaptureAt > CAPTURE_COOLDOWN_MS)) {
        lastCaptureAt = now;

        char trig[220];
        snprintf(trig, sizeof(trig),
            "{\"nodeId\":\"%s\",\"tsMs\":\"%s\",\"reason\":\"pir_and_radar\"}",
            NODE_ID, tsMsStr
        );

        mqtt.publish(TOPIC_CAM_CAPTURE, trig, false);
    }

    lastBothTrue = both;
}

void publishRadarState() {
    // Radar + PIR signals
    // If your LD2410 OUT is inverted, swap HIGH/LOW here.
    bool radarMotion = (digitalRead(RADAR_PIN) == HIGH);
    bool pirMotion = (digitalRead(PIR_PIN) == HIGH);

    // Keep your existing motion pipeline: motion = radar OR pir
    bool motion = radarMotion || pirMotion;

    // Timestamp (epoch ms as string)
    uint32_t tsSec = ntpReady ? timeClient.getEpochTime() : 0;
    uint16_t msPart = (uint16_t)(millis() % 1000);

    char tsMsStr[32];
    snprintf(tsMsStr, sizeof(tsMsStr), "%lu%03u", (unsigned long)tsSec, (unsigned)msPart);

    // Trigger camera when PIR && RADAR (edge + cooldown)
    maybeTriggerCamera(radarMotion, pirMotion, tsMsStr);

    // GPS fields
    const bool fix = gpsHasFreshFix();
    double lat = fix ? gps.location.lat() : 0.0;
    double lon = fix ? gps.location.lng() : 0.0;
    uint32_t sats = gps.satellites.isValid() ? gps.satellites.value() : 0;
    uint32_t hdop = gps.hdop.isValid() ? gps.hdop.value() : 0; // TinyGPS++ gives HDOP * 100
    uint32_t ageMs = gps.location.isValid() ? gps.location.age() : 0;

    // Battery fields
    float battV = NAN;
    float battPct = NAN;
    uint8_t chipId = 0;

    if (battOk) {
        battV = max17048.cellVoltage();
        battPct = max17048.cellPercent();
        chipId = max17048.getChipID();

        // Self-heal if I2C glitches
        if (isnan(battV) || isnan(battPct)) {
            battOk = false;
            safeDelay(20);
            initMax17048();
            safeDelay(20);
            if (battOk) {
                battV = max17048.cellVoltage();
                battPct = max17048.cellPercent();
                chipId = max17048.getChipID();
            }
        }
    }

    // Never publish NaN -> emit null to keep JSON valid
    char battVStr[16];
    char battPctStr[16];
    snprintf(battVStr, sizeof(battVStr), (battOk && !isnan(battV)) ? "%.3f" : "null", battV);
    snprintf(battPctStr, sizeof(battPctStr), (battOk && !isnan(battPct)) ? "%.1f" : "null", battPct);

    char payload[980];
    snprintf(payload, sizeof(payload),
        "{"
        "\"nodeId\":\"%s\","
        "\"motion\":%s,"
        "\"radar\":%s,"
        "\"pir\":%s,"
        "\"tsMs\":\"%s\","
        "\"gpsPresent\":%s,"
        "\"gpsFix\":%s,"
        "\"lat\":%s,"
        "\"lon\":%s,"
        "\"sats\":%lu,"
        "\"hdopX100\":%lu,"
        "\"fixAgeMs\":%lu,"
        "\"battOk\":%s,"
        "\"battV\":%s,"
        "\"battPct\":%s,"
        "\"max17048ChipId\":%u"
        "}",
        NODE_ID,
        motion ? "true" : "false",
        radarMotion ? "true" : "false",
        pirMotion ? "true" : "false",
        tsMsStr,
        gpsPresent ? "true" : "false",
        fix ? "true" : "false",
        fix ? String(lat, 6).c_str() : "null",
        fix ? String(lon, 6).c_str() : "null",
        (unsigned long)sats,
        (unsigned long)hdop,
        (unsigned long)ageMs,
        battOk ? "true" : "false",
        battVStr,
        battPctStr,
        (unsigned)chipId
    );

    mqtt.publish(TOPIC_EVENT, payload, false);
}

void setup() {
    Serial.begin(115200);
    delay(200);

    // Radar OUT sometimes benefits from pullup; if it reads backwards, invert in code.
    pinMode(RADAR_PIN, INPUT_PULLUP);

    // PIR OUT is typically push-pull HIGH on motion
    pinMode(PIR_PIN, INPUT);

    // GPS UART
    gpsSer.begin(GPS_BAUD, SERIAL_8N1, GPS_RX_PIN, GPS_TX_PIN);
    gpsLastByteAt = millis();
    gpsPresent = false;

    connectWiFi();
    ensureNtp();
    connectMQTT();

    initMax17048();
}

void loop() {
    connectWiFi();
    ensureNtp();
    connectMQTT();
    mqtt.loop();

    readGps();

    unsigned long now = millis();
    if (now - lastPublishAt >= PUBLISH_INTERVAL_MS) {
        lastPublishAt = now;
        publishRadarState();
    }

    delay(10);
}