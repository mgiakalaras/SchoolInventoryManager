using System.ComponentModel.DataAnnotations;
using System.Reflection;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Utilities;

public static class DisplayExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var attr = member?.GetCustomAttribute<DisplayAttribute>();
        return attr?.Name ?? value.ToString();
    }

    public static string CssClass(this EquipmentCondition condition)
    {
        return condition switch
        {
            EquipmentCondition.Working => "badge badge-success",
            EquipmentCondition.NeedsCheck => "badge badge-warning",
            EquipmentCondition.Broken => "badge badge-danger",
            EquipmentCondition.ToWithdraw => "badge badge-dark",
            EquipmentCondition.Stored => "badge badge-info",
            _ => "badge badge-muted"
        };
    }

    public static string Symbol(this EquipmentCondition condition)
    {
        return condition switch
        {
            EquipmentCondition.Working => "✅",
            EquipmentCondition.NeedsCheck => "⚠️",
            EquipmentCondition.Broken => "❌",
            EquipmentCondition.ToWithdraw => "♻️",
            EquipmentCondition.Stored => "📦",
            _ => "❔"
        };
    }
}