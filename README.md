# DataPorts

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
