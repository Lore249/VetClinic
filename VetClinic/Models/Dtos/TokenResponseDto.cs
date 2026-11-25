namespace VetClinic.Models.Dtos
{
    // POST /api/auth/token cevabı. Token, sonraki isteklerde "Authorization: Bearer <token>" başlığıyla gönderilir.
    public class TokenResponseDto
    {
        public string Token { get; set; } = string.Empty;

        // UTC zaman; bu andan sonra token geçersizdir ve yenisi alınmalıdır.
        public DateTime ExpiresAt { get; set; }

        public string Email { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new List<string>();
    }
}
