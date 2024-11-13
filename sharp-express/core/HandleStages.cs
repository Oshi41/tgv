namespace sharp_express.core;

public enum HandleStages
{
    Before,
    Handle,
    After,
    Error,
    SendingError,
    SendingOk,
    Sent,
}