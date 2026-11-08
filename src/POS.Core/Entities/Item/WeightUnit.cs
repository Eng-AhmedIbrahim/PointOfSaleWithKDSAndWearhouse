namespace POS.Core.Entities.Item;

/// <summary>
/// وحدات الوزن للأصناف التي تُباع بالوزن
/// </summary>
public enum WeightUnit
{
    /// <summary>ربع كيلو (250g)</summary>
    Quarter = 0,

    /// <summary>ثلث كيلو (333g)</summary>
    Third = 1,

    /// <summary>نصف كيلو (500g)</summary>
    Half = 2,

    /// <summary>ثلاثة أرباع كيلو (750g)</summary>
    ThreeQuarters = 3,

    /// <summary>كيلو (1000g)</summary>
    Kilo = 4
}
