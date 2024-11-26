using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Identity.Handlers.Models
{
  public class VoyagerUser : IdentityUser<Guid>
  {
    [Key]
    public override Guid Id { get; set; } = Guid.NewGuid();
    [StringLength(64)]
    public string FirstName { get; set; }
    [StringLength(64)]
    public string LastName { get; set; }
    public double Ratings { get; set; }
    [StringLength(64)]
    public override string PhoneNumber { get; set; }
    public bool IsDriver { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
    public DateTime LastLogin { get; set; } = DateTime.UtcNow;
  }
}
