using System;

namespace POS.Domain.Models
{
    public interface IPerson
    {
        int Id { get; set; }
        string Name { get; set; }
        string? ContactName { get; set; }
        string? Email { get; set; }
        string? Phone { get; set; }
        string? Address { get; set; }
        string? City { get; set; }
        string? Country { get; set; }
        string? PostalCode { get; set; }
        string? Website { get; set; }
        string? Notes { get; set; }
        string? CommercialRegister { get; set; }
        string? TaxCard { get; set; }
        string? Image { get; set; }
    }
}
