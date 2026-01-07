namespace CommunicationHub;

public enum ChargeStatus : byte
{
    NoCharging = 0,
    Charging = 1,
    FullyCharged = 2,
    ChargerError = 3,
    Unknown = 255,
}


