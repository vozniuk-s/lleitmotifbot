using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class BotMessage
    {
        [Key]
        public string Key { get;set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
