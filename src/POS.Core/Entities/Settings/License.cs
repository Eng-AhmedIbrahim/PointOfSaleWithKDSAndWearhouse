using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Core.Entities.Settings;

[Table("Licenses")]
public class License
{
    [Key, Column(Order = 0)]
    public int CustomerID { get; set; }

    [Key, Column(Order = 1)]
    public int BranchID { get; set; }

    [Key, Column(Order = 2)]
    [MaxLength(250)]
    public string LicenseKey { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? CustomerName { get; set; }

    [MaxLength(250)]
    public string? BranchName { get; set; }

    [MaxLength(250)]
    public string? AliasName { get; set; }

    [MaxLength(250)]
    public string? GenerateDate { get; set; }

    [MaxLength(250)]
    public string? DateLimit { get; set; }

    [MaxLength(255)]
    public string? MacAddress { get; set; }

    [MaxLength(255)]
    public string? ComputerName { get; set; }

    [MaxLength(100)]
    public string? CPUSpeed { get; set; }

    [MaxLength(100)]
    public string? RamSize { get; set; }

    [MaxLength(100)]
    public string? HDDSize { get; set; }

    [MaxLength(500)]
    public string? ConnectionString { get; set; }

    [MaxLength(50)]
    public string? LicenseType { get; set; }

    [MaxLength(50)]
    public string? CreationDate { get; set; }

    public DateTime? JoinDate { get; set; }

    [MaxLength(250)]
    public string? TechnicalName { get; set; }
}
