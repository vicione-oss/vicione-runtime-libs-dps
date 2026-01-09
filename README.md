# DataPorts

Provider                                                       |Eingangsseite      |Ausgangsseite      |freie JSON Struktur    |Andere Provider spezifische Features
-:|:-:|:-:|:-:|:-:
[MQTT](#mqtt)                                                  |:heavy_check_mark: |:heavy_check_mark: |:heavy_check_mark:     |:heavy_check_mark:
[HTTP Client](#http-client)                                                  |:heavy_check_mark: |:heavy_check_mark: |:heavy_check_mark:     |:heavy_check_mark: / :x:
||||
[**Relationale Datenbank**](#relationale-datenbanken)
SQLite                                                         |-                  |:heavy_check_mark: |:heavy_check_mark:     |-
SQL-Server                                                     |-                  |:heavy_check_mark: |:heavy_check_mark:     |-
PostgreSQL                                                     |-                  |:heavy_check_mark: |:heavy_check_mark:     |-
MySql/MariaDB                                                  |-                  |:heavy_check_mark: |:heavy_check_mark:     |-
||||
**Zeitreihendatenbank**
InfluxDB 2.0                                                   |-                  |:heavy_check_mark: |:heavy_check_mark:     |-
||||
**Cloud**
[Azure IoT Hub](./src/Azure.IotHub/README.md)                  |:heavy_check_mark: |:heavy_check_mark: |:heavy_check_mark:     |:x:
[Azure EventGrid Topic](./src/Azure.EventGrid.Topic/README.md) |-                  |:heavy_check_mark: |:heavy_check_mark:     |:x:
[AWS IoT](./src/Aws.Iot/README.md)                                            |:heavy_check_mark: |:heavy_check_mark: |:heavy_check_mark:     |-
||||
**moneo embedded**
OPC UA Client                                                  |:heavy_check_mark: |:heavy_check_mark: |:x:                    |?
[OPC UA Server](#opc-ua-server)                                |:heavy_check_mark: |:heavy_check_mark: |:x:                    |?

## Konfiguration - Verwendung

Alle DataPort Provider (incoming, outgoing) bekommen im Konstruktor eine [`IDataPortCommunication`](src/DataPort/IDataPortCommunication.cs) übergeben. Diese enthält Informationen über den Baum und welche Knoten wie beeinflusst und/oder gesendet werden. Beide Möglichkeiten sind Listen, da ein Knoten mehrfach abgebildet sein kann. (z.B. als Gruppen Knoten und einzel Knoten gleichzeitig)

- _AffectedChannels_
  <br/>befindet sich immer nur an Blättern im Baum
- _TransferredChannels_
  <br/>kann an jeder Ebene sein und wird durch das Design begrenzt

### Beispiel

Übertragen werden soll `Device 1` im gesamten und der einzelne `Temperature` Wert.

- ifm Zwickau
  - **D**evice **1**
    - **T**emperature
    - **H**umidity

Node|AffectedChannels|TransferredChannels
-|-|-
D1|n/A|D1T,D1H
T|T,D1T|T
H|D1H|-

- `Device 1` bekommt seine Werte über die Kanäle `Device 1 Temperature` und `Device 1 Humidity`
- `Temperature` bekommt seinen Wert über den Kanal `Temperature`
- `Humidity` wird einzeln nicht versendet

## MQTT

### Node Eigenschaften

Richtung|Name|Typ|Werte
-|-|-|-
Outgoing|Retain|`bool`|true/false
Incoming,Outgoing|Serializer|`byte`|0-Inherited, 1-JSON, 2-PlainText

## HTTP Client

### Node Eigenschaften

Richtung|Name|Typ|Werte
-|-|-|-
Incoming, Outgoing|Headers|`Dictionary<string, string>`
Outgoing|HttpMethod|`int`|0-GET, 1-POST, 2-PATCH, 3-PUT, 4-DELETE

## Relationale Datenbanken

### Node Eigenschaften

PostgreSQL, SQL-Server, SQLite, MariaDB

Name|Typ
-|-
PrimaryKey|`bool`
NotNull|`bool`
AutoIncrement|`bool`

## OPC UA Server

### Node Eigenschaften

Node Typ|Name|Verwendung
-|-|-
Variable|Maximum|Gibt den Maximalwert an den ein Client senden darf bevor ein 'BadOutOfRange' Status als Antwort kommt. Wird nicht für Knoten vom Typ 'string' unterstützt
Variable|Minimum|Gibt den Minimalwert an den ein Client senden darf bevor ein 'BadOutOfRange' Status als Antwort kommt. Wird nicht für Knoten vom Typ 'string' unterstützt
Variable, Writeable Variable|Status|Soll zur Laufzeit dazu verwendet werden den Status der Nodes zu setzen. Es können sowohl die Namen der Statuscodes als string als auch der Wert als uint übergeben werden. Eine Liste der Statuscodes ist hier verfügbar: <https://reference.opcfoundation.org/Core/Part6/v104/docs/A.2>
