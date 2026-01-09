using System;

namespace ViciOne.Suite.DataPort;

internal record ToSendData(Guid NodeId, object Value);
