#include <ESP8266WiFi.h>
#include <PubSubClient.h>
#include <SoftwareSerial.h>
#include <TinyGPSPlus.h>

// ====== WIFI ======
const char* WIFI_SSID = "YOUR_WIFI_NAME";
const char* WIFI_PASS = "YOUR_WIFI_PASSWORD";

// ====== MQTT ======
const char* MQTT_HOST = "192.168.1.197";
const int   MQTT_PORT = 1883;

// ====== TOPICS / NODE ======
const char* NODE_ID = "RADR-esp-1";
const char* TOPIC_EVT = "/RADR-esp-1";
const char* TOPIC_STAT = "/RADR-esp-1/status";

// ====== LD2410C OUT ======
const int RADAR_OUT_PIN = D2; // GPIO4

// ====== GPS ======
static const uint32_t GPS_BAUD = 9600;
const int GPS_RX_PIN = D5; // ESP receives from GPS TX
const int GPS_TX_PIN = D6; // optional

SoftwareSerial gpsSer(GPS_RX_PIN, GPS_TX_PIN);
TinyGPSPlus gps;

WiFiClient wifiClient;
PubSubClient mqtt(wifiClient);

bool lastPresence = false;
unsigned long lastHeartbeatAt = 0;
unsigned long lastPeriodicAt = 0;
const unsigned long PUBLISH_INTERVAL_MS = 3000;

unsigned long gpsLastByteAt = 0;
bool gpsPresent = false;
static const unsigned long GPS_PRESENT_TIMEOUT_MS = 5000;

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

    Serial.print("WiFi connecting to ");
    Serial.println(WIFI_SSID);

    WiFi.mode(WIFI_STA);
    WiFi.persistent(false);
    WiFi.setAutoReconnect(true);
    WiFi.begin(WIFI_SSID, WIFI_PASS);

    unsigned long start = millis();
    while (WiFi.status() != WL_CONNECTED) {
        if (millis() - start > 20000) {
            Serial.println("WiFi timeout, retry...");
            WiFi.disconnect();
            safeDelay(250);
            WiFi.begin(WIFI_SSID, WIFI_PASS);
            start = millis();
        }
        Serial.print(".");
        safeDelay(250);
    }

    Serial.println("\nWiFi connected");
    Serial.print("IP: ");
    Serial.println(WiFi.localIP());
}

void connectMQTT() {
    if (mqtt.connected()) return;

    mqtt.setServer(MQTT_HOST, MQTT_PORT);
    mqtt.setBufferSize(512);

    while (!mqtt.connected()) {
        Serial.print("MQTT connecting... ");
        String clientId = String("esp8266-") + NODE_ID + "-" + String(ESP.getChipId(), HEX);

        bool ok = mqtt.connect(clientId.c_str(), TOPIC_STAT, 1, true, "offline");
        if (ok) {
            Serial.println("OK");
            mqtt.publish(TOPIC_STAT, "online", true);
        }
        else {
            Serial.print("fail rc=");
            Serial.println(mqtt.state());
            safeDelay(1500);
        }
    }
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
    else if (gpsPresent && (millis() - gpsLastByteAt > GPS_PRESENT_TIMEOUT_MS)) {
        gpsPresent = false;
    }
}

bool gpsHasFix() {
    return gps.location.isValid() && gps.location.age() < 15000;
}

void publishFrame(bool presence) {
    const bool fix = gpsHasFix();
    unsigned long sats = gps.satellites.isValid() ? gps.satellites.value() : 0;
    unsigned long hdop = gps.hdop.isValid() ? gps.hdop.value() : 0;

    char payload[420];
    snprintf(payload, sizeof(payload),
        "{"
        "\"nodeId\":\"%s\","
        "\"tsMs\":%lu,"
        "\"presence\":%s,"
        "\"gpsPresent\":%s,"
        "\"gpsFix\":%s,"
        "\"lat\":%s,"
        "\"lon\":%s,"
        "\"sats\":%lu,"
        "\"hdopX100\":%lu"
        "}",
        NODE_ID,
        (unsigned long)millis(),
        presence ? "true" : "false",
        gpsPresent ? "true" : "false",
        fix ? "true" : "false",
        fix ? String(gps.location.lat(), 6).c_str() : "null",
        fix ? String(gps.location.lng(), 6).c_str() : "null",
        sats,
        hdop
    );

    mqtt.publish(TOPIC_EVT, payload, false);

    // Serial Monitor visibility
    Serial.print("PUB presence=");
    Serial.print(presence ? "true" : "false");
    Serial.print(" gpsPresent=");
    Serial.print(gpsPresent ? "true" : "false");
    Serial.print(" fix=");
    Serial.print(fix ? "true" : "false");
    Serial.print(" sats=");
    Serial.print(sats);
    Serial.print(" hdopX100=");
    Serial.println(hdop);
}

void setup() {
    Serial.begin(115200);
    delay(300);
    Serial.println("\nBoot OK (LD2410 OUT + GPS + MQTT periodic)");

    pinMode(RADAR_OUT_PIN, INPUT);
    gpsSer.begin(GPS_BAUD);

    connectWiFi();
    connectMQTT();

    lastPresence = (digitalRead(RADAR_OUT_PIN) == HIGH);
    publishFrame(lastPresence);
    lastPeriodicAt = millis();
}

void loop() {
    connectWiFi();
    connectMQTT();
    mqtt.loop();

    readGps();

    bool presence = (digitalRead(RADAR_OUT_PIN) == HIGH);

    // Publish immediately on change
    if (presence != lastPresence) {
        safeDelay(30);
        presence = (digitalRead(RADAR_OUT_PIN) == HIGH);
        if (presence != lastPresence) {
            lastPresence = presence;
            publishFrame(presence);
        }
    }

    // Publish every 3 seconds no matter what
    if (millis() - lastPeriodicAt >= PUBLISH_INTERVAL_MS) {
        lastPeriodicAt = millis();
        publishFrame(presence);
    }

    // Heartbeat every 30s
    if (millis() - lastHeartbeatAt > 30000UL) {
        lastHeartbeatAt = millis();
        mqtt.publish(TOPIC_STAT, "online", true);
    }

    delay(5);
}
