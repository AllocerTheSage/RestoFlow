namespace Business.DTOs.AuthDtos
{
    public class UpdatePermissionDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<string> Claims { get; set; } = new List<string>();
    }
}