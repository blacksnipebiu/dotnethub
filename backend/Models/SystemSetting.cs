
using System.ComponentModel.DataAnnotations;

namespace DotNetHub.Server.Models;

public class SystemSetting
{
    [Key, MaxLength(100)]
    public string Key { get; set; } = "";
    
    [MaxLength(500)]
    public string Value { get; set; } = "";
    
    [MaxLength(200)]
    public string Description { get; set; } = "";
}

public class SystemSettingDto
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string Description { get; set; } = "";
}

public class SystemSettingUpdateRequest
{
    public string Value { get; set; } = "";
}
