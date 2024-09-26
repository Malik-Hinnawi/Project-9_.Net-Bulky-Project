using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.JavaScript;

namespace BulkyWeb.Models;

public class Category
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(30, ErrorMessage = "Max Length is 30 Characters")]
    [DisplayName("Category Name")]
    public string Name { get; set; }
    
    [DisplayName("Display Oder")]
    [Range(1,100, ErrorMessage = "Display Order must be between 1-100")]
    public int DisplayOrder { get; set; }
}