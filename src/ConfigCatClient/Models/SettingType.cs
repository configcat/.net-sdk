namespace ConfigCat.Client;

public enum SettingType : byte
{
    Boolean = 0,
    String = 1,
    Int = 2,
    Double = 3,
    Unknown = byte.MaxValue,
}
