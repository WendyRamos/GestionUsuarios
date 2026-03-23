using System.ComponentModel.DataAnnotations;

namespace GestionUsuarios.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string PaternalSurname { get; set; }

        public string? MaternalSurname { get; set; }

        [Required]
        public string TypeDocument { get; set; }
        [Required]
        public string Document { get; set; }

        [Required]
        public string Sex { get; set; }

        [Required]
        public string Nationality { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Birthdate { get; set; }

        public string? Phone { get; set; }
        public string? ScondaryPhone { get; set; }

        public string? SecondaryEmail { get; set; } 

        // Datos de Contratación
        public string? TypeContract { get; set; }

        [DataType(DataType.Date)]
        public DateTime? HiringDate { get; set; }
    }
}
