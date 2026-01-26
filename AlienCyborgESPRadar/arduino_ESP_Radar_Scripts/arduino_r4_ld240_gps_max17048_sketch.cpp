/*
  UNO R4 (WiFi or Minima) + LD2410C OUT + NEO-6M (Serial1) + MAX17048 (I2C) + MQTT + NTP

  Wiring (UNO R4):
  - MAX17048: SDA -> SDA pin, SCL -> SCL pin, GND -> GND, VIN/VCC -> 3.3V (recommended)
  - GPS NEO-6M: TX -> RX1, RX -> TX1 (optional), VCC -> 5V (or per your module), GND -> GND
  - LD2410C: OUT -> D2 (or any digital input), VCC/GND per module
*/

#include <WiFiS3.h>        // UNO R4 WiFi only (comment out if Minima)
#include <WiFiUdp.h>
#include <NTPClient.h>
#include <PubSubClient.h>
#include <TinyGPSPlus.h>
#include <Wire.h>
#include <Adafruit_MAX1704X.h>

// ====== WIFI ======
const char* WIFI_SSID = "";
const char* WIFI_PASS = "";

// ====== MQTT (Mosquitto) ======
const char* MQTT_HOST = "192.168.x.xxx";
const int   MQTT_PORT = 1883;
const char* MQTT_USER = "";
const char* MQTT_PASS = "";

// ====== NODE / TOPICS ======
const char* NODE_ID = "RADR-r4-1";
const char* TOPIC_EVENT = "/RADR-r4-1";
const char* TOPIC_STAT = "/RADR-r4-1/status"; // LWT/status

// ====== HARDWARE ======
const int RADAR_PIN = 2;  // LD2410C OUT -> D2
const unsigned long PUBLISH_INTERVAL_MS = 3000;

// ====== GPS (NEO-6M on Serial1) ======
static const uint32_t GPS_BAUD = 9600;
static const uint32_t GPS_PRESENT_TIMEOUT_MS = 5000;
static const uint32_t GPS_FIX_STALE_MS = 15000;

TinyGPSPlus gps;
unsigned long gpsLastByteAt = 0;
bool gpsPresent = false;

// ====== MAX17048 Fuel Gauge (I2C) ======
Adafruit_MAX17048 max17048;
bool battOk = false;

// ====== NTP ======
WiFiUDP ntpUDP;
NTPClient timeClient(ntpUDP, "pool.ntp.org", 0 /*UTC*/, 60UL * 60UL * 1000UL);

WiFiClient wifiClient;
PubSubClient mqtt(wifiClient);

bool ntpReady = false;
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

void publishStat(const char* msg) {
    Serial.println(msg);
    if (mqtt.connected()) mqtt.publish(TOPIC_STAT, msg, true);
}

void connectWiFi() {
#if defined(ARDUINO_UNOR4_WIFI)
    if (WiFi.status() == WL_CONNECTED) return;

    Serial.print("WiFi connecting to: ");
    Serial.println(WIFI_SSID);

    WiFi.begin(WIFI_SSID, WIFI_PASS);

    unsigned long start = millis();
    while (WiFi.status() != WL_CONNECTED) {
        if (millis() - start > 20000) {
            Serial.println("\nWiFi timeout, retrying...");
            WiFi.disconnect();
            safeDelay(250);
            WiFi.begin(WIFI_SSID, WIFI_PASS);
            start = millis();
        }
        safeDelay(250);
        Serial.print(".");
    }

    Serial.println("\nWiFi connected!");
    Serial.print("IP: ");
    Serial.println(WiFi.localIP());
#else
    // UNO R4 Minima has no WiFi; this function will do nothing.
#endif
}

void ensureNtp() {
    if (!ntpReady) {
        Serial.println("Starting NTP...");
        timeClient.begin();
    }

    for (int i = 0; i < 10; i++) {
        if (timeClient.forceUpdate()) {
            uint32_t s = timeClient.getEpochTime();
            if (s > 1700000000UL) {
                ntpReady = true;
                Serial.print("NTP synced. EpochSec=");
                Serial.println(s);
                return;
            }
        }
        safeDelay(300);
    }

    Serial.println("NTP not ready yet (will retry).");
}

void connectMQTT() {
    if (mqtt.connected()) return;

    mqtt.setServer(MQTT_HOST, MQTT_PORT);
    mqtt.setBufferSize(768);

#if defined(ARDUINO_UNOR4_WIFI)
    while (!mqtt.connected()) {
        Serial.print("MQTT connecting to ");
        Serial.print(MQTT_HOST);
        Serial.print(":");
        Serial.print(MQTT_PORT);
        Serial.print(" ... ");

        String clientId = String("r4-") + NODE_ID + "-" + String((uint32_t)millis(), HEX);

        bool ok;
        if (strlen(MQTT_USER) > 0) {
            ok = mqtt.connect(clientId.c_str(),
                MQTT_USER, MQTT_PASS,
                TOPIC_STAT, 1, true, "offline");
        }
        else {
            ok = mqtt.connect(clientId.c_str(),
                TOPIC_STAT, 1, true, "offline");
        }

        if (ok) {
            Serial.println("connected");
            mqtt.publish(TOPIC_STAT, "online", true);
            Serial.print("Status: ");
            Serial.print(TOPIC_STAT);
            Serial.println(" -> online");
        }
        else {
            Serial.print("failed rc=");
            Serial.println(mqtt.state());
            safeDelay(1500);
        }
    }
#endif
}

void initMax17048() {
    Serial.println("Init MAX17048...");
    Wire.begin();           // UNO R4 uses hardware I2C on SDA/SCL pins
    Wire.setClock(100000);

    battOk = max17048.begin(&Wire);
    if (battOk) {
        Serial.println("MAX17048 OK");
        Serial.print("Chip ID: 0x");
        Serial.println(max17048.getChipID(), HEX);

        Serial.print("Cell V=");
        Serial.print(max17048.cellVoltage(), 3);
        Serial.print("V  %=");
        Serial.println(max17048.cellPercent(), 1);

        publishStat("max17048_ok");
    }
    else {
        Serial.println("WARN: MAX17048 not found on I2C (addr usually 0x36).");
        publishStat("max17048_not_found");
    }
}

void readGps() {
    bool gotAny = false;
    while (Serial1.available() > 0) {
        char c = (char)Serial1.read();
        gotAny = true;
        gps.encode(c);
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

bool gpsHasFreshFix() {
    if (!gps.location.isValid()) return false;
    if (gps.location.age() > GPS_FIX_STALE_MS) return false;
    return true;
}

void publishRadarState() {
    bool motion = (digitalRead(RADAR_PIN) == HIGH);

    uint32_t tsSec = ntpReady ? timeClient.getEpochTime() : 0;
    uint16_t msPart = (uint16_t)(millis() % 1000);

    char tsMsStr[32];
    snprintf(tsMsStr, sizeof(tsMsStr), "%lu%03u", (unsigned long)tsSec, (unsigned)msPart);

    // GPS fields
    const bool fix = gpsHasFreshFix();
    double lat = fix ? gps.location.lat() : 0.0;
    double lon = fix ? gps.location.lng() : 0.0;
    uint32_t sats = gps.satellites.isValid() ? gps.satellites.value() : 0;
    uint32_t hdop = gps.hdop.isValid() ? gps.hdop.value() : 0; // HDOP * 100
    uint32_t ageMs = gps.location.isValid() ? gps.location.age() : 0;

    // Battery fields
    float battV = battOk ? max17048.cellVoltage() : 0.0f;
    float battPct = battOk ? max17048.cellPercent() : 0.0f;
    uint8_t chipId = battOk ? max17048.getChipID() : 0;

    char payload[680];
    snprintf(payload, sizeof(payload),
        "{"
        "\"nodeId\":\"%s\","
        "\"motion\":%s,"
        "\"tsMs\":\"%s\","
        "\"gpsPresent\":%s,"
        "\"gpsFix\":%s,"
        "\"lat\":%s,"
        "\"lon\":%s,"
        "\"sats\":%lu,"
        "\"hdopX100\":%lu,"
        "\"fixAgeMs\":%lu,"
        "\"battOk\":%s,"
        "\"battV\":%.3f,"
        "\"battPct\":%.1f,"
        "\"max17048ChipId\":%u"
        "}",
        NODE_ID,
        motion ? "true" : "false",
        tsMsStr,
        gpsPresent ? "true" : "false",
        fix ? "true" : "false",
        fix ? String(lat, 6).c_str() : "null",
        fix ? String(lon, 6).c_str() : "null",
        (unsigned long)sats,
        (unsigned long)hdop,
        (unsigned long)ageMs,
        battOk ? "true" : "false",
        battV,
        battPct,
        (unsigned)chipId
    );

    Serial.print("RADAR=");
    Serial.print(motion ? "MOTION" : "idle");
    Serial.print(" | ");
    Serial.println(payload);

    if (mqtt.connected()) {
        bool ok = mqtt.publish(TOPIC_EVENT, payload, false);
        Serial.println(ok ? "Publish OK" : "Publish FAILED");
    }
}

void setup() {
    Serial.begin(115200);
    delay(300);

    pinMode(RADAR_PIN, INPUT);

    // GPS
    Serial1.begin(GPS_BAUD);
    gpsLastByteAt = millis();
    gpsPresent = false;

#if defined(ARDUINO_UNOR4_WIFI)
    connectWiFi();
    connectMQTT();
    ensureNtp();
#else
    // If Minima, you can still print serial JSON but MQTT won't run without external WiFi.
    ntpReady = false;
#endif

    initMax17048();

    Serial.println("Ready. Publishing every 3 seconds.");
}

void loop() {
#if defined(ARDUINO_UNOR4_WIFI)
    connectWiFi();
    connectMQTT();
    mqtt.loop();
    ensureNtp();
#endif

    readGps();

    unsigned long now = millis();
    if (now - lastPublishAt >= PUBLISH_INTERVAL_MS) {
        lastPublishAt = now;
        publishRadarState();
    }

    delay(10);
}
