namespace Business.DTOs.AuthDtos
{
    public class RegisterDto
    {
        public string FirstName { get; set; } // Eklendi
        public string LastName { get; set; }  // Eklendi
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}