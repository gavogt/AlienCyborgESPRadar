#include <WiFiS3.h>
#include <WiFiUdp.h>
#include <NTPClient.h>
#include <PubSubClient.h>

// ====== WIFI ======
const char* WIFI_SSID = "YOUR_WIFI_NAME";
const char* WIFI_PASS = "YOUR_WIFI_PASSWORD";

// ====== MQTT (Mosquitto) ======
const char* MQTT_HOST = "192.168.1.50";  // broker IP
const int   MQTT_PORT = 1883;
const char* MQTT_USER = "";              // optional
const char* MQTT_PASS = "";              // optional

// ====== NODE / TOPICS ======
const char* NODE_ID = "RADR-uno-1";
const char* TOPIC_EVENT = "/RADR-uno-1";
const char* TOPIC_STAT = "/RADR-uno-1/status"; // LWT/status

// ====== HARDWARE ======
const int RADAR_PIN = 2;                        // D2 <- RCWL OUT
const unsigned long PUBLISH_INTERVAL_MS = 3000; // publish every 3 seconds

// ====== NTP ======
WiFiUDP ntpUDP;
NTPClient timeClient(ntpUDP, "pool.ntp.org", 0 /*UTC*/, 60UL * 60UL * 1000UL);

WiFiClient wifiClient;
PubSubClient mqtt(wifiClient);

bool ntpReady = false;
unsigned long lastPublishAt = 0;

void connectWiFi()
{
    if (WiFi.status() == WL_CONNECTED) return;

    Serial.print("WiFi connecting to: ");
    Serial.println(WIFI_SSID);

    WiFi.begin(WIFI_SSID, WIFI_PASS);

    unsigned long start = millis();
    while (WiFi.status() != WL_CONNECTED)
    {
        if (millis() - start > 20000)
        {
            Serial.println("WiFi timeout, retrying...");
            WiFi.disconnect();
            WiFi.begin(WIFI_SSID, WIFI_PASS);
            start = millis();
        }
        delay(250);
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
            if (s > 1700000000UL)  // sanity check
            {
                ntpReady = true;
                Serial.print("NTP synced. EpochSec=");
                Serial.println(s);
                return;
            }
        }
        delay(300);
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

        String clientId = String("unoR4-") + NODE_ID + "-" + String(random(0xffff), HEX);

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
            delay(1500);
        }
    }
}

void publishRadarState()
{
    bool motion = (digitalRead(RADAR_PIN) == HIGH);

    // Build epoch milliseconds as a STRING to avoid 64-bit printf issues
    uint32_t tsSec = ntpReady ? timeClient.getEpochTime() : 0;
    uint16_t msPart = (uint16_t)(millis() % 1000);

    // tsMsStr = (tsSec * 1000) + msPart, computed as string
    unsigned long ms3 = (unsigned long)msPart; // 0..999
    unsigned long sec = (unsigned long)tsSec;

    // Convert to a string without 64-bit math:
    // (sec * 1000 + msPart) => string = (sec as string) + (msPart padded to 3 digits)
    // Example: 1734740000 + 123ms => "1734740000123"
    char tsMsStr[32];
    snprintf(tsMsStr, sizeof(tsMsStr), "%lu%03lu", sec, ms3);

    char payload[240];
    snprintf(payload, sizeof(payload),
        "{\"nodeId\":\"%s\",\"motion\":%s,\"tsMs\":\"%s\"}",
        NODE_ID,
        motion ? "true" : "false",
        tsMsStr);

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
    Serial.begin(115200);
    delay(200);

    pinMode(RADAR_PIN, INPUT);
    randomSeed(analogRead(A0));

    connectWiFi();
    ensureNtp();
    connectMQTT();

    Serial.println("Ready. Publishing every 3 seconds.");
}

void loop()
{
    connectWiFi();
    ensureNtp();
    connectMQTT();
    mqtt.loop();

    unsigned long now = millis();
    if (now - lastPublishAt >= PUBLISH_INTERVAL_MS)
    {
        lastPublishAt = now;
        publishRadarState();
    }

    delay(10);
}
