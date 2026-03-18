using System;

namespace Kwerty.DviZe.Hid;

public class HidException(string message = "Could not complete operation.", Exception innerException = null)
    : Exception(message, innerException);

public class HidAccessException(string message = "Could not access the device.", Exception innerException = null)
    : HidException(message, innerException);

public sealed class HidAccessConflictException(string message = "Device is opened with exclusivity.", Exception innerException = null)
    : HidAccessException(message, innerException);
