using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.RequestFormat;
public class BuyProductRequest : AuthRequest
{
    [Required]
    public Int32 UID { get; set; }

    public Int32 ProductCode { get; set; }

    public string ReceiptCode { get; set; }
}