namespace CommunicationHub;

public enum Notification : byte
{
    NONE,
    DeviceAdded,
    DeviceRemoved,
    KeyPressNotification,
    WirelessRFDongleStatusNotification,
    DpiSwitchNotification,
    BatteryLevelChanged,
    BrightnessChangedNotification,
    BatteryChargingNotification,
    LowBatteryWarningNotification,
    MicMuteNotification,
    SidetoneStateChangedNotification,
    MicBoomPlugNotification,
    MicGainLevelChangedNotification,
    BatteryLevelIndicatorChangedNotification,
    ChatMixLevelChangedNotification,
    PlaybackMuteNotification,
    PlaybackLevelChangedNotification,
    PlaybackEqChangedNotification,
    SidetoneLevelChangedNotification,
    StreamingLevelChangedNotification,
    BalanceLevelChangedNotification,
    MicPolarPatternChangedNotification,
    LightingEffectEnableStatusNotification,
    FactoryResetNotification,
}


