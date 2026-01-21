#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
#include <NTPClient.h>
#include <PubSubClient.h>
#include <TinyGPSPlus.h>

// ====== WIFI ======
const char* WIFI_SSID = "YOUR_WIFI_NAME";
const char* WIFI_PASS = "YOUR_WIFI_PASSWORD";

// ====== MQTT (Mosquitto) ======
const char* MQTT_HOST = "192.168.x.xxx";
const int   MQTT_PORT = 1883;
const char* MQTT_USER = "";
const char* MQTT_PASS = "";

// ====== NODE / TOPICS ======
const char* NODE_ID = "RADR-esp-1";
const char* TOPIC_EVENT = "/RADR-esp-1";
const char* TOPIC_STAT = "/RADR-esp-1/status"; // LWT/status

// ====== HARDWARE ======
const int RADAR_PIN = D2;
const unsigned long PUBLISH_INTERVAL_MS = 3000;

// ====== GPS (NEO-6M on UART0 RX = GPIO3) ======
static const uint32_t GPS_BAUD = 9600;              // NEO-6M typically default 9600
static const uint32_t GPS_PRESENT_TIMEOUT_MS = 5000; // no bytes for 5s => "not connected"
static const uint32_t GPS_FIX_STALE_MS = 15000;      // fix older than this => treat as stale

TinyGPSPlus gps;
unsigned long gpsLastByteAt = 0;
bool gpsPresent = false;

// ====== NTP ======
WiFiUDP ntpUDP;
NTPClient timeClient(ntpUDP, "pool.ntp.org", 0 /*UTC*/, 60UL * 60UL * 1000UL);

WiFiClient wifiClient;
PubSubClient mqtt(wifiClient);

bool ntpReady = false;
unsigned long lastPublishAt = 0;

// ---------- helpers ----------
static inline void safeDelay(unsigned long ms) {
    // keep WiFi/MQTT fed during delays
    unsigned long start = millis();
    while (millis() - start < ms) {
        mqtt.loop();
        delay(1);
        yield();
    }
}

void connectWiFi()
{
    if (WiFi.status() == WL_CONNECTED) return;

    Serial.print("WiFi connecting to: ");
    Serial.println(WIFI_SSID);

    WiFi.mode(WIFI_STA);
    WiFi.persistent(false);
    WiFi.setAutoReconnect(true);

    WiFi.begin(WIFI_SSID, WIFI_PASS);

    unsigned long start = millis();
    while (WiFi.status() != WL_CONNECTED)
    {
        if (millis() - start > 20000)
        {
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
}

void ensureNtp()
{
    if (ntpReady) {
        timeClient.update();
        return;
    }

    Serial.println("Starting NTP...");
    timeClient.begin();

    for (int i = 0; i < 10; i++)
    {
        if (timeClient.forceUpdate())
        {
            uint32_t s = timeClient.getEpochTime();
            if (s > 1700000000UL)
            {
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

void connectMQTT()
{
    if (mqtt.connected()) return;

    mqtt.setServer(MQTT_HOST, MQTT_PORT);
    mqtt.setBufferSize(512);

    while (!mqtt.connected())
    {
        Serial.print("MQTT connecting to ");
        Serial.print(MQTT_HOST);
        Serial.print(":");
        Serial.print(MQTT_PORT);
        Serial.print(" ... ");

        String clientId = String("esp8266-") + NODE_ID + "-" + String(ESP.getChipId(), HEX);

        bool ok;
        if (strlen(MQTT_USER) > 0)
        {
            ok = mqtt.connect(clientId.c_str(),
                MQTT_USER, MQTT_PASS,
                TOPIC_STAT, 1, true, "offline");
        }
        else
        {
            ok = mqtt.connect(clientId.c_str(),
                TOPIC_STAT, 1, true, "offline");
        }

        if (ok)
        {
            Serial.println("connected");
            mqtt.publish(TOPIC_STAT, "online", true);
            Serial.print("Status: ");
            Serial.print(TOPIC_STAT);
            Serial.println(" -> online");
        }
        else
        {
            Serial.print("failed rc=");
            Serial.println(mqtt.state());
            safeDelay(1500);
        }
    }
}

// Read GPS bytes from UART0 (GPIO3 is RX0).
void readGps()
{
    bool gotAny = false;

    while (Serial.available() > 0) {
        char c = (char)Serial.read();
        gotAny = true;
        gps.encode(c);
    }

    if (gotAny) {
        gpsLastByteAt = millis();
        gpsPresent = true;
    }
    else {
        // If we've gone too long with no bytes, declare "not connected"
        if (gpsPresent && (millis() - gpsLastByteAt > GPS_PRESENT_TIMEOUT_MS)) {
            gpsPresent = false;
        }
    }
}

bool gpsHasFreshFix()
{
    if (!gps.location.isValid()) return false;
    if (gps.location.age() > GPS_FIX_STALE_MS) return false;
    return true;
}

void publishRadarState()
{
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
    uint32_t hdop = gps.hdop.isValid() ? gps.hdop.value() : 0; // TinyGPS++ gives HDOP * 100
    uint32_t ageMs = gps.location.isValid() ? gps.location.age() : 0;

    char payload[420];
    // If no fix, lat/lon sent as null (easier to handle on dashboard)
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
        "\"fixAgeMs\":%lu"
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
        (unsigned long)ageMs
    );

    // “Exception-like” visibility: print + publish status signals
    if (!gpsPresent) {
        Serial.println("WARN: GPS not detected (no serial bytes). Check wiring: GPS TX -> GPIO3.");
    }
    else if (!fix) {
        Serial.println("INFO: GPS present but no fresh fix yet (may need sky view).");
    }

    Serial.print("RADAR=");
    Serial.print(motion ? "MOTION" : "idle");
    Serial.print(" | PUB ");
    Serial.print(TOPIC_EVENT);
    Serial.print(" ");
    Serial.println(payload);

    bool ok = mqtt.publish(TOPIC_EVENT, payload, false);
    Serial.println(ok ? "Publish OK" : "Publish FAILED");
}

void setup()
{
    // IMPORTANT: Serial is UART0. If GPS TX is on GPIO3 (RX0),
    // Serial MUST be at the GPS baud.
    Serial.begin(GPS_BAUD);
    delay(200);

    pinMode(RADAR_PIN, INPUT);

    // Start "GPS presence" window:
    gpsLastByteAt = millis();
    gpsPresent = false;

    connectWiFi();
    ensureNtp();
    connectMQTT();

    Serial.println("Ready. Publishing every 3 seconds.");
    Serial.println("NOTE: If uploads fail, unplug GPS TX from GPIO3 while flashing, or add ~1k series resistor.");
}

void loop()
{
    connectWiFi();
    ensureNtp();
    connectMQTT();
    mqtt.loop();

    // continuously read GPS stream
    readGps();

    unsigned long now = millis();
    if (now - lastPublishAt >= PUBLISH_INTERVAL_MS)
    {
        lastPublishAt = now;
        publishRadarState();
    }

    delay(10);
}
