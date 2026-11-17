namespace VetClinic.Helpers
{
    // Rol adları tek yerde tutulur; [Authorize(Roles = ...)] ve seed içinde yazım hatası olmasın diye sabit.
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Veterinarian = "Veterinarian";
        public const string Member = "Member";
    }
}
