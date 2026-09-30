---
title: Migrate between versions
---

# Migrate between versions

`DataPort.Extensions` is released as part of the DataPorts repository and shares its version. When a
release changes the public API, the repository changelog records *what* changed in short form and links
here; these guides cover *how* to upgrade an implementation built on the base classes — one page per
release that carries breaking changes.

Upgrading across several releases? Work through the guides in order, oldest first.

| Target version               | What it covers                                                                                                                                          |
|------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------|
| [1.0.0](1.0.0.md)            | Type-safe data points — non-generic `IDataPoint` / `IDataPointValue`, `DataPointValueConversion<T>`, range-only validation, renamed `Channels` members   |
| [0.30.0](0.30.0.md)          | The public-API refactor — renamed `IDataPoint` properties, namespace reorganisation, new base-class constructors, `IClientLifecycleManager`             |
