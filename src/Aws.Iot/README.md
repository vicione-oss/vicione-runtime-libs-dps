# AWS IoT Data Port [↩](./../../README.md)

1. [Zugriffsschlüssel](#zugriffsschlüssel) erstellen
2. [Zertifikat](#zertifikat) erstellen
3. [Richtlinie](#richtlinie) erstellen
4. [Richtlinie an Zertifikat anhängen](#richtlinie-an-zertifikat-anhängen)

## Node Eigenschaften

Im Hintergrund wird der [MQTT](mqtt) DataPort verwendet, daher sind die Eigenschaften identisch. Sie sind nur nicht an jedem Knoten verfügbar.

Richtung|Name|Typ|Werte
-|-|-|-
Outgoing|Retain|`bool`|true/false
Incoming,Outgoing|Serializer|`byte`|0-Inherited, 1-JSON, 2-PlainText

## Zugriffsschlüssel

1. Wechsel zu **Mein Benutzername** (oben Rechts) -> **Sicherheitsanmeldeinformationen**
2. Im Bereich **Zugriffsschlüssel** auf **Zugriffsschlüssel erstellen** klicken
3. **Zugriffsschlüssel** und **Geheimer Zugriffsschlüssel** speichern

## Zertifikat

Zur Anmeldung wird ein Zertifikat benötigt, welches die entsprechenden Berechtigungen hat.

### Zertifikat erstellen

1. Wechsel zu **IoT Core** -> **Verwalten** -> **Sicherheit** -> **Zertifikate**
2. Richtige **Region** auswählen (oben Rechts)
3. **Zertifikat hinzufügen** -> **Zertifikat erstellen**
4. **Zertifikatsstatus** auf **Aktiv** setzen
5. **Gerätezertifikat**  und **Privaten Schlüssel** herunterladen

### Richtlinie erstellen

1. Wechsel zu **IoT Core** -> **Verwalten** -> **Sicherheit** -> **Richtlinien**
2. Richtige **Region** auswählen (oben Rechts)
3. Richtlinie hinzufügen
4. Name vergeben
5. Im Bereich **Richtlinienanweisungen** auf **JSON** umstellen
6. [JSON Text](#richtlinie) einfügen
7. **Erstellen** klicken

#### Richtlinie

```JSON
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
          "iot:Connect",
          "iot:Publish",
          "iot:Subscribe",
          "iot:Receive",
          "iot:RetainPublish",
          "iot:UpdateThingShadow"
      ],
      "Resource": "*"
    }
  ]
}
```

#### Richtlinie an Zertifikat anhängen

1. Wechsel zu **IoT Core** -> **Verwalten** -> **Sicherheit** -> **Zertifikate**
2. Richtige **Region** auswählen (oben Rechts)
3. Zertifikat auswählen
4. **Richtlinie anfügen** klicken
5. Richtlinie auswählen
6. **Richtlinie anfügen** klicken
